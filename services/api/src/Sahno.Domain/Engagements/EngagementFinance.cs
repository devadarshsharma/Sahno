namespace Sahno.Domain.Engagements;

/// <summary>
/// The money side of one booking (Slice 11, D-008, D-016). Quoted and agreed
/// fees, the deposit, and whether each has actually arrived. Deliberately
/// lightweight: enough for an organiser to know what was promised and what is
/// still owed, and nothing that would make Sahno accounting software.
///
/// Readable only with financial access — the Owner, and Admins the Owner has
/// explicitly granted it (D-016). Members never reach this type at all.
/// Amounts are in the organisation's own currency; Sahno does not convert.
/// </summary>
public sealed class EngagementFinance
{
    public const int NotesMaxLength = 2000;

    private EngagementFinance(
        Guid engagementId,
        decimal? quotedFee,
        decimal? agreedFee,
        decimal? depositAmount,
        DateOnly? depositReceivedOn,
        DateOnly? balanceReceivedOn,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        EngagementId = engagementId;
        QuotedFee = quotedFee;
        AgreedFee = agreedFee;
        DepositAmount = depositAmount;
        DepositReceivedOn = depositReceivedOn;
        BalanceReceivedOn = balanceReceivedOn;
        Notes = notes;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid EngagementId { get; }

    /// <summary>What was asked for. Kept once the agreed fee differs.</summary>
    public decimal? QuotedFee { get; private set; }

    public decimal? AgreedFee { get; private set; }

    public decimal? DepositAmount { get; private set; }

    public DateOnly? DepositReceivedOn { get; private set; }

    public DateOnly? BalanceReceivedOn { get; private set; }

    /// <summary>Financial notes — payment terms, who to invoice, what was agreed.</summary>
    public string? Notes { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Agreed fee less the deposit. Null until a fee is agreed.</summary>
    public decimal? Balance =>
        AgreedFee is { } fee ? fee - (DepositAmount ?? 0m) : null;

    /// <summary>
    /// Money the customer still owes. A deposit that was asked for and has not
    /// arrived, or a balance not yet received. Null fees mean nothing is known
    /// to be owed, not that nothing is.
    /// </summary>
    public bool IsCustomerBalanceOutstanding =>
        (DepositAmount is > 0m && DepositReceivedOn is null)
        || (Balance is > 0m && BalanceReceivedOn is null);

    public static EngagementFinance Empty(Guid engagementId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException("An engagement is required.", nameof(engagementId));
        }

        return new EngagementFinance(
            engagementId,
            quotedFee: null,
            agreedFee: null,
            depositAmount: null,
            depositReceivedOn: null,
            balanceReceivedOn: null,
            notes: null,
            DateTimeOffset.UtcNow);
    }

    public void Update(
        decimal? quotedFee,
        decimal? agreedFee,
        decimal? depositAmount,
        DateOnly? depositReceivedOn,
        DateOnly? balanceReceivedOn,
        string? notes)
    {
        QuotedFee = NormalizeAmount(quotedFee);
        AgreedFee = NormalizeAmount(agreedFee);
        DepositAmount = NormalizeAmount(depositAmount);
        DepositReceivedOn = depositReceivedOn;
        BalanceReceivedOn = balanceReceivedOn;
        Notes = NormalizeNotes(notes);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        return trimmed.Length > NotesMaxLength ? trimmed[..NotesMaxLength] : trimmed;
    }

    /// <summary>
    /// A negative fee is a typo, not a refund; it is refused rather than
    /// stored. Two decimal places is what every currency Sahno will meet uses.
    /// </summary>
    internal static decimal? NormalizeAmount(decimal? amount)
    {
        if (amount is null)
        {
            return null;
        }

        if (amount < 0m)
        {
            throw new ArgumentException("An amount cannot be negative.", nameof(amount));
        }

        return Math.Round(amount.Value, 2, MidpointRounding.AwayFromZero);
    }
}
