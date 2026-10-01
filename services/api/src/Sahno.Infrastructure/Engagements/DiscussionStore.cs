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

    public async Task<IReadOnlyDictionary<Guid, DiscussionMessage>> LatestForEngagementsAsync(
        IReadOnlyCollection<Guid> engagementIds,
        CancellationToken cancellationToken)
    {
        if (engagementIds.Count == 0)
        {
            return new Dictionary<Guid, DiscussionMessage>();
        }

        // One query: the newest row per engagement (PostgreSQL does this with
        // a window function), not one round trip per conversation.
        var latest = await dbContext.DiscussionMessages
            .AsNoTracking()
            .Where(message => engagementIds.Contains(message.EngagementId))
            .GroupBy(message => message.EngagementId)
            .Select(thread => thread
                .OrderByDescending(message => message.PostedAtUtc)
                .First())
            .ToListAsync(cancellationToken);

        return latest.ToDictionary(message => message.EngagementId);
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
