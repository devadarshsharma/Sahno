using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Users;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Api.IntegrationTests;

public sealed class MeEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task GetMe_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAuthenticatedIdentity_CreatesSahnoUser()
    {
        using var client = CreateAuthenticatedClient(
            "auth0|creates-user",
            email: "person@example.com",
            name: "A Person");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.UserId);
        Assert.Equal("person@example.com", payload.Email);
        Assert.Equal("A Person", payload.DisplayName);
    }

    [Fact]
    public async Task GetMe_RepeatedRequests_ReturnSameUserWithoutDuplicates()
    {
        const string subject = "auth0|repeat-requests";
        using var client = CreateAuthenticatedClient(
            subject,
            email: "repeat@example.com",
            name: "Repeat Person");

        var first = await client.GetFromJsonAsync<MeResponse>("/api/me");
        var second = await client.GetFromJsonAsync<MeResponse>("/api/me");
        var third = await client.GetFromJsonAsync<MeResponse>("/api/me");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);
        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(first.UserId, third.UserId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SahnoDbContext>();
        var count = await dbContext.Users.CountAsync(
            user => user.ExternalSubject == subject);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetMe_WithoutOptionalClaims_StillCreatesUser()
    {
        using var client = CreateAuthenticatedClient("auth0|no-optional-claims");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.UserId);
        Assert.Null(payload.Email);
        Assert.Null(payload.DisplayName);
    }

    /// <summary>
    /// The passwordless email connection sets the provider "name" claim to the
    /// email address itself. That is not a name, and showing it would expose an
    /// address D-018 keeps private, so it is reported as no name at all.
    /// </summary>
    [Fact]
    public async Task GetMe_WhenProviderNameEchoesTheEmail_ReportsNoDisplayName()
    {
        using var client = CreateAuthenticatedClient(
            "email|echoes-address",
            email: "echo@example.com",
            name: "echo@example.com");

        var payload = await client.GetFromJsonAsync<MeResponse>("/api/me");

        Assert.NotNull(payload);
        Assert.Equal("echo@example.com", payload.Email);
        Assert.Null(payload.DisplayName);
    }

    [Fact]
    public async Task PatchMe_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            "/api/me",
            new UpdateMeRequest("A Person", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchMe_SetsDisplayName()
    {
        using var client = CreateAuthenticatedClient(
            "email|sets-name",
            email: "sets@example.com",
            name: "sets@example.com");

        var response = await client.PatchAsJsonAsync(
            "/api/me",
            new UpdateMeRequest("  Adarsh Sharma  ", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Adarsh Sharma", payload.DisplayName);

        var reread = await client.GetFromJsonAsync<MeResponse>("/api/me");
        Assert.NotNull(reread);
        Assert.Equal("Adarsh Sharma", reread.DisplayName);
    }

    /// <summary>
    /// Every login carries the provider hint again. Without precedence for the
    /// chosen name, the next request would silently revert it to the email.
    /// </summary>
    [Fact]
    public async Task PatchMe_ChosenName_SurvivesLaterLoginsCarryingTheProviderHint()
    {
        using var client = CreateAuthenticatedClient(
            "email|keeps-chosen-name",
            email: "keeps@example.com",
            name: "keeps@example.com");

        await client.PatchAsJsonAsync("/api/me", new UpdateMeRequest("Real Person", null));

        var afterNextLogin = await client.GetFromJsonAsync<MeResponse>("/api/me");

        Assert.NotNull(afterNextLogin);
        Assert.Equal("Real Person", afterNextLogin.DisplayName);
    }

    [Fact]
    public async Task PatchMe_ChosenName_IsNotOverwrittenByAGenuineProviderName()
    {
        const string subject = "google-oauth2|keeps-chosen-over-provider";
        using var initial = CreateAuthenticatedClient(
            subject,
            email: "chooser@example.com",
            name: "Provider Name");

        await initial.PatchAsJsonAsync("/api/me", new UpdateMeRequest("Chosen Name", null));

        using var later = CreateAuthenticatedClient(
            subject,
            email: "chooser@example.com",
            name: "Renamed At Provider");

        var payload = await later.GetFromJsonAsync<MeResponse>("/api/me");

        Assert.NotNull(payload);
        Assert.Equal("Chosen Name", payload.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PatchMe_WithBlankName_ReturnsBadRequest(string displayName)
    {
        using var client = CreateAuthenticatedClient("email|blank-name");

        var response = await client.PatchAsJsonAsync(
            "/api/me",
            new UpdateMeRequest(displayName, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchMe_WithOverlongName_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient("email|overlong-name");

        var response = await client.PatchAsJsonAsync(
            "/api/me",
            new UpdateMeRequest(new string('a', 201), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(
        string subject,
        string? email = null,
        string? name = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        if (email is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthDefaults.EmailHeader, email);
        }

        if (name is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthDefaults.NameHeader, name);
        }

        return client;
    }
}
