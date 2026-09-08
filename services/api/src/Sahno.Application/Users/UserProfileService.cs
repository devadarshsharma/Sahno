using Sahno.Domain.Users;

namespace Sahno.Application.Users;

/// <summary>
/// Changes a person's own profile. Only the display name is editable in the
/// MVP profile (D-046); the remaining optional fields arrive with the
/// membership-directory slice.
/// </summary>
public sealed class UserProfileService(IUserStore userStore)
{
    /// <summary>The longest display name the store accepts.</summary>
    public const int MaxDisplayNameLength = 200;

    public async Task<User> SetDisplayNameAsync(
        User user,
        string displayName,
        CancellationToken cancellationToken)
    {
        user.SetDisplayName(displayName);
        await userStore.SaveAsync(user, cancellationToken);
        return user;
    }
}
