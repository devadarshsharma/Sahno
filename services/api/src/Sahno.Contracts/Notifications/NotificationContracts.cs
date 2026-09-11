namespace Sahno.Contracts.Notifications;

/// <summary>
/// One thing the bell shows (Slice 10, D-049). <see cref="EngagementId"/> is
/// where tapping it goes; null for organisation-level news like a new member.
/// </summary>
public sealed record NotificationResponse(
    Guid Id,
    string Kind,
    string Title,
    string? Body,
    Guid? EngagementId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc);

public sealed record UnreadCountResponse(int Unread);
