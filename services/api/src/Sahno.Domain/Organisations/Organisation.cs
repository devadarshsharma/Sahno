namespace Sahno.Domain.Organisations;

/// <summary>
/// A private group using Sahno. Creation requires only a name (D-057);
/// logo upload is deferred, so <see cref="LogoUrl"/> stays null for now.
/// </summary>
public sealed class Organisation
{
    public const int NameMaxLength = 120;
    public const int GroupTypeMaxLength = 60;
    public const int TimeZoneMaxLength = 80;

    private Organisation(
        Guid id,
        string name,
        string? groupType,
        string timeZoneId,
        string? logoUrl,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        GroupType = groupType;
        TimeZoneId = timeZoneId;
        LogoUrl = logoUrl;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? GroupType { get; }

    /// <summary>IANA time-zone identifier, e.g. "Australia/Melbourne".</summary>
    public string TimeZoneId { get; }

    public string? LogoUrl { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// How long a deleted organisation can still be restored by its Owner
    /// before it and everything in it are removed for good (D-101).
    /// </summary>
    public static readonly TimeSpan RestoreWindow = TimeSpan.FromDays(30);

    /// <summary>
    /// When the Owner deleted it; null while it is in use. A deleted
    /// organisation is invisible everywhere except to its Owner's restore
    /// list, and is purged once <see cref="RestoreWindow"/> has passed.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    /// <summary>When it goes for good, if it is deleted.</summary>
    public DateTimeOffset? PurgeAfterUtc => DeletedAtUtc + RestoreWindow;

    public void MarkDeleted(Guid userId, DateTimeOffset now)
    {
        DeletedAtUtc ??= now;
        DeletedByUserId ??= userId;
    }

    public void Restore()
    {
        DeletedAtUtc = null;
        DeletedByUserId = null;
    }

    public static Organisation Create(
        string name,
        string? groupType,
        string? timeZoneId)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            throw new ArgumentException(
                "An organisation name is required.",
                nameof(name));
        }

        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"An organisation name must be at most {NameMaxLength} characters.",
                nameof(name));
        }

        var trimmedTimeZone = string.IsNullOrWhiteSpace(timeZoneId)
            ? "UTC"
            : timeZoneId.Trim();

        return new Organisation(
            Guid.CreateVersion7(),
            trimmedName,
            NormalizeOptional(groupType, GroupTypeMaxLength),
            trimmedTimeZone.Length > TimeZoneMaxLength ? "UTC" : trimmedTimeZone,
            logoUrl: null,
            DateTimeOffset.UtcNow);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
