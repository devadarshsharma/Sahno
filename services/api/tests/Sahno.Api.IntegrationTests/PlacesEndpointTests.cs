using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Application.Places;
using Sahno.Contracts.Places;
using Sahno.Infrastructure.Places;

namespace Sahno.Api.IntegrationTests;

public sealed class PlacesEndpointTests(SahnoApiFactory factory)
    : IClassFixture<SahnoApiFactory>
{
    [Fact]
    public async Task WithoutAKey_SuggestionsAreOff_AndTheFieldIsPlainText()
    {
        using var client = CreateClient(factory, "auth0|places-unconfigured");

        var suggestions = await client.GetFromJsonAsync<List<PlaceSuggestionResponse>>(
            "/api/places/autocomplete?input=Hamer%20Hall");

        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task Suggestions_ComeFromTheProvider_OnlyOnceEnoughIsTyped()
    {
        var fake = new FakePlaces();
        using var configured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IPlaceSuggestions>(fake)));
        using var client = CreateClient(configured, "auth0|places-configured");

        var tooShort = await client.GetFromJsonAsync<List<PlaceSuggestionResponse>>("/api/places/autocomplete?input=Ha");
        Assert.NotNull(tooShort);
        Assert.Empty(tooShort);
        Assert.Equal(0, fake.Calls);

        var suggestions = await client.GetFromJsonAsync<List<PlaceSuggestionResponse>>(
            "/api/places/autocomplete?input=Hamer&sessionToken=abc123&regionCode=au");

        Assert.NotNull(suggestions);
        var only = Assert.Single(suggestions);
        Assert.Equal("Hamer Hall, 100 St Kilda Rd, Southbank VIC, Australia", only.Text);
        Assert.Equal(("Hamer", "abc123", "AU"), fake.Last);
    }

    [Fact]
    public async Task GoogleIsAskedForPredictionsOnly_AndTheirTextFillsTheField()
    {
        var handler = new StubHandler("""
            {
              "suggestions": [
                {
                  "placePrediction": {
                    "text": { "text": "Hamer Hall, 100 St Kilda Rd, Southbank VIC, Australia" },
                    "structuredFormat": {
                      "mainText": { "text": "Hamer Hall" },
                      "secondaryText": { "text": "100 St Kilda Rd, Southbank VIC, Australia" }
                    }
                  }
                },
                { "queryPrediction": { "text": { "text": "hamer hall parking" } } }
              ]
            }
            """);
        var places = new GooglePlaceSuggestions(
            new HttpClient(handler) { BaseAddress = new Uri("https://places.googleapis.com/v1/") },
            Options.Create(new PlacesOptions { GoogleApiKey = "test-key" }),
            NullLogger<GooglePlaceSuggestions>.Instance);

        var suggestions = await places.SuggestAsync("Hamer", "session-1", "AU", CancellationToken.None);

        var only = Assert.Single(suggestions);
        Assert.Equal("Hamer Hall", only.Name);
        Assert.Equal("100 St Kilda Rd, Southbank VIC, Australia", only.Address);
        Assert.Equal("Hamer Hall, 100 St Kilda Rd, Southbank VIC, Australia", only.Text);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://places.googleapis.com/v1/places:autocomplete", request.Uri);
        Assert.Equal("test-key", request.ApiKey);
        Assert.DoesNotContain("places.", request.FieldMask!.Replace("suggestions.placePrediction", string.Empty));
        Assert.Equal("Hamer", request.Body.GetProperty("input").GetString());
        Assert.Equal("session-1", request.Body.GetProperty("sessionToken").GetString());
    }

    [Fact]
    public async Task AGoogleFailure_IsNoSuggestions_NotAnError()
    {
        var places = new GooglePlaceSuggestions(
            new HttpClient(new StubHandler("{}", HttpStatusCode.Forbidden)) { BaseAddress = new Uri("https://places.googleapis.com/v1/") },
            Options.Create(new PlacesOptions { GoogleApiKey = "revoked" }),
            NullLogger<GooglePlaceSuggestions>.Instance);

        Assert.Empty(await places.SuggestAsync("Hamer", null, null, CancellationToken.None));
    }

    private static HttpClient CreateClient(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> app, string subject)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthDefaults.SubjectHeader, subject);
        return client;
    }

    private sealed class FakePlaces : IPlaceSuggestions
    {
        public int Calls { get; private set; }

        public (string Input, string? Session, string? Region) Last { get; private set; }

        public Task<IReadOnlyList<PlaceSuggestion>> SuggestAsync(
            string input,
            string? sessionToken,
            string? regionCode,
            CancellationToken cancellationToken)
        {
            Calls++;
            Last = (input, sessionToken, regionCode);
            return Task.FromResult<IReadOnlyList<PlaceSuggestion>>(
                [new PlaceSuggestion("Hamer Hall", "100 St Kilda Rd, Southbank VIC, Australia", "Hamer Hall, 100 St Kilda Rd, Southbank VIC, Australia")]);
        }
    }

    private sealed record SeenRequest(string Uri, string? ApiKey, string? FieldMask, JsonElement Body);

    private sealed class StubHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<SeenRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? default
                : JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellationToken));
            Requests.Add(new SeenRequest(
                request.RequestUri!.ToString(),
                request.Headers.TryGetValues("X-Goog-Api-Key", out var key) ? key.Single() : null,
                request.Headers.TryGetValues("X-Goog-FieldMask", out var mask) ? mask.Single() : null,
                body));
            return new HttpResponseMessage(status) { Content = new StringContent(responseJson) };
        }
    }
}
