using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Availability collection (Slice 5, D-021, D-027 to D-029). Two things carry
/// the weight: an answer belongs to the person who gave it, and changing a
/// lineup must never disturb answers already given.
/// </summary>
public sealed class AvailabilityEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task RequestingAvailability_MovesADraftIntoCheckingAvailability()
    {
        var org = await NewOrganisationAsync("request-moves", members: 2);
        var engagement = await NewDatedDraftAsync(org, "Needs a lineup");

        var sent = await RequestAsync(org, engagement.Id, org.MemberIds);

        Assert.Equal(HttpStatusCode.NoContent, sent.StatusCode);
        Assert.Equal(
            "CheckingAvailability",
            (await GetEngagementAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task RequestingWithoutADate_IsRefused()
    {
        var org = await NewOrganisationAsync("request-no-date", members: 1);
        var engagement = await NewDraftAsync(org, "Undated");

        var sent = await RequestAsync(org, engagement.Id, org.MemberIds);

        Assert.Equal(HttpStatusCode.BadRequest, sent.StatusCode);
        Assert.Equal("Draft", (await GetEngagementAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task MembersAnswerAvailableMaybeOrUnavailable()
    {
        var org = await NewOrganisationAsync("answers", members: 3);
        var engagement = await NewAskedAsync(org, "Three answers");

        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await RespondAsync(org.Members[1], org, engagement.Id, "Maybe");
        await RespondAsync(org.Members[2], org, engagement.Id, "Unavailable");

        var view = await AvailabilityAsync(org, engagement.Id);

        Assert.Equal(3, view.Summary.Selected);
        Assert.Equal(1, view.Summary.Available);
        Assert.Equal(1, view.Summary.Maybe);
        Assert.Equal(1, view.Summary.Unavailable);
        Assert.Equal(0, view.Summary.Outstanding);
    }

    [Fact]
    public async Task AnUnknownAnswer_IsRejected()
    {
        var org = await NewOrganisationAsync("bad-answer", members: 1);
        var engagement = await NewAskedAsync(org, "Bad answer");

        var response = await org.Members[0].PutAsJsonAsync(
            $"{Availability(org, engagement.Id)}/me",
            new RespondAvailabilityRequest("Probably"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// The privacy rule: a Member is handed their own answer and nothing else,
    /// so nobody can be influenced by what the rest of the lineup said.
    /// </summary>
    [Fact]
    public async Task AMemberSeesOnlyTheirOwnAnswer()
    {
        var org = await NewOrganisationAsync("privacy", members: 2);
        var engagement = await NewAskedAsync(org, "Private answers");
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await RespondAsync(org.Members[1], org, engagement.Id, "Unavailable");

        var everyone = await org.Members[0].GetAsync(Availability(org, engagement.Id));
        Assert.Equal(HttpStatusCode.Forbidden, everyone.StatusCode);

        var own = await org.Members[0].GetFromJsonAsync<OwnAvailabilityResponse>(
            $"{Availability(org, engagement.Id)}/me");
        Assert.NotNull(own);
        Assert.True(own.IsSelected);
        Assert.Equal("Available", own.Response);
    }

    [Fact]
    public async Task SomeoneNeverAsked_CannotAnswer()
    {
        var org = await NewOrganisationAsync("not-asked", members: 2);
        // Only the first member is asked.
        var engagement = await NewDatedDraftAsync(org, "Selective");
        await RequestAsync(org, engagement.Id, [org.MemberIds[0]]);

        var attempt = await org.Members[1].PutAsJsonAsync(
            $"{Availability(org, engagement.Id)}/me",
            new RespondAvailabilityRequest("Available"));

        // 404 rather than 403: an engagement they are not on is not theirs to know about.
        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
    }

    /// <summary>
    /// D-027, the rule that makes a lineup workable: replacing one person must
    /// not cost everyone else their answer.
    /// </summary>
    [Fact]
    public async Task AddingSomeoneLater_LeavesExistingAnswersAlone()
    {
        var org = await NewOrganisationAsync("add-later", members: 3);
        var engagement = await NewDatedDraftAsync(org, "Growing lineup");
        await RequestAsync(org, engagement.Id, [org.MemberIds[0], org.MemberIds[1]]);
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await RespondAsync(org.Members[1], org, engagement.Id, "Maybe");

        await RequestAsync(org, engagement.Id, [org.MemberIds[2]]);

        var view = await AvailabilityAsync(org, engagement.Id);
        Assert.Equal(3, view.Summary.Selected);
        Assert.Equal(1, view.Summary.Available);
        Assert.Equal(1, view.Summary.Maybe);
        Assert.Equal(1, view.Summary.Outstanding);
        Assert.Equal(
            "Available",
            view.Participants.Single(p => p.UserId == org.MemberIds[0]).Response);
    }

    [Fact]
    public async Task AddingSomeoneWhileTentative_KeepsItTentative()
    {
        var org = await NewOrganisationAsync("tentative-add", members: 2);
        var engagement = await NewDatedDraftAsync(org, "Provisionally on");
        await RequestAsync(org, engagement.Id, [org.MemberIds[0]]);
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await TransitionAsync(org, engagement.Id, "Tentative");

        await RequestAsync(org, engagement.Id, [org.MemberIds[1]]);

        // The customer position has not moved just because the lineup changed.
        Assert.Equal(
            "Tentative",
            (await GetEngagementAsync(org, engagement.Id)).Status);
        Assert.Equal(1, (await AvailabilityAsync(org, engagement.Id)).Summary.Outstanding);
    }

    [Fact]
    public async Task ARemovedMemberLosesAccessButTheirAnswerIsKept()
    {
        var org = await NewOrganisationAsync("removal", members: 2);
        var engagement = await NewAskedAsync(org, "Someone drops out");
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");

        var removed = await org.Owner.DeleteAsync(
            $"{Availability(org, engagement.Id)}/{org.MemberIds[0]}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var view = await AvailabilityAsync(org, engagement.Id);

        // Out of the lineup and its totals...
        Assert.Equal(1, view.Summary.Selected);
        Assert.Equal(0, view.Summary.Available);

        // ...but the answer they gave is still on the record.
        var history = view.Participants.Single(p => p.UserId == org.MemberIds[0]);
        Assert.False(history.IsActive);
        Assert.Equal("Available", history.Response);
        Assert.NotNull(history.RemovedAtUtc);

        // And the engagement is no longer theirs to see.
        var peek = await org.Members[0].GetAsync(
            $"/api/organisations/{org.Id}/engagements/{engagement.Id}");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
    }

    [Fact]
    public async Task ReSelectingSomeone_RestoresTheAnswerTheyAlreadyGave()
    {
        var org = await NewOrganisationAsync("restore", members: 1);
        var engagement = await NewAskedAsync(org, "Back on");
        await RespondAsync(org.Members[0], org, engagement.Id, "Maybe");
        await org.Owner.DeleteAsync(
            $"{Availability(org, engagement.Id)}/{org.MemberIds[0]}");

        await RequestAsync(org, engagement.Id, [org.MemberIds[0]]);

        var view = await AvailabilityAsync(org, engagement.Id);
        var back = view.Participants.Single(p => p.UserId == org.MemberIds[0]);
        Assert.True(back.IsActive);
        Assert.Equal("Maybe", back.Response);
        Assert.Equal(0, view.Summary.Outstanding);
    }

    [Fact]
    public async Task NonRespondersCanBeIdentifiedAndReminded()
    {
        var org = await NewOrganisationAsync("remind", members: 2);
        var engagement = await NewAskedAsync(org, "Chasing");
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");

        var before = await AvailabilityAsync(org, engagement.Id);
        var silent = before.Participants.Single(p => p.Response is null);
        Assert.Null(silent.RemindedAtUtc);

        var reminded = await org.Owner.PostAsync(
            $"{Availability(org, engagement.Id)}/{silent.UserId}/reminders",
            content: null);
        Assert.Equal(HttpStatusCode.NoContent, reminded.StatusCode);

        var after = await AvailabilityAsync(org, engagement.Id);
        Assert.NotNull(after.Participants.Single(p => p.UserId == silent.UserId).RemindedAtUtc);

        // Someone who already answered has nothing to be chased about.
        var pointless = await org.Owner.PostAsync(
            $"{Availability(org, engagement.Id)}/{org.MemberIds[0]}/reminders",
            content: null);
        Assert.Equal(HttpStatusCode.BadRequest, pointless.StatusCode);
    }

    /// <summary>
    /// D-029: a booking is a fact about the customer, not about the lineup. It
    /// can be confirmed with answers outstanding, but not by accident.
    /// </summary>
    [Fact]
    public async Task ConfirmingWithOutstandingAnswers_NeedsAcknowledgement()
    {
        var org = await NewOrganisationAsync("confirm-warning", members: 2);
        var engagement = await NewAskedAsync(org, "Confirm anyway");
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");

        var blocked = await TransitionAsync(org, engagement.Id, "Confirmed");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal(
            "CheckingAvailability",
            (await GetEngagementAsync(org, engagement.Id)).Status);

        var acknowledged = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements/{engagement.Id}/transition",
            new TransitionEngagementRequest("Confirmed", null, true));
        Assert.Equal(HttpStatusCode.NoContent, acknowledged.StatusCode);
        Assert.Equal("Confirmed", (await GetEngagementAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task WithEveryAnswerIn_ConfirmingNeedsNoAcknowledgement()
    {
        var org = await NewOrganisationAsync("confirm-clean", members: 1);
        var engagement = await NewAskedAsync(org, "All answered");
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");

        var confirmed = await TransitionAsync(org, engagement.Id, "Confirmed");

        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
    }

    [Fact]
    public async Task OutstandingAnswersStayVisibleAfterConfirmation()
    {
        var org = await NewOrganisationAsync("after-confirm", members: 2);
        var engagement = await NewAskedAsync(org, "Still waiting");
        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements/{engagement.Id}/transition",
            new TransitionEngagementRequest("Confirmed", null, true));

        var view = await AvailabilityAsync(org, engagement.Id);

        Assert.Equal(1, view.Summary.Outstanding);
    }

    [Fact]
    public async Task AdvancingToTentativeStaysManual()
    {
        var org = await NewOrganisationAsync("manual-tentative", members: 1);
        var engagement = await NewAskedAsync(org, "Everyone answered");

        await RespondAsync(org.Members[0], org, engagement.Id, "Available");

        // Every answer is in, and it has still not moved on its own.
        Assert.Equal(
            "CheckingAvailability",
            (await GetEngagementAsync(org, engagement.Id)).Status);
    }

    [Fact]
    public async Task AMemberSeesOnlyTheEngagementsTheyAreOn()
    {
        var org = await NewOrganisationAsync("visibility", members: 2);
        var mine = await NewDatedDraftAsync(org, "Asked about this one");
        await NewDatedDraftAsync(org, "Nothing to do with them");
        await RequestAsync(org, mine.Id, [org.MemberIds[0]]);

        var visible = await org.Members[0]
            .GetFromJsonAsync<List<EngagementResponse>>(
                $"/api/organisations/{org.Id}/engagements");

        Assert.NotNull(visible);
        Assert.Single(visible);
        Assert.Equal(mine.Id, visible[0].Id);
    }

    [Fact]
    public async Task AMemberCannotSelectOrRemoveAnyone()
    {
        var org = await NewOrganisationAsync("member-powerless", members: 2);
        var engagement = await NewAskedAsync(org, "Not their call");

        var select = await org.Members[0].PostAsJsonAsync(
            $"{Availability(org, engagement.Id)}/requests",
            new RequestAvailabilityRequest([org.MemberIds[1]]));
        Assert.Equal(HttpStatusCode.Forbidden, select.StatusCode);

        var remove = await org.Members[0].DeleteAsync(
            $"{Availability(org, engagement.Id)}/{org.MemberIds[1]}");
        Assert.Equal(HttpStatusCode.Forbidden, remove.StatusCode);
    }

    [Fact]
    public async Task SomeoneOutsideTheOrganisation_CannotBeSelected()
    {
        var org = await NewOrganisationAsync("outsider", members: 1);
        var other = await NewOrganisationAsync("outsider-other", members: 1);
        var engagement = await NewDatedDraftAsync(org, "Only our people");

        var attempt = await RequestAsync(org, engagement.Id, [other.MemberIds[0]]);

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Availability(TestOrganisation org, Guid engagementId) =>
        $"/api/organisations/{org.Id}/engagements/{engagementId}/availability";

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

        // The directory gives the user ids, in join order after the owner.
        var directory = await owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{organisation.Id}/members");
        Assert.NotNull(directory);
        var memberIds = directory.Skip(1).Select(row => row.UserId).ToList();

        return new TestOrganisation(organisation.Id, owner, clients, memberIds);
    }

    private async Task<EngagementResponse> NewDraftAsync(
        TestOrganisation org,
        string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements",
            new CreateEngagementRequest(title, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    private async Task<EngagementResponse> NewDatedDraftAsync(
        TestOrganisation org,
        string title)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements",
            new CreateEngagementRequest(
                title,
                new DateOnly(2027, 6, 12),
                null,
                null,
                null));
        response.EnsureSuccessStatusCode();
        var engagement = await response.Content.ReadFromJsonAsync<EngagementResponse>();
        Assert.NotNull(engagement);
        return engagement;
    }

    /// <summary>A dated engagement with every member already asked.</summary>
    private async Task<EngagementResponse> NewAskedAsync(
        TestOrganisation org,
        string title)
    {
        var engagement = await NewDatedDraftAsync(org, title);
        var sent = await RequestAsync(org, engagement.Id, org.MemberIds);
        sent.EnsureSuccessStatusCode();
        return engagement;
    }

    private Task<HttpResponseMessage> RequestAsync(
        TestOrganisation org,
        Guid engagementId,
        IReadOnlyList<Guid> userIds)
    {
        return org.Owner.PostAsJsonAsync(
            $"{Availability(org, engagementId)}/requests",
            new RequestAvailabilityRequest(userIds));
    }

    private static async Task RespondAsync(
        HttpClient member,
        TestOrganisation org,
        Guid engagementId,
        string response)
    {
        var result = await member.PutAsJsonAsync(
            $"{Availability(org, engagementId)}/me",
            new RespondAvailabilityRequest(response));
        result.EnsureSuccessStatusCode();
    }

    private Task<HttpResponseMessage> TransitionAsync(
        TestOrganisation org,
        Guid engagementId,
        string status)
    {
        return org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/engagements/{engagementId}/transition",
            new TransitionEngagementRequest(status, null));
    }

    private static async Task<EngagementResponse> GetEngagementAsync(
        TestOrganisation org,
        Guid engagementId)
    {
        var engagement = await org.Owner.GetFromJsonAsync<EngagementResponse>(
            $"/api/organisations/{org.Id}/engagements/{engagementId}");
        Assert.NotNull(engagement);
        return engagement;
    }

    private static async Task<EngagementAvailabilityResponse> AvailabilityAsync(
        TestOrganisation org,
        Guid engagementId)
    {
        var view = await org.Owner.GetFromJsonAsync<EngagementAvailabilityResponse>(
            Availability(org, engagementId));
        Assert.NotNull(view);
        return view;
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
