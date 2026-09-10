using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class EngagementResourceStore(SahnoDbContext dbContext)
    : IEngagementResourceStore
{
    public async Task<IReadOnlyList<EngagementResource>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngagementResources
            .AsNoTracking()
            .Where(resource => resource.EngagementId == engagementId)
            .OrderBy(resource => resource.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<EngagementResource?> FindAsync(
        Guid engagementId,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return dbContext.EngagementResources.FirstOrDefaultAsync(
            resource =>
                resource.Id == resourceId && resource.EngagementId == engagementId,
            cancellationToken);
    }

    public async Task AddAsync(
        EngagementResource resource,
        CancellationToken cancellationToken)
    {
        dbContext.EngagementResources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RemoveAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        return dbContext.EngagementResources
            .Where(resource => resource.Id == resourceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> EngagementIdsWithResourcesAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.EngagementResources
            .AsNoTracking()
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                resource => resource.EngagementId,
                engagement => engagement.Id,
                (resource, engagement) => engagement.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
