namespace Sahno.Domain.Engagements;

/// <summary>
/// Who the booking is for, and the organiser's private notes about it
/// (Slice 11, D-022). Kept off the engagement itself rather than as more
/// columns on it, so that "members never see the customer" is structural:
/// nothing here is ever on an engagement response, and a member has no
/// endpoint that returns this type.
///
/// Non-financial. Every organiser may read and write it (D-022: "non-financial
/// administrative customer information is available to Admins"). Money is in
/// <see cref="EngagementFinance"/>, behind a different permission.
/// </summary>
public sealed class EngagementCustomer
{
    public const int NameMaxLength = 200;
    public const int ContactMaxLength = 200;
    public const int PhoneMaxLength = 40;
    public const int EmailMaxLength = 320;
    public const int NotesMaxLength = 4000;

    private EngagementCustomer(
        Guid engagementId,
        string? name,
        string? contactName,
        string? phone,
        string? email,
        string? privateNotes,
        DateTimeOffset updatedAtUtc)
    {
        EngagementId = engagementId;
        Name = name;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        PrivateNotes = privateNotes;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>One per engagement; the engagement id is the key.</summary>
    public Guid EngagementId { get; }

    /// <summary>The customer — a family, a venue, a company.</summary>
    public string? Name { get; private set; }

    /// <summary>The person to ring, when that is not the customer itself.</summary>
    public string? ContactName { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    /// <summary>
    /// The organiser's own notes: how the enquiry came in, what was said,
    /// what to remember next time. Never participant-facing (D-022).
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
            name: null,
            contactName: null,
            phone: null,
            email: null,
            privateNotes: null,
            DateTimeOffset.UtcNow);
    }

    public void Update(
        string? name,
        string? contactName,
        string? phone,
        string? email,
        string? privateNotes)
    {
        Name = Normalize(name, NameMaxLength);
        ContactName = Normalize(contactName, ContactMaxLength);
        Phone = Normalize(phone, PhoneMaxLength);
        Email = Normalize(email, EmailMaxLength);
        PrivateNotes = Normalize(privateNotes, NotesMaxLength);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
