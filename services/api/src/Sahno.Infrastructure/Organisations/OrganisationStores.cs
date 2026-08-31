using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sahno.Application.Organisations;
using Sahno.Domain.Organisations;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Organisations;

public sealed class OrganisationStore(SahnoDbContext dbContext) : IOrganisationStore
{
    public Task<Organisation?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Set<Organisation>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                organisation => organisation.Id == id,
                cancellationToken);
    }

    public async Task AddWithOwnerAsync(
        Organisation organisation,
        Membership ownerMembership,
        CancellationToken cancellationToken)
    {
        dbContext.Set<Organisation>().Add(organisation);
        dbContext.Set<Membership>().Add(ownerMembership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class MembershipStore(SahnoDbContext dbContext) : IMembershipStore
{
    public Task<Membership?> FindAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Membership>()
            .SingleOrDefaultAsync(
                membership =>
                    membership.OrganisationId == organisationId
                    && membership.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<OrganisationMembership>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<Membership>()
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Join(
                dbContext.Set<Organisation>().AsNoTracking(),
                membership => membership.OrganisationId,
                organisation => organisation.Id,
                (membership, organisation) =>
                    new OrganisationMembership(organisation, membership))
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.Membership.JoinedAtUtc)
            .ToList();
    }

    public async Task<bool> AddAsync(
        Membership membership,
        CancellationToken cancellationToken)
    {
        dbContext.Set<Membership>().Add(membership);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            dbContext.Entry(membership).State = EntityState.Detached;
            return false;
        }
    }

    public Task SaveAsync(Membership membership, CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}

public sealed class InvitationStore(SahnoDbContext dbContext) : IInvitationStore
{
    public Task<Invitation?> FindByTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Invitation>()
            .SingleOrDefaultAsync(
                invitation => invitation.Token == token,
                cancellationToken);
    }

    public Task<Invitation?> FindByIdAsync(
        Guid organisationId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Invitation>()
            .SingleOrDefaultAsync(
                invitation =>
                    invitation.Id == invitationId
                    && invitation.OrganisationId == organisationId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Invitation>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<Invitation>()
            .AsNoTracking()
            .Where(invitation => invitation.OrganisationId == organisationId)
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken)
    {
        dbContext.Set<Invitation>().Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(Invitation invitation, CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
