namespace Sahno.Contracts.Organisations;

/// <summary>
/// One row of the member directory. Identity — name and organisation function
/// — is visible to every member (D-018). Contact details are withheld unless
/// the caller is entitled to them, and <see cref="InternalNotes"/> is present
/// only for organisers. Withheld fields are null rather than empty, so a
/// client cannot mistake "not shared" for "not set".
/// </summary>
public sealed record MemberResponse(
    Guid MembershipId,
    Guid UserId,
    string? DisplayName,
    string? Function,
    string? Email,
    string? PhoneNumber,
    string Role,
    bool CanManageFinances,
    bool SharesContactDetails,
    string? InternalNotes,
    bool IsYou,
    DateTimeOffset JoinedAtUtc);

/// <summary>
/// Changes another member's standing. Exactly one field is acted on per
/// request: a role move, a financial grant, and a note are separate decisions
/// with different authority behind them.
/// </summary>
public sealed record UpdateMemberRequest(
    string? Role,
    bool? CanManageFinances,
    string? InternalNotes);

/// <summary>
/// What a person controls about their own place in an organisation: the
/// function others see, and whether ordinary Members may see their contact
/// details (D-018). Null leaves a field as it is.
/// </summary>
public sealed record UpdateOwnMembershipRequest(
    string? Function,
    bool? SharesContactDetails);

/// <summary>Hands ownership to the named membership (D-014).</summary>
public sealed record TransferOwnershipRequest(Guid MembershipId);
