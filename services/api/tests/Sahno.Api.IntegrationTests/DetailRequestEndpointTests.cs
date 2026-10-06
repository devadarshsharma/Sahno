using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Notifications;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Asking the organisers for a missing detail (D-089): only what is missing
/// can be asked for, the organisers are told once per person per detail, and
/// filling the detail in closes the request — telling whoever asked about an
/// outfit, which no other notification covers.
/// </summary>
public sealed class DetailRequestEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AMemberAsksAndTheOrganisersAreToldOnce()
    {
        var org = await NewOrganisationAsync("details-ask", members: 2);
        var engagement = await NewDraftAsync(org, "Saturday gig");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var requests = $"{Engagements(org)}/{engagement.Id}/detail-requests";

        var before = await org.Members[0].GetFromJsonAsync<DetailRequestsResponse>(requests);
        Assert.Equal(["Venue", "CallTime", "StartTime", "Dress"], before!.Missing);
        Assert.Empty(before.Asked);
        Assert.Null(before.Askers);

        (await org.Members[0].PostAsJsonAsync(requests, new AskForDetailsRequest(["CallTime", "Dress"]))).EnsureSuccessStatusCode();
        // Again, and a second person: the first is quiet, the second is news.
        (await org.Members[0].PostAsJsonAsync(requests, new AskForDetailsRequest(["CallTime"]))).EnsureSuccessStatusCode();
        (await org.Members[1].PostAsJsonAsync(requests, new AskForDetailsRequest(["Dress"]))).EnsureSuccessStatusCode();

        var asks = (await BellAsync(org.Owner, org)).Where(row => row.Kind == "DetailsRequested").ToList();
        Assert.Equal(2, asks.Count);
        Assert.Contains(asks, row => row.Body == "What is the sound-check time and outfit?");
        Assert.All(asks, row => Assert.Equal($"/engagement/{engagement.Id}/details", row.Route));
        Assert.DoesNotContain(await BellAsync(org.Members[1], org), row => row.Kind == "DetailsRequested");

        var mine = await org.Members[0].GetFromJsonAsync<DetailRequestsResponse>(requests);
        Assert.Equal(["CallTime", "Dress"], mine!.Asked);

        var organiser = await org.Owner.GetFromJsonAsync<DetailRequestsResponse>(requests);
        var dress = Assert.Single(organiser!.Askers!, row => row.Item == "Dress");
        Assert.Equal(2, dress.Names.Count);

        // Organisers fill details in rather than asking; nonsense is refused.
        Assert.Equal(HttpStatusCode.Forbidden, (await org.Owner.PostAsJsonAsync(requests, new AskForDetailsRequest(["Venue"]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await org.Members[0].PostAsJsonAsync(requests, new AskForDetailsRequest(["Lineup"]))).StatusCode);
    }

    [Fact]
    public async Task FillingADetailInClosesTheRequest()
    {
        var org = await NewOrganisationAsync("details-answer", members: 1);
        var engagement = await NewDraftAsync(org, "Wedding");
        await RequestAvailabilityAsync(org, engagement.Id, org.MemberIds);
        var requests = $"{Engagements(org)}/{engagement.Id}/detail-requests";

        (await org.Members[0].PostAsJsonAsync(requests, new AskForDetailsRequest(["Venue", "Dress"]))).EnsureSuccessStatusCode();

        (await org.Owner.PatchAsJsonAsync(
            $"{Engagements(org)}/{engagement.Id}",
            new UpdateEngagementRequest("Wedding", null, null, "Black kurta", null))).EnsureSuccessStatusCode();

        var after = await org.Members[0].GetFromJsonAsync<DetailRequestsResponse>(requests);
        Assert.Equal(["Venue"], after!.Asked);
        Assert.DoesNotContain("Dress", after.Missing);

        var told = Assert.Single(await BellAsync(org.Members[0], org), row => row.Title == "Wedding: what to wear");
        Assert.Equal("Black kurta", told.Body);

        // Somebody not on the event cannot see or ask.
        var outsider = await NewOrganisationAsync("details-outsider");
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await outsider.Owner.GetAsync($"/api/organisations/{org.Id}/engagements/{engagement.Id}/detail-requests")).StatusCode);
    }

    private static async Task<List<NotificationResponse>> BellAsync(HttpClient client, TestOrganisation org)
    {
        var rows = await client.GetFromJsonAsync<List<NotificationResponse>>(
            $"/api/organisations/{org.Id}/notifications");
        return rows!;
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members,
        IReadOnlyList<Guid> MemberIds);

    private static string Engagements(TestOrganisation org) =>
        $"/api/organisations/{org.Id}/engagements";

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
