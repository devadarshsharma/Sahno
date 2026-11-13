using Sahno.Domain.Users;

namespace Sahno.Application.Users;

/// <summary>
/// Maps an external identity to exactly one Sahno user, creating the minimal
/// local record on first contact. A first sign-in through a new method with a
/// verified email that already has an account opens that account instead of
/// making a second one (D-131). Safe under concurrent first requests: the
/// database enforces subject uniqueness and the loser of a race re-reads the
/// winner's record.
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
            // Later logins may carry newer or previously missing profile
            // hints (e.g. once access tokens include email/name claims).
            if (existing.RefreshProfileHints(identity.Email, identity.DisplayName))
            {
                await userStore.SaveAsync(existing, cancellationToken);
            }

            return existing;
        }

        // Same person, another button: Google before, the emailed code now.
        // Only on an address the provider has proved — otherwise typing
        // somebody else's email would open their account.
        if (identity.EmailVerified && !string.IsNullOrWhiteSpace(identity.Email))
        {
            var owner = await userStore.FindByEmailAsync(identity.Email.Trim(), cancellationToken);
            if (owner is not null)
            {
                if (await userStore.LinkAsync(owner.Id, identity.Subject, cancellationToken))
                {
                    return owner;
                }

                return await RereadAsync(identity, cancellationToken);
            }
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

        return await RereadAsync(identity, cancellationToken);
    }

    private async Task<User> RereadAsync(ExternalIdentity identity, CancellationToken cancellationToken)
    {
        return await userStore.FindByExternalSubjectAsync(
                identity.Subject,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "User creation conflicted but no existing user was found.");
    }
}
