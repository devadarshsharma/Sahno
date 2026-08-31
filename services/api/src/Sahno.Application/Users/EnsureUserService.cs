using Sahno.Domain.Users;

namespace Sahno.Application.Users;

/// <summary>
/// Maps a canonical external identity to exactly one Sahno user, creating the
/// minimal local record on first contact. Safe under concurrent first
/// requests: the database enforces subject uniqueness and the loser of a
/// creation race re-reads the winner's record.
/// </summary>
public sealed class EnsureUserService(IUserStore userStore)
{
    public async Task<User> EnsureAsync(
        ExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var existing = await userStore.FindByExternalSubjectAsync(
            identity.Subject,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var user = User.Create(
            identity.Subject,
            identity.Email,
            identity.DisplayName);

        var added = await userStore.AddAsync(user, cancellationToken);
        if (added)
        {
            return user;
        }

        return await userStore.FindByExternalSubjectAsync(
                identity.Subject,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "User creation conflicted but no existing user was found.");
    }
}
