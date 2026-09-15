using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Notifications;

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
    IPushDeviceStore pushDevices)
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

        await TellAsync(
            engagement,
            audience,
            people,
            NotificationKind.DiscussionMessage,
            $"{name} in {engagement.Title}",
            preview.Length > 140 ? preview[..140] + "…" : preview,
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
        CancellationToken cancellationToken)
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
                    OutboxMessage.Push(phone.Token, notification.Title, notification.Body, data)));
            }
        }

        notifications.Stage(inApp);
        outbox.Stage(messages);
    }

    /// <summary>
    /// Which kinds are worth buzzing a pocket for. Organiser bookkeeping —
    /// somebody answered, somebody joined — shows on the bell, which updates
    /// live, and stays out of the tray (D-049: non-critical kinds are the ones
    /// a preference will one day switch off).
    /// </summary>
    private static bool PushFor(NotificationKind kind) =>
        kind is not (NotificationKind.AvailabilityAnswered or NotificationKind.MemberJoined);

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
