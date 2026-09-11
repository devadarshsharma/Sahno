using Microsoft.Extensions.Logging;

namespace Sahno.Application.Notifications;

/// <summary>
/// One pass over the outbox: send what is due, record what happened. The host
/// decides how often to call it — a hosted service in the API today, a
/// separate worker if the API ever needs to scale without it.
///
/// Each message is its own attempt and its own save. One failing address must
/// not hold up the other forty, and a crash mid-pass must not lose the record
/// of the ones that already went — which is what would make them go twice.
/// </summary>
public sealed class OutboxDispatcher(
    IOutboxStore outbox,
    IEmailSender emailSender,
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
                await emailSender.SendAsync(
                    message.ToEmail,
                    message.Subject,
                    message.TextBody,
                    cancellationToken);

                message.MarkSent();
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
                    "Email {OutboxId} to {Recipient} failed on attempt {Attempt}",
                    message.Id,
                    message.ToEmail,
                    message.AttemptCount);
            }

            await outbox.SaveAsync(cancellationToken);
        }

        return due.Count;
    }
}
