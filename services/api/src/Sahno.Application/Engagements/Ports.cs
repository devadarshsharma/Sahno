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

public interface IEngagementParticipantStore
{
    /// <summary>
    /// Everyone ever selected, including those since removed. Callers decide
    /// which they need: the lineup is the active ones, the internal history is
    /// all of them (D-027).
    /// </summary>
    Task<IReadOnlyList<EngagementParticipant>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken);

    Task<EngagementParticipant?> FindAsync(
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The engagements this person is currently on the lineup for. Members see
    /// these and nothing else (D-020).
    /// </summary>
    Task<IReadOnlyList<Guid>> ListEngagementIdsForUserAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>How many selected people have still not answered (D-029).</summary>
    Task<int> CountOutstandingAsync(
        Guid engagementId,
        CancellationToken cancellationToken);

    /// <summary>Lineup totals for every engagement of one organisation.</summary>
    Task<IReadOnlyDictionary<Guid, EngagementLineup>> LineupsForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// This person's own answer against each engagement they are on. Absent
    /// means they were never asked; a null value means asked and still silent.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, AvailabilityResponse?>> OwnResponsesAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(
        IReadOnlyList<EngagementParticipant> participants,
        CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}

/// <summary>
/// How a lineup stands, for a list view. Carrying it with the engagement keeps
/// a pipeline of twenty bookings to one query rather than twenty.
/// </summary>
public sealed record EngagementLineup(int Selected, int Outstanding);
