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
    bool IsDefault = false);

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
