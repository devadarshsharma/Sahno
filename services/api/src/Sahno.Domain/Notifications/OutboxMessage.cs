namespace Sahno.Domain.Notifications;

/// <summary>How an outbox message leaves the building.</summary>
public enum OutboxChannel
{
    Email = 1,

    /// <summary>A native push to one device, through Expo's push service.</summary>
    Push = 2,
}

/// <summary>
/// A message waiting to be sent (TECHNICAL_ARCHITECTURE: accepted notification
/// architecture) — an email, or a push to one phone.
///
/// The row is written in the same transaction as the change it describes, so
/// a confirmation can never be saved without its messages being queued, and a
/// message can never be queued for a confirmation that was rolled back. A
/// worker sends it afterwards and records the attempt; a provider outage
/// delays the message rather than failing the booking.
/// </summary>
public sealed class OutboxMessage
{
    public const int SubjectMaxLength = 200;
    public const int MaxAttempts = 5;

    private OutboxMessage(
        Guid id,
        OutboxChannel channel,
        string recipient,
        string subject,
        string textBody,
        string? dataJson,
        DateTimeOffset createdAtUtc,
        int attemptCount,
        DateTimeOffset? lastAttemptAtUtc,
        DateTimeOffset? sentAtUtc,
        string? lastError)
    {
        Id = id;
        Channel = channel;
        Recipient = recipient;
        Subject = subject;
        TextBody = textBody;
        DataJson = dataJson;
        CreatedAtUtc = createdAtUtc;
        AttemptCount = attemptCount;
        LastAttemptAtUtc = lastAttemptAtUtc;
        SentAtUtc = sentAtUtc;
        LastError = lastError;
    }

    public Guid Id { get; }

    public OutboxChannel Channel { get; }

    /// <summary>An email address, or an Expo push token, by channel.</summary>
    public string Recipient { get; }

    /// <summary>The email subject, or the push title.</summary>
    public string Subject { get; }

    /// <summary>
    /// Plain text. A performer reading a cancellation on a phone in a car park
    /// needs the words, not a layout, and plain text is what survives every
    /// mail client. For a push it is the body under the title.
    /// </summary>
    public string TextBody { get; }

    /// <summary>
    /// Push only: the structured payload the app reads to know where to go —
    /// kind, notification id, engagement, route. Null on email.
    /// </summary>
    public string? DataJson { get; }

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

        return new OutboxMessage(
            Guid.CreateVersion7(),
            OutboxChannel.Email,
            toEmail.Trim(),
            RequireSubject(subject),
            textBody?.Trim() ?? string.Empty,
            dataJson: null,
            DateTimeOffset.UtcNow,
            attemptCount: 0,
            lastAttemptAtUtc: null,
            sentAtUtc: null,
            lastError: null);
    }

    public static OutboxMessage Push(
        string pushToken,
        string title,
        string? body,
        string dataJson)
    {
        if (!PushDevice.IsValidToken(pushToken))
        {
            throw new ArgumentException("A push needs an Expo push token.", nameof(pushToken));
        }

        return new OutboxMessage(
            Guid.CreateVersion7(),
            OutboxChannel.Push,
            pushToken.Trim(),
            RequireSubject(title),
            body?.Trim() ?? string.Empty,
            dataJson,
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
        LastError = Clip(error);
    }

    /// <summary>
    /// Will never succeed — the device is gone, the address bounced for good.
    /// Retrying would be noise; the reason is kept so the row still explains
    /// itself.
    /// </summary>
    public void MarkUndeliverable(string reason)
    {
        AttemptCount = MaxAttempts;
        LastAttemptAtUtc = DateTimeOffset.UtcNow;
        LastError = Clip(reason);
    }

    private static string RequireSubject(string subject)
    {
        var cleanSubject = subject?.Trim() ?? string.Empty;
        if (cleanSubject.Length == 0)
        {
            throw new ArgumentException("A message needs a subject.", nameof(subject));
        }

        return cleanSubject.Length > SubjectMaxLength
            ? cleanSubject[..SubjectMaxLength]
            : cleanSubject;
    }

    private static string Clip(string text) => text.Length > 1000 ? text[..1000] : text;
}
