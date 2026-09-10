using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class RehearsalStore(SahnoDbContext dbContext) : IRehearsalStore
{
    public async Task<IReadOnlyList<Rehearsal>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Rehearsals
            .AsNoTracking()
            .Where(rehearsal => rehearsal.EngagementId == engagementId)
            .OrderBy(rehearsal => rehearsal.Date)
            .ThenBy(rehearsal => rehearsal.StartTime)
            .ToListAsync(cancellationToken);
    }

    public Task<Rehearsal?> FindAsync(
        Guid engagementId,
        Guid rehearsalId,
        CancellationToken cancellationToken)
    {
        return dbContext.Rehearsals.FirstOrDefaultAsync(
            rehearsal =>
                rehearsal.Id == rehearsalId
                && rehearsal.EngagementId == engagementId,
            cancellationToken);
    }

    public async Task AddAsync(Rehearsal rehearsal, CancellationToken cancellationToken)
    {
        dbContext.Rehearsals.Add(rehearsal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RemoveAsync(Guid rehearsalId, CancellationToken cancellationToken)
    {
        return dbContext.Rehearsals
            .Where(rehearsal => rehearsal.Id == rehearsalId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> EngagementIdsWithRehearsalsAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.Rehearsals
            .AsNoTracking()
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                rehearsal => rehearsal.EngagementId,
                engagement => engagement.Id,
                (rehearsal, engagement) => engagement.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
