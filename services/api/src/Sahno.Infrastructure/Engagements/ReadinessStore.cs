using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class ReadinessStore(SahnoDbContext dbContext) : IReadinessStore
{
    public async Task<IReadOnlyList<ReadinessWaiver>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ReadinessWaivers
            .AsNoTracking()
            .Where(waiver => waiver.EngagementId == engagementId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        ReadinessWaiver waiver,
        CancellationToken cancellationToken)
    {
        // Waiving an item twice is the same fact, so a repeat is not an error.
        var already = await dbContext.ReadinessWaivers.AnyAsync(
            existing =>
                existing.EngagementId == waiver.EngagementId
                && existing.Item == waiver.Item,
            cancellationToken);
        if (already)
        {
            return;
        }

        dbContext.ReadinessWaivers.Add(waiver);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RemoveAsync(
        Guid engagementId,
        ReadinessItem item,
        CancellationToken cancellationToken)
    {
        return dbContext.ReadinessWaivers
            .Where(waiver =>
                waiver.EngagementId == engagementId && waiver.Item == item)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
