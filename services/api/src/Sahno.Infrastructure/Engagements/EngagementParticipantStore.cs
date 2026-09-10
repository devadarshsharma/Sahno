using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class EngagementParticipantStore(SahnoDbContext dbContext)
    : IEngagementParticipantStore
{
    public async Task<IReadOnlyList<EngagementParticipant>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        // Tracked: organisers mutate what they list — responding, reminding,
        // removing — and save it back.
        return await dbContext.EngagementParticipants
            .Where(participant => participant.EngagementId == engagementId)
            .OrderBy(participant => participant.RequestedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<EngagementParticipant?> FindAsync(
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.EngagementParticipants
            .SingleOrDefaultAsync(
                participant =>
                    participant.EngagementId == engagementId
                    && participant.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListEngagementIdsForUserAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngagementParticipants
            .AsNoTracking()
            .Where(participant =>
                participant.UserId == userId
                && participant.RemovedAtUtc == null)
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                participant => participant.EngagementId,
                engagement => engagement.Id,
                (participant, engagement) => engagement.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountOutstandingAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return dbContext.EngagementParticipants
            .CountAsync(
                participant =>
                    participant.EngagementId == engagementId
                    && participant.RemovedAtUtc == null
                    && participant.Response == null,
                cancellationToken);
    }


    public async Task<IReadOnlyDictionary<Guid, EngagementLineup>>
        LineupsForOrganisationAsync(
            Guid organisationId,
            CancellationToken cancellationToken)
    {
        var rows = await dbContext.EngagementParticipants
            .AsNoTracking()
            .Where(participant => participant.RemovedAtUtc == null)
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                participant => participant.EngagementId,
                engagement => engagement.Id,
                (participant, engagement) => new
                {
                    engagement.Id,
                    HasAnswered = participant.Response != null,
                })
            .GroupBy(row => row.Id)
            .Select(group => new
            {
                EngagementId = group.Key,
                Selected = group.Count(),
                Outstanding = group.Count(row => !row.HasAnswered),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.EngagementId,
            row => new EngagementLineup(row.Selected, row.Outstanding));
    }

    public async Task<IReadOnlyDictionary<Guid, AvailabilityResponse?>>
        OwnResponsesAsync(
            Guid organisationId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        var rows = await dbContext.EngagementParticipants
            .AsNoTracking()
            .Where(participant =>
                participant.UserId == userId
                && participant.RemovedAtUtc == null)
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                participant => participant.EngagementId,
                engagement => engagement.Id,
                (participant, engagement) => new
                {
                    engagement.Id,
                    participant.Response,
                })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.Response);
    }
    public async Task AddAsync(
        IReadOnlyList<EngagementParticipant> participants,
        CancellationToken cancellationToken)
    {
        dbContext.EngagementParticipants.AddRange(participants);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
