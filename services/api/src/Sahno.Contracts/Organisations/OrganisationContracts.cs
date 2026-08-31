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
    bool ShowSetupChecklist);

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
