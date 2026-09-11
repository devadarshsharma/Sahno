using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Application.Notifications;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Notifications;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Notifications and email (Slice 10, D-049).
///
/// Two things matter and both are tested from the outside. Which events tell
/// whom, and by which channel, is D-049's list and not a matter of taste. And
/// the outbox is what makes email reliable, so its retry path is driven here
/// with a sender that is told to fail, rather than assumed to work.
/// </summary>
public sealed class NotificationEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AnAvailabilityRequestTellsThePeopleAskedInAppAndByEmail()
    {
        var org = await NewOrganisationAsync("notif-request", members: 2);
        var engagement = await NewDraftAsync(org, "Wedding in Dural");

        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        var inApp = await NotificationsAsync(org.Members[0], org);
        var told = Assert.Single(inApp);
        Assert.Equal("AvailabilityRequested", told.Kind);
        Assert.Contains("Wedding in Dural", told.Title);
        Assert.Equal(engagement.Id, told.EngagementId);
        Assert.False(told.IsRead);

        var sent = await DispatchOutboxAsync();
        Assert.Contains(sent, mail =>
            mail.To == org.MemberEmails[0] && mail.Subject.Contains("Wedding in Dural"));
        Assert.Contains(sent, mail =>
            mail.To == org.MemberEmails[1] && mail.Subject.Contains("Wedding in Dural"));
    }

    /// <summary>
    /// The organiser hears that somebody answered. In-app only: useful to know,
    /// not worth an email.
    /// </summary>
    [Fact]
    public async Task AnAnswerTellsTheOrganisersInAppOnly()
    {
        var org = await NewOrganisationAsync("notif-answer", members: 1);
        var engagement = await NewDraftAsync(org, "Answered");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await DispatchOutboxAsync();
        var before = Sender().Sent.Count;

        await RespondAsync(org.Members[0], org, engagement.Id, "Available");

        var ownerSees = await NotificationsAsync(org.Owner, org);
        var answered = Assert.Single(ownerSees, row => row.Kind == "AvailabilityAnswered");
        Assert.Contains("is available", answered.Body);

        await DispatchOutboxAsync();
        Assert.Equal(before, Sender().Sent.Count);
    }

    [Fact]
    public async Task ConfirmingTellsTheLineupWithTheEmail()
    {
        var org = await NewOrganisationAsync("notif-confirm", members: 1);
        var engagement = await NewDraftAsync(org, "Confirmed gig");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await TransitionAsync(org, engagement.Id, "Tentative", null);

        await TransitionAsync(org, engagement.Id, "Confirmed", null);

        var member = await NotificationsAsync(org.Members[0], org);
        Assert.Contains(member, row => row.Kind == "EngagementConfirmed");

        var sent = await DispatchOutboxAsync();
        Assert.Contains(sent, mail =>
            mail.To == org.MemberEmails[0] && mail.Subject == "Confirmed gig is confirmed");
    }

    /// <summary>
    /// "Cancelled" on its own leaves people guessing. The reason the organiser
    /// had to give travels in the message.
    /// </summary>
    [Fact]
    public async Task CancellingCarriesTheReason()
    {
        var org = await NewOrganisationAsync("notif-cancel", members: 1);
        var engagement = await NewDraftAsync(org, "Cancelled gig");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        await TransitionAsync(org, engagement.Id, "Cancelled", "Venue flooded.");

        var member = await NotificationsAsync(org.Members[0], org);
        var cancelled = Assert.Single(member, row => row.Kind == "EngagementCancelled");
        Assert.Contains("Venue flooded.", cancelled.Body);

        var sent = await DispatchOutboxAsync();
        var mail = Assert.Single(sent, row => row.Subject.Contains("cancelled"));
        Assert.Contains("Venue flooded.", mail.Body);
    }

    [Fact]
    public async Task MovingTheCallTimeOnASharedEventIsNews()
    {
        var org = await NewOrganisationAsync("notif-details", members: 1);
        var engagement = await NewDraftAsync(org, "Moved call");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        await UpdateAsync(org, engagement.Id, callTime: new TimeOnly(17, 0));

        var member = await NotificationsAsync(org.Members[0], org);
        var changed = Assert.Single(member, row => row.Kind == "EngagementDetailsChanged");
        Assert.Contains("call time", changed.Title);

        var sent = await DispatchOutboxAsync();
        Assert.Contains(sent, mail => mail.Subject.Contains("call time"));
    }

    /// <summary>
    /// A Draft is private (D-025). Editing it tells nobody, because nobody has
    /// been told it exists.
    /// </summary>
    [Fact]
    public async Task EditingADraftTellsNobody()
    {
        var org = await NewOrganisationAsync("notif-draft", members: 1);
        var engagement = await NewDraftAsync(org, "Still private");

        await UpdateAsync(org, engagement.Id, callTime: new TimeOnly(17, 0));

        Assert.Empty(await NotificationsAsync(org.Members[0], org));
    }

    /// <summary>
    /// Changing the dress note is not on D-049's list. A performer plans their
    /// day around the venue and the times, not the shirt.
    /// </summary>
    [Fact]
    public async Task ADressNoteEditDoesNotEmail()
    {
        var org = await NewOrganisationAsync("notif-dress", members: 1);
        var engagement = await NewDraftAsync(org, "Dress only");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await DispatchOutboxAsync();
        var before = Sender().Sent.Count;

        await UpdateAsync(org, engagement.Id, dressNotes: "Black kurta.");

        await DispatchOutboxAsync();
        Assert.Equal(before, Sender().Sent.Count);
        Assert.DoesNotContain(
            await NotificationsAsync(org.Members[0], org),
            row => row.Kind == "EngagementDetailsChanged");
    }

    [Fact]
    public async Task BeingHandedAJobIsToldInApp()
    {
        var org = await NewOrganisationAsync("notif-job", members: 1);
        var engagement = await NewDraftAsync(org, "Jobs");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        var created = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/responsibilities",
            new CreateResponsibilityRequest("Bring the tabla", null, org.MemberIds[0]));
        created.EnsureSuccessStatusCode();

        var member = await NotificationsAsync(org.Members[0], org);
        var job = Assert.Single(member, row => row.Kind == "ResponsibilityAssigned");
        Assert.Contains("Bring the tabla", job.Title);
    }

    [Fact]
    public async Task AJoinTellsTheOrganisersAndNotTheJoiner()
    {
        var org = await NewOrganisationAsync("notif-join", members: 1);

        var owner = await NotificationsAsync(org.Owner, org);
        var joined = Assert.Single(owner, row => row.Kind == "MemberJoined");
        Assert.Null(joined.EngagementId);

        Assert.DoesNotContain(
            await NotificationsAsync(org.Members[0], org),
            row => row.Kind == "MemberJoined");
    }

    /// <summary>
    /// A refused change stages nothing. If the request had leaked its
    /// notifications despite failing, this member would hear about an event
    /// that never asked them.
    /// </summary>
    [Fact]
    public async Task ARefusedRequestLeavesNoNotificationBehind()
    {
        var org = await NewOrganisationAsync("notif-atomic", members: 1);
        var undated = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest("No date yet", null, null, null, null));
        undated.EnsureSuccessStatusCode();
        var engagement = await undated.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);

        var refused = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/availability/requests",
            new RequestAvailabilityRequest(org.MemberIds));
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        Assert.Empty(await NotificationsAsync(org.Members[0], org));
    }

    [Fact]
    public async Task NotificationsArePersonal()
    {
        var org = await NewOrganisationAsync("notif-personal", members: 2);
        var engagement = await NewDraftAsync(org, "Only one asked");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);

        Assert.Single(await NotificationsAsync(org.Members[0], org));
        Assert.Empty(await NotificationsAsync(org.Members[1], org));
    }

    [Fact]
    public async Task ReadingClearsTheBadge()
    {
        var org = await NewOrganisationAsync("notif-read", members: 1);
        var first = await NewDraftAsync(org, "One");
        var second = await NewDraftAsync(org, "Two");
        await RequestAvailabilityAsync(org, first.Id, org.MemberIds);
        await RequestAvailabilityAsync(org, second.Id, org.MemberIds);

        Assert.Equal(2, await UnreadAsync(org.Members[0], org));

        var rows = await NotificationsAsync(org.Members[0], org);
        var marked = await org.Members[0].PostAsync(
            $"{Notifications(org)}/{rows[0].Id}/read",
            content: null);
        Assert.Equal(HttpStatusCode.NoContent, marked.StatusCode);
        Assert.Equal(1, await UnreadAsync(org.Members[0], org));

        var all = await org.Members[0].PostAsync($"{Notifications(org)}/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, all.StatusCode);
        Assert.Equal(0, await UnreadAsync(org.Members[0], org));
    }

    [Fact]
    public async Task YouCannotMarkSomebodyElsesNotificationRead()
    {
        var org = await NewOrganisationAsync("notif-read-others", members: 2);
        var engagement = await NewDraftAsync(org, "Not yours");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);
        var theirs = Assert.Single(await NotificationsAsync(org.Members[0], org));

        var attempt = await org.Members[1].PostAsync(
            $"{Notifications(org)}/{theirs.Id}/read",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
        Assert.Equal(1, await UnreadAsync(org.Members[0], org));
    }

    /// <summary>
    /// The provider being down delays the email; it does not lose it. A failed
    /// attempt is recorded and the row is tried again once its backoff passes.
    /// </summary>
    [Fact]
    public async Task AFailedSendIsRetriedNotLost()
    {
        var org = await NewOrganisationAsync("notif-retry", members: 1);
        var engagement = await NewDraftAsync(org, "Retry me");
        await DispatchOutboxAsync();
        var sender = Sender();
        var before = sender.Sent.Count;

        sender.FailWith = "Resend is down";
        try
        {
            await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
            await DispatchOutboxAsync();
            Assert.Equal(before, sender.Sent.Count);
        }
        finally
        {
            sender.FailWith = null;
        }

        // Backoff after the first failure is a minute, so this pass finds
        // nothing due — which is the point. Then it is forced due and sent.
        Assert.Empty(await DispatchOutboxAsync());

        await ForceDueAsync();
        var sent = await DispatchOutboxAsync();
        Assert.Contains(sent, mail => mail.Subject.Contains("Retry me"));
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds,
        IReadOnlyList<string> MemberEmails);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static string Notifications(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/notifications";

    private RecordingEmailSender Sender() =>
        factory.Services.GetRequiredService<RecordingEmailSender>();

    /// <summary>
    /// Runs the outbox once and returns what it sent. The worker is off in
    /// tests, so this is the only way email moves.
    /// </summary>
    private async Task<IReadOnlyList<(string To, string Subject, string Body)>> DispatchOutboxAsync()
    {
        var sender = Sender();
        var before = sender.Sent.Count;

        using var scope = factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchDueAsync(CancellationToken.None);

        return sender.Sent.Skip(before).ToList();
    }

    /// <summary>Winds every unsent row's backoff back so it is due now.</summary>
    private async Task ForceDueAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Sahno.Infrastructure.Persistence.SahnoDbContext>();
        var anHourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        await dbContext.OutboxMessages
            .Where(message => message.SentAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                message => message.LastAttemptAtUtc,
                anHourAgo));
    }

    private static async Task<List<NotificationResponse>> NotificationsAsync(
        HttpClient client,
        TestOrganisation org)
    {
        var rows = await client.GetFromJsonAsync<List<NotificationResponse>>(
            Notifications(org));
        Assert.NotNull(rows);
        return rows;
    }

    private static async Task<int> UnreadAsync(HttpClient client, TestOrganisation org)
    {
        var response = await client.GetFromJsonAsync<UnreadCountResponse>(
            $"{Notifications(org)}/unread-count");
        Assert.NotNull(response);
        return response.Unread;
    }

    private static async Task RequestAvailabilityAsync(
        TestOrganisation org,
        Guid engagementId,
        IReadOnlyList<Guid> userIds)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/availability/requests",
            new RequestAvailabilityRequest(userIds));
        response.EnsureSuccessStatusCode();
    }

    private static async Task RespondAsync(
        HttpClient member,
        TestOrganisation org,
        Guid engagementId,
        string response)
    {
        var result = await member.PutAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/availability/me",
            new RespondAvailabilityRequest(response));
        result.EnsureSuccessStatusCode();
    }

    private static async Task TransitionAsync(
        TestOrganisation org,
        Guid engagementId,
        string target,
        string? reason)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/transition",
            new TransitionEngagementRequest(target, reason, true));
        response.EnsureSuccessStatusCode();
    }

    private static async Task UpdateAsync(
        TestOrganisation org,
        Guid engagementId,
        TimeOnly? callTime = null,
        string? dressNotes = null)
    {
        var response = await org.Owner.PatchAsJsonAsync(
            $"{Engagements(org)}/{engagementId}",
            new UpdateEngagementRequest(null, null, callTime, dressNotes, null));
        response.EnsureSuccessStatusCode();
    }

    private async Task<TestOrganisation> NewOrganisationAsync(
        string prefix,
        int members = 0)
    {
        var owner = CreateClient($"auth0|{prefix}-owner", $"{prefix}-owner@example.test");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, null));
        created.EnsureSuccessStatusCode();
        var organisation = await created.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);

        var clients = new List<HttpClient>();
        var emails = new List<string>();
        if (members > 0)
        {
            var inviteResponse = await owner.PostAsJsonAsync(
                $"/api/organisations/{organisation.Id}/invitations",
                new CreateInvitationRequest(null));
            inviteResponse.EnsureSuccessStatusCode();
            var invitation =
                await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
            Assert.NotNull(invitation);

            for (var index = 0; index < members; index++)
            {
                var email = $"{prefix}-member-{index}@example.test";
                var member = CreateClient($"auth0|{prefix}-member-{index}", email);
                var accept = await member.PostAsync(
                    $"/api/invitations/{invitation.Token}/accept",
                    content: null);
                accept.EnsureSuccessStatusCode();
                clients.Add(member);
                emails.Add(email);
            }
        }

        var directory = await owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{organisation.Id}/members");
        Assert.NotNull(directory);

        return new TestOrganisation(
            organisation.Id,
            owner,
            clients,
            directory.Skip(1).Select(row => row.UserId).ToList(),
            emails);
    }

    private async Task<EngagementResponse> NewDraftAsync(
        TestOrganisation org,
        string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest(
                title,
                new DateOnly(2027, 8, 14),
                null,
                null,
                null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    private HttpClient CreateClient(string subject, string email)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(TestAuthDefaults.EmailHeader, email);
        return client;
    }
}
