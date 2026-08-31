using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Organisations;
using Sahno.Domain.Organisations;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Api.IntegrationTests;

public sealed class OrganisationEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task CreateOrganisation_MakesCallerTheOwner()
    {
        using var client = CreateClient("auth0|org-owner-1");

        var createResponse = await client.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest("Australian Qawwal Party", "music", "Australia/Melbourne"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(created);
        Assert.Equal("Owner", created.Role);
        Assert.True(created.ShowSetupChecklist);
        Assert.Equal("Australia/Melbourne", created.TimeZoneId);

        var list = await client.GetFromJsonAsync<List<OrganisationResponse>>(
            "/api/organisations");
        Assert.NotNull(list);
        Assert.Contains(list, organisation => organisation.Id == created.Id);
    }

    [Fact]
    public async Task Organisations_AreIsolatedBetweenAccounts()
    {
        using var alice = CreateClient("auth0|isolation-alice");
        using var bob = CreateClient("auth0|isolation-bob");

        var aliceOrg = await CreateOrganisationAsync(alice, "Alice Choir");

        var bobList = await bob.GetFromJsonAsync<List<OrganisationResponse>>(
            "/api/organisations");
        Assert.NotNull(bobList);
        Assert.DoesNotContain(bobList, organisation => organisation.Id == aliceOrg.Id);

        // Non-members receive 404 — organisation existence is not disclosed.
        var direct = await bob.GetAsync($"/api/organisations/{aliceOrg.Id}");
        Assert.Equal(HttpStatusCode.NotFound, direct.StatusCode);
    }

    [Fact]
    public async Task InvitationFlow_PreviewAcceptAndIdempotentRejoin()
    {
        using var owner = CreateClient("auth0|invite-owner");
        using var joiner = CreateClient("auth0|invite-joiner");

        var organisation = await CreateOrganisationAsync(owner, "Invite Flow Org");

        var inviteResponse = await owner.PostAsJsonAsync(
            $"/api/organisations/{organisation.Id}/invitations",
            new CreateInvitationRequest(null));
        Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
        var invitation = await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(invitation);

        var preview = await joiner.GetFromJsonAsync<InvitationPreviewResponse>(
            $"/api/invitations/{invitation.Token}");
        Assert.NotNull(preview);
        Assert.Equal("Invite Flow Org", preview.OrganisationName);

        var accept = await joiner.PostAsync(
            $"/api/invitations/{invitation.Token}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var joined = await accept.Content.ReadFromJsonAsync<AcceptInvitationResponse>();
        Assert.NotNull(joined);
        Assert.Equal("Member", joined.Role);

        // Accepting again is idempotent and the link stays usable (multi-use).
        var acceptAgain = await joiner.PostAsync(
            $"/api/invitations/{invitation.Token}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.OK, acceptAgain.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SahnoDbContext>();
        var membershipCount = await dbContext.Memberships.CountAsync(
            membership => membership.OrganisationId == organisation.Id);
        Assert.Equal(2, membershipCount);
    }

    [Fact]
    public async Task Member_CannotCreateInvitations()
    {
        using var owner = CreateClient("auth0|perm-owner");
        using var member = CreateClient("auth0|perm-member");

        var organisation = await CreateOrganisationAsync(owner, "Permissions Org");
        var invitation = await CreateInvitationAsync(owner, organisation.Id);

        var accept = await member.PostAsync(
            $"/api/invitations/{invitation.Token}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);

        var attempt = await member.PostAsJsonAsync(
            $"/api/organisations/{organisation.Id}/invitations",
            new CreateInvitationRequest(null));
        Assert.Equal(HttpStatusCode.Forbidden, attempt.StatusCode);
    }

    [Fact]
    public async Task RevokedAndExpiredInvitations_AreRejected()
    {
        using var owner = CreateClient("auth0|revoke-owner");
        using var joiner = CreateClient("auth0|revoke-joiner");

        var organisation = await CreateOrganisationAsync(owner, "Revocation Org");

        var revocable = await CreateInvitationAsync(owner, organisation.Id);
        var revoke = await owner.DeleteAsync(
            $"/api/organisations/{organisation.Id}/invitations/{revocable.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var preview = await joiner.GetAsync($"/api/invitations/{revocable.Token}");
        Assert.Equal(HttpStatusCode.NotFound, preview.StatusCode);

        var expiredResponse = await owner.PostAsJsonAsync(
            $"/api/organisations/{organisation.Id}/invitations",
            new CreateInvitationRequest(DateTimeOffset.UtcNow.AddMinutes(-1)));
        var expired = await expiredResponse.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(expired);

        var expiredAccept = await joiner.PostAsync(
            $"/api/invitations/{expired.Token}/accept",
            content: null);
        Assert.Equal(HttpStatusCode.NotFound, expiredAccept.StatusCode);
    }

    [Fact]
    public async Task Database_RejectsASecondOwner()
    {
        using var owner = CreateClient("auth0|single-owner");
        var organisation = await CreateOrganisationAsync(owner, "Single Owner Org");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SahnoDbContext>();

        var rogueUser = Sahno.Domain.Users.User.Create(
            "auth0|rogue-second-owner",
            null,
            null);
        dbContext.Users.Add(rogueUser);
        await dbContext.SaveChangesAsync();

        dbContext.Memberships.Add(
            Membership.CreateOwner(organisation.Id, rogueUser.Id));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    private HttpClient CreateClient(string subject)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }

    private static async Task<OrganisationResponse> CreateOrganisationAsync(
        HttpClient client,
        string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/organisations",
            new CreateOrganisationRequest(name, null, null));
        response.EnsureSuccessStatusCode();
        var organisation = await response.Content.ReadFromJsonAsync<OrganisationResponse>();
        Assert.NotNull(organisation);
        return organisation;
    }

    private static async Task<InvitationResponse> CreateInvitationAsync(
        HttpClient client,
        Guid organisationId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/organisations/{organisationId}/invitations",
            new CreateInvitationRequest(null));
        response.EnsureSuccessStatusCode();
        var invitation = await response.Content.ReadFromJsonAsync<InvitationResponse>();
        Assert.NotNull(invitation);
        return invitation;
    }
}
