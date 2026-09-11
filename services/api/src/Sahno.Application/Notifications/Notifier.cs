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
/// date, venue, or time changes. Everything else is in-app only.
/// </summary>
public sealed class Notifier(
    IMembershipStore memberships,
    INotificationStore notifications,
    IOutboxStore outbox)
{
    public async Task AvailabilityRequestedAsync(
        Engagement engagement,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        Tell(
            engagement,
            userIds,
            people,
            NotificationKind.AvailabilityRequested,
            $"Are you available for {engagement.Title}?",
            $"{When(engagement)}. Open the event to answer.",
            email: true);
    }

    public async Task AvailabilityReminderAsync(
        Engagement engagement,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        Tell(
            engagement,
            [userId],
            people,
            NotificationKind.AvailabilityReminder,
            $"Reminder: can you do {engagement.Title}?",
            $"{When(engagement)}. The organiser is still waiting on your answer.",
            email: true);
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

        Tell(
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
            email: false);
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
                Tell(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementConfirmed,
                    $"{engagement.Title} is confirmed",
                    Join($"{When(engagement)}.", detail),
                    email: true);
                break;

            case EngagementStatus.Postponed:
                Tell(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementPostponed,
                    $"{engagement.Title} has been postponed",
                    Join("A new date will follow.", detail),
                    email: true);
                break;

            case EngagementStatus.Cancelled:
                Tell(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementCancelled,
                    $"{engagement.Title} has been cancelled",
                    detail,
                    email: true);
                break;

            // Reopening straight to Confirmed is told as a confirmation above,
            // which is what it is. The other two resumed states need their own.
            case EngagementStatus.CheckingAvailability
                or EngagementStatus.Tentative
                when from is EngagementStatus.Cancelled or EngagementStatus.Completed:
                Tell(
                    engagement,
                    participantUserIds,
                    people,
                    NotificationKind.EngagementReopened,
                    $"{engagement.Title} is back on",
                    Join($"{When(engagement)}.", detail),
                    email: true);
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

        Tell(
            engagement,
            participantUserIds,
            people,
            NotificationKind.EngagementDateChanged,
            $"{engagement.Title} has a new date",
            $"{When(engagement)}. Your availability for the old date no longer applies.",
            email: true);
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

        Tell(
            engagement,
            participantUserIds,
            people,
            NotificationKind.EngagementDetailsChanged,
            $"{engagement.Title}: {string.Join(", ", whatChanged)} changed",
            $"{When(engagement)}. Open the event for the latest details.",
            email: true);
    }

    public async Task ResponsibilityAssignedAsync(
        Engagement engagement,
        Guid assigneeUserId,
        string jobTitle,
        CancellationToken cancellationToken)
    {
        var people = await DirectoryAsync(engagement.OrganisationId, cancellationToken);

        Tell(
            engagement,
            [assigneeUserId],
            people,
            NotificationKind.ResponsibilityAssigned,
            $"You are down for: {jobTitle}",
            $"For {engagement.Title}, {When(engagement)}.",
            email: false);
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

        Tell(
            engagement,
            audience,
            people,
            NotificationKind.DiscussionMessage,
            $"{name} in {engagement.Title}",
            preview.Length > 140 ? preview[..140] + "…" : preview,
            email: false);
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

        notifications.Stage(
            Organisers(people)
                .Where(userId => userId != joinedUserId)
                .Select(userId => Notification.Create(
                    organisationId,
                    userId,
                    engagementId: null,
                    NotificationKind.MemberJoined,
                    $"{name} joined",
                    "They can now be selected for events."))
                .ToList());
    }

    private void Tell(
        Engagement engagement,
        IEnumerable<Guid> recipients,
        IReadOnlyList<OrganisationMember> people,
        NotificationKind kind,
        string title,
        string? body,
        bool email)
    {
        var byUser = people.ToDictionary(row => row.Membership.UserId);
        var inApp = new List<Notification>();
        var mail = new List<OutboxMessage>();

        foreach (var userId in recipients.Distinct())
        {
            inApp.Add(Notification.Create(
                engagement.OrganisationId,
                userId,
                engagement.Id,
                kind,
                title,
                body));

            // No address means no email, not an error: somebody who signed in
            // with Apple's relay off still gets the in-app copy.
            if (email
                && byUser.TryGetValue(userId, out var person)
                && !string.IsNullOrWhiteSpace(person.Email))
            {
                mail.Add(OutboxMessage.Email(
                    person.Email,
                    title,
                    EmailBody(person.DisplayName, title, body, engagement)));
            }
        }

        notifications.Stage(inApp);
        outbox.Stage(mail);
    }

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
