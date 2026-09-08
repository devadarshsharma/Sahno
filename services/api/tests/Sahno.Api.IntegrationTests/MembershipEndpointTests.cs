using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Organisations;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Slice 3 safeguards. Each test names the rule it protects: these are the
/// checks that stop one person quietly acquiring authority over a group.
/// </summary>
public sealed class MembershipEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task Directory_ListsEveryMemberToEveryMember()
    {
        var org = await NewOrganisationAsync("directory", memberCount: 2);

        var asMember = await org.Members[0].GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{org.Id}/members");

        Assert.NotNull(asMember);
        Assert.Equal(3, asMember.Count);
        Assert.Single(asMember, row => row.Role == "Owner");
        Assert.Single(asMember, row => row.IsYou);
    }

    [Fact]
    public async Task NonMember_CannotSeeTheDirectory()
    {
        var org = await NewOrganisationAsync("directory-outsider");
        using var outsider = CreateClient("auth0|directory-outsider-stranger");

        var response = await outsider.GetAsync($"/api/organisations/{org.Id}/members");

        // 404 rather than 403: existence is not disclosed to non-members.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OwnerAppointsAndRemovesAdmins()
    {
        var org = await NewOrganisationAsync("appoint", memberCount: 1);
        var target = await MembershipIdOfAsync(org, index: 0);

        var promote = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest("Admin", null));
        Assert.Equal(HttpStatusCode.NoContent, promote.StatusCode);
        Assert.Equal("Admin", await RoleOfAsync(org, target));

        var demote = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest("Member", null));
        Assert.Equal(HttpStatusCode.NoContent, demote.StatusCode);
        Assert.Equal("Member", await RoleOfAsync(org, target));
    }

    [Fact]
    public async Task Admin_CannotAppointAnotherAdmin()
    {
        var org = await NewOrganisationAsync("admin-appoint", memberCount: 2);
        var adminMembership = await MembershipIdOfAsync(org, index: 0);
        var otherMembership = await MembershipIdOfAsync(org, index: 1);
        await PromoteToAdminAsync(org, adminMembership);

        var attempt = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{otherMembership}",
            new UpdateMemberRequest("Admin", null));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
        Assert.Equal("Member", await RoleOfAsync(org, otherMembership));
    }

    [Fact]
    public async Task Admin_CannotDemoteAnotherAdmin()
    {
        var org = await NewOrganisationAsync("admin-demote", memberCount: 2);
        var firstAdmin = await MembershipIdOfAsync(org, index: 0);
        var secondAdmin = await MembershipIdOfAsync(org, index: 1);
        await PromoteToAdminAsync(org, firstAdmin);
        await PromoteToAdminAsync(org, secondAdmin);

        var attempt = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{secondAdmin}",
            new UpdateMemberRequest("Member", null));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
        Assert.Equal("Admin", await RoleOfAsync(org, secondAdmin));
    }

    /// <summary>
    /// The self-escalation guard: the rule that makes the rest hold, since
    /// every other restriction is about acting on someone else.
    /// </summary>
    [Fact]
    public async Task NobodyCanChangeTheirOwnRole()
    {
        var org = await NewOrganisationAsync("self-escalation", memberCount: 1);
        var own = await MembershipIdOfAsync(org, index: 0);
        await PromoteToAdminAsync(org, own);

        var attempt = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{own}",
            new UpdateMemberRequest("Admin", null));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task Member_CannotChangeAnyoneElsesRole()
    {
        var org = await NewOrganisationAsync("member-powerless", memberCount: 2);
        var target = await MembershipIdOfAsync(org, index: 1);

        var attempt = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest("Admin", null));

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task OwnersRole_CannotBeChangedByAnyone()
    {
        var org = await NewOrganisationAsync("owner-immovable", memberCount: 1);
        var adminMembership = await MembershipIdOfAsync(org, index: 0);
        await PromoteToAdminAsync(org, adminMembership);
        var ownerMembership = await OwnerMembershipIdAsync(org);

        var byAdmin = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{ownerMembership}",
            new UpdateMemberRequest("Member", null));
        Assert.Equal(HttpStatusCode.Forbidden, byAdmin.StatusCode);

        var byOwner = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{ownerMembership}",
            new UpdateMemberRequest("Member", null));
        Assert.Equal(HttpStatusCode.Forbidden, byOwner.StatusCode);

        Assert.Equal("Owner", await RoleOfAsync(org, ownerMembership));
    }

    [Fact]
    public async Task PromotingToOwnerThroughARoleEdit_IsRejected()
    {
        var org = await NewOrganisationAsync("no-owner-by-edit", memberCount: 1);
        var target = await MembershipIdOfAsync(org, index: 0);

        var attempt = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest("Owner", null));

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
        Assert.Equal("Member", await RoleOfAsync(org, target));
    }

    [Fact]
    public async Task FinancialAccess_IsOffByDefaultAndOwnerControlled()
    {
        var org = await NewOrganisationAsync("finances", memberCount: 2);
        var adminMembership = await MembershipIdOfAsync(org, index: 0);
        await PromoteToAdminAsync(org, adminMembership);

        // Off by default on appointment (D-016).
        Assert.False(await CanManageFinancesAsync(org, adminMembership));

        var granted = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{adminMembership}",
            new UpdateMemberRequest(null, true));
        Assert.Equal(HttpStatusCode.NoContent, granted.StatusCode);
        Assert.True(await CanManageFinancesAsync(org, adminMembership));

        // An Admin cannot grant it, to themselves or to anyone else.
        var otherMembership = await MembershipIdOfAsync(org, index: 1);
        await PromoteToAdminAsync(org, otherMembership);
        var byAdmin = await org.Members[0].PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{otherMembership}",
            new UpdateMemberRequest(null, true));
        Assert.Equal(HttpStatusCode.Forbidden, byAdmin.StatusCode);

        var revoked = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{adminMembership}",
            new UpdateMemberRequest(null, false));
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        Assert.False(await CanManageFinancesAsync(org, adminMembership));
    }

    [Fact]
    public async Task DemotingAnAdmin_RevokesFinancialAccess()
    {
        var org = await NewOrganisationAsync("finance-revoke", memberCount: 1);
        var target = await MembershipIdOfAsync(org, index: 0);
        await PromoteToAdminAsync(org, target);
        await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest(null, true));
        Assert.True(await CanManageFinancesAsync(org, target));

        await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest("Member", null));

        // Re-appointment must not silently restore the old grant.
        await PromoteToAdminAsync(org, target);
        Assert.False(await CanManageFinancesAsync(org, target));
    }

    [Fact]
    public async Task FinancialAccess_CannotBeGrantedToAnOrdinaryMember()
    {
        var org = await NewOrganisationAsync("finance-member", memberCount: 1);
        var target = await MembershipIdOfAsync(org, index: 0);

        var attempt = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{target}",
            new UpdateMemberRequest(null, true));

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    [Fact]
    public async Task OwnerAndAdminsRemoveMembers_ButNotTheOwner()
    {
        var org = await NewOrganisationAsync("removal", memberCount: 3);
        var adminMembership = await MembershipIdOfAsync(org, index: 0);
        await PromoteToAdminAsync(org, adminMembership);
        var byOwnerTarget = await MembershipIdOfAsync(org, index: 1);
        var byAdminTarget = await MembershipIdOfAsync(org, index: 2);
        var ownerMembership = await OwnerMembershipIdAsync(org);

        var byOwner = await org.Owner.DeleteAsync(
            $"/api/organisations/{org.Id}/members/{byOwnerTarget}");
        Assert.Equal(HttpStatusCode.NoContent, byOwner.StatusCode);

        var byAdmin = await org.Members[0].DeleteAsync(
            $"/api/organisations/{org.Id}/members/{byAdminTarget}");
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);

        // The Owner cannot be removed — ownership must be transferred first.
        var removeOwner = await org.Members[0].DeleteAsync(
            $"/api/organisations/{org.Id}/members/{ownerMembership}");
        Assert.Equal(HttpStatusCode.Forbidden, removeOwner.StatusCode);

        var remaining = await org.Owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{org.Id}/members");
        Assert.NotNull(remaining);
        Assert.Equal(2, remaining.Count);
    }

    [Fact]
    public async Task Admin_CannotRemoveAnotherAdmin()
    {
        var org = await NewOrganisationAsync("admin-remove", memberCount: 2);
        var firstAdmin = await MembershipIdOfAsync(org, index: 0);
        var secondAdmin = await MembershipIdOfAsync(org, index: 1);
        await PromoteToAdminAsync(org, firstAdmin);
        await PromoteToAdminAsync(org, secondAdmin);

        var attempt = await org.Members[0].DeleteAsync(
            $"/api/organisations/{org.Id}/members/{secondAdmin}");

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task Member_CannotRemoveAnyone()
    {
        var org = await NewOrganisationAsync("member-remove", memberCount: 2);
        var target = await MembershipIdOfAsync(org, index: 1);

        var attempt = await org.Members[0].DeleteAsync(
            $"/api/organisations/{org.Id}/members/{target}");

        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task TransferOwnership_MovesOwnershipAndLeavesExactlyOneOwner()
    {
        var org = await NewOrganisationAsync("transfer", memberCount: 1);
        var successor = await MembershipIdOfAsync(org, index: 0);
        var ownerMembership = await OwnerMembershipIdAsync(org);

        var transfer = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/members/transfer-ownership",
            new TransferOwnershipRequest(successor));
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);

        var rows = await org.Owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{org.Id}/members");
        Assert.NotNull(rows);
        Assert.Single(rows, row => row.Role == "Owner");
        Assert.Equal("Owner", rows.Single(row => row.MembershipId == successor).Role);

        // The outgoing Owner stays on as an Admin rather than being stranded.
        Assert.Equal(
            "Admin",
            rows.Single(row => row.MembershipId == ownerMembership).Role);
    }

    [Fact]
    public async Task TransferOwnership_IsTheOwnersAlone()
    {
        var org = await NewOrganisationAsync("transfer-guard", memberCount: 2);
        var adminMembership = await MembershipIdOfAsync(org, index: 0);
        await PromoteToAdminAsync(org, adminMembership);
        var target = await MembershipIdOfAsync(org, index: 1);

        var byAdmin = await org.Members[0].PostAsJsonAsync(
            $"/api/organisations/{org.Id}/members/transfer-ownership",
            new TransferOwnershipRequest(target));
        Assert.Equal(HttpStatusCode.Forbidden, byAdmin.StatusCode);

        var byMember = await org.Members[1].PostAsJsonAsync(
            $"/api/organisations/{org.Id}/members/transfer-ownership",
            new TransferOwnershipRequest(adminMembership));
        Assert.Equal(HttpStatusCode.Forbidden, byMember.StatusCode);
    }

    [Fact]
    public async Task TransferOwnership_ToYourself_IsRejected()
    {
        var org = await NewOrganisationAsync("transfer-self", memberCount: 1);
        var ownerMembership = await OwnerMembershipIdAsync(org);

        var attempt = await org.Owner.PostAsJsonAsync(
            $"/api/organisations/{org.Id}/members/transfer-ownership",
            new TransferOwnershipRequest(ownerMembership));

        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    [Fact]
    public async Task MembershipsOfOtherOrganisations_AreOutOfReach()
    {
        var first = await NewOrganisationAsync("reach-first", memberCount: 1);
        var second = await NewOrganisationAsync("reach-second", memberCount: 1);
        var foreignMembership = await MembershipIdOfAsync(second, index: 0);

        var attempt = await first.Owner.PatchAsJsonAsync(
            $"/api/organisations/{first.Id}/members/{foreignMembership}",
            new UpdateMemberRequest("Admin", null));

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
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

        var members = new List<HttpClient>();
        if (memberCount > 0)
        {
            var inviteResponse = await owner.PostAsJsonAsync(
                $"/api/organisations/{organisation.Id}/invitations",
                new CreateInvitationRequest(null));
            inviteResponse.EnsureSuccessStatusCode();
            var invitation =
                await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
            Assert.NotNull(invitation);

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

        return new TestOrganisation(organisation.Id, owner, members);
    }

    private static async Task<List<MemberResponse>> DirectoryAsync(TestOrganisation org)
    {
        var rows = await org.Owner.GetFromJsonAsync<List<MemberResponse>>(
            $"/api/organisations/{org.Id}/members");
        Assert.NotNull(rows);
        return rows;
    }

    /// <summary>
    /// The nth joiner's membership id. The directory is ordered by join time
    /// and the creator joined first, so skipping one row lines up with
    /// <see cref="TestOrganisation.Members"/> regardless of later role changes.
    /// </summary>
    private static async Task<Guid> MembershipIdOfAsync(
        TestOrganisation org,
        int index)
    {
        var rows = await DirectoryAsync(org);
        return rows.Skip(1).ElementAt(index).MembershipId;
    }

    private static async Task<Guid> OwnerMembershipIdAsync(TestOrganisation org)
    {
        var rows = await DirectoryAsync(org);
        return rows.Single(row => row.Role == "Owner").MembershipId;
    }

    private static async Task<string> RoleOfAsync(TestOrganisation org, Guid membershipId)
    {
        var rows = await DirectoryAsync(org);
        return rows.Single(row => row.MembershipId == membershipId).Role;
    }

    private static async Task<bool> CanManageFinancesAsync(
        TestOrganisation org,
        Guid membershipId)
    {
        var rows = await DirectoryAsync(org);
        return rows.Single(row => row.MembershipId == membershipId).CanManageFinances;
    }

    private static async Task PromoteToAdminAsync(TestOrganisation org, Guid membershipId)
    {
        var response = await org.Owner.PatchAsJsonAsync(
            $"/api/organisations/{org.Id}/members/{membershipId}",
            new UpdateMemberRequest("Admin", null));
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }
}
