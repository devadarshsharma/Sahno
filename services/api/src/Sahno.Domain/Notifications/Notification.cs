namespace Sahno.Domain.Notifications;

/// <summary>
/// What happened. The kind is stored as text so a row read straight from the
/// table says what it is, and so that adding a kind later never renumbers an
/// old one.
/// </summary>
public enum NotificationKind
{
    /// <summary>You have been asked whether you can do an event.</summary>
    AvailabilityRequested = 1,

    /// <summary>You still have not answered, and someone has chased.</summary>
    AvailabilityReminder = 2,

    /// <summary>Somebody on the lineup answered. Organisers only.</summary>
    AvailabilityAnswered = 3,

    EngagementConfirmed = 10,
    EngagementPostponed = 11,
    EngagementCancelled = 12,

    /// <summary>A cancellation was reversed or a completion undone.</summary>
    EngagementReopened = 13,

    /// <summary>The date moved after a postponement.</summary>
    EngagementDateChanged = 14,

    /// <summary>Venue, call time, or start time changed on a shared event.</summary>
    EngagementDetailsChanged = 15,

    /// <summary>A job on an event has been given to you.</summary>
    ResponsibilityAssigned = 20,

    /// <summary>Somebody said something in an event's discussion.</summary>
    DiscussionMessage = 30,

    /// <summary>Somebody accepted an invitation. Organisers only.</summary>
    MemberJoined = 40,

    /// <summary>An organiser wrote to the whole organisation.</summary>
    OrganiserAnnouncement = 50,
}

/// <summary>
/// One thing one person is told, inside the app (Slice 10, D-049). Scoped to
/// an organisation like everything else, so switching organisations switches
/// what you are told about (D-013).
///
/// Email is a separate channel with its own row in the outbox; a notification
/// here is what the bell shows, and it exists whether or not an email went.
/// </summary>
public sealed class Notification
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 500;

    private Notification(
        Guid id,
        Guid organisationId,
        Guid recipientUserId,
        Guid? engagementId,
        NotificationKind kind,
        string title,
        string? body,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? readAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        RecipientUserId = recipientUserId;
        EngagementId = engagementId;
        Kind = kind;
        Title = title;
        Body = body;
        CreatedAtUtc = createdAtUtc;
        ReadAtUtc = readAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public Guid RecipientUserId { get; }

    /// <summary>Where tapping it goes. Null for organisation-level news.</summary>
    public Guid? EngagementId { get; }

    public NotificationKind Kind { get; }

    public string Title { get; }

    public string? Body { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public bool IsRead => ReadAtUtc is not null;

    public static Notification Create(
        Guid organisationId,
        Guid recipientUserId,
        Guid? engagementId,
        NotificationKind kind,
        string title,
        string? body)
    {
        var cleanTitle = title?.Trim() ?? string.Empty;
        if (cleanTitle.Length == 0)
        {
            throw new ArgumentException("A notification needs a title.", nameof(title));
        }

        var cleanBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim();

        return new Notification(
            Guid.CreateVersion7(),
            organisationId,
            recipientUserId,
            engagementId,
            kind,
            cleanTitle.Length > TitleMaxLength ? cleanTitle[..TitleMaxLength] : cleanTitle,
            cleanBody is { Length: > BodyMaxLength } ? cleanBody[..BodyMaxLength] : cleanBody,
            DateTimeOffset.UtcNow,
            readAtUtc: null);
    }

    /// <summary>Reading twice is the same fact; the first time stands.</summary>
    public void MarkRead()
    {
        ReadAtUtc ??= DateTimeOffset.UtcNow;
    }
}
