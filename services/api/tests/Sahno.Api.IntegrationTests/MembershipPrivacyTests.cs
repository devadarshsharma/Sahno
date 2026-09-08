using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Organisations;
using Sahno.Contracts.Users;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Directory privacy (D-018, D-046). Identity is shared inside an
/// organisation; contact details are not, until the person says so.
/// </summary>
public sealed class MembershipPrivacyTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task MembersSeeNamesAndFunctions_ButNotEachOthersContactDetails()
    {
        var org = await NewOrganisationAsync("privacy-basic", memberCount: 2);
        await SetOwnProfileAsync(org.Members[1], org.Id, "Tabla player", shares: false);

        var rows = await DirectoryAsAsync(org, org.Members[0]);
        var other = rows.Single(row =>
            row.Function == "Tabla player" && !row.IsYou);

        Assert.Equal("Tabla player", other.Function);
        Assert.Null(other.Email);
        Assert.Null(other.PhoneNumber);
    }

    [Fact]
    public async Task SharingContactDetails_RevealsThemToOrdinaryMembers()
    {
        var org = await NewOrganisationAsync("privacy-share", memberCount: 2);
        await SetPhoneAsync(org.Members[1], "+61 400 000 111");

        var before = await DirectoryAsAsync(org, org.Members[0]);
        Assert.All(
            before.Where(row => !row.IsYou),
            row => Assert.Null(row.PhoneNumber));

        await SetOwnProfileAsync(org.Members[1], org.Id, function: null, shares: true);

        var after = await DirectoryAsAsync(org, org.Members[0]);
        var shared = after.Single(row => row.SharesContactDetails && !row.IsYou);
        Assert.Equal("+61 400 000 111", shared.PhoneNumber);
    }

    [Fact]
    public async Task SharingIsPerOrganisation_NotAccountWide()
    {
        var first = await NewOrganisationAsync("privacy-scope-a", memberCount: 1);
        var second = await NewOrganisationAsync("privacy-scope-b");

        // The same person joins the second organisation as well.
        var invitation = await InviteAsync(second);
        var join = await first.Members[0].PostAsync(
            $"/api/invitations/{invitation.Token}/accept",
            content: null);
        join.EnsureSuccessStatusCode();

        await SetPhoneAsync(first.Members[0], "+61 400 222 333");
        await SetOwnProfileAsync(first.Members[0], first.Id, null, shares: true);

        // Shared in the first organisation only; the second still withholds it
        // from an ordinary Member's view.
        var elsewhere = await DirectoryAsAsync(second, second.Owner);
        var row = elsewhere.Single(entry => !entry.IsYou);
        Assert.False(row.SharesContactDetails);
    }

    [Fact]
    public async Task OrganisersSeeContactDetails_WithoutSharing()
    {
        var org = await NewOrganisationAsync("privacy-organiser", memberCount: 1);
        await SetPhoneAsync(org.Members[0], "+61 400 444 555");

        var rows = await DirectoryAsAsync(org, org.Owner);
        var member = rows.Single(row => !row.IsYou);

        Assert.False(member.SharesContactDetails);
        Assert.Equal("+61 400 444 555", member.PhoneNumber);
    }

    [Fact]
    public async Task YourOwnDetails_AreAlwaysVisibleToYou()
    {
        var org = await NewOrganisationAsync("privacy-self", memberCount: 1);
        await SetPhoneAsync(org.Members[0], "+61 400 666 777");

        var rows = await DirectoryAsAsync(org, org.Members[0]);
        var you = rows.Single(row => row.IsYou);

        Assert.Equal("+61 400 666 777", you.PhoneNumber);
    }

    [Fact]
    public async Task ContactSharing_IsNotAnOrganisersToChange()
    {
        var org = await NewOrganisationAsync("privacy-not-organisers", memberCount: 1);
        var membershipId = await MemberMembershipIdAsync(org);

        // The management route carries no sharing field at all, and the
        // self-service route only ever reaches the caller's own membership.
        var attempt = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{membershipId}",
            new UpdateMemberRequest(null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    [Fact]
    public async Task InternalNotes_AreVisibleToOrganisersOnly()
    {
        var org = await NewOrganisationAsync("privacy-notes", memberCount: 2);
        var membershipId = await MemberMembershipIdAsync(org);

        var written = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{membershipId}",
            new UpdateMemberRequest(null, null, "Prefers early call times."));
        Assert.Equal(HttpStatusCode.NoContent, written.StatusCode);

        var asOwner = await DirectoryAsAsync(org, org.Owner);
        Assert.Equal(
            "Prefers early call times.",
            asOwner.Single(row => row.MembershipId == membershipId).InternalNotes);

        // Not even to the member the note is about.
        var asSubject = await DirectoryAsAsync(org, org.Members[0]);
        Assert.All(asSubject, row => Assert.Null(row.InternalNotes));

        var asPeer = await DirectoryAsAsync(org, org.Members[1]);
        Assert.All(asPeer, row => Assert.Null(row.InternalNotes));
    }

    [Fact]
    public async Task Member_CannotWriteInternalNotes()
    {
        var org = await NewOrganisationAsync("privacy-notes-guard", memberCount: 2);
        var rows = await DirectoryAsAsync(org, org.Owner);
        var target = rows.Last().MembershipId;

        var attempt = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest(null, null, "should not stick"));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task OwnProfile_IsOnlyEverYourOwn()
    {
        var org = await NewOrganisationAsync("privacy-own", memberCount: 2);

        await SetOwnProfileAsync(org.Members[0], org.Id, "Singer", shares: false);

        var rows = await DirectoryAsAsync(org, org.Owner);
        Assert.Single(rows, row => row.Function == "Singer");
    }

    [Fact]
    public async Task NonMember_CannotSetAProfileInThatOrganisation()
    {
        var org = await NewOrganisationAsync("privacy-outsider");
        using var outsider = CreateClient("auth0|privacy-outsider-stranger");

        var attempt = await outsider.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/me",
            new UpdateOwnMembershipRequest("Interloper", true));

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
    }

    [Fact]
    public async Task PhoneNumber_CanBeClearedByTheirOwner()
    {
        var org = await NewOrganisationAsync("privacy-clear", memberCount: 1);
        await SetPhoneAsync(org.Members[0], "+61 400 888 999");
        await SetOwnProfileAsync(org.Members[0], org.Id, null, shares: true);

        var withNumber = await DirectoryAsAsync(org, org.Members[0]);
        Assert.Equal(
            "+61 400 888 999",
            withNumber.Single(row => row.IsYou).PhoneNumber);

        await SetPhoneAsync(org.Members[0], "");

        var cleared = await DirectoryAsAsync(org, org.Members[0]);
        Assert.Null(cleared.Single(row => row.IsYou).PhoneNumber);
    }

    private sealed record TestOrganisation(
        Guid Id,
        HttpClient Owner,
        IReadOnlyList<HttpClient> Members);

    private async Task<TestOrganisation> NewOrganisationAsync(
        string prefix,
        int memberCount = 0)
    {
        var owner = CreateClient($"auth0|{prefix}-owner");
        var created = await owner.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest($"{prefix} org", null, null));
        created.EnsureSuccessStatusCode();
        var organisation = await created.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);

        var org = new TestOrganisation(organisation.Id, owner, []);

        var members = new List<HttpClient>();
        if (memberCount > 0)
        {
            var invitation = await InviteAsync(org);
            for (var index = 0; index < memberCount; index++)
            {
                var member = CreateClient($"auth0|{prefix}-member-{index}");
                var accept = await member.PostAsync(
                    $"/api/invitations/{invitation.Token}/accept",
                    content: null);
                accept.EnsureSuccessStatusCode();
                members.Add(member);
            }
        }

        return org with { Members = members };
    }

    private static async Task<InvitationResponse> InviteAsync(TestOrganisation org)
    {
        var response = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/invitations",
            new CreateInvitationRequest(null));
        response.EnsureSuccessStatusCode();
        var invitation = await response.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(invitation);
        return invitation;
    }

    private static async Task<List<MemberResponse>> DirectoryAsAsync(
        TestOrganisation org,
        HttpClient client)
    {
        var rows = await client.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{org.Id}/members");
        Assert.NotNull(rows);
        return rows;
    }

    private static async Task<Guid> MemberMembershipIdAsync(TestOrganisation org)
    {
        var rows = await DirectoryAsAsync(org, org.Owner);
        return rows.Skip(1).First().MembershipId;
    }

    private static async Task SetOwnProfileAsync(
        HttpClient client,
        Guid organisationId,
        string? function,
        bool shares)
    {
        var response = await client.PatchAsJsonAsync(
            $"/api/organisations/{organisationId}/members/me",
            new UpdateOwnMembershipRequest(function, shares));
        response.EnsureSuccessStatusCode();
    }

    private static async Task SetPhoneAsync(HttpClient client, string phoneNumber)
    {
        var response = await client.PatchAsJsonAsync(
            "/api/me",
            new UpdateMeRequest(null, phoneNumber));
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
