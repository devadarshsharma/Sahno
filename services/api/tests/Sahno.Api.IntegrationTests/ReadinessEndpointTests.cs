using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Event readiness (Slice 7, D-048). The checklist is derived from the booking
/// rather than kept by hand, so these tests mostly assert that changing the
/// booking changes the list — a checklist that needed maintaining would drift
/// out of step with the event it describes.
/// </summary>
public sealed class ReadinessEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task ANewBookingHasEverythingOutstanding()
    {
        var org = await NewOrganisationAsync("readiness-new");
        var engagement = await NewDatedDraftAsync(org, "Nothing done yet");

        var readiness = await ReadinessAsync(org, engagement.Id);

        Assert.Equal(8, readiness.Count);
        Assert.All(readiness, entry => Assert.Equal("Outstanding", entry.State));
    }

    [Fact]
    public async Task AddingAVenueTicksTheVenueItem()
    {
        var org = await NewOrganisationAsync("readiness-venue");
        var engagement = await NewDatedDraftAsync(org, "Venue to add");

        await UpdateAsync(org, engagement.Id, venue: "Dural Community Hall");

        var readiness = await ReadinessAsync(org, engagement.Id);
        Assert.Equal("Done", StateOf(readiness, "Venue"));
        // Nothing else was touched, so nothing else moved.
        Assert.Equal("Outstanding", StateOf(readiness, "CallTime"));
    }

    [Fact]
    public async Task TimesAndDressAreEachTheirOwnItem()
    {
        var org = await NewOrganisationAsync("readiness-times");
        var engagement = await NewDatedDraftAsync(org, "Timings");

        await UpdateAsync(
            org,
            engagement.Id,
            startTime: new TimeOnly(19, 30),
            callTime: new TimeOnly(17, 0),
            dressNotes: "Black kurta, white shalwar.");

        var readiness = await ReadinessAsync(org, engagement.Id);
        Assert.Equal("Done", StateOf(readiness, "StartTime"));
        Assert.Equal("Done", StateOf(readiness, "CallTime"));
        Assert.Equal("Done", StateOf(readiness, "Dress"));
    }

    /// <summary>
    /// The lineup is only resolved once people have been asked and everyone
    /// has answered. Nobody asked at all is a step not yet taken, not a
    /// settled lineup.
    /// </summary>
    [Fact]
    public async Task TheLineupIsResolvedOnlyWhenEveryoneHasAnswered()
    {
        var org = await NewOrganisationAsync("readiness-lineup", members: 2);
        var engagement = await NewDatedDraftAsync(org, "Who is on");

        Assert.Equal(
            "Outstanding",
            StateOf(await ReadinessAsync(org, engagement.Id), "Lineup"));

        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        Assert.Equal(
            "Outstanding",
            StateOf(await ReadinessAsync(org, engagement.Id), "Lineup"));

        await RespondAsync(org.Members[0], org, engagement.Id, "Available");
        Assert.Equal(
            "Outstanding",
            StateOf(await ReadinessAsync(org, engagement.Id), "Lineup"));

        await RespondAsync(org.Members[1], org, engagement.Id, "Available");
        Assert.Equal(
            "Done",
            StateOf(await ReadinessAsync(org, engagement.Id), "Lineup"));
    }

    [Fact]
    public async Task AnItemCanBeMarkedNotRequiredAndPutBack()
    {
        var org = await NewOrganisationAsync("readiness-waive");
        var engagement = await NewDatedDraftAsync(org, "No dress code");

        var waived = await SetReadinessAsync(org, engagement.Id, "Dress", true);
        Assert.Equal(HttpStatusCode.NoContent, waived.StatusCode);
        Assert.Equal(
            "NotRequired",
            StateOf(await ReadinessAsync(org, engagement.Id), "Dress"));

        var restored = await SetReadinessAsync(org, engagement.Id, "Dress", false);
        Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
        Assert.Equal(
            "Outstanding",
            StateOf(await ReadinessAsync(org, engagement.Id), "Dress"));
    }

    /// <summary>
    /// Not required outranks done, so an item does not flip back to a chore
    /// because the underlying field happened to get filled in later.
    /// </summary>
    [Fact]
    public async Task NotRequiredHoldsEvenOnceTheDetailExists()
    {
        var org = await NewOrganisationAsync("readiness-waive-holds");
        var engagement = await NewDatedDraftAsync(org, "Waived then filled");
        await SetReadinessAsync(org, engagement.Id, "Venue", true);

        await UpdateAsync(org, engagement.Id, venue: "Added anyway");

        Assert.Equal(
            "NotRequired",
            StateOf(await ReadinessAsync(org, engagement.Id), "Venue"));
    }

    [Fact]
    public async Task WaivingTheSameItemTwiceIsNotAnError()
    {
        var org = await NewOrganisationAsync("readiness-twice");
        var engagement = await NewDatedDraftAsync(org, "Twice");

        await SetReadinessAsync(org, engagement.Id, "Rehearsal", true);
        var again = await SetReadinessAsync(org, engagement.Id, "Rehearsal", true);

        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(
            "NotRequired",
            StateOf(await ReadinessAsync(org, engagement.Id), "Rehearsal"));
    }

    [Fact]
    public async Task AnUnknownItemIsRejected()
    {
        var org = await NewOrganisationAsync("readiness-unknown");
        var engagement = await NewDatedDraftAsync(org, "Unknown item");

        var attempt = await SetReadinessAsync(org, engagement.Id, "Catering", true);

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    /// <summary>
    /// Readiness is the organiser's working list. A member is on the event,
    /// but what remains to organise is not theirs to see.
    /// </summary>
    [Fact]
    public async Task MembersCannotSeeOrChangeReadiness()
    {
        var org = await NewOrganisationAsync("readiness-member", members: 1);
        var engagement = await NewDatedDraftAsync(org, "Organisers only");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);

        var read = await org.Members[0].GetAsync(
            $"{Engagements(org)}/{engagement.Id}/readiness");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var write = await org.Members[0].PutAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}/readiness",
            new SetReadinessRequest("Dress", true));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task CallTimeAndDressReachTheMembersOnTheEvent()
    {
        var org = await NewOrganisationAsync("readiness-participant", members: 1);
        var engagement = await NewDatedDraftAsync(org, "Day-of detail");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        await UpdateAsync(
            org,
            engagement.Id,
            startTime: new TimeOnly(19, 30),
            callTime: new TimeOnly(17, 0),
            dressNotes: "Black kurta.",
            venue: "Dural Community Hall");

        var asMember = await org.Members[0].GetFromJsonAsync<EngagementResponse>(
            $"{Engagements(org)}/{engagement.Id}");

        Assert.NotNull(asMember);
        Assert.Equal(new TimeOnly(17, 0), asMember.CallTime);
        Assert.Equal("Black kurta.", asMember.DressNotes);
        Assert.Equal("Dural Community Hall", asMember.Venue);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

    private static string StateOf(
        List<ReadinessEntryResponse> readiness,
        string item)
    {
        return readiness.Single(entry => entry.Item == item).State;
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

    private async Task<EngagementResponse> NewDatedDraftAsync(
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

    private static async Task UpdateAsync(
        TestOrganisation org,
        Guid engagementId,
        TimeOnly? startTime = null,
        TimeOnly? callTime = null,
        string? dressNotes = null,
        string? venue = null)
    {
        var response = await org.Owner.PatchAsJsonAsync(
            $"{Engagements(org)}/{engagementId}",
            new UpdateEngagementRequest(null, startTime, callTime, dressNotes, venue));
        response.EnsureSuccessStatusCode();
    }

    private static Task<HttpResponseMessage> SetReadinessAsync(
        TestOrganisation org,
        Guid engagementId,
        string item,
        bool notRequired)
    {
        return org.Owner.PutAsJsonAsync(
            $"{Engagements(org)}/{engagementId}/readiness",
            new SetReadinessRequest(item, notRequired));
    }

    private static async Task<List<ReadinessEntryResponse>> ReadinessAsync(
        TestOrganisation org,
        Guid engagementId)
    {
        var readiness = await org.Owner
            .GetFromJsonAsync<List<ReadinessEntryResponse>>(
                $"{Engagements(org)}/{engagementId}/readiness");
        Assert.NotNull(readiness);
        return readiness;
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

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
