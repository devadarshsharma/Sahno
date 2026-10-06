using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class DetailRequestStore(SahnoDbContext dbContext) : IDetailRequestStore
{
    public async Task<IReadOnlyList<DetailRequest>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.DetailRequests
            .Where(request => request.EngagementId == engagementId)
            .OrderBy(request => request.RequestedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public void Add(IEnumerable<DetailRequest> requests) => dbContext.DetailRequests.AddRange(requests);

    public void Remove(IEnumerable<DetailRequest> requests) => dbContext.DetailRequests.RemoveRange(requests);

    public Task SaveAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
