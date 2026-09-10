using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Discussion inside an engagement (Slice 9, D-024).
///
/// Three rules carry it, and each has a test that would fail loudly if it were
/// dropped: access follows the engagement, a message belongs to whoever wrote
/// it, and an organiser can take any message down but never change one.
/// </summary>
public sealed class DiscussionEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task ParticipantsAndOrganisersTalkInTheSameThread()
    {
        var org = await NewOrganisationAsync("chat-thread", members: 1);
        var engagement = await NewDraftAsync(org, "Coordination");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        await PostAsync(org.Owner, org, engagement.Id, "Call time moved to six.");
        await PostAsync(org.Members[0], org, engagement.Id, "Noted, I will be there.");

        var thread = await ThreadAsync(org.Members[0], org, engagement.Id);

        Assert.Equal(2, thread.Count);
        Assert.Equal("Call time moved to six.", thread[0].Body);
        Assert.False(thread[0].IsYours);
        Assert.True(thread[1].IsYours);
    }

    /// <summary>
    /// There is no separate permission for a thread. Somebody who cannot open
    /// the event gets the same nothing here that they get there.
    /// </summary>
    [Fact]
    public async Task SomebodyNotOnTheEventCannotReadOrPost()
    {
        var org = await NewOrganisationAsync("chat-outsider", members: 2);
        var engagement = await NewDraftAsync(org, "Not yours");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);
        await PostAsync(org.Members[0], org, engagement.Id, "Only for us.");

        var read = await org.Members[1].GetAsync(Discussion(org, engagement.Id));
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);

        var write = await org.Members[1].PostAsJsonAsync(
            Discussion(org, engagement.Id),
            new PostDiscussionMessageRequest("Let me in."));
        Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
    }

    /// <summary>
    /// Taking somebody off the lineup takes the conversation with it. Their
    /// own past messages stay in the thread for everyone else — they were said.
    /// </summary>
    [Fact]
    public async Task RemovalFromTheLineupEndsAccessToTheThread()
    {
        var org = await NewOrganisationAsync("chat-removed", members: 1);
        var engagement = await NewDraftAsync(org, "Lineup change");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await PostAsync(org.Members[0], org, engagement.Id, "I can do it.");

        var removed = await org.Owner.DeleteAsync(
            $"{Engagements(org)}/{engagement.Id}/availability/{org.MemberIds[0]}");
        removed.EnsureSuccessStatusCode();

        var read = await org.Members[0].GetAsync(Discussion(org, engagement.Id));
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);

        var stillThere = await ThreadAsync(org.Owner, org, engagement.Id);
        Assert.Equal("I can do it.", Assert.Single(stillThere).Body);
    }

    [Fact]
    public async Task EditingYourOwnMessageMarksItEdited()
    {
        var org = await NewOrganisationAsync("chat-edit", members: 1);
        var engagement = await NewDraftAsync(org, "Second thoughts");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(
            org.Members[0],
            org,
            engagement.Id,
            "Six o'clock.");

        Assert.False(message.IsEdited);

        var edited = await org.Members[0].PutAsJsonAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}",
            new EditDiscussionMessageRequest("Half six, sorry."));
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);

        var updated = Assert.Single(await ThreadAsync(org.Owner, org, engagement.Id));
        Assert.Equal("Half six, sorry.", updated.Body);
        Assert.True(updated.IsEdited);
        Assert.NotNull(updated.EditedAtUtc);
    }

    [Fact]
    public async Task AMemberCannotEditSomebodyElsesMessage()
    {
        var org = await NewOrganisationAsync("chat-edit-others", members: 2);
        var engagement = await NewDraftAsync(org, "Not your words");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(org.Members[0], org, engagement.Id, "Mine.");

        var attempt = await org.Members[1].PutAsJsonAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}",
            new EditDiscussionMessageRequest("Not any more."));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    /// <summary>
    /// Moderation is removal, not authorship. An organiser holds the strongest
    /// role in the organisation and still cannot put words in somebody's mouth.
    /// </summary>
    [Fact]
    public async Task AnOrganiserCannotRewriteSomebodyElsesMessageEither()
    {
        var org = await NewOrganisationAsync("chat-owner-edit", members: 1);
        var engagement = await NewDraftAsync(org, "Moderation limits");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(
            org.Members[0],
            org,
            engagement.Id,
            "I said this.");

        var attempt = await org.Owner.PutAsJsonAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}",
            new EditDiscussionMessageRequest("I said something else."));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    /// <summary>
    /// Taking your own message back leaves a tombstone, not a hole: the words
    /// go, the fact that something was here stays, and it is not marked as
    /// moderated because nobody moderated it.
    /// </summary>
    [Fact]
    public async Task TakingYourOwnMessageBackLeavesATombstone()
    {
        var org = await NewOrganisationAsync("chat-own-delete", members: 1);
        var engagement = await NewDraftAsync(org, "Withdrawn");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(
            org.Members[0],
            org,
            engagement.Id,
            "Ignore that.");

        var removed = await org.Members[0].DeleteAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var tombstone = Assert.Single(await ThreadAsync(org.Owner, org, engagement.Id));
        Assert.True(tombstone.IsDeleted);
        Assert.False(tombstone.WasModerated);
        Assert.Null(tombstone.Body);
    }

    [Fact]
    public async Task AnOrganiserCanRemoveAnybodysMessageAndItSaysSo()
    {
        var org = await NewOrganisationAsync("chat-moderate", members: 1);
        var engagement = await NewDraftAsync(org, "Moderated");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(
            org.Members[0],
            org,
            engagement.Id,
            "Something out of order.");

        var removed = await org.Owner.DeleteAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var tombstone = Assert.Single(
            await ThreadAsync(org.Members[0], org, engagement.Id));
        Assert.True(tombstone.IsDeleted);
        Assert.True(tombstone.WasModerated);
        Assert.Null(tombstone.Body);
    }

    [Fact]
    public async Task AMemberCannotRemoveSomebodyElsesMessage()
    {
        var org = await NewOrganisationAsync("chat-delete-others", members: 2);
        var engagement = await NewDraftAsync(org, "Not yours to remove");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(org.Members[0], org, engagement.Id, "Mine.");

        var attempt = await org.Members[1].DeleteAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task ARemovedMessageCannotBeEditedBackIntoExistence()
    {
        var org = await NewOrganisationAsync("chat-edit-removed", members: 1);
        var engagement = await NewDraftAsync(org, "Gone");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var message = await PostAsync(org.Members[0], org, engagement.Id, "Oops.");
        await org.Members[0].DeleteAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}");

        var attempt = await org.Members[0].PutAsJsonAsync(
            $"{Discussion(org, engagement.Id)}/{message.Id}",
            new EditDiscussionMessageRequest("Back again."));

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    [Fact]
    public async Task AnEmptyMessageIsRefused()
    {
        var org = await NewOrganisationAsync("chat-empty");
        var engagement = await NewDraftAsync(org, "Nothing to say");

        var refused = await org.Owner.PostAsJsonAsync(
            Discussion(org, engagement.Id),
            new PostDiscussionMessageRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static string Discussion(TestOrganisation org, Guid engagementId) =>
        $"{Engagements(org)}/{engagementId}/discussion";

    private static async Task<DiscussionMessageResponse> PostAsync(
        HttpClient client,
        TestOrganisation org,
        Guid engagementId,
        string body)
    {
        var response = await client.PostAsJsonAsync(
            Discussion(org, engagementId),
            new PostDiscussionMessageRequest(body));
        response.EnsureSuccessStatusCode();
        var posted =
            await response.Content.ReadFromJsonAsync<DiscussionMessageResponse>();
        Assert.NotNull(posted);
        return posted;
    }

    private static async Task<List<DiscussionMessageResponse>> ThreadAsync(
        HttpClient client,
        TestOrganisation org,
        Guid engagementId)
    {
        var thread = await client.GetFromJsonAsync<List<DiscussionMessageResponse>>(
            Discussion(org, engagementId));
        Assert.NotNull(thread);
        return thread;
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

    private async Task<TestOrganisation> NewOrganisationAsync(
        string prefix,
        int members = 0)
    {
        var owner = CreateClient($"auth0|{prefix}-owner");
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
            var invitation =
                await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
            Assert.NotNull(invitation);

            for (var index = 0; index < members; index++)
            {
                var member = CreateClient($"auth0|{prefix}-member-{index}");
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

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
