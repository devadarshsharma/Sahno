using Microsoft.Extensions.Logging;
using Sahno.Domain.Notifications;

namespace Sahno.Application.Notifications;

/// <summary>
/// One pass over the outbox: send what is due, record what happened. The host
/// decides how often to call it — a hosted service in the API today, a
/// separate worker if the API ever needs to scale without it.
///
/// Each message is its own attempt and its own save. One failing address must
/// not hold up the other forty, and a crash mid-pass must not lose the record
/// of the ones that already went — which is what would make them go twice.
///
/// Email and push share the queue and the retry rules; only the adapter
/// differs. A push to a device the service says is gone is not retried: the
/// device is disabled so nothing further is queued for it, and the row is
/// marked undeliverable with the reason.
/// </summary>
public sealed class OutboxDispatcher(
    IOutboxStore outbox,
    IPushDeviceStore pushDevices,
    IEmailSender emailSender,
    IPushSender pushSender,
    ILogger<OutboxDispatcher> logger)
{
    public const int BatchSize = 25;

    /// <summary>Returns how many were attempted, for the caller's own pacing.</summary>
    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var due = await outbox.ListDueAsync(BatchSize, cancellationToken);

        foreach (var message in due)
        {
            try
            {
                await (message.Channel switch
                {
                    OutboxChannel.Push => SendPushAsync(message, cancellationToken),
                    _ => SendEmailAsync(message, cancellationToken),
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutting down. Leave the row untouched so it is tried again.
                throw;
            }
            catch (Exception exception)
            {
                message.MarkFailed(exception.Message);
                logger.LogWarning(
                    exception,
                    "{Channel} {OutboxId} to {Recipient} failed on attempt {Attempt}",
                    message.Channel,
                    message.Id,
                    message.Recipient,
                    message.AttemptCount);
            }

            await outbox.SaveAsync(cancellationToken);
        }

        return due.Count;
    }

    private async Task SendEmailAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await emailSender.SendAsync(
            message.Recipient,
            message.Subject,
            message.TextBody,
            cancellationToken);

        message.MarkSent();
    }

    private async Task SendPushAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var result = await pushSender.SendAsync(
            message.Recipient,
            message.Subject,
            message.TextBody,
            message.DataJson,
            cancellationToken);

        switch (result.Outcome)
        {
            case PushSendOutcome.Sent:
                message.MarkSent();
                break;

            case PushSendOutcome.DeviceNotRegistered:
                message.MarkUndeliverable(result.Error ?? "DeviceNotRegistered");
                var device = await pushDevices.FindByTokenAsync(message.Recipient, cancellationToken);
                if (device is not null)
                {
                    device.Disable(result.Error ?? "DeviceNotRegistered");
                    logger.LogInformation(
                        "Push device {DeviceId} for user {UserId} disabled: {Reason}",
                        device.Id,
                        device.UserId,
                        device.DisabledReason);
                }

                break;

            default:
                throw new InvalidOperationException(result.Error ?? "Push failed");
        }
    }
}
