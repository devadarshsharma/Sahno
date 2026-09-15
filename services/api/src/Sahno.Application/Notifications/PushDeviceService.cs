using Sahno.Domain.Notifications;

namespace Sahno.Application.Notifications;

/// <summary>
/// The phones a person can be pushed to (D-080). The app registers its token
/// after sign-in and whenever the token changes; the same token from the
/// same phone is one row, kept current. Signing out unregisters it, so the
/// next person to sign in on that phone is not shown the last person's news.
/// </summary>
public sealed class PushDeviceService(IPushDeviceStore devices)
{
    /// <summary>
    /// Returns false when the token is not an Expo push token; the app sent
    /// something it should not have, and nothing was stored.
    /// </summary>
    public async Task<bool> RegisterAsync(
        Guid userId,
        string token,
        string platform,
        string? deviceName,
        CancellationToken cancellationToken)
    {
        if (!PushDevice.IsValidToken(token))
        {
            return false;
        }

        var existing = await devices.FindByTokenAsync(token.Trim(), cancellationToken);
        if (existing is null)
        {
            devices.Add(PushDevice.Register(userId, token, platform, deviceName));
        }
        else
        {
            existing.Touch(userId, platform, deviceName);
        }

        await devices.SaveAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Only the person the token belongs to may remove it; anybody else's
    /// request is silently nothing, the same as a token that was never there.
    /// </summary>
    public async Task UnregisterAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken)
    {
        var existing = await devices.FindByTokenAsync(token.Trim(), cancellationToken);
        if (existing is null || existing.UserId != userId)
        {
            return;
        }

        await devices.RemoveAsync(existing.Id, cancellationToken);
    }

    public Task<IReadOnlyList<PushDevice>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return devices.ListActiveForUserAsync(userId, cancellationToken);
    }
}
