using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Notifications;

/// <summary>
/// Turns something that happened into what people are told (Slice 10, D-049).
///
/// Every method here STAGES rows and returns; it never saves. The service that
/// made the change saves next, and the notifications and any emails commit in
/// the same transaction as the change they describe. That is the whole
/// guarantee: a confirmation cannot exist without its emails queued, and an
/// email cannot be queued for a confirmation that rolled back.
///
/// Which events also email is decided by D-049 and not here: availability
/// requests and reminders, confirmation, postponement, cancellation, and major
/// date, venue, or time changes. Everything else is in-app only — where
/// "in-app" now means the bell and a push to the phone (D-080), which is what
/// makes the in-app copy reachable from a locked screen.
/// </summary>
public sealed class Notifier(
    IMembershipStore memberships,
    INotificationStore notifications,
    IOutboxStore outbox,
    IPushDeviceStore pushDevices,
    IDiscussionStore discussion)
{
    public async Task AvailabilityRequestedAsync(
        Engagement engagement,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            userIds,
            people,
            NotificationKind.AvailabilityRequested,
            $"Are you available for {engagement.Title}?",
            $"{When(engagement)}. Open the event to answer.",
            email: true,
            cancellationToken);
    }

    public async Task AvailabilityReminderAsync(
        Engagement engagement,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            [userId],
            people,
            NotificationKind.AvailabilityReminder,
            $"Reminder: can you do {engagement.Title}?",
            $"{When(engagement)}. The organiser is still waiting on your answer.",
            email: true,
            cancellationToken);
    }

    /// <summary>
    /// Organisers hear that somebody answered. In-app only — it is useful to
    /// know, not urgent enough to interrupt an inbox for.
    /// </summary>
    public async Task AvailabilityAnsweredAsync(
        Engagement engagement,
        Guid answeredByUserId,
        AvailabilityResponse response,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var name = NameOf(people, answeredByUserId);

        await TellAsync(
            engagement,
            Organisers(people),
            people,
            NotificationKind.AvailabilityAnswered,
            $"{name} answered for {engagement.Title}",
            response switch
            {
                AvailabilityResponse.Available => $"{name} is available.",
                AvailabilityResponse.Maybe => $"{name} might be available.",
                _ => $"{name} is not available.",
            },
            email: false,
            cancellationToken);
    }

    /// <summary>
    /// A lifecycle change everybody on the lineup needs to hear. Which kinds
    /// email is D-049's list; the reason, where one was given, travels with it
    /// because "cancelled" on its own leaves people guessing.
    /// </summary>
    public async Task StatusChangedAsync(
        Engagement engagement,
        IReadOnlyList<Guid> participantUserIds,
        EngagementStatus from,
        string? reason,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var detail = string.IsNullOrWhiteSpace(reason) ? null : $"Reason: {reason.Trim()}";

        switch (engagement.Status)
        {
            case EngagementStatus.Confirmed:
                await TellAsync(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementConfirmed,
                    $"{engagement.Title} is confirmed",
                    Join($"{When(engagement)}.", detail),
                    email: true,
                    cancellationToken);
                break;

            case EngagementStatus.Postponed:
                await TellAsync(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementPostponed,
                    $"{engagement.Title} has been postponed",
                    Join("A new date will follow.", detail),
                    email: true,
                    cancellationToken);
                break;

            case EngagementStatus.Cancelled:
                await TellAsync(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementCancelled,
                    $"{engagement.Title} has been cancelled",
                    detail,
                    email: true,
                    cancellationToken);
                break;

            // Reopening straight to Confirmed is told as a confirmation above,
            // which is what it is. The other two resumed states need their own.
            case EngagementStatus.CheckingAvailability
                or EngagementStatus.Tentative
                when from is EngagementStatus.Cancelled or EngagementStatus.Completed:
                await TellAsync(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementReopened,
                    $"{engagement.Title} is back on",
                    Join($"{When(engagement)}.", detail),
                    email: true,
                    cancellationToken);
                break;

            default:
                // Draft → Checking Availability is told through the request
                // itself; Tentative and Completed are organiser bookkeeping.
                break;
        }
    }

    public async Task DateChangedAsync(
        Engagement engagement,
        IReadOnlyList<Guid> participantUserIds,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            participantUserIds,
            people,
            NotificationKind.EngagementDateChanged,
            $"{engagement.Title} has a new date",
            $"{When(engagement)}. Your availability for the old date no longer applies.",
            email: true,
            cancellationToken);
    }

    /// <summary>
    /// Venue, call time, or start time moved on an event people already hold.
    /// These are the details a performer plans their day around, which is why
    /// they email and a dress-note edit does not.
    /// </summary>
    public async Task DetailsChangedAsync(
        Engagement engagement,
        IReadOnlyList<Guid> participantUserIds,
        IReadOnlyList<string> whatChanged,
        CancellationToken cancellationToken)
    {
        if (whatChanged.Count == 0)
        {
            return;
        }

        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            participantUserIds,
            people,
            NotificationKind.EngagementDetailsChanged,
            $"{engagement.Title}: {string.Join(", ", whatChanged)} changed",
            $"{When(engagement)}. Open the event for the latest details.",
            email: true,
            cancellationToken);
    }

    /// <summary>
    /// Somebody on the event asked for details that are not set yet (D-089).
    /// Organisers only — they are the ones who can fill them in.
    /// </summary>
    /// <summary>
    /// Somebody was taken off an event (D-097). Without this the event simply
    /// disappears from their phone and they are left wondering why.
    /// </summary>
    public async Task RemovedFromEngagementAsync(
        Engagement engagement,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            [userId],
            people,
            NotificationKind.RemovedFromEngagement,
            $"You are off {engagement.Title}",
            $"{When(engagement)}. An organiser took you off this event, so it no longer shows in your events.",
            email: false,
            cancellationToken);
    }

    public async Task DetailsRequestedAsync(
        Engagement engagement,
        Guid askedByUserId,
        IReadOnlyList<ReadinessItem> items,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var what = JoinNaturally(items.Select(DetailRequest.Describe).ToList());

        await TellAsync(
            engagement,
            Organisers(people).Where(userId => userId != askedByUserId),
            people,
            NotificationKind.DetailsRequested,
            $"{NameOf(people, askedByUserId)} asked about {engagement.Title}",
            $"What is the {what}?",
            email: false,
            cancellationToken);
    }

    /// <summary>
    /// The outfit somebody asked for has been set (D-089). Venue and times
    /// already reach everyone on the event as a details change; what to wear
    /// does not, so the people who asked are told here.
    /// </summary>
    public async Task OutfitAnsweredAsync(
        Engagement engagement,
        IReadOnlyList<Guid> askedByUserIds,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            askedByUserIds,
            people,
            NotificationKind.EngagementDetailsChanged,
            $"{engagement.Title}: what to wear",
            engagement.DressNotes,
            email: false,
            cancellationToken);
    }

    /// <summary>"venue", "venue and outfit", "venue, start time and outfit".</summary>
    private static string JoinNaturally(IReadOnlyList<string> parts) => parts.Count switch
    {
        0 => string.Empty,
        1 => parts[0],
        _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}",
    };

    public async Task ResponsibilityAssignedAsync(
        Engagement engagement,
        Guid assigneeUserId,
        string jobTitle,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            [assigneeUserId],
            people,
            NotificationKind.ResponsibilityAssigned,
            $"You are down for: {jobTitle}",
            $"For {engagement.Title}, {When(engagement)}.",
            email: false,
            cancellationToken);
    }

    /// <summary>
    /// A rehearsal was booked, or its date, time or place changed (D-085).
    /// Told to the lineup that is expected at it; never to the organiser who
    /// just made the change.
    /// </summary>
    public async Task RehearsalScheduledAsync(
        Engagement engagement,
        Rehearsal rehearsal,
        IReadOnlyList<Guid> lineup,
        Guid changedByUserId,
        bool isChange,
        CancellationToken cancellationToken)
    {
        var recipients = lineup.Where(userId => userId != changedByUserId).ToList();
        if (recipients.Count == 0)
        {
            return;
        }

        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var what = RehearsalName(rehearsal, engagement);

        await TellAsync(
            engagement,
            recipients,
            people,
            NotificationKind.RehearsalScheduled,
            isChange ? $"{what} has changed" : $"{what} booked",
            DescribeRehearsal(rehearsal),
            email: false,
            cancellationToken);
    }

    /// <summary>The morning of a rehearsal (D-085).</summary>
    public async Task RehearsalReminderAsync(
        Engagement engagement,
        Rehearsal rehearsal,
        IReadOnlyList<Guid> lineup,
        CancellationToken cancellationToken)
    {
        if (lineup.Count == 0)
        {
            return;
        }

        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        await TellAsync(
            engagement,
            lineup,
            people,
            NotificationKind.RehearsalReminder,
            $"Today: {RehearsalName(rehearsal, engagement)}",
            DescribeRehearsal(rehearsal),
            email: false,
            cancellationToken);
    }

    /// <summary>
    /// The morning of a confirmed event (D-085). Somebody with jobs still open
    /// gets them in the same message, which opens on the Jobs page; everybody
    /// else gets the plain reminder, which opens the event.
    /// </summary>
    public async Task EngagementDayReminderAsync(
        Engagement engagement,
        IReadOnlyList<Guid> lineup,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> openJobsByUser,
        CancellationToken cancellationToken)
    {
        if (lineup.Count == 0)
        {
            return;
        }

        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var title = $"Today: {engagement.Title}";
        var day = DescribeDay(engagement);

        var plain = lineup.Where(userId => !openJobsByUser.ContainsKey(userId)).ToList();
        if (plain.Count > 0)
        {
            await TellAsync(
                engagement,
                plain,
                people,
                NotificationKind.EngagementDayReminder,
                title,
                day,
                email: false,
                cancellationToken);
        }

        // Each person's own list, so each is told only about their own jobs.
        foreach (var userId in lineup.Where(openJobsByUser.ContainsKey))
        {
            var jobs = openJobsByUser[userId];
            await TellAsync(
                engagement,
                [userId],
                people,
                NotificationKind.ResponsibilityReminder,
                title,
                $"{day} Still to do: {string.Join(", ", jobs)}.",
                email: false,
                cancellationToken);
        }
    }

    public async Task DiscussionMessageAsync(
        Engagement engagement,
        IReadOnlyList<Guid> participantUserIds,
        Guid authorUserId,
        string preview,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var name = NameOf(people, authorUserId);

        // Everyone on the event, plus the organisers who may not be on the
        // lineup themselves, minus whoever wrote it.
        var audience = participantUserIds
            .Concat(Organisers(people))
            .Where(userId => userId != authorUserId)
            .Distinct()
            .ToList();

        var title = $"{name} in {engagement.Title}";
        var body = Clip(preview, 140);

        // On Android each chat is one notification that the next message
        // replaces (D-088), so the push carries everything still unread there:
        // "Saturday gig · 3 new messages" over the latest lines. The bell row
        // stays one message, as before. iOS groups by thread and keeps each
        // message, so it is sent as it is.
        var unread = (await notifications.ListUnreadForEngagementAsync(
                engagement.OrganisationId,
                engagement.Id,
                NotificationKind.DiscussionMessage,
                audience,
                cancellationToken))
            .GroupBy(row => row.RecipientUserId)
            .ToDictionary(
                group => group.Key,
                // A message is stamped a moment before its notification is,
                // so the window opens a little earlier; how many lines to
                // quote comes from the unread count, not from the window.
                group => (Count: group.Count(), Since: group.Min(row => row.CreatedAtUtc) - TimeSpan.FromMinutes(1)));

        IReadOnlyList<DiscussionMessage> earlier = unread.Count == 0
            ? []
            : await discussion.ListSinceAsync(
                engagement.Id,
                unread.Values.Min(row => row.Since),
                cancellationToken);

        (string Title, string? Body)? Bundled(Guid userId, PushDevice phone)
        {
            if (phone.Platform != "android" || !unread.TryGetValue(userId, out var waiting))
            {
                return null;
            }

            var lines = earlier
                .Where(message => message.PostedAtUtc >= waiting.Since
                    && message.AuthorUserId != userId
                    && !message.IsDeleted)
                .TakeLast(waiting.Count)
                .Select(message => $"{NameOf(people, message.AuthorUserId)}: {Clip(message.Body!, 80)}")
                .Append($"{name}: {Clip(preview, 80)}")
                .TakeLast(5);

            return ($"{engagement.Title} · {waiting.Count + 1} new messages", string.Join("\n", lines));
        }

        await StageAsync(
            engagement.OrganisationId,
            engagement.Id,
            audience,
            NotificationKind.DiscussionMessage,
            title,
            body,
            emailBody: null,
            cancellationToken,
            Bundled);
    }

    private static string Clip(string text, int length) =>
        text.Length > length ? text[..length] + "…" : text;

    /// <summary>
    /// Somebody reacted to a message (D-087). Only its author is told — the
    /// rest of the chat sees the count change, which is enough for them.
    /// </summary>
    public async Task DiscussionReactionAsync(
        Engagement engagement,
        Guid messageAuthorUserId,
        Guid reactorUserId,
        string emoji,
        string preview,
        CancellationToken cancellationToken)
    {
        if (messageAuthorUserId == reactorUserId)
        {
            return;
        }

        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);
        var quote = preview.Length > 100 ? preview[..100] + "…" : preview;

        await TellAsync(
            engagement,
            [messageAuthorUserId],
            people,
            NotificationKind.DiscussionReaction,
            $"{NameOf(people, reactorUserId)} in {engagement.Title}",
            $"Reacted {emoji} to \"{quote}\"",
            email: false,
            cancellationToken);
    }

    /// <summary>
    /// Somebody accepted an invitation. Organisers only, in-app only — the
    /// "recent joins" card on Home covers the same ground, and this is what
    /// lets it eventually go.
    /// </summary>
    public async Task MemberJoinedAsync(
        Guid organisationId,
        Guid joinedUserId,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(organisationId, cancellationToken);
        var name = NameOf(people, joinedUserId);

        await StageAsync(
            organisationId,
            engagementId: null,
            Organisers(people).Where(userId => userId != joinedUserId),
            NotificationKind.MemberJoined,
            $"{name} joined",
            "They can now be selected for events.",
            emailBody: null,
            cancellationToken);
    }

    /// <summary>
    /// The Owner deleted the organisation (D-101). Push only: the bell is per
    /// organisation, and this one's is hidden with it. Without this the group
    /// just vanishes from people's phones. The payload carries no
    /// notification, so tapping it opens the app and nothing more.
    /// </summary>
    public Task OrganisationDeletedAsync(
        Organisation organisation,
        IEnumerable<Guid> recipients,
        CancellationToken cancellationToken)
    {
        var until = organisation.PurgeAfterUtc is { } purge ? $" Its owner can restore it until {LocalDay(organisation, purge)}." : string.Empty;
        return PushOnlyAsync(
            recipients,
            $"{organisation.Name} was deleted",
            $"Its owner deleted {organisation.Name}, so it no longer shows in Sahno.{until}",
            cancellationToken);
    }

    /// <summary>The Owner brought it back (D-101): everything is as it was.</summary>
    public Task OrganisationRestoredAsync(
        Organisation organisation,
        IEnumerable<Guid> recipients,
        CancellationToken cancellationToken)
    {
        return PushOnlyAsync(
            recipients,
            $"{organisation.Name} is back",
            $"Its owner restored {organisation.Name}, with everything in it.",
            cancellationToken);
    }

    private async Task PushOnlyAsync(
        IEnumerable<Guid> recipients,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        var devices = await pushDevices.ListActiveForUsersAsync(recipients.Distinct().ToList(), cancellationToken);
        outbox.Stage(devices
            .Select(phone => OutboxMessage.Push(phone.Token, title, body, "{}"))
            .ToList());
    }

    private static string LocalDay(Organisation organisation, DateTimeOffset utc)
    {
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(organisation.TimeZoneId, out var found)
            ? found
            : TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTime(utc, zone).ToString("d MMMM", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// An organiser writing to everybody in the organisation. Reaches every
    /// member except the author, in-app and by push; no email, because an
    /// announcement is a message, not a change to anybody's diary.
    /// </summary>
    public async Task AnnouncementAsync(
        Guid organisationId,
        Guid authorUserId,
        string title,
        string? body,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(organisationId, cancellationToken);

        await StageAsync(
            organisationId,
            engagementId: null,
            people
                .Select(row => row.Membership.UserId)
                .Where(userId => userId != authorUserId),
            NotificationKind.OrganiserAnnouncement,
            title,
            body,
            emailBody: null,
            cancellationToken);
    }

    private Task TellAsync(
        Engagement engagement,
        IEnumerable<Guid> recipients,
        IReadOnlyList<OrganisationMember> people,
        NotificationKind kind,
        string title,
        string? body,
        bool email,
        CancellationToken cancellationToken)
    {
        var byUser = people.ToDictionary(row => row.Membership.UserId);

        // No address means no email, not an error: somebody who signed in
        // with Apple's relay off still gets the in-app copy.
        Func<Guid, OutboxMessage?>? emailBody = email
            ? userId =>
                byUser.TryGetValue(userId, out var person)
                && !string.IsNullOrWhiteSpace(person.Email)
                    ? OutboxMessage.Email(
                        person.Email,
                        title,
                        EmailBody(person.DisplayName, title, body, engagement))
                    : null
            : null;

        return StageAsync(
            engagement.OrganisationId,
            engagement.Id,
            recipients,
            kind,
            title,
            body,
            emailBody,
            cancellationToken);
    }

    /// <summary>
    /// The one place rows are staged. Every recipient gets the in-app row;
    /// every active device they are signed in on gets a push; email goes
    /// where the kind asks for it. All in the same unit of work as the change
    /// itself, which is what makes push as durable as email: a phone that is
    /// off tonight is pushed when it is back, and a booking that rolled back
    /// is never pushed at all.
    /// </summary>
    private async Task StageAsync(
        Guid organisationId,
        Guid? engagementId,
        IEnumerable<Guid> recipients,
        NotificationKind kind,
        string title,
        string? body,
        Func<Guid, OutboxMessage?>? emailBody,
        CancellationToken cancellationToken,
        Func<Guid, PushDevice, (string Title, string? Body)?>? pushText = null)
    {
        var userIds = recipients.Distinct().ToList();
        if (userIds.Count == 0)
        {
            return;
        }

        var devices = PushFor(kind)
            ? (await pushDevices.ListActiveForUsersAsync(userIds, cancellationToken))
                .GroupBy(device => device.UserId)
                .ToDictionary(group => group.Key, group => group.ToList())
            : new Dictionary<Guid, List<PushDevice>>();

        var inApp = new List<Notification>();
        var messages = new List<OutboxMessage>();

        foreach (var userId in userIds)
        {
            var notification = Notification.Create(
                organisationId,
                userId,
                engagementId,
                kind,
                title,
                body);
            inApp.Add(notification);

            if (emailBody?.Invoke(userId) is { } mail)
            {
                messages.Add(mail);
            }

            if (devices.TryGetValue(userId, out var phones))
            {
                var data = NotificationPayload.From(notification).ToJson();
                messages.AddRange(phones.Select(phone =>
                    pushText?.Invoke(userId, phone) is { } text
                        ? OutboxMessage.Push(phone.Token, text.Title, text.Body, data)
                        : OutboxMessage.Push(phone.Token, notification.Title, notification.Body, data)));
            }
        }

        notifications.Stage(inApp);
        outbox.Stage(messages);
    }

    /// <summary>
    /// Which kinds are worth buzzing a pocket for. Somebody joining is
    /// bookkeeping: it shows on the bell, which updates live, and stays out of
    /// the tray. An availability answer is not — it is what the organiser is
    /// waiting on to settle the lineup, so it reaches their phone (D-080, as
    /// amended). D-049: non-critical kinds are the ones a preference will one
    /// day switch off.
    /// </summary>
    private static bool PushFor(NotificationKind kind) =>
        kind is not NotificationKind.MemberJoined;

    private static string EmailBody(
        string? displayName,
        string title,
        string? body,
        Engagement engagement)
    {
        var greeting = string.IsNullOrWhiteSpace(displayName)
            ? "Hello,"
            : $"Hello {displayName.Split(' ')[0]},";

        var lines = new List<string> { greeting, string.Empty, title };
        if (body is not null)
        {
            lines.Add(body);
        }

        if (engagement.Venue is not null)
        {
            lines.Add($"Where: {engagement.Venue}");
        }

        lines.Add(string.Empty);
        lines.Add("Open Sahno for the full details.");
        lines.Add(string.Empty);
        lines.Add("— Sahno");

        return string.Join("\n", lines);
    }

    /// <summary>"Rehearsal for Saturday gig", or its own name when it has one.</summary>
    private static string RehearsalName(Rehearsal rehearsal, Engagement engagement) =>
        string.IsNullOrWhiteSpace(rehearsal.Title)
            ? $"Rehearsal for {engagement.Title}"
            : $"{rehearsal.Title} ({engagement.Title})";

    /// <summary>"Thu 2 Oct, 7:00 PM–9:00 PM at Community Hall."</summary>
    private static string DescribeRehearsal(Rehearsal rehearsal)
    {
        var when = rehearsal.Date.ToString("ddd d MMM");
        if (rehearsal.StartTime is { } start)
        {
            when += rehearsal.EndTime is { } end
                ? $", {start:h:mm tt}–{end:h:mm tt}"
                : $", {start:h:mm tt}";
        }

        return string.IsNullOrWhiteSpace(rehearsal.Venue)
            ? $"{when}."
            : $"{when} at {rehearsal.Venue}.";
    }

    /// <summary>"Arrive by 6:30 PM at Dural Hall." — only what is known.</summary>
    private static string DescribeDay(Engagement engagement)
    {
        var parts = new List<string>();
        if (engagement.CallTime is { } call)
        {
            parts.Add($"Arrive by {call:h:mm tt}");
        }
        else if (engagement.StartTime is { } start)
        {
            parts.Add($"Starts {start:h:mm tt}");
        }

        if (!string.IsNullOrWhiteSpace(engagement.Venue))
        {
            parts.Add(parts.Count == 0 ? $"At {engagement.Venue}" : $"at {engagement.Venue}");
        }

        return parts.Count == 0 ? "It is today." : $"{string.Join(" ", parts)}.";
    }

    private static string When(Engagement engagement)
    {
        if (engagement.StartDate is null)
        {
            return "Date to be confirmed";
        }

        var date = engagement.StartDate.Value.ToString("ddd d MMM yyyy");
        return engagement.StartTime is { } time
            ? $"{date}, {time:h:mm tt}"
            : date;
    }

    private static string? Join(string first, string? second)
    {
        return second is null ? first : $"{first} {second}";
    }

    private static IReadOnlyList<Guid> Organisers(IReadOnlyList<OrganisationMember> people)
    {
        return people
            .Where(row => OrganisationAuthorizationService.IsOrganiser(row.Membership))
            .Select(row => row.Membership.UserId)
            .ToList();
    }

    private static string NameOf(IReadOnlyList<OrganisationMember> people, Guid userId)
    {
        var person = people.FirstOrDefault(row => row.Membership.UserId == userId);
        return string.IsNullOrWhiteSpace(person?.DisplayName)
            ? "Someone"
            : person.DisplayName;
    }

    private Task<IReadOnlyList<OrganisationMember>> DirectoryAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return memberships.ListForOrganisationAsync(organisationId, cancellationToken);
    }
}
