using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>Who is waiting on one detail. Organisers see this; members do not.</summary>
public sealed record DetailAskers(ReadinessItem Item, IReadOnlyList<string> Names);

/// <summary>
/// The caller's view of the details still to come (D-089): which can be asked
/// for, which they already asked for, and — for an organiser — who is waiting.
/// </summary>
public sealed record DetailRequestsView(
    IReadOnlyList<ReadinessItem> Missing,
    IReadOnlyList<ReadinessItem> Asked,
    IReadOnlyList<DetailAskers>? Askers);

/// <summary>
/// Performers asking the organisers for the details they plan their day
/// around (D-089). Somebody on the event can ask for a detail that is not set
/// and has not been marked as not needed; the organisers are told, once per
/// person per detail. Filling the detail in closes the request
/// (<see cref="EngagementService.UpdateDetailsAsync"/>).
/// </summary>
public sealed class DetailRequestService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IReadinessStore readiness,
    IDetailRequestStore requests,
    IMembershipStore memberships,
    Notifier notifier)
{
    public async Task<DetailRequestsView?> ListAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await VisibleAsync(actor, engagementId, cancellationToken);
        if (engagement is null)
        {
            return null;
        }

        var missing = await MissingAsync(engagement, cancellationToken);
        var open = (await requests.ListForEngagementAsync(engagementId, cancellationToken))
            .Where(request => missing.Contains(request.Item))
            .ToList();

        var asked = open
            .Where(request => request.UserId == actor.UserId)
            .Select(request => request.Item)
            .ToList();

        IReadOnlyList<DetailAskers>? askers = null;
        if (OrganisationAuthorizationService.IsOrganiser(actor))
        {
            var names = (await memberships.ListForOrganisationAsync(actor.OrganisationId, cancellationToken))
                .ToDictionary(row => row.Membership.UserId, row => row.DisplayName);
            askers = open
                .GroupBy(request => request.Item)
                .OrderBy(group => group.Key)
                .Select(group => new DetailAskers(
                    group.Key,
                    group.Select(request => names.GetValueOrDefault(request.UserId) ?? "Someone").ToList()))
                .ToList();
        }

        return new DetailRequestsView(missing, asked, askers);
    }

    /// <summary>
    /// Asks for one or more details. Only what is still missing and not yet
    /// asked for by this person goes to the organisers, in one notification;
    /// asking again for the same thing is a quiet success.
    /// </summary>
    public async Task<EngagementResult> AskAsync(
        Membership actor,
        Guid engagementId,
        IReadOnlyCollection<ReadinessItem> items,
        CancellationToken cancellationToken)
    {
        var engagement = await VisibleAsync(actor, engagementId, cancellationToken);
        if (engagement is null)
        {
            return EngagementResult.NotFound;
        }

        // Organisers fill details in; they do not ask themselves for them.
        if (OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        if (items.Count == 0 || items.Any(item => !DetailRequest.Askable.Contains(item)))
        {
            return EngagementResult.Invalid;
        }

        var missing = await MissingAsync(engagement, cancellationToken);
        var already = (await requests.ListForEngagementAsync(engagementId, cancellationToken))
            .Where(request => request.UserId == actor.UserId)
            .Select(request => request.Item)
            .ToHashSet();

        var fresh = items
            .Distinct()
            .Where(item => missing.Contains(item) && !already.Contains(item))
            .OrderBy(item => item)
            .ToList();
        if (fresh.Count == 0)
        {
            return EngagementResult.Success;
        }

        requests.Add(fresh.Select(item => DetailRequest.Ask(engagementId, actor.UserId, item)));
        await notifier.DetailsRequestedAsync(engagement, actor.UserId, fresh, cancellationToken);
        await requests.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// The event, if the caller may see it: an organiser always; anyone else
    /// only while they are on it and it has been shared with them.
    /// </summary>
    private async Task<Engagement?> VisibleAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(actor.OrganisationId, engagementId, cancellationToken);
        if (engagement is null || OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return engagement;
        }

        var participant = await participants.FindAsync(engagementId, actor.UserId, cancellationToken);
        return participant is { IsActive: true } && engagement.IsSharedWithMembers ? engagement : null;
    }

    /// <summary>The askable details that are not set and not marked as not needed.</summary>
    private async Task<IReadOnlyList<ReadinessItem>> MissingAsync(
        Engagement engagement,
        CancellationToken cancellationToken)
    {
        var waived = (await readiness.ListForEngagementAsync(engagement.Id, cancellationToken))
            .Select(waiver => waiver.Item)
            .ToHashSet();

        return DetailRequest.Askable
            .Where(item => !waived.Contains(item) && !IsSet(engagement, item))
            .ToList();
    }

    public static bool IsSet(Engagement engagement, ReadinessItem item) => item switch
    {
        ReadinessItem.Venue => engagement.Venue is not null,
        ReadinessItem.CallTime => engagement.CallTime is not null,
        ReadinessItem.StartTime => engagement.StartTime is not null,
        ReadinessItem.Dress => engagement.DressNotes is not null,
        _ => true,
    };
}
