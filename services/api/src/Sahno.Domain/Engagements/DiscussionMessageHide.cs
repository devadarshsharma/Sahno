namespace Sahno.Domain.Engagements;

/// <summary>
/// "Delete for me" (D-086): one person no longer sees one message. Nobody
/// else is affected and nothing about the message changes — it is the
/// reader's view, not the thread.
/// </summary>
public sealed class DiscussionMessageHide
{
    private DiscussionMessageHide(Guid messageId, Guid userId, DateTimeOffset hiddenAtUtc)
    {
        MessageId = messageId;
        UserId = userId;
        HiddenAtUtc = hiddenAtUtc;
    }

    public Guid MessageId { get; }

    public Guid UserId { get; }

    public DateTimeOffset HiddenAtUtc { get; }

    public static DiscussionMessageHide For(Guid messageId, Guid userId) =>
        new(messageId, userId, DateTimeOffset.UtcNow);
}
