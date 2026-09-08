using Microsoft.EntityFrameworkCore;
using Sahno.Application.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Engagements;

public sealed class EngagementStore(SahnoDbContext dbContext) : IEngagementStore
{
    public Task<Engagement?> FindByIdAsync(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        // Tracked: callers mutate what they find and save it back.
        return dbContext.Engagements
            .SingleOrDefaultAsync(
                engagement =>
                    engagement.Id == engagementId
                    && engagement.OrganisationId == organisationId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Engagement>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Engagements
            .AsNoTracking()
            .Where(engagement => engagement.OrganisationId == organisationId)
            // Dated work first and soonest first; undated drafts after it,
            // newest first, since they are the ones still being worked out.
            .OrderBy(engagement => engagement.StartDate == null)
            .ThenBy(engagement => engagement.StartDate)
            .ThenByDescending(engagement => engagement.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EngagementActivity>> ListActivityAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EngagementActivities
            .AsNoTracking()
            .Where(activity => activity.EngagementId == engagementId)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Engagement engagement,
        EngagementActivity activity,
        CancellationToken cancellationToken)
    {
        dbContext.Engagements.Add(engagement);
        dbContext.EngagementActivities.Add(activity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(
        Engagement engagement,
        EngagementActivity? activity,
        CancellationToken cancellationToken)
    {
        if (activity is not null)
        {
            dbContext.EngagementActivities.Add(activity);
        }

        // One SaveChanges, so the change and the entry explaining it land
        // together or not at all.
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        Engagement engagement,
        CancellationToken cancellationToken)
    {
        await dbContext.EngagementActivities
            .Where(activity => activity.EngagementId == engagement.Id)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.Engagements.Remove(engagement);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
