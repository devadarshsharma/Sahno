namespace Sahno.Domain.Organisations;

/// <summary>
/// Somebody who books the group: a family, a venue, a company. One record per
/// customer per organisation, kept across bookings, so the people who come
/// back — and they do — are not typed in fresh each time with a new spelling.
/// A booking links to one of these; the link is on the booking.
///
/// Organiser-only, like everything commercial (D-022). Contact details here
/// are the customer's, not a member's, so D-018's contact-privacy rules do
/// not apply — but Members never reach this type at all.
/// </summary>
public sealed class Customer
{
    public const int NameMaxLength = 200;
    public const int ContactMaxLength = 200;
    public const int PhoneMaxLength = 40;
    public const int EmailMaxLength = 320;
    public const int NotesMaxLength = 4000;

    private Customer(
        Guid id,
        Guid organisationId,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? notes,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public string Name { get; private set; }

    /// <summary>The person to ring, when that is not the customer itself.</summary>
    public string? ContactName { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    /// <summary>
    /// What the organiser knows about them across bookings — how they like
    /// things, who signs off, what went wrong last time. Per-booking notes
    /// live on the booking.
    /// </summary>
    public string? Notes { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Customer Create(
        Guid organisationId,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? notes,
        Guid createdByUserId)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException("An organisation is required.", nameof(organisationId));
        }

        var now = DateTimeOffset.UtcNow;
        return new Customer(
            Guid.CreateVersion7(),
            organisationId,
            RequireName(name),
            Normalize(contactName, ContactMaxLength),
            Normalize(phone, PhoneMaxLength),
            Normalize(email, EmailMaxLength),
            Normalize(notes, NotesMaxLength),
            createdByUserId,
            now,
            now);
    }

    public void Update(
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? notes)
    {
        Name = RequireName(name);
        ContactName = Normalize(contactName, ContactMaxLength);
        Phone = Normalize(phone, PhoneMaxLength);
        Email = Normalize(email, EmailMaxLength);
        Notes = Normalize(notes, NotesMaxLength);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string RequireName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A customer needs a name.", nameof(name));
        }

        return trimmed.Length > NameMaxLength ? trimmed[..NameMaxLength] : trimmed;
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
