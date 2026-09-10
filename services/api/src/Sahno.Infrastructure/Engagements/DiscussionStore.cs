using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class DiscussionStore(SahnoDbContext dbContext) : IDiscussionStore
{
    public async Task<IReadOnlyList<DiscussionMessage>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.DiscussionMessages
            .AsNoTracking()
            .Where(message => message.EngagementId == engagementId)
            .OrderBy(message => message.PostedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<DiscussionMessage?> FindAsync(
        Guid engagementId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return dbContext.DiscussionMessages.FirstOrDefaultAsync(
            message =>
                message.Id == messageId && message.EngagementId == engagementId,
            cancellationToken);
    }

    public async Task AddAsync(
        DiscussionMessage message,
        CancellationToken cancellationToken)
    {
        dbContext.DiscussionMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
