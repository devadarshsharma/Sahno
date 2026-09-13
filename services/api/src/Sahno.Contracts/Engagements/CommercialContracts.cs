namespace Sahno.Contracts.Engagements;

/// <summary>
/// Who the booking is for (Slice 11, D-022): the directory customer this
/// booking is linked to, resolved, plus the organiser's notes about this
/// booking in particular. Organisers only; never on an engagement response,
/// so a member's client has no field to accidentally render.
/// </summary>
public sealed record EngagementCustomerResponse(
    Sahno.Contracts.Organisations.CustomerResponse? Customer,
    string? PrivateNotes,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Null CustomerId clears the link; notes are per booking.</summary>
public sealed record UpdateEngagementCustomerRequest(
    Guid? CustomerId,
    string? PrivateNotes);

/// <summary>
/// The money on one booking (D-008, D-016). Financial access only. Balance is
/// derived — agreed fee less deposit — so it cannot disagree with them.
/// </summary>
public sealed record FinanceResponse(
    decimal? QuotedFee,
    decimal? AgreedFee,
    decimal? DepositAmount,
    DateOnly? DepositReceivedOn,
    decimal? Balance,
    DateOnly? BalanceReceivedOn,
    bool IsCustomerBalanceOutstanding,
    string? Notes,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateFinanceRequest(
    decimal? QuotedFee,
    decimal? AgreedFee,
    decimal? DepositAmount,
    DateOnly? DepositReceivedOn,
    DateOnly? BalanceReceivedOn,
    string? Notes);

/// <summary>What one performer is owed, and whether it has been settled.</summary>
public sealed record PerformerPaymentResponse(
    Guid Id,
    Guid UserId,
    string? DisplayName,
    decimal Amount,
    string? Notes,
    DateOnly? PaidOn,
    bool IsPaid,
    DateTimeOffset CreatedAtUtc);

public sealed record CreatePerformerPaymentRequest(
    Guid UserId,
    decimal Amount,
    string? Notes);

/// <summary>
/// The whole row. <see cref="PaidOn"/> null un-settles; a date settles, and
/// the first date given stands.
/// </summary>
public sealed record UpdatePerformerPaymentRequest(
    decimal Amount,
    string? Notes,
    DateOnly? PaidOn);
