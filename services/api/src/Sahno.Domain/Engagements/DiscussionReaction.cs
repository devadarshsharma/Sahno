namespace Sahno.Domain.Engagements;

/// <summary>
/// One person's reaction to one message (D-086). One each, like a phone's
/// chat: reacting again with something else replaces it, and the same one
/// again takes it back. From a short fixed set, so a reaction is a feeling and
/// not a second message.
/// </summary>
public sealed class DiscussionReaction
{
    /// <summary>The reactions on offer, in the order the picker shows them.</summary>
    public static readonly IReadOnlyList<string> Allowed = ["👍", "❤️", "😂", "😮", "😢", "🙏"];

    private DiscussionReaction(
        Guid messageId,
        Guid engagementId,
        Guid userId,
        string emoji,
        DateTimeOffset reactedAtUtc)
    {
        MessageId = messageId;
        EngagementId = engagementId;
        UserId = userId;
        Emoji = emoji;
        ReactedAtUtc = reactedAtUtc;
    }

    public Guid MessageId { get; }

    /// <summary>Carried so a reaction is live like the message it is on.</summary>
    public Guid EngagementId { get; }

    public Guid UserId { get; }

    public string Emoji { get; private set; }

    public DateTimeOffset ReactedAtUtc { get; private set; }

    public static bool IsAllowed(string? emoji) => emoji is not null && Allowed.Contains(emoji);

    public static DiscussionReaction React(DiscussionMessage message, Guid userId, string emoji)
    {
        if (!IsAllowed(emoji))
        {
            throw new ArgumentException("That reaction is not one Sahno offers.", nameof(emoji));
        }

        return new DiscussionReaction(message.Id, message.EngagementId, userId, emoji, DateTimeOffset.UtcNow);
    }

    public void Change(string emoji)
    {
        if (!IsAllowed(emoji))
        {
            throw new ArgumentException("That reaction is not one Sahno offers.", nameof(emoji));
        }

        Emoji = emoji;
        ReactedAtUtc = DateTimeOffset.UtcNow;
    }
}
