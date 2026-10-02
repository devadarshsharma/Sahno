using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Notifications;

/// <summary>
/// Scheduled reminders (D-085): the morning of a rehearsal, and the morning of
/// a confirmed event — with each person's still-open jobs folded into theirs.
///
/// Run every few minutes by a background worker. Each pass works in every
/// organisation's own time zone, sends what has fallen due today, and logs it
/// in the same save as the notifications, so a reminder goes out exactly once
/// however often the pass runs, and however the process restarts.
///
/// "Due" is 8:00 on the day, or two hours before the time that matters (the
/// rehearsal's start, the event's arrival) when that is earlier. Once that
/// time has passed the reminder is no longer sent: telling somebody at noon
/// that a 10:00 rehearsal is today helps nobody.
/// </summary>
public sealed class ReminderService(
    IOrganisationStore organisations,
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IRehearsalStore rehearsals,
    IResponsibilityStore responsibilities,
    IReminderLogStore log,
    Notifier notifier,
    TimeProvider time)
{
    public static readonly TimeOnly MorningSendTime = new(8, 0);

    public static readonly TimeSpan LeadTime = TimeSpan.FromHours(2);

    /// <summary>Sends every reminder now due. Returns how many reminders went out.</summary>
    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        // UTC whatever the clock hands back: it is stored, and PostgreSQL keeps
        // timestamps only as UTC.
        var utcNow = time.GetUtcNow().ToUniversalTime();
        var sent = 0;

        foreach (var organisation in await organisations.ListAllAsync(cancellationToken))
        {
            sent += await SendDueForAsync(organisation, utcNow, cancellationToken);
        }

        return sent;
    }

    private async Task<int> SendDueForAsync(
        Organisation organisation,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var zone = ZoneOf(organisation);
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone);
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var nowTime = TimeOnly.FromDateTime(localNow.DateTime);
        var sent = 0;

        var all = await engagements.ListForOrganisationAsync(organisation.Id, cancellationToken);

        // The day of a confirmed event.
        foreach (var engagement in all.Where(engagement =>
                     engagement.Status == EngagementStatus.Confirmed
                     && engagement.StartDate == today))
        {
            var arrival = engagement.CallTime ?? engagement.StartTime;
            if (!IsDue(arrival, nowTime)
                || await log.WasSentAsync(ReminderKind.EngagementDay, engagement.Id, today, cancellationToken))
            {
                continue;
            }

            var lineup = ExpectedLineup.Of(
                await participants.ListForEngagementAsync(engagement.Id, cancellationToken));
            var expected = lineup.ToHashSet();

            var openJobs = (await responsibilities.ListForEngagementAsync(engagement.Id, cancellationToken))
                .Where(job => !job.IsDone && job.AssignedUserId is { } owner && expected.Contains(owner))
                .GroupBy(job => job.AssignedUserId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group.Select(job => job.Title).ToList());

            await notifier.EngagementDayReminderAsync(engagement, lineup, openJobs, cancellationToken);
            log.Stage(ReminderLog.Sent(ReminderKind.EngagementDay, engagement.Id, today, utcNow));
            await log.SaveAsync(cancellationToken);
            sent++;
        }

        // The day of a rehearsal, for events still going ahead.
        var goingAhead = all
            .Where(ExpectedLineup.IsGoingAhead)
            .ToDictionary(engagement => engagement.Id);
        var todays = (await rehearsals.ListFromDateAsync(goingAhead.Keys, today, cancellationToken))
            .Where(rehearsal => rehearsal.Date == today);

        foreach (var rehearsal in todays)
        {
            if (!IsDue(rehearsal.StartTime, nowTime))
            {
                continue;
            }

            // Booked after its reminder was due: the booking notification has
            // only just told everybody, so a "today" buzz on top is noise.
            var dueAtUtc = new DateTimeOffset(
                today.ToDateTime(SendTimeFor(rehearsal.StartTime)),
                zone.GetUtcOffset(today.ToDateTime(SendTimeFor(rehearsal.StartTime))));
            if (rehearsal.CreatedAtUtc >= dueAtUtc
                || await log.WasSentAsync(ReminderKind.RehearsalDay, rehearsal.Id, today, cancellationToken))
            {
                continue;
            }

            var engagement = goingAhead[rehearsal.EngagementId];
            var lineup = ExpectedLineup.Of(
                await participants.ListForEngagementAsync(engagement.Id, cancellationToken));

            await notifier.RehearsalReminderAsync(engagement, rehearsal, lineup, cancellationToken);
            log.Stage(ReminderLog.Sent(ReminderKind.RehearsalDay, rehearsal.Id, today, utcNow));
            await log.SaveAsync(cancellationToken);
            sent++;
        }

        return sent;
    }

    /// <summary>
    /// 8:00, or two hours before <paramref name="at"/> when that is earlier —
    /// a 9:00 rehearsal is reminded about at 7:00, not as it starts. Never
    /// before midnight: an early start still gets its reminder on the day.
    /// </summary>
    public static TimeOnly SendTimeFor(TimeOnly? at)
    {
        if (at is not { } time || time >= MorningSendTime.Add(LeadTime))
        {
            return MorningSendTime;
        }

        return time.ToTimeSpan() >= LeadTime
            ? time.Add(-LeadTime)
            : TimeOnly.MinValue;
    }

    /// <summary>Due once its send time has come, and while the thing itself has not yet begun.</summary>
    private static bool IsDue(TimeOnly? at, TimeOnly now) =>
        now >= SendTimeFor(at) && (at is null || now < at.Value);

    /// <summary>
    /// The organisation's zone. An id this server does not know falls back to
    /// UTC rather than stopping everybody's reminders.
    /// </summary>
    private static TimeZoneInfo ZoneOf(Organisation organisation) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(organisation.TimeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;
}
