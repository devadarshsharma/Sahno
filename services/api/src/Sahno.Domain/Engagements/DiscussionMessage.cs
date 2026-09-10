namespace Sahno.Domain.Engagements;

/// <summary>
/// One message in an engagement's discussion (Slice 9, D-024, D-047 §6).
///
/// Removal is a tombstone rather than an erasure of the row. A message that
/// simply vanishes from a group thread leaves the replies around it answering
/// nothing, and makes people doubt they ever read it. The words themselves are
/// dropped, so nothing removed is still readable — what remains is the fact
/// that something was here and who took it away.
/// </summary>
public sealed class DiscussionMessage
{
    public const int BodyMaxLength = 4000;

    private DiscussionMessage(
        Guid id,
        Guid engagementId,
        Guid authorUserId,
        string? body,
        DateTimeOffset postedAtUtc,
        DateTimeOffset? editedAtUtc,
        DateTimeOffset? deletedAtUtc,
        Guid? deletedByUserId)
    {
        Id = id;
        EngagementId = engagementId;
        AuthorUserId = authorUserId;
        Body = body;
        PostedAtUtc = postedAtUtc;
        EditedAtUtc = editedAtUtc;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    /// <summary>
    /// The person, not their membership. Someone can leave the organisation
    /// and what they said about the event stays true.
    /// </summary>
    public Guid AuthorUserId { get; }

    /// <summary>Null once removed — the text is not kept anywhere.</summary>
    public string? Body { get; private set; }

    public DateTimeOffset PostedAtUtc { get; }

    /// <summary>
    /// When it was last changed. Null until the first edit, which is what
    /// drives the "edited" indicator (D-024).
    /// </summary>
    public DateTimeOffset? EditedAtUtc { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <summary>
    /// Who removed it. The same person as the author means they took their own
    /// message back; anyone else means an organiser moderated it, and readers
    /// are told which.
    /// </summary>
    public Guid? DeletedByUserId { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;

    public bool IsEdited => EditedAtUtc is not null;

    /// <summary>
    /// True when an organiser removed somebody else's message. Moderation that
    /// leaves no trace is worse than moderation that does.
    /// </summary>
    public bool WasModerated =>
        IsDeleted && DeletedByUserId is { } remover && remover != AuthorUserId;

    public static DiscussionMessage Post(
        Guid engagementId,
        Guid authorUserId,
        string body)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException(
                "An engagement is required.",
                nameof(engagementId));
        }

        return new DiscussionMessage(
            Guid.CreateVersion7(),
            engagementId,
            authorUserId,
            NormalizeBody(body),
            DateTimeOffset.UtcNow,
            editedAtUtc: null,
            deletedAtUtc: null,
            deletedByUserId: null);
    }

    /// <summary>
    /// Rewrites the message. Only its author ever reaches this — an organiser
    /// moderating can remove a message but never change words attributed to
    /// somebody else.
    /// </summary>
    public void Edit(string body)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A removed message cannot be edited.");
        }

        Body = NormalizeBody(body);
        EditedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Takes the message down. Removing twice is the same fact, so the first
    /// removal and its remover stand.
    /// </summary>
    public void Remove(Guid removedByUserId)
    {
        if (IsDeleted)
        {
            return;
        }

        Body = null;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        DeletedByUserId = removedByUserId;
    }

    private static string NormalizeBody(string body)
    {
        var trimmed = body?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A message needs something in it.", nameof(body));
        }

        return trimmed.Length > BodyMaxLength ? trimmed[..BodyMaxLength] : trimmed;
    }
}
