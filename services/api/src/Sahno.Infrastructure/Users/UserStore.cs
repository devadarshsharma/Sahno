using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sahno.Application.Users;
using Sahno.Domain.Users;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Users;

public sealed class UserStore(SahnoDbContext dbContext) : IUserStore
{
    public async Task<User?> FindByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken)
    {
        // Tracked so profile-hint refreshes can be saved. Every sign-in an
        // account has is in user_identities, its first included (D-131).
        var linked = await dbContext.Users
            .Where(user => dbContext.UserIdentities.Any(
                identity => identity.Subject == externalSubject && identity.UserId == user.Id))
            .SingleOrDefaultAsync(cancellationToken);
        if (linked is not null)
        {
            return linked;
        }

        // Only for a sign-in with no row at all: an account made just before
        // the identities table was filled. A folded duplicate keeps its old
        // subject in this column, but its row points at the real account,
        // which is found above.
        return await dbContext.Users
            .SingleOrDefaultAsync(user => user.ExternalSubject == externalSubject, cancellationToken);
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var lowered = email.ToLowerInvariant();
        return dbContext.Users
            .Where(user => user.Email != null && user.Email.ToLower() == lowered)
            .OrderBy(user => user.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddAsync(User user, CancellationToken cancellationToken)
    {
        var identity = UserIdentity.Create(user.ExternalSubject, user.Id);
        dbContext.Users.Add(user);
        dbContext.UserIdentities.Add(identity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            // Another request created the user for this subject first; the
            // caller re-reads the winner's record.
            dbContext.Entry(user).State = EntityState.Detached;
            dbContext.Entry(identity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> LinkAsync(Guid userId, string externalSubject, CancellationToken cancellationToken)
    {
        var identity = UserIdentity.Create(externalSubject, userId);
        dbContext.UserIdentities.Add(identity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            dbContext.Entry(identity).State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
