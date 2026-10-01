using Sahno.Domain.Engagements;

namespace Sahno.Application.Engagements;

/// <summary>
/// Who is expected to turn up (D-085): everybody on the lineup who has not
/// said they cannot. "Maybe" and not-yet-answered are still expected — a
/// reminder is the nudge they need — but "not available" is not reminded about
/// something they already declined.
/// </summary>
public static class ExpectedLineup
{
    public static IReadOnlyList<Guid> Of(IEnumerable<EngagementParticipant> participants) =>
        participants
            .Where(participant =>
                participant.IsActive
                && participant.Response != AvailabilityResponse.Unavailable)
            .Select(participant => participant.UserId)
            .ToList();

    /// <summary>Statuses in which an event is still going ahead, so its rehearsals are too.</summary>
    public static bool IsGoingAhead(Engagement engagement) =>
        engagement.Status is EngagementStatus.CheckingAvailability
            or EngagementStatus.Tentative
            or EngagementStatus.Confirmed;
}
