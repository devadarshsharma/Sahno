using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Sahno.Contracts.Health;

namespace Sahno.Api.IntegrationTests;

public sealed class HealthEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WhenApiStarts_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload =
            await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload.Status);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}