namespace Sahno.Domain.Users;

/// <summary>
/// A person's profile photo (D-129): one per account, shown in every
/// organisation they belong to. Kept small — the app crops it square and
/// shrinks it to 256 pixels before it is sent — and only as an image.
/// </summary>
public sealed class UserAvatar
{
    /// <summary>Far more than a 256-pixel photo needs; anything bigger was not shrunk.</summary>
    public const int MaxBytes = 300 * 1024;

    private UserAvatar(Guid userId, byte[] content, string contentType, DateTimeOffset updatedAtUtc)
    {
        UserId = userId;
        Content = content;
        ContentType = contentType;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid UserId { get; }

    public byte[] Content { get; private set; }

    public string ContentType { get; private set; }

    /// <summary>Doubles as the photo's version: a new photo is a new address, so caches never show the old one.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static UserAvatar Create(Guid userId, byte[] content, DateTimeOffset now)
    {
        var type = SniffImageType(content) ?? throw new ArgumentException("Not a JPEG, PNG or WebP image.", nameof(content));
        return new UserAvatar(userId, content, type, now);
    }

    public void Replace(byte[] content, DateTimeOffset now)
    {
        ContentType = SniffImageType(content) ?? throw new ArgumentException("Not a JPEG, PNG or WebP image.", nameof(content));
        Content = content;
        UpdatedAtUtc = now;
    }

    /// <summary>
    /// The image's type from its first bytes, whatever the upload claimed:
    /// JPEG, PNG or WebP, or null for anything else. What is stored is
    /// served back as exactly this type.
    /// </summary>
    public static string? SniffImageType(ReadOnlySpan<byte> content)
    {
        if (content.Length < 12 || content.Length > MaxBytes)
        {
            return null;
        }

        if (content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        return content[..4].SequenceEqual("RIFF"u8) && content[8..12].SequenceEqual("WEBP"u8)
            ? "image/webp"
            : null;
    }
}
