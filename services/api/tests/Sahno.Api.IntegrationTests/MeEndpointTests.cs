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
