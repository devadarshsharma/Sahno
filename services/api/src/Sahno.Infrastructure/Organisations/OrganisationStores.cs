using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sahno.Application.Organisations;
using Sahno.Domain.Organisations;
using Sahno.Domain.Users;
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
                (membership, organisation) => new
                {
                    membership,
                    organisation,
                    memberCount = dbContext.Set<Membership>().Count(
                        other => other.OrganisationId == organisation.Id),
                })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.membership.JoinedAtUtc)
            .Select(row => new OrganisationMembership(
                row.organisation,
                row.membership,
                row.memberCount))
            .ToList();
    }

    public Task<int> CountForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Membership>()
            .CountAsync(
                membership => membership.OrganisationId == organisationId,
                cancellationToken);
    }

    public Task<Membership?> FindByIdAsync(
        Guid organisationId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Membership>()
            .SingleOrDefaultAsync(
                membership =>
                    membership.Id == membershipId
                    && membership.OrganisationId == organisationId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<OrganisationMember>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<Membership>()
            .AsNoTracking()
            .Where(membership => membership.OrganisationId == organisationId)
            .Join(
                dbContext.Set<User>().AsNoTracking(),
                membership => membership.UserId,
                user => user.Id,
                (membership, user) => new
                {
                    membership,
                    user.DisplayName,
                    user.Email,
                    user.PhoneNumber,
                })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.membership.JoinedAtUtc)
            .Select(row => new OrganisationMember(
                row.membership,
                row.DisplayName,
                row.Email,
                row.PhoneNumber))
            .ToList();
    }

    public Task RemoveAsync(Membership membership, CancellationToken cancellationToken)
    {
        dbContext.Set<Membership>().Remove(membership);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferOwnershipAsync(
        Membership outgoingOwner,
        Membership incomingOwner,
        CancellationToken cancellationToken)
    {
        // The single-Owner index (D-013) is checked per statement, so the two
        // rows cannot move in one SaveChanges: the organisation would briefly
        // hold two Owners. They are written as ordered statements instead —
        // demote, then promote — inside one transaction, so a failure between
        // them leaves the original Owner in place rather than none at all.
        //
        // Written directly rather than through the change tracker: marking one
        // side unmodified to hold it back would revert the very change being
        // saved, since clearing IsModified restores the original value.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Set<Membership>()
            .Where(membership => membership.Id == outgoingOwner.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(membership => membership.Role, outgoingOwner.Role)
                    .SetProperty(
                        membership => membership.CanManageFinances,
                        outgoingOwner.CanManageFinances),
                cancellationToken);

        await dbContext.Set<Membership>()
            .Where(membership => membership.Id == incomingOwner.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(membership => membership.Role, incomingOwner.Role)
                    .SetProperty(
                        membership => membership.CanManageFinances,
                        incomingOwner.CanManageFinances),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
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
