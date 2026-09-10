using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>
/// Event readiness (Slice 7, D-048). The checklist is derived from the booking
/// itself, so it cannot fall out of step with it — the only thing stored is an
/// organiser's decision that an item does not apply to this event.
/// </summary>
public sealed class ReadinessService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IReadinessStore waivers)
{
    public async Task<IReadOnlyList<ReadinessEntry>> ForEngagementAsync(
        Engagement engagement,
        CancellationToken cancellationToken)
    {
        var lineup = await participants.ListForEngagementAsync(
            engagement.Id,
            cancellationToken);

        var active = lineup.Where(participant => participant.IsActive).ToList();

        // A lineup counts as resolved once somebody has been asked and nobody
        // is still to answer. Nobody asked at all is not readiness, it is a
        // step not yet taken.
        var lineupResolved =
            active.Count > 0 && active.All(participant => participant.Response is not null);

        var waived = (await waivers.ListForEngagementAsync(
                engagement.Id,
                cancellationToken))
            .Select(waiver => waiver.Item)
            .ToHashSet();

        return engagement.Readiness(lineupResolved, waived);
    }

    /// <summary>
    /// Marks a checklist item as not applying to this event, or puts it back.
    /// Organisers only: readiness is their working list (D-048).
    /// </summary>
    public async Task<EngagementResult> SetNotRequiredAsync(
        Membership actor,
        Guid engagementId,
        ReadinessItem item,
        bool notRequired,
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

        if (notRequired)
        {
            await waivers.AddAsync(
                ReadinessWaiver.Create(engagementId, item, actor.UserId),
                cancellationToken);
        }
        else
        {
            await waivers.RemoveAsync(engagementId, item, cancellationToken);
        }

        return EngagementResult.Success;
    }

    /// <summary>
    /// Whether this engagement has readiness worth chasing. Drives the
    /// organiser's Needs attention list: a confirmed booking missing critical
    /// detail is unresolved work (Slice 7, D-041).
    /// </summary>
    public static bool HasOutstandingReadiness(IReadOnlyList<ReadinessEntry> readiness)
    {
        return CountOutstanding(readiness) > 0;
    }

    /// <summary>
    /// How many checklist items are still to sort out, counting only the ones
    /// Sahno can actually answer today (see <see cref="ReadinessItems.IsTracked"/>).
    /// </summary>
    public static int CountOutstanding(IReadOnlyList<ReadinessEntry> readiness)
    {
        return readiness.Count(entry =>
            entry.State == ReadinessState.Outstanding && entry.Item.IsTracked());
    }
}
