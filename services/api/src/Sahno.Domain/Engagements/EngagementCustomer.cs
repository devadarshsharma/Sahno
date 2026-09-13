namespace Sahno.Domain.Engagements;

/// <summary>
/// Which customer a booking is for, and the organiser's private notes about
/// this booking in particular (Slice 11, D-022). The customer itself is an
/// organisation record — the same family or venue across every booking they
/// make — so this row is a link plus notes, not a copy of their details.
///
/// Kept off the engagement rather than as columns on it, so that "members
/// never see the customer" is structural: nothing here is ever on an
/// engagement response, and a member has no endpoint that returns this type.
/// </summary>
public sealed class EngagementCustomer
{
    public const int NotesMaxLength = 4000;

    private EngagementCustomer(
        Guid engagementId,
        Guid? customerId,
        string? privateNotes,
        DateTimeOffset updatedAtUtc)
    {
        EngagementId = engagementId;
        CustomerId = customerId;
        PrivateNotes = privateNotes;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>One per engagement; the engagement id is the key.</summary>
    public Guid EngagementId { get; }

    /// <summary>Null while nobody has been chosen yet.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>
    /// About this booking: how the enquiry came in, what was said, what to
    /// remember. Never participant-facing (D-022).
    /// </summary>
    public string? PrivateNotes { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static EngagementCustomer Empty(Guid engagementId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException("An engagement is required.", nameof(engagementId));
        }

        return new EngagementCustomer(
            engagementId,
            customerId: null,
            privateNotes: null,
            DateTimeOffset.UtcNow);
    }

    public void Update(Guid? customerId, string? privateNotes)
    {
        CustomerId = customerId;
        PrivateNotes = string.IsNullOrWhiteSpace(privateNotes)
            ? null
            : privateNotes.Trim().Length > NotesMaxLength
                ? privateNotes.Trim()[..NotesMaxLength]
                : privateNotes.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
