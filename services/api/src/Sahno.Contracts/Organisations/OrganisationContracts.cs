namespace Sahno.Contracts.Organisations;

public sealed record CreateOrganisationRequest(
    string Name,
    string? GroupType,
    string? TimeZoneId);

public sealed record OrganisationResponse(
    Guid Id,
    string Name,
    string? GroupType,
    string TimeZoneId,
    string? LogoUrl,
    string Role,
    /// <summary>
    /// Whether the caller may see money in this organisation: the Owner, or an
    /// Admin the Owner has granted it (D-016). Sent so the client can leave the
    /// finance cards out rather than render them and be refused.
    /// </summary>
    bool HasFinancialAccess,
    bool ShowSetupChecklist,
    int MemberCount,
    /// <summary>The organisation the app opens in for the caller (D-099).</summary>
    bool IsDefault = false,
    /// <summary>When event reminders go out: morning, 1h, 2h, 3h, day-before or off (D-118).</summary>
    string EventReminder = "morning",
    /// <summary>When rehearsal reminders go out, from the same choices (D-118).</summary>
    string RehearsalReminder = "morning",
    /// <summary>How soon unanswered availability is nudged: 2d, 1d or off (D-120).</summary>
    string AvailabilityNudge = "2d");

/// <summary>
/// Changes when the organisation's reminders go out (D-118, D-120). Owner and
/// Admins only. Without <c>AvailabilityNudge</c> the nudge setting is kept.
/// </summary>
public sealed record SetRemindersRequest(string EventReminder, string RehearsalReminder, string? AvailabilityNudge = null);

public sealed record CreateInvitationRequest(DateTimeOffset? ExpiresAtUtc);

public sealed record InvitationResponse(
    Guid Id,
    string Token,
    string Type,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc);

public sealed record InvitationPreviewResponse(
    string OrganisationName,
    string? GroupType,
    string? LogoUrl);

public sealed record AcceptInvitationResponse(
    Guid OrganisationId,
    string OrganisationName,
    string Role);

/// <summary>Deleting needs the organisation's name typed out (D-101).</summary>
public sealed record DeleteOrganisationRequest(string? Name);

/// <summary>A deleted organisation its Owner can still restore (D-101).</summary>
public sealed record DeletedOrganisationResponse(
    Guid Id,
    string Name,
    DateTimeOffset DeletedAtUtc,
    /// <summary>When it, and everything in it, goes for good.</summary>
    DateTimeOffset PurgeAfterUtc);
