using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Engagements;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// A person's calendar across their organisations (D-091): each organisation
/// contributes what that person already sees in it, labelled with whose it
/// is — and nobody else ever sees it.
/// </summary>
public sealed class MyCalendarTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task AFreelancerSeesEveryGroupTheyPlayWith()
    {
        // Somebody who plays for Group B and runs their own Group A.
        var groupB = await NewOrganisationAsync("cal-b", members: 1);
        var freelancer = groupB.Members[0];
        var created = await freelancer.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest("cal-a org", null, null));
        created.EnsureSuccessStatusCode();
        var groupA = (await created.Content.ReadFromJsonAsync<OrganisationResponse>())!;

        var ownGig = await freelancer.PostAsJsonAsync(
            $"/api/organisations/{groupA.Id}/engagements",
            new CreateEngagementRequest("Own gig", new DateOnly(2027, 8, 14), null, null, null));
        ownGig.EnsureSuccessStatusCode();

        var asked = await NewDraftAsync(groupB, "Group B wedding");
        await RequestAvailabilityAsync(groupB, asked.Id, groupB.MemberIds);
        var notAsked = await NewDraftAsync(groupB, "Group B private enquiry");

        var mine = await freelancer.GetFromJsonAsync<MyCalendarResponse>("/api/me/calendar?from=2027-01-01");
        Assert.Contains(mine!.Events, row => row.Title == "Own gig" && row.OrganisationName == "cal-a org" && row.IsOrganiser);
        Assert.Contains(mine.Events, row => row.Id == asked.Id && row.OrganisationName == "cal-b org" && !row.IsOrganiser);
        Assert.DoesNotContain(mine.Events, row => row.Id == notAsked.Id);

        // Group B's organiser sees Group B, and nothing of Group A.
        var theirs = await groupB.Owner.GetFromJsonAsync<MyCalendarResponse>("/api/me/calendar?from=2027-01-01");
        Assert.All(theirs!.Events, row => Assert.Equal(groupB.Id, row.OrganisationId));
        Assert.Contains(theirs.Events, row => row.Id == notAsked.Id);
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
