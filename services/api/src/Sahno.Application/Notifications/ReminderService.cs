using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Notifications;

/// <summary>
/// Scheduled reminders (D-085): each rehearsal, and each confirmed event —
/// with each person's still-open jobs folded into theirs — at the time the
/// organisation chose for each (D-118).
///
/// Run every few minutes by a background worker. Each pass works in every
/// organisation's own time zone, sends what has fallen due today, and logs it
/// in the same save as the notifications, so a reminder goes out exactly once
/// however often the pass runs, and however the process restarts.
///
/// "Due" is <see cref="ReminderTiming.DueAt"/>: by default 8:00 on the day,
/// or two hours before the time that matters (the rehearsal's start, the
/// event's arrival) when that is earlier; or one to three hours before; or
/// 18:00 the evening before. Once the thing has begun the reminder is no
/// longer sent: telling somebody at noon that a 10:00 rehearsal is today
/// helps nobody.
/// </summary>
public sealed class ReminderService(
    IOrganisationStore organisations,
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IMembershipStore memberships,
    IRehearsalStore rehearsals,
    IResponsibilityStore responsibilities,
    IReminderLogStore log,
    Notifier notifier,
    TimeProvider time)
{
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
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone).DateTime;
        var today = DateOnly.FromDateTime(localNow);
        // Reminders can go days ahead (D-118, D-124), so look that far.
        var horizon = today.AddDays(ReminderTiming.MaxLeadDays);
        var sent = 0;

        var all = await engagements.ListForOrganisationAsync(organisation.Id, cancellationToken);

        // Organisers are reminded too (D-115), whether or not they are on the lineup.
        var organisers = (await memberships.ListForOrganisationAsync(organisation.Id, cancellationToken))
            .Where(row => OrganisationAuthorizationService.IsOrganiser(row.Membership))
            .Select(row => row.Membership.UserId)
            .ToList();

        // The day of a confirmed event, at the time the organisation chose (D-118).
        foreach (var engagement in all.Where(engagement =>
                     engagement.Status == EngagementStatus.Confirmed
                     && engagement.StartDate is { } start && start >= today && start <= horizon))
        {
            var day = engagement.StartDate!.Value;
            var arrival = engagement.CallTime ?? engagement.StartTime;
            if (DueSince(organisation.EventReminder, day, arrival, localNow) is null
                || await log.WasSentAsync(ReminderKind.EngagementDay, engagement.Id, day, cancellationToken))
            {
                continue;
            }

            var lineup = ExpectedLineup.WithOrganisers(
                await participants.ListForEngagementAsync(engagement.Id, cancellationToken),
                organisers);
            var expected = lineup.ToHashSet();

            var openJobs = (await responsibilities.ListForEngagementAsync(engagement.Id, cancellationToken))
                .Where(job => !job.IsDone && job.AssignedUserId is { } owner && expected.Contains(owner))
                .GroupBy(job => job.AssignedUserId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group.Select(job => job.Title).ToList());

            await notifier.EngagementDayReminderAsync(engagement, lineup, openJobs, cancellationToken, WhenWord(day, today));
            // Logged against the event's day, so it goes once whatever the timing.
            log.Stage(ReminderLog.Sent(ReminderKind.EngagementDay, engagement.Id, day, utcNow));
            await log.SaveAsync(cancellationToken);
            sent++;
        }

        // Each rehearsal of an event still going ahead, at its own chosen time.
        var goingAhead = all
            .Where(ExpectedLineup.IsGoingAhead)
            .ToDictionary(engagement => engagement.Id);
        var soon = (await rehearsals.ListFromDateAsync(goingAhead.Keys, today, cancellationToken))
            .Where(rehearsal => rehearsal.Date <= horizon);

        foreach (var rehearsal in soon)
        {
            if (DueSince(organisation.RehearsalReminder, rehearsal.Date, rehearsal.StartTime, localNow) is not { } due)
            {
                continue;
            }

            // Booked after its reminder was due: the booking notification has
            // only just told everybody, so a reminder on top is noise.
            var dueAtUtc = new DateTimeOffset(due, zone.GetUtcOffset(due));
            if (rehearsal.CreatedAtUtc >= dueAtUtc
                || await log.WasSentAsync(ReminderKind.RehearsalDay, rehearsal.Id, rehearsal.Date, cancellationToken))
            {
                continue;
            }

            var engagement = goingAhead[rehearsal.EngagementId];
            var lineup = ExpectedLineup.WithOrganisers(
                await participants.ListForEngagementAsync(engagement.Id, cancellationToken),
                organisers);

            await notifier.RehearsalReminderAsync(engagement, rehearsal, lineup, cancellationToken, WhenWord(rehearsal.Date, today));
            log.Stage(ReminderLog.Sent(ReminderKind.RehearsalDay, rehearsal.Id, rehearsal.Date, utcNow));
            await log.SaveAsync(cancellationToken);
            sent++;
        }

        sent += await NudgeUnansweredAsync(organisation, all, zone, localNow, utcNow, cancellationToken);

        return sent;
    }

    /// <summary>
    /// Availability nobody has answered (D-120): a first nudge some days after
    /// the person was asked, then a last call at 10:00 the day before. Only
    /// while it is still unanswered and the event is still ahead; once each,
    /// logged per participant against the event's day. A request made after a
    /// nudge would have been due is not chased on top of itself, and an
    /// organiser's own Remind since then counts as the first nudge.
    /// </summary>
    private async Task<int> NudgeUnansweredAsync(
        Organisation organisation,
        IReadOnlyList<Engagement> all,
        TimeZoneInfo zone,
        DateTime localNow,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var nudge = organisation.AvailabilityNudge;
        if (nudge == AvailabilityNudge.Off || !AvailabilityNudge.IsSendingHour(localNow))
        {
            return 0;
        }

        DateTime Local(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, zone).DateTime;
        var today = DateOnly.FromDateTime(localNow);
        var sent = 0;

        foreach (var engagement in all.Where(engagement =>
                     ExpectedLineup.IsGoingAhead(engagement) && engagement.StartDate is { } day && day > today))
        {
            var day = engagement.StartDate!.Value;
            var unanswered = (await participants.ListForEngagementAsync(engagement.Id, cancellationToken))
                .Where(participant => participant.IsActive && participant.Response is null);

            foreach (var participant in unanswered)
            {
                var asked = Local(participant.RequestedAtUtc);
                var lastCall = AvailabilityNudge.LastCallAt(nudge, day);

                // The day before: the last call, which also stands in for a
                // first nudge not yet sent.
                if (lastCall is { } call && localNow >= call && asked < call)
                {
                    if (await log.WasSentAsync(ReminderKind.AvailabilityLastCall, participant.Id, day, cancellationToken))
                    {
                        continue;
                    }

                    await notifier.AvailabilityReminderAsync(engagement, participant.UserId, cancellationToken, lastCall: true);
                    log.Stage(ReminderLog.Sent(ReminderKind.AvailabilityLastCall, participant.Id, day, utcNow));
                    if (!await log.WasSentAsync(ReminderKind.AvailabilityNudge, participant.Id, day, cancellationToken))
                    {
                        log.Stage(ReminderLog.Sent(ReminderKind.AvailabilityNudge, participant.Id, day, utcNow));
                    }

                    await log.SaveAsync(cancellationToken);
                    sent++;
                    continue;
                }

                var first = AvailabilityNudge.FirstDueAt(nudge, asked);
                if (first is not { } due || localNow < due || (lastCall is { } before && due >= before)
                    || await log.WasSentAsync(ReminderKind.AvailabilityNudge, participant.Id, day, cancellationToken))
                {
                    continue;
                }

                // An organiser pressed Remind within a day of this nudge: that was it.
                var remindedByHand = participant.RemindedAtUtc is { } reminded && Local(reminded) >= due.AddDays(-1);
                if (!remindedByHand)
                {
                    await notifier.AvailabilityReminderAsync(engagement, participant.UserId, cancellationToken);
                    sent++;
                }

                log.Stage(ReminderLog.Sent(ReminderKind.AvailabilityNudge, participant.Id, day, utcNow));
                await log.SaveAsync(cancellationToken);
            }
        }

        return sent;
    }

    /// <summary>"Today", "Tomorrow", or "Sat 24 Oct" for a reminder sent further ahead.</summary>
    private static string WhenWord(DateOnly day, DateOnly today) =>
        day == today ? "Today"
        : day == today.AddDays(1) ? "Tomorrow"
        : day.ToString("ddd d MMM", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// When the reminder became due, if it is due now: its time has come and
    /// the thing itself has not begun. Null when it is not yet due, already
    /// too late, or reminders of this kind are off.
    /// </summary>
    private static DateTime? DueSince(string timing, DateOnly day, TimeOnly? at, DateTime localNow)
    {
        var due = ReminderTiming.DueAt(timing, day, at);
        return due is { } moment && localNow >= moment && localNow < ReminderTiming.Deadline(day, at)
            ? moment
            : null;
    }

    /// <summary>
    /// The organisation's zone. An id this server does not know falls back to
    /// UTC rather than stopping everybody's reminders.
    /// </summary>
    private static TimeZoneInfo ZoneOf(Organisation organisation) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(organisation.TimeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;
}
