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

    public async Task<IReadOnlyList<DiscussionReaction>> ListReactionsAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.DiscussionReactions
            .AsNoTracking()
            .Where(reaction => reaction.EngagementId == engagementId)
            .ToListAsync(cancellationToken);
    }

    public Task<DiscussionReaction?> FindReactionAsync(
        Guid messageId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.DiscussionReactions.SingleOrDefaultAsync(
            reaction => reaction.MessageId == messageId && reaction.UserId == userId,
            cancellationToken);
    }

    public void AddReaction(DiscussionReaction reaction)
    {
        dbContext.DiscussionReactions.Add(reaction);
    }

    public void RemoveReaction(DiscussionReaction reaction)
    {
        dbContext.DiscussionReactions.Remove(reaction);
    }

    public async Task<IReadOnlySet<Guid>> HiddenMessageIdsAsync(
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.DiscussionMessageHides
            .AsNoTracking()
            .Where(hide => hide.UserId == userId)
            .Join(
                dbContext.DiscussionMessages.AsNoTracking()
                    .Where(message => message.EngagementId == engagementId),
                hide => hide.MessageId,
                message => message.Id,
                (hide, message) => hide.MessageId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task HideAsync(Guid messageId, Guid userId, CancellationToken cancellationToken)
    {
        var already = await dbContext.DiscussionMessageHides.AnyAsync(
            hide => hide.MessageId == messageId && hide.UserId == userId,
            cancellationToken);
        if (already)
        {
            return;
        }

        dbContext.DiscussionMessageHides.Add(DiscussionMessageHide.For(messageId, userId));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
