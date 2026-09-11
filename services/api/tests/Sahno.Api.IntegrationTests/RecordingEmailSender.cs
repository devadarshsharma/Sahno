using Sahno.Application.Notifications;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Stands in for Resend. Records every send so a test can assert who was
/// emailed what, and can be told to fail so the outbox's retry path is
/// exercised rather than trusted.
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly List<(string To, string Subject, string Body)> _sent = [];

    public IReadOnlyList<(string To, string Subject, string Body)> Sent
    {
        get
        {
            lock (_sent)
            {
                return _sent.ToList();
            }
        }
    }

    /// <summary>When set, every send throws with this message.</summary>
    public string? FailWith { get; set; }

    public Task SendAsync(
        string toEmail,
        string subject,
        string textBody,
        CancellationToken cancellationToken)
    {
        if (FailWith is { } reason)
        {
            throw new InvalidOperationException(reason);
        }

        lock (_sent)
        {
            _sent.Add((toEmail, subject, textBody));
        }

        return Task.CompletedTask;
    }
}
