namespace Sahno.Domain.Notifications;

/// <summary>
/// An email waiting to be sent (TECHNICAL_ARCHITECTURE: accepted notification
/// architecture).
///
/// The row is written in the same transaction as the change it describes, so
/// a confirmation can never be saved without its email being queued, and an
/// email can never be queued for a confirmation that was rolled back. A worker
/// sends it afterwards and records the attempt; a provider outage delays the
/// email rather than failing the booking.
/// </summary>
public sealed class OutboxMessage
{
    public const int SubjectMaxLength = 200;
    public const int MaxAttempts = 5;

    private OutboxMessage(
        Guid id,
        string toEmail,
        string subject,
        string textBody,
        DateTimeOffset createdAtUtc,
        int attemptCount,
        DateTimeOffset? lastAttemptAtUtc,
        DateTimeOffset? sentAtUtc,
        string? lastError)
    {
        Id = id;
        ToEmail = toEmail;
        Subject = subject;
        TextBody = textBody;
        CreatedAtUtc = createdAtUtc;
        AttemptCount = attemptCount;
        LastAttemptAtUtc = lastAttemptAtUtc;
        SentAtUtc = sentAtUtc;
        LastError = lastError;
    }

    public Guid Id { get; }

    public string ToEmail { get; }

    public string Subject { get; }

    /// <summary>
    /// Plain text. A performer reading a cancellation on a phone in a car park
    /// needs the words, not a layout, and plain text is what survives every
    /// mail client.
    /// </summary>
    public string TextBody { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? LastAttemptAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public bool IsSent => SentAtUtc is not null;

    /// <summary>
    /// Given up on. Five tries over a widening interval is a provider that is
    /// down, not a blip, and a row that retries forever is a queue that never
    /// drains.
    /// </summary>
    public bool IsAbandoned => !IsSent && AttemptCount >= MaxAttempts;

    /// <summary>
    /// When it may next be tried. Doubles each attempt so a struggling
    /// provider is not hammered: one minute, two, four, eight.
    /// </summary>
    public DateTimeOffset NotBeforeUtc =>
        LastAttemptAtUtc is { } last
            ? last.AddMinutes(Math.Pow(2, AttemptCount - 1))
            : CreatedAtUtc;

    public static OutboxMessage Email(string toEmail, string subject, string textBody)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException("An email needs a recipient.", nameof(toEmail));
        }

        var cleanSubject = subject?.Trim() ?? string.Empty;
        if (cleanSubject.Length == 0)
        {
            throw new ArgumentException("An email needs a subject.", nameof(subject));
        }

        return new OutboxMessage(
            Guid.CreateVersion7(),
            toEmail.Trim(),
            cleanSubject.Length > SubjectMaxLength
                ? cleanSubject[..SubjectMaxLength]
                : cleanSubject,
            textBody?.Trim() ?? string.Empty,
            DateTimeOffset.UtcNow,
            attemptCount: 0,
            lastAttemptAtUtc: null,
            sentAtUtc: null,
            lastError: null);
    }

    public void MarkSent()
    {
        AttemptCount += 1;
        LastAttemptAtUtc = DateTimeOffset.UtcNow;
        SentAtUtc = LastAttemptAtUtc;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        AttemptCount += 1;
        LastAttemptAtUtc = DateTimeOffset.UtcNow;
        LastError = error.Length > 1000 ? error[..1000] : error;
    }
}
