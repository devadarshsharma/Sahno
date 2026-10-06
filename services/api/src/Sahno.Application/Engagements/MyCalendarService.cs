using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>One event on a person's own calendar, with the organisation it belongs to.</summary>
public sealed record MyCalendarEvent(Organisation Organisation, bool IsOrganiser, EngagementView View);

/// <summary>One rehearsal on a person's own calendar, with the organisation it belongs to.</summary>
public sealed record MyCalendarRehearsal(Organisation Organisation, UpcomingRehearsal Row);

/// <summary>
/// A person's calendar across every organisation they belong to (D-091) — for
/// the freelancer playing with several groups, the one place their whole
/// diary shows, and what a clash is checked against.
///
/// Each organisation contributes exactly what that person already sees in it:
/// every booking where they are an organiser, and only the events they are on
/// where they are a member. Nothing here is ever shown to anybody else — an
/// organiser in one group never learns what a member has on with another.
/// </summary>
public sealed class MyCalendarService(
    IMembershipStore memberships,
    EngagementService engagementService,
    PreparationService preparationService)
{
    public async Task<(IReadOnlyList<MyCalendarEvent> Events, IReadOnlyList<MyCalendarRehearsal> Rehearsals)> ForUserAsync(
        Guid userId,
        DateOnly fromDate,
        CancellationToken cancellationToken)
    {
        var events = new List<MyCalendarEvent>();
        var rehearsals = new List<MyCalendarRehearsal>();

        foreach (var row in await memberships.ListForUserAsync(userId, cancellationToken))
        {
            var isOrganiser = OrganisationAuthorizationService.IsOrganiser(row.Membership);

            events.AddRange(
                (await engagementService.ListVisibleWithContextAsync(row.Membership, cancellationToken))
                    .Where(view => OnTheCalendar(view.Engagement, fromDate))
                    .Select(view => new MyCalendarEvent(row.Organisation, isOrganiser, view)));

            rehearsals.AddRange(
                (await preparationService.CalendarRehearsalsAsync(row.Membership, fromDate, cancellationToken))
                    .Select(rehearsal => new MyCalendarRehearsal(row.Organisation, rehearsal)));
        }

        return (
            events.OrderBy(item => item.View.Engagement.StartDate).ToList(),
            rehearsals.OrderBy(item => item.Row.Rehearsal.Date).ToList());
    }

    /// <summary>
    /// Dated, not over before the window opens, and still a real day: what was
    /// cancelled did not happen, and a postponed event's old date is no longer
    /// its date. Done stays (D-090).
    /// </summary>
    private static bool OnTheCalendar(Engagement engagement, DateOnly fromDate) =>
        engagement.StartDate is { } start
        && (engagement.EndDate ?? start) >= fromDate
        && engagement.Status is not (EngagementStatus.Cancelled or EngagementStatus.Postponed);
}
