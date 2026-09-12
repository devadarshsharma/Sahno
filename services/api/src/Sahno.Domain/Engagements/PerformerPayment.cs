namespace Sahno.Domain.Engagements;

/// <summary>
/// What one performer is owed for one engagement, and whether it has been
/// paid (Slice 11, D-008). This is the obligation, not the transaction:
/// Sahno records that Imran is owed $150 for Saturday and that it was settled
/// on Tuesday, and nothing about how.
///
/// An obligation outlives the event. A Completed engagement with an unpaid
/// row is exactly the thing an organiser needs chasing, so it keeps appearing
/// in their attention list until it is marked paid (Slice 11 criteria).
/// Financial access only (D-016); a performer does not see their own row in
/// the MVP, per D-022.
/// </summary>
public sealed class PerformerPayment
{
    public const int NotesMaxLength = 500;

    private PerformerPayment(
        Guid id,
        Guid engagementId,
        Guid userId,
        decimal amount,
        string? notes,
        DateOnly? paidOn,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        UserId = userId;
        Amount = amount;
        Notes = notes;
        PaidOn = paidOn;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    /// <summary>The person, not their membership — they may leave before they are paid.</summary>
    public Guid UserId { get; }

    public decimal Amount { get; private set; }

    public string? Notes { get; private set; }

    public DateOnly? PaidOn { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public bool IsPaid => PaidOn is not null;

    public static PerformerPayment Create(
        Guid engagementId,
        Guid userId,
        decimal amount,
        string? notes)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException("An engagement is required.", nameof(engagementId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A performer is required.", nameof(userId));
        }

        return new PerformerPayment(
            Guid.CreateVersion7(),
            engagementId,
            userId,
            RequireAmount(amount),
            NormalizeNotes(notes),
            paidOn: null,
            DateTimeOffset.UtcNow);
    }

    public void Update(decimal amount, string? notes)
    {
        Amount = RequireAmount(amount);
        Notes = NormalizeNotes(notes);
    }

    /// <summary>
    /// Settles or un-settles. Marking paid twice keeps the first date: the
    /// day money changed hands is a fact, not a preference.
    /// </summary>
    public void SetPaid(DateOnly? paidOn)
    {
        if (paidOn is null)
        {
            PaidOn = null;
            return;
        }

        PaidOn ??= paidOn;
    }

    private static decimal RequireAmount(decimal amount)
    {
        var normalized = EngagementFinance.NormalizeAmount(amount);
        if (normalized is null or <= 0m)
        {
            throw new ArgumentException("A payment needs an amount.", nameof(amount));
        }

        return normalized.Value;
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
}
