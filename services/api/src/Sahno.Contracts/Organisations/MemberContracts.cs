namespace Sahno.Contracts.Organisations;

/// <summary>
/// One row of the member directory. Identity is visible to every member of the
/// organisation; <see cref="Email"/> is null unless the caller is entitled to
/// see it (D-018), rather than being present and empty.
/// </summary>
public sealed record MemberResponse(
    Guid MembershipId,
    Guid UserId,
    string? DisplayName,
    string? Email,
    string Role,
    bool CanManageFinances,
    bool IsYou,
    DateTimeOffset JoinedAtUtc);

/// <summary>
/// Changes one member's standing. Exactly one field is acted on per request:
/// a role move and a financial grant are separate decisions with different
/// authority behind them.
/// </summary>
public sealed record UpdateMemberRequest(string? Role, bool? CanManageFinances);

/// <summary>Hands ownership to the named membership (D-014).</summary>
public sealed record TransferOwnershipRequest(Guid MembershipId);
