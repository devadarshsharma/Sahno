namespace Sahno.Domain.Engagements;

/// <summary>
/// How far one person has read one event's chat (D-096): the last moment they
/// had it open. Everything posted up to then they have seen, which is what
/// puts their initial under a message for everyone else.
/// </summary>
public sealed class DiscussionReadMark
{
    private DiscussionReadMark(Guid engagementId, Guid userId, DateTimeOffset readAtUtc)
    {
        EngagementId = engagementId;
        UserId = userId;
        ReadAtUtc = readAtUtc;
    }

    public Guid EngagementId { get; }

    public Guid UserId { get; }

    public DateTimeOffset ReadAtUtc { get; private set; }

    public static DiscussionReadMark Now(Guid engagementId, Guid userId) =>
        new(engagementId, userId, DateTimeOffset.UtcNow);

    /// <summary>Read again: never moves backwards.</summary>
    public void ReadNow()
    {
        var now = DateTimeOffset.UtcNow;
        if (now > ReadAtUtc)
        {
            ReadAtUtc = now;
        }
    }
}
