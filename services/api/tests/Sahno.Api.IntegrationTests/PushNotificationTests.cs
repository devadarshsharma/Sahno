using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Application.Notifications;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Notifications;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Push (D-080). A registered phone is pushed whatever the in-app bell is
/// told, through the same outbox as email; a phone that is gone is disabled
/// and not retried; nobody is pushed about their own action; and the payload
/// says where to go.
/// </summary>
public sealed class PushNotificationTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task APersonRegistersSeveralPhonesAndRefreshesOne()
    {
        var org = await NewOrganisationAsync("push-register");

        Assert.Equal(HttpStatusCode.NoContent, (await RegisterAsync(org.Owner, Token("a"), "android", "S23")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await RegisterAsync(org.Owner, Token("b"), "ios", "iPhone")).StatusCode);
        // Registering the same token again is a refresh, not a second phone.
        Assert.Equal(HttpStatusCode.NoContent, (await RegisterAsync(org.Owner, Token("a"), "android", "S23 Ultra")).StatusCode);

        var devices = await org.Owner.GetFromJsonAsync<List<PushDeviceResponse>>("/api/me/push-devices");
        Assert.NotNull(devices);
        Assert.Equal(2, devices.Count);
        Assert.Equal("S23 Ultra", devices.Single(device => device.Token == Token("a")).DeviceName);

        var bad = await RegisterAsync(org.Owner, "not-a-token", "android", null);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var removed = await org.Owner.DeleteAsync($"/api/me/push-devices/{Token("b")}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        var remaining = await org.Owner.GetFromJsonAsync<List<PushDeviceResponse>>("/api/me/push-devices");
        Assert.Single(remaining!);
    }

    [Fact]
    public async Task AJobAssignmentPushesTheAssigneeOnEveryPhoneWithARoute()
    {
        var org = await NewOrganisationAsync("push-job", members: 1);
        var engagement = await NewDraftAsync(org, "Saturday wedding");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RegisterAsync(org.Members[0], Token("job-1"), "android", null);
        await RegisterAsync(org.Members[0], Token("job-2"), "ios", null);
        // The organiser's own phone is registered too, and must stay quiet.
        await RegisterAsync(org.Owner, Token("job-owner"), "android", null);

        var created = await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/responsibilities",
            new CreateResponsibilityRequest("Bring the tabla", null, org.MemberIds[0]));
        created.EnsureSuccessStatusCode();

        var sent = await DispatchPushAsync();
        Assert.Equal(2, sent.Count);
        Assert.Equal(
            [Token("job-1"), Token("job-2")],
            sent.Select(push => push.Token).OrderBy(token => token).ToArray());
        Assert.DoesNotContain(sent, push => push.Token == Token("job-owner"));
        Assert.Contains("Bring the tabla", sent[0].Title);

        using var data = JsonDocument.Parse(sent[0].Data!);
        var root = data.RootElement;
        Assert.Equal("ResponsibilityAssigned", root.GetProperty("notificationType").GetString());
        Assert.Equal(engagement.Id, root.GetProperty("engagementId").GetGuid());
        Assert.Equal(org.Id, root.GetProperty("organisationId").GetGuid());
        Assert.Equal($"/engagement/{engagement.Id}/jobs", root.GetProperty("route").GetString());

        // The bell carries the same route, so tapping either lands in one place.
        var bell = await org.Members[0].GetFromJsonAsync<List<NotificationResponse>>(Notifications(org));
        var job = Assert.Single(bell!, row => row.Kind == "ResponsibilityAssigned");
        Assert.Equal($"/engagement/{engagement.Id}/jobs", job.Route);
        Assert.Equal(job.Id, root.GetProperty("notificationId").GetGuid());
    }

    /// <summary>
    /// A chat is one notification on Android (D-088): every message in it
    /// shares the chat's tag, so each push replaces the last, and carries
    /// everything still unread. iOS stacks them by thread instead, one message
    /// each. Opening the chat starts the count again.
    /// </summary>
    [Fact]
    public async Task ChatPushesBundleByChat()
    {
        var org = await NewOrganisationAsync("push-chat", members: 1);
        var engagement = await NewDraftAsync(org, "Saturday gig");
        var other = await NewDraftAsync(org, "Sunday gig");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await RequestAvailabilityAsync(org, other.Id, org.MemberIds);
        await RegisterAsync(org.Members[0], Token("chat-android"), "android", null);
        await RegisterAsync(org.Members[0], Token("chat-ios"), "ios", null);
        await DispatchPushAsync();

        var discussion = $"{Engagements(org)}/{engagement.Id}/discussion";
        foreach (var body in new[] { "Soundcheck at five.", "Bring the stands.", "Parking is out back." })
        {
            (await org.Owner.PostAsJsonAsync(discussion, new PostDiscussionMessageRequest(body))).EnsureSuccessStatusCode();
        }
        (await org.Owner.PostAsJsonAsync(
            $"{Engagements(org)}/{other.Id}/discussion",
            new PostDiscussionMessageRequest("Different chat."))).EnsureSuccessStatusCode();

        var sent = await DispatchPushAsync();
        var android = sent.Where(push => push.Token == Token("chat-android")).ToList();
        var ios = sent.Where(push => push.Token == Token("chat-ios")).ToList();
        Assert.Equal(4, android.Count);
        Assert.Equal(4, ios.Count);

        var latest = android.Last(push => push.Body.Contains("Parking"));
        Assert.Equal("Saturday gig · 3 new messages", latest.Title);
        Assert.Equal(3, latest.Body.Split('\n').Length);
        Assert.EndsWith(": Soundcheck at five.", latest.Body.Split('\n')[0]);
        Assert.EndsWith("Parking is out back.", latest.Body);

        Assert.Contains(ios, push => push.Body == "Parking is out back." && push.Title.EndsWith("in Saturday gig"));

        var saturday = PresentationOf(latest);
        var sunday = PresentationOf(android.Single(push => push.Body.Contains("Different chat.")));
        Assert.Equal($"chat-{engagement.Id}", saturday.Tag);
        Assert.Equal(saturday.Tag, saturday.ThreadId);
        Assert.Equal(PushPresentation.ChatMessageCategory, saturday.CategoryId);
        Assert.NotEqual(saturday.Tag, sunday.Tag);

        // Read, then one more: the bundle starts again from that one.
        (await org.Members[0].PostAsync($"{discussion}/read", content: null)).EnsureSuccessStatusCode();
        (await org.Owner.PostAsJsonAsync(discussion, new PostDiscussionMessageRequest("See you there."))).EnsureSuccessStatusCode();
        var fresh = Assert.Single(await DispatchPushAsync(), push => push.Token == Token("chat-android"));
        Assert.EndsWith("in Saturday gig", fresh.Title);
        Assert.Equal("See you there.", fresh.Body);
    }

    private static PushPresentation PresentationOf((string Token, string Title, string Body, string? Data) push)
    {
        using var data = JsonDocument.Parse(push.Data!);
        return PushPresentation.For(
            data.RootElement.GetProperty("notificationType").GetString(),
            data.RootElement.GetProperty("engagementId").GetGuid());
    }

    [Fact]
    public async Task ADeadPhoneIsDisabledAndNotRetried()
    {
        var org = await NewOrganisationAsync("push-dead", members: 1);
        var engagement = await NewDraftAsync(org, "Dead phone");
        await RegisterAsync(org.Members[0], Token("dead"), "android", "Old phone");
        Sender().MarkDead(Token("dead"));

        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        // First pass: Expo says the device is gone.
        var sentFirst = await DispatchPushAsync();
        Assert.Empty(sentFirst);

        var devices = await org.Members[0].GetFromJsonAsync<List<PushDeviceResponse>>("/api/me/push-devices");
        Assert.Empty(devices!);

        // Second pass: nothing is retried, and a new notification stages no push.
        var reminder = await org.Owner.PostAsync(
            $"{Engagements(org)}/{engagement.Id}/availability/{org.MemberIds[0]}/reminders",
            content: null);
        reminder.EnsureSuccessStatusCode();
        var sentSecond = await DispatchPushAsync();
        Assert.Empty(sentSecond);

        // Registering again from that phone brings it back.
        await RegisterAsync(org.Members[0], Token("dead"), "android", "Old phone");
        var again = await org.Members[0].GetFromJsonAsync<List<PushDeviceResponse>>("/api/me/push-devices");
        Assert.Single(again!);
    }

    [Fact]
    public async Task AnAnnouncementReachesEveryoneButTheAuthor()
    {
        var org = await NewOrganisationAsync("push-announce", members: 2);
        await RegisterAsync(org.Owner, Token("ann-owner"), "android", null);
        await RegisterAsync(org.Members[0], Token("ann-0"), "android", null);
        await RegisterAsync(org.Members[1], Token("ann-1"), "ios", null);

        var memberTries = await org.Members[0].PostAsJsonAsync(
            $"{Notifications(org)}/announcements",
            new AnnouncementRequest("Nope", null));
        Assert.Equal(HttpStatusCode.Forbidden, memberTries.StatusCode);

        var blank = await org.Owner.PostAsJsonAsync(
            $"{Notifications(org)}/announcements",
            new AnnouncementRequest("  ", null));
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

        var sent = await org.Owner.PostAsJsonAsync(
            $"{Notifications(org)}/announcements",
            new AnnouncementRequest("Rehearsal moved to Thursday", "Same place, 7pm."));
        Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);

        var pushed = await DispatchPushAsync();
        Assert.Equal(2, pushed.Count);
        Assert.DoesNotContain(pushed, push => push.Token == Token("ann-owner"));
        Assert.All(pushed, push => Assert.Equal("Rehearsal moved to Thursday", push.Title));

        var bell = await org.Members[1].GetFromJsonAsync<List<NotificationResponse>>(Notifications(org));
        var row = Assert.Single(bell!, item => item.Kind == "OrganiserAnnouncement");
        Assert.Equal("/notifications", row.Route);
        Assert.False((await org.Owner.GetFromJsonAsync<List<NotificationResponse>>(Notifications(org)))!
            .Any(item => item.Kind == "OrganiserAnnouncement"));
    }

    [Fact]
    public async Task AnAnswerReachesTheOrganisersPhone()
    {
        var org = await NewOrganisationAsync("push-answer", members: 1);
        var engagement = await NewDraftAsync(org, "Answered");
        await RegisterAsync(org.Owner, Token("answer-owner"), "android", null);
        await RegisterAsync(org.Members[0], Token("answer-member"), "android", null);
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await DispatchPushAsync();

        // The member answers: it is what the organiser is waiting on, so it
        // buzzes the organiser's phone — and only theirs, not the answerer's.
        var answered = await org.Members[0].PutAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/availability/me",
            new RespondAvailabilityRequest("Available"));
        answered.EnsureSuccessStatusCode();

        var pushed = await DispatchPushAsync();
        var push = Assert.Single(pushed);
        Assert.Equal(Token("answer-owner"), push.Token);
        Assert.EndsWith("answered for Answered", push.Title);

        var bell = await org.Owner.GetFromJsonAsync<List<NotificationResponse>>(Notifications(org));
        var row = Assert.Single(bell!, item => item.Kind == "AvailabilityAnswered");
        Assert.Equal($"/engagement/{engagement.Id}/people", row.Route);
    }

    private static string Token(string suffix) => $"ExponentPushToken[{suffix}]";

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static string Notifications(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/notifications";

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string token,
        string platform,
        string? deviceName)
    {
        return client.PutAsJsonAsync(
            "/api/me/push-devices",
            new RegisterPushDeviceRequest(token, platform, deviceName));
    }

    private RecordingPushSender Sender() =>
        factory.Services.GetRequiredService<RecordingPushSender>();

    /// <summary>Runs the outbox once and returns the pushes it sent this pass.</summary>
    private async Task<IReadOnlyList<(string Token, string Title, string Body, string? Data)>> DispatchPushAsync()
    {
        var sender = Sender();
        var before = sender.Sent.Count;

        using var scope = factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        await dispatcher.DispatchDueAsync(CancellationToken.None);

        return sender.Sent.Skip(before).ToList();
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

    private async Task<TestOrganisation> NewOrganisationAsync(string prefix, int members = 0)
    {
        var owner = CreateClient($"auth0|{prefix}-owner", $"{prefix}-owner@example.test");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, null));
        created.EnsureSuccessStatusCode();
        var organisation = await created.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);

        var clients = new List<HttpClient>();
        if (members > 0)
        {
            var inviteResponse = await owner.PostAsJsonAsync(
                $"/api/organisations/{organisation.Id}/invitations",
                new CreateInvitationRequest(null));
            inviteResponse.EnsureSuccessStatusCode();
            var invitation = await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
            Assert.NotNull(invitation);

            for (var index = 0; index < members; index++)
            {
                var member = CreateClient(
                    $"auth0|{prefix}-member-{index}",
                    $"{prefix}-member-{index}@example.test");
                var accept = await member.PostAsync(
                    $"/api/invitations/{invitation.Token}/accept",
                    content: null);
                accept.EnsureSuccessStatusCode();
                clients.Add(member);
            }
        }

        var directory = await owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{organisation.Id}/members");
        Assert.NotNull(directory);

        return new TestOrganisation(
            organisation.Id,
            owner,
            clients,
            directory.Skip(1).Select(row => row.UserId).ToList());
    }

    private async Task<EngagementResponse> NewDraftAsync(TestOrganisation org, string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Engagements(org),
            new CreateEngagementRequest(title, new DateOnly(2027, 8, 14), null, null, null));
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
