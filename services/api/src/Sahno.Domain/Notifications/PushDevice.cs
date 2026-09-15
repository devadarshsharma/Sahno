namespace Sahno.Domain.Notifications;

/// <summary>
/// One phone that can be pushed to. A person has as many of these as they
/// have devices signed in; each is one Expo push token, and the token is the
/// identity — the same token registered again is the same device, touched.
///
/// Disabled rather than deleted when the push service says the device is
/// gone, so the row explains a silence: "this phone stopped receiving on the
/// 3rd because it uninstalled" is an answer, an absent row is not. The next
/// registration from that phone re-enables it.
/// </summary>
public sealed class PushDevice
{
    public const int TokenMaxLength = 200;
    public const int PlatformMaxLength = 20;
    public const int DeviceNameMaxLength = 120;
    public const int ReasonMaxLength = 200;

    private PushDevice(
        Guid id,
        Guid userId,
        string token,
        string platform,
        string? deviceName,
        DateTimeOffset createdAtUtc,
        DateTimeOffset lastSeenAtUtc,
        DateTimeOffset? disabledAtUtc,
        string? disabledReason)
    {
        Id = id;
        UserId = userId;
        Token = token;
        Platform = platform;
        DeviceName = deviceName;
        CreatedAtUtc = createdAtUtc;
        LastSeenAtUtc = lastSeenAtUtc;
        DisabledAtUtc = disabledAtUtc;
        DisabledReason = disabledReason;
    }

    public Guid Id { get; }

    public Guid UserId { get; private set; }

    /// <summary>An Expo push token: <c>ExponentPushToken[…]</c>.</summary>
    public string Token { get; }

    /// <summary>"ios" or "android", as the app reports it.</summary>
    public string Platform { get; private set; }

    public string? DeviceName { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>The last time the app registered this token.</summary>
    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public DateTimeOffset? DisabledAtUtc { get; private set; }

    public string? DisabledReason { get; private set; }

    public bool IsActive => DisabledAtUtc is null;

    public static PushDevice Register(
        Guid userId,
        string token,
        string platform,
        string? deviceName)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        var now = DateTimeOffset.UtcNow;
        return new PushDevice(
            Guid.CreateVersion7(),
            userId,
            RequireToken(token),
            NormalizePlatform(platform),
            Normalize(deviceName, DeviceNameMaxLength),
            now,
            now,
            disabledAtUtc: null,
            disabledReason: null);
    }

    /// <summary>
    /// The same token registered again — by the same person, or by whoever
    /// is now signed in on that phone. A phone belongs to whoever holds it,
    /// so the token follows the sign-in; the previous owner's account stops
    /// being pushed to on it, which is the right way round.
    /// </summary>
    public void Touch(Guid userId, string platform, string? deviceName)
    {
        UserId = userId;
        Platform = NormalizePlatform(platform);
        DeviceName = Normalize(deviceName, DeviceNameMaxLength) ?? DeviceName;
        LastSeenAtUtc = DateTimeOffset.UtcNow;
        DisabledAtUtc = null;
        DisabledReason = null;
    }

    public void Disable(string reason)
    {
        if (DisabledAtUtc is not null)
        {
            return;
        }

        DisabledAtUtc = DateTimeOffset.UtcNow;
        DisabledReason = Normalize(reason, ReasonMaxLength) ?? "disabled";
    }

    /// <summary>
    /// Expo tokens look like <c>ExponentPushToken[xxxx]</c>. Anything else is
    /// a client bug, and storing it would only produce a row that never
    /// delivers and never says why.
    /// </summary>
    public static bool IsValidToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var trimmed = token.Trim();
        return trimmed.Length <= TokenMaxLength
            && (trimmed.StartsWith("ExponentPushToken[", StringComparison.Ordinal)
                || trimmed.StartsWith("ExpoPushToken[", StringComparison.Ordinal))
            && trimmed.EndsWith(']');
    }

    private static string RequireToken(string token)
    {
        if (!IsValidToken(token))
        {
            throw new ArgumentException("That is not an Expo push token.", nameof(token));
        }

        return token.Trim();
    }

    private static string NormalizePlatform(string platform)
    {
        var value = platform?.Trim().ToLowerInvariant() ?? string.Empty;
        return value is "ios" or "android" ? value : "unknown";
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
