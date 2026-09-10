using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class ResponsibilityStore(SahnoDbContext dbContext)
    : IResponsibilityStore
{
    public async Task<IReadOnlyList<Responsibility>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Responsibilities
            .AsNoTracking()
            .Where(responsibility => responsibility.EngagementId == engagementId)
            .OrderBy(responsibility => responsibility.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Responsibility?> FindAsync(
        Guid engagementId,
        Guid responsibilityId,
        CancellationToken cancellationToken)
    {
        return dbContext.Responsibilities.FirstOrDefaultAsync(
            responsibility =>
                responsibility.Id == responsibilityId
                && responsibility.EngagementId == engagementId,
            cancellationToken);
    }

    public async Task AddAsync(
        Responsibility responsibility,
        CancellationToken cancellationToken)
    {
        dbContext.Responsibilities.Add(responsibility);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RemoveAsync(Guid responsibilityId, CancellationToken cancellationToken)
    {
        return dbContext.Responsibilities
            .Where(responsibility => responsibility.Id == responsibilityId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> EngagementIdsWithAllAssignedAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        // Grouped in the database rather than pulling every job back: the
        // readiness list only needs one bit per engagement.
        var rows = await dbContext.Responsibilities
            .AsNoTracking()
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                responsibility => responsibility.EngagementId,
                engagement => engagement.Id,
                (responsibility, engagement) => new
                {
                    engagement.Id,
                    responsibility.AssignedUserId,
                })
            .GroupBy(row => row.Id)
            .Select(group => new
            {
                EngagementId = group.Key,
                Unassigned = group.Count(row => row.AssignedUserId == null),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Where(row => row.Unassigned == 0)
            .Select(row => row.EngagementId)
            .ToHashSet();
    }
}
