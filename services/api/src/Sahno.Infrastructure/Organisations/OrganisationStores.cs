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

    public async Task<IReadOnlyList<Organisation>> ListAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Organisation>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
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

    public async Task SaveAsync(Organisation organisation, CancellationToken cancellationToken)
    {
        // Loaded untracked; marking it modified writes it back.
        dbContext.Organisations.Update(organisation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Organisation?> FindIncludingDeletedAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Organisations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(organisation => organisation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Organisation>> ListDeletedOwnedByAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Organisations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(organisation => organisation.DeletedAtUtc != null
                && dbContext.Memberships.IgnoreQueryFilters().Any(membership =>
                    membership.OrganisationId == organisation.Id
                    && membership.UserId == userId
                    && membership.Role == MembershipRole.Owner))
            .OrderByDescending(organisation => organisation.DeletedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListDeletedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        return await dbContext.Organisations
            .IgnoreQueryFilters()
            .Where(organisation => organisation.DeletedAtUtc != null && organisation.DeletedAtUtc < cutoff)
            .Select(organisation => organisation.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Guid UserId, MembershipRole Role)>> ListMembersIncludingDeletedAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Memberships
            .IgnoreQueryFilters()
            .Where(membership => membership.OrganisationId == organisationId)
            .Select(membership => new { membership.UserId, membership.Role })
            .ToListAsync(cancellationToken);
        return rows.Select(row => (row.UserId, row.Role)).ToList();
    }

    public async Task PurgeAsync(Guid organisationId, CancellationToken cancellationToken)
    {
        // Every org-scoped set is read past the deleted-organisation filter
        // (SahnoDbContext): this is the one place meant to see it.
        // Most tables carry an organisation or engagement id without a foreign
        // key, so nothing cascades on its own: every one is cleared here, the
        // engagement-scoped rows before the engagements they hang off.
        var engagements = dbContext.Engagements.IgnoreQueryFilters()
            .Where(engagement => engagement.OrganisationId == organisationId)
            .Select(engagement => engagement.Id);
        var messages = dbContext.DiscussionMessages
            .Where(message => engagements.Contains(message.EngagementId))
            .Select(message => message.Id);
        var rehearsals = dbContext.Rehearsals
            .Where(rehearsal => engagements.Contains(rehearsal.EngagementId))
            .Select(rehearsal => rehearsal.Id);
        var pieces = dbContext.Pieces.IgnoreQueryFilters()
            .Where(piece => piece.OrganisationId == organisationId)
            .Select(piece => piece.Id);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.ReminderLogs
            .Where(log => engagements.Contains(log.SubjectId) || rehearsals.Contains(log.SubjectId))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.DiscussionMessageHides.Where(hide => messages.Contains(hide.MessageId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DiscussionReactions.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DiscussionReadMarks.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DiscussionMessages.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.DetailRequests.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.EngagementActivities.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.EngagementCustomers.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.EngagementFinance.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.PerformerPayments.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.EngagementParticipants.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.EngagementResources.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.ReadinessWaivers.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Responsibilities.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.SetListEntries.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Rehearsals.Where(row => engagements.Contains(row.EngagementId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Engagements.IgnoreQueryFilters().Where(row => row.OrganisationId == organisationId).ExecuteDeleteAsync(cancellationToken);

        await dbContext.PieceLinks.Where(link => pieces.Contains(link.PieceId)).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Pieces.IgnoreQueryFilters().Where(row => row.OrganisationId == organisationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Customers.IgnoreQueryFilters().Where(row => row.OrganisationId == organisationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Notifications.IgnoreQueryFilters().Where(row => row.OrganisationId == organisationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Invitations.IgnoreQueryFilters().Where(row => row.OrganisationId == organisationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Memberships.IgnoreQueryFilters().Where(row => row.OrganisationId == organisationId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Organisations.IgnoreQueryFilters().Where(row => row.Id == organisationId).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
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
