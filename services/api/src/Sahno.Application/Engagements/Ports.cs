using Sahno.Domain.Engagements;

namespace Sahno.Application.Engagements;

public interface IEngagementStore
{
    Task<Engagement?> FindByIdAsync(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Every engagement of one organisation. Which of them a Member may see is
    /// decided above this: the store does not filter by role.
    /// </summary>
    Task<IReadOnlyList<Engagement>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngagementActivity>> ListActivityAsync(
        Guid engagementId,
        CancellationToken cancellationToken);

    /// <summary>Persists a new engagement together with its first history entry.</summary>
    Task AddAsync(
        Engagement engagement,
        EngagementActivity activity,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves a change and the history entry that explains it, together. They
    /// are written in one transaction so a transition can never be recorded
    /// without its trace, or the other way round.
    /// </summary>
    Task SaveAsync(
        Engagement engagement,
        EngagementActivity? activity,
        CancellationToken cancellationToken);

    /// <summary>Removes a discarded Draft and its history (D-034).</summary>
    Task RemoveAsync(Engagement engagement, CancellationToken cancellationToken);
}
