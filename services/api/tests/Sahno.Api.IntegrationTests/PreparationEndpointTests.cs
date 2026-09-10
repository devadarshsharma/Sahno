using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Rehearsals and resources (Slice 8, D-047 §4 and §5, D-023).
///
/// The audience rule carries most of the weight here. An Admins-only note that
/// merely renders differently on a client is not protected at all, so these
/// tests check what the API hands over rather than what a screen shows.
/// </summary>
public sealed class PreparationEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AnOrganiserSchedulesARehearsalAndTheLineupSeesIt()
    {
        var org = await NewOrganisationAsync("prep-rehearsal", members: 1);
        var engagement = await NewDraftAsync(org, "Wedding");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        var created = await org.Owner.PostAsJsonAsync(
            Rehearsals(org, engagement.Id),
            new SaveRehearsalRequest(
                "Full run",
                new DateOnly(2027, 8, 7),
                new TimeOnly(18, 0),
                new TimeOnly(20, 0),
                "Dural hall",
                "Bring your own harmonium."));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var asMember = await org.Members[0]
            .GetFromJsonAsync<List<RehearsalResponse>>(Rehearsals(org, engagement.Id));

        Assert.NotNull(asMember);
        var rehearsal = Assert.Single(asMember);
        Assert.Equal("Full run", rehearsal.Title);
        Assert.Equal(new TimeOnly(18, 0), rehearsal.StartTime);
        Assert.Equal("Dural hall", rehearsal.Venue);
    }

    [Fact]
    public async Task AMemberCannotScheduleOrDeleteARehearsal()
    {
        var org = await NewOrganisationAsync("prep-rehearsal-write", members: 1);
        var engagement = await NewDraftAsync(org, "Not yours to book");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var rehearsal = await ScheduleAsync(org, engagement.Id);

        var create = await org.Members[0].PostAsJsonAsync(
            Rehearsals(org, engagement.Id),
            new SaveRehearsalRequest(null, new DateOnly(2027, 8, 8), null, null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var delete = await org.Members[0].DeleteAsync(
            $"{Rehearsals(org, engagement.Id)}/{rehearsal.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    /// <summary>
    /// An end before its start is a typo. Keeping the start is more useful
    /// than refusing the whole rehearsal over it.
    /// </summary>
    [Fact]
    public async Task AnEndBeforeItsStartIsDropped()
    {
        var org = await NewOrganisationAsync("prep-backwards");
        var engagement = await NewDraftAsync(org, "Backwards");

        await org.Owner.PostAsJsonAsync(
            Rehearsals(org, engagement.Id),
            new SaveRehearsalRequest(
                null,
                new DateOnly(2027, 8, 7),
                new TimeOnly(20, 0),
                new TimeOnly(18, 0),
                null,
                null));

        var rows = await org.Owner
            .GetFromJsonAsync<List<RehearsalResponse>>(Rehearsals(org, engagement.Id));
        Assert.NotNull(rows);
        var rehearsal = Assert.Single(rows);
        Assert.Equal(new TimeOnly(20, 0), rehearsal.StartTime);
        Assert.Null(rehearsal.EndTime);
    }

    [Fact]
    public async Task AResourceIsForParticipantsUnlessSaidOtherwise()
    {
        var org = await NewOrganisationAsync("prep-default");
        var engagement = await NewDraftAsync(org, "Default audience");

        await org.Owner.PostAsJsonAsync(
            Resources(org, engagement.Id),
            new CreateResourceRequest("Note", "Running order", "Qaul first.", null, null));

        var rows = await ResourcesAsync(org.Owner, org, engagement.Id);
        Assert.Equal("Participants", Assert.Single(rows).Audience);
    }

    /// <summary>
    /// The protected one never leaves the service. Filtering on the client
    /// would mean the private note had already been sent.
    /// </summary>
    [Fact]
    public async Task AdminsOnlyResourcesNeverReachAMember()
    {
        var org = await NewOrganisationAsync("prep-audience", members: 1);
        var engagement = await NewDraftAsync(org, "Two audiences");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        await AddNoteAsync(org, engagement.Id, "Running order", "Qaul first.", "Participants");
        await AddNoteAsync(org, engagement.Id, "Fee talk", "Ask for more.", "AdminsOnly");

        var asOrganiser = await ResourcesAsync(org.Owner, org, engagement.Id);
        Assert.Equal(2, asOrganiser.Count);

        var asMember = await ResourcesAsync(org.Members[0], org, engagement.Id);
        var visible = Assert.Single(asMember);
        Assert.Equal("Running order", visible.Title);
        Assert.DoesNotContain(asMember, row => row.Title == "Fee talk");
    }

    [Fact]
    public async Task ChangingTheAudienceTakesAResourceBackOffAMemberScreen()
    {
        var org = await NewOrganisationAsync("prep-reaudience", members: 1);
        var engagement = await NewDraftAsync(org, "Second thoughts");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var note = await AddNoteAsync(
            org,
            engagement.Id,
            "Draft plan",
            "Not settled yet.",
            "Participants");

        Assert.Single(await ResourcesAsync(org.Members[0], org, engagement.Id));

        var updated = await org.Owner.PutAsJsonAsync(
            $"{Resources(org, engagement.Id)}/{note.Id}",
            new UpdateResourceRequest("Draft plan", "Not settled yet.", null, "AdminsOnly"));
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        Assert.Empty(await ResourcesAsync(org.Members[0], org, engagement.Id));
    }

    [Fact]
    public async Task AMemberCannotAttachOrChangeResources()
    {
        var org = await NewOrganisationAsync("prep-member-write", members: 1);
        var engagement = await NewDraftAsync(org, "Read only");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var note = await AddNoteAsync(org, engagement.Id, "Order", "Qaul first.", null);

        var create = await org.Members[0].PostAsJsonAsync(
            Resources(org, engagement.Id),
            new CreateResourceRequest("Note", "Mine", "Hello.", null, null));
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var update = await org.Members[0].PutAsJsonAsync(
            $"{Resources(org, engagement.Id)}/{note.Id}",
            new UpdateResourceRequest("Order", "Changed.", null, null));
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        var delete = await org.Members[0].DeleteAsync(
            $"{Resources(org, engagement.Id)}/{note.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    /// <summary>
    /// A link is opened by tapping it, so anything that is not http(s) either
    /// cannot work for the people receiving it or should not.
    /// </summary>
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/secret.txt")]
    [InlineData("not a link at all")]
    public async Task ALinkMustBeAWebAddress(string url)
    {
        var org = await NewOrganisationAsync($"prep-url-{url.GetHashCode():x}");
        var engagement = await NewDraftAsync(org, "Bad link");

        var refused = await org.Owner.PostAsJsonAsync(
            Resources(org, engagement.Id),
            new CreateResourceRequest("Link", "Recording", null, url, null));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    [Fact]
    public async Task ANoteNeedsSomethingInIt()
    {
        var org = await NewOrganisationAsync("prep-empty-note");
        var engagement = await NewDraftAsync(org, "Empty");

        var refused = await org.Owner.PostAsJsonAsync(
            Resources(org, engagement.Id),
            new CreateResourceRequest("Note", "Nothing", "   ", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    /// <summary>
    /// Guessing at what somebody meant to protect is how private material ends
    /// up shared, so an unrecognised audience is refused rather than defaulted.
    /// </summary>
    [Fact]
    public async Task AnUnknownAudienceIsRefusedRatherThanAssumed()
    {
        var org = await NewOrganisationAsync("prep-bad-audience");
        var engagement = await NewDraftAsync(org, "Unknown audience");

        var refused = await org.Owner.PostAsJsonAsync(
            Resources(org, engagement.Id),
            new CreateResourceRequest("Note", "Order", "Qaul first.", null, "Everyone"));

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
    }

    [Fact]
    public async Task SomebodyNotOnTheEventSeesNeitherRehearsalsNorResources()
    {
        var org = await NewOrganisationAsync("prep-outsider", members: 2);
        var engagement = await NewDraftAsync(org, "Not yours");
        await RequestAvailabilityAsync(org, engagement.Id, [org.MemberIds[0]]);

        var rehearsals = await org.Members[1].GetAsync(Rehearsals(org, engagement.Id));
        Assert.Equal(HttpStatusCode.NotFound, rehearsals.StatusCode);

        var resources = await org.Members[1].GetAsync(Resources(org, engagement.Id));
        Assert.Equal(HttpStatusCode.NotFound, resources.StatusCode);
    }

    /// <summary>
    /// Both items are derived, so scheduling and attaching are what tick them.
    /// An Admins-only note still counts: the organiser has done the work, and
    /// the checklist is theirs.
    /// </summary>
    [Fact]
    public async Task SchedulingAndAttachingTickTheirChecklistItems()
    {
        var org = await NewOrganisationAsync("prep-readiness");
        var engagement = await NewDraftAsync(org, "Checklist");

        Assert.Equal("Outstanding", await StateAsync(org, engagement.Id, "Rehearsal"));
        Assert.Equal("Outstanding", await StateAsync(org, engagement.Id, "Resources"));

        await ScheduleAsync(org, engagement.Id);
        await AddNoteAsync(org, engagement.Id, "Fee talk", "Internal.", "AdminsOnly");

        Assert.Equal("Done", await StateAsync(org, engagement.Id, "Rehearsal"));
        Assert.Equal("Done", await StateAsync(org, engagement.Id, "Resources"));
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static string Rehearsals(TestOrganisation org, Guid engagementId) =>
        $"{Engagements(org)}/{engagementId}/rehearsals";

    private static string Resources(TestOrganisation org, Guid engagementId) =>
        $"{Engagements(org)}/{engagementId}/resources";

    private static async Task<RehearsalResponse> ScheduleAsync(
        TestOrganisation org,
        Guid engagementId)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Rehearsals(org, engagementId),
            new SaveRehearsalRequest(
                null,
                new DateOnly(2027, 8, 7),
                new TimeOnly(18, 0),
                null,
                null,
                null));
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<RehearsalResponse>();
        Assert.NotNull(created);
        return created;
    }

    private static async Task<EngagementResourceResponse> AddNoteAsync(
        TestOrganisation org,
        Guid engagementId,
        string title,
        string body,
        string? audience)
    {
        var response = await org.Owner.PostAsJsonAsync(
            Resources(org, engagementId),
            new CreateResourceRequest("Note", title, body, null, audience));
        response.EnsureSuccessStatusCode();
        var created =
            await response.Content.ReadFromJsonAsync<EngagementResourceResponse>();
        Assert.NotNull(created);
        return created;
    }

    private static async Task<List<EngagementResourceResponse>> ResourcesAsync(
        HttpClient client,
        TestOrganisation org,
        Guid engagementId)
    {
        var rows = await client.GetFromJsonAsync<List<EngagementResourceResponse>>(
            Resources(org, engagementId));
        Assert.NotNull(rows);
        return rows;
    }

    private static async Task<string> StateAsync(
        TestOrganisation org,
        Guid engagementId,
        string item)
    {
        var readiness = await org.Owner
            .GetFromJsonAsync<List<ReadinessEntryResponse>>(
                $"{Engagements(org)}/{engagementId}/readiness");
        Assert.NotNull(readiness);
        return readiness.Single(entry => entry.Item == item).State;
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
