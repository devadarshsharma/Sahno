using Sahno.Domain.Users;

namespace Sahno.Application.Users;

/// <summary>
/// Changes a person's own account details. The MVP profile is deliberately
/// small (D-046): a required display name and an optional phone number, with
/// travel-grade identity collected just-in-time instead (D-076).
/// </summary>
public sealed class UserProfileService(IUserStore userStore)
{
    /// <summary>The longest display name the store accepts.</summary>
    public const int MaxDisplayNameLength = 200;

    public Task<User> SetDisplayNameAsync(
        User user,
        string displayName,
        CancellationToken cancellationToken)
    {
        return UpdateAsync(user, displayName, phoneNumber: null, cancellationToken);
    }

    /// <summary>
    /// Applies whichever details were sent. A null argument leaves the stored
    /// value alone; a blank phone number clears it, which is how someone stops
    /// sharing a number they previously gave.
    /// </summary>
    public async Task<User> UpdateAsync(
        User user,
        string? displayName,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        if (displayName is not null)
        {
            user.SetDisplayName(displayName);
        }

        if (phoneNumber is not null)
        {
            user.SetPhoneNumber(phoneNumber);
        }

        await userStore.SaveAsync(user, cancellationToken);
        return user;
    }
}
