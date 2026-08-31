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
