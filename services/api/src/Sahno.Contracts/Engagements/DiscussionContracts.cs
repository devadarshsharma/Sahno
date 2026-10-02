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
    DateTimeOffset? EditedAtUtc,
    /// <summary>The message this replies to, as it stands now; null if not a reply (D-086).</summary>
    DiscussionQuoteResponse? ReplyTo = null,
    /// <summary>Reactions, one row per emoji, in the picker's order (D-086).</summary>
    IReadOnlyList<DiscussionReactionResponse>? Reactions = null);

/// <summary>
/// What a reply quotes. <see cref="Body"/> is null once the original is
/// removed — a quote never keeps words their author took back.
/// </summary>
public sealed record DiscussionQuoteResponse(
    Guid Id,
    string? AuthorDisplayName,
    bool IsYours,
    string? Body,
    bool IsDeleted);

public sealed record DiscussionReactionResponse(string Emoji, int Count, bool IncludesYou);

/// <summary>One of the offered reactions; reacting with the one you already have takes it back.</summary>
public sealed record SetReactionRequest(string Emoji);

/// <summary><see cref="ReplyToMessageId"/> makes it a reply to a message in the same thread.</summary>
public sealed record PostDiscussionMessageRequest(string Body, Guid? ReplyToMessageId = null);

/// <summary>The author's own rewrite. Nobody else can reach it.</summary>
public sealed record EditDiscussionMessageRequest(string Body);

/// <summary>
/// One conversation in the Chat inbox (D-084): which event, what was said
/// last and by whom, and how much the caller has not read. The preview is
/// clipped; a removed latest message has no preview and
/// <see cref="LastMessageRemoved"/> set.
/// </summary>
public sealed record ChatInboxEntryResponse(
    Guid EngagementId,
    string Title,
    string Status,
    DateOnly? StartDate,
    string? LastAuthorDisplayName,
    bool LastIsYours,
    string? LastMessagePreview,
    bool LastMessageRemoved,
    DateTimeOffset LastMessageAtUtc,
    int UnreadMessages);
