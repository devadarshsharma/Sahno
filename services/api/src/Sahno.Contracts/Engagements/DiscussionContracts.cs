namespace Sahno.Contracts.Engagements;

/// <summary>
/// One message in an engagement's discussion (Slice 9, D-024).
///
/// A removed message still appears, with <see cref="Body"/> null: a thread
/// with silent gaps in it stops making sense, and moderation that leaves no
/// trace is worse than moderation that does. <see cref="WasModerated"/> says
/// whether an organiser took it down or the author took it back.
/// </summary>
public sealed record DiscussionMessageResponse(
    Guid Id,
    Guid AuthorUserId,
    string? AuthorDisplayName,
    bool IsYours,
    string? Body,
    bool IsEdited,
    bool IsDeleted,
    bool WasModerated,
    DateTimeOffset PostedAtUtc,
    DateTimeOffset? EditedAtUtc);

public sealed record PostDiscussionMessageRequest(string Body);

/// <summary>The author's own rewrite. Nobody else can reach it.</summary>
public sealed record EditDiscussionMessageRequest(string Body);
