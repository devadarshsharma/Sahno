using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sahno.Application.Users;
using Sahno.Domain.Users;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Users;

public sealed class UserStore(SahnoDbContext dbContext) : IUserStore
{
    public Task<User?> FindByExternalSubjectAsync(
        string externalSubject,
        CancellationToken cancellationToken)
    {
        // Tracked so profile-hint refreshes can be saved.
        return dbContext.Users
            .SingleOrDefaultAsync(
                user => user.ExternalSubject == externalSubject,
                cancellationToken);
    }

    public Task SaveAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);

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
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
