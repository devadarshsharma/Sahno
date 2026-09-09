using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>A participant with the organisation-facing detail organisers need.</summary>
public sealed record ParticipantRow(
    EngagementParticipant Participant,
    string? DisplayName);

/// <summary>
/// The lineup summary organisers work from, and the count that drives the
/// confirmation warning (D-029).
/// </summary>
public sealed record AvailabilitySummary(
    int Selected,
    int Available,
    int Maybe,
    int Unavailable,
    int Outstanding);

/// <summary>
/// Availability collection (Slice 5, D-021, D-027 to D-029).
///
/// Two rules shape everything here. Answers belong to individuals, so changing
/// the lineup never disturbs anyone else's; and an answer is private to the
/// person who gave it until an organiser looks at the summary, which is why
/// Members are only ever handed their own.
/// </summary>
public sealed class AvailabilityService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IMembershipStore memberships)
{
    /// <summary>
    /// Selects Members and asks them. The first request is what moves a Draft
    /// into Checking Availability (D-025); later ones leave the state alone, so
    /// replacing someone while Tentative does not drag the engagement
    /// backwards (D-028).
    /// </summary>
    public async Task<EngagementResult> RequestAsync(
        Membership actor,
        Guid engagementId,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return EngagementResult.NotFound;
        }

        if (userIds.Count == 0)
        {
            return EngagementResult.Invalid;
        }

        // Only people who are actually in this organisation can be asked.
        var organisationMembers = await memberships.ListForOrganisationAsync(
            actor.OrganisationId,
            cancellationToken);
        var eligible = organisationMembers
            .Select(row => row.Membership.UserId)
            .ToHashSet();

        if (userIds.Any(userId => !eligible.Contains(userId)))
        {
            return EngagementResult.Invalid;
        }

        var existing = await participants.ListForEngagementAsync(
            engagementId,
            cancellationToken);
        var byUser = existing.ToDictionary(participant => participant.UserId);

        var added = new List<EngagementParticipant>();
        foreach (var userId in userIds.Distinct())
        {
            if (byUser.TryGetValue(userId, out var already))
            {
                // Re-selecting someone who was dropped puts them back with the
                // answer they already gave, rather than asking again (D-027).
                already.Restore();
                continue;
            }

            added.Add(EngagementParticipant.Request(engagementId, userId));
        }

        // A Draft becomes Checking Availability the moment the first request
        // goes out. Anything already past that stays where it is.
        EngagementActivity? activity = null;
        if (engagement.Status == EngagementStatus.Draft)
        {
            try
            {
                activity = engagement.TransitionTo(
                    EngagementStatus.CheckingAvailability,
                    actor.UserId,
                    reason: null);
            }
            catch (InvalidOperationException)
            {
                // No date, so there is nothing to ask about yet.
                return EngagementResult.Invalid;
            }
        }

        if (added.Count > 0)
        {
            await participants.AddAsync(added, cancellationToken);
        }

        await participants.SaveAsync(cancellationToken);
        await engagements.SaveAsync(engagement, activity, cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Takes someone off the lineup. Their answer is kept but hidden from the
    /// active list, so the record of who said what stays true while they lose
    /// access (D-027).
    /// </summary>
    public async Task<EngagementResult> RemoveAsync(
        Membership actor,
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return EngagementResult.NotFound;
        }

        var participant = await participants.FindAsync(
            engagementId,
            userId,
            cancellationToken);
        if (participant is null)
        {
            return EngagementResult.NotFound;
        }

        participant.Remove();
        await participants.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>Chases someone who has not answered.</summary>
    public async Task<EngagementResult> RemindAsync(
        Membership actor,
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return EngagementResult.NotFound;
        }

        var participant = await participants.FindAsync(
            engagementId,
            userId,
            cancellationToken);
        if (participant is null)
        {
            return EngagementResult.NotFound;
        }

        try
        {
            participant.MarkReminded();
        }
        catch (InvalidOperationException)
        {
            return EngagementResult.Invalid;
        }

        await participants.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Records the caller's own answer. There is deliberately no way to answer
    /// on someone else's behalf: availability is personal, and a lineup built
    /// on guessed answers is worse than an incomplete one (D-021).
    /// </summary>
    public async Task<EngagementResult> RespondAsync(
        Membership actor,
        Guid engagementId,
        AvailabilityResponse response,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return EngagementResult.NotFound;
        }

        var participant = await participants.FindAsync(
            engagementId,
            actor.UserId,
            cancellationToken);

        // Not asked, or since removed: the engagement is not theirs to answer,
        // and saying so plainly would tell them it exists.
        if (participant is null || !participant.IsActive)
        {
            return EngagementResult.NotFound;
        }

        participant.Respond(response);
        await participants.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// The lineup as an organiser sees it: every active participant with their
    /// answer, plus those since removed for the internal history.
    /// </summary>
    public async Task<IReadOnlyList<ParticipantRow>> ListAsync(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var rows = await participants.ListForEngagementAsync(
            engagementId,
            cancellationToken);

        var directory = await memberships.ListForOrganisationAsync(
            organisationId,
            cancellationToken);
        var names = directory.ToDictionary(
            row => row.Membership.UserId,
            row => row.DisplayName);

        return rows
            .Select(participant => new ParticipantRow(
                participant,
                names.GetValueOrDefault(participant.UserId)))
            .ToList();
    }

    /// <summary>The caller's own participation, or null if they were not asked.</summary>
    public Task<EngagementParticipant?> FindOwnAsync(
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return participants.FindAsync(engagementId, userId, cancellationToken);
    }

    public static AvailabilitySummary Summarise(IEnumerable<EngagementParticipant> rows)
    {
        var active = rows.Where(participant => participant.IsActive).ToList();

        return new AvailabilitySummary(
            active.Count,
            active.Count(p => p.Response == AvailabilityResponse.Available),
            active.Count(p => p.Response == AvailabilityResponse.Maybe),
            active.Count(p => p.Response == AvailabilityResponse.Unavailable),
            active.Count(p => p.Response is null));
    }
}
