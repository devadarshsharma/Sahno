using System.Net;
using System.Net.Http.Json;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Contracts.Organisations;
using Sahno.Contracts.Users;

namespace Sahno.Api.IntegrationTests;

/// <summary>One verified email, one account, whichever way you sign in (D-131).</summary>
public sealed class AccountLinkingTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task ANewSignIn_WithTheSameVerifiedEmail_OpensTheSameAccount()
    {
        using var google = CreateClient("google-oauth2|linking-1", "Linking.One@example.com", verified: null);
        google.DefaultRequestHeaders.Add(TestAuthDefaults.NameHeader, "Linda Ng");
        var organisation = await CreateOrganisationAsync(google, "Linked Org");

        // The emailed code to the same address, in another case: same account, same group.
        using var emailCode = CreateClient("email|linking-1", "linking.one@example.com", verified: null);
        emailCode.DefaultRequestHeaders.Add(TestAuthDefaults.NameHeader, "linking.one@example.com");
        var organisations = await emailCode.GetFromJsonAsync<List<OrganisationResponse>>("/api/organisations");
        Assert.Contains(organisations!, row => row.Id == organisation.Id);

        // Its "name" is only the address: the Google name stays.
        var me = await emailCode.GetFromJsonAsync<MeResponse>("/api/me");
        Assert.Equal("Linda Ng", me!.DisplayName);

        // And it stays that way on later requests.
        var again = await emailCode.GetFromJsonAsync<List<OrganisationResponse>>("/api/organisations");
        Assert.Contains(again!, row => row.Id == organisation.Id);
    }

    [Fact]
    public async Task AnUnverifiedEmail_NeverOpensSomebodyElsesAccount()
    {
        using var owner = CreateClient("google-oauth2|linking-2", "linking.two@example.com", verified: null);
        await CreateOrganisationAsync(owner, "Private Org");

        // Says it is unverified, explicitly.
        using var claimed = CreateClient("apple|linking-2", "linking.two@example.com", verified: "false");
        Assert.Empty((await claimed.GetFromJsonAsync<List<OrganisationResponse>>("/api/organisations"))!);

        // No claim, and a connection that is not trusted to have verified it.
        using var other = CreateClient("auth0|linking-2", "linking.two@example.com", verified: null);
        Assert.Empty((await other.GetFromJsonAsync<List<OrganisationResponse>>("/api/organisations"))!);
    }

    [Fact]
    public async Task AVerifiedClaim_IsHonouredForAnyConnection()
    {
        using var owner = CreateClient("email|linking-3", "linking.three@example.com", verified: null);
        var organisation = await CreateOrganisationAsync(owner, "Apple Org");

        using var apple = CreateClient("apple|linking-3", "linking.three@example.com", verified: "true");
        var organisations = await apple.GetFromJsonAsync<List<OrganisationResponse>>("/api/organisations");
        Assert.Contains(organisations!, row => row.Id == organisation.Id);
    }

    private HttpClient CreateClient(string subject, string email, string? verified)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(TestAuthDefaults.EmailHeader, email);
        if (verified is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthDefaults.EmailVerifiedHeader, verified);
        }

        return client;
    }

    private static async Task<OrganisationResponse> CreateOrganisationAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/organisations", new CreateOrganisationRequest(name, null, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrganisationResponse>())!;
    }
}
