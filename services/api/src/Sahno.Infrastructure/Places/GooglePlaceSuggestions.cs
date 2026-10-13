using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sahno.Application.Places;
using Sahno.Domain.Engagements;

namespace Sahno.Infrastructure.Places;

public sealed class PlacesOptions
{
    public const string SectionName = "Places";

    /// <summary>
    /// Google Maps Platform key with the Places API (New) enabled, restricted
    /// to that API. A secret: it lives on the server, never in the app, so it
    /// cannot be lifted from an app download and billed. Unset, venue
    /// suggestions are off and the field is plain text.
    /// </summary>
    public string? GoogleApiKey { get; set; }
}

/// <summary>
/// Venue suggestions from Google's Places Autocomplete (New) (D-102). Only the
/// prediction text is asked for (the field mask), which keeps each request on
/// the cheapest Autocomplete SKU; nothing is fetched about the place itself.
/// </summary>
public sealed class GooglePlaceSuggestions(
    HttpClient httpClient,
    IOptions<PlacesOptions> options,
    ILogger<GooglePlaceSuggestions> logger) : IPlaceSuggestions
{
    private const string FieldMask =
        "suggestions.placePrediction.text.text,suggestions.placePrediction.structuredFormat";

    public async Task<IReadOnlyList<PlaceSuggestion>> SuggestAsync(
        string input,
        string? sessionToken,
        string? regionCode,
        CancellationToken cancellationToken)
    {
        var key = options.Value.GoogleApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            return [];
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            // "places:autocomplete" alone would parse as an absolute URI with
            // the scheme "places"; the "./" keeps it relative to the base.
            new Uri("./places:autocomplete", UriKind.Relative))
        {
            Content = JsonContent.Create(new
            {
                input,
                // Groups one person's keystrokes into one search for billing.
                sessionToken,
                // Biases suggestions towards, and formats them for, the
                // person's country without excluding a venue abroad.
                regionCode,
            }),
        };
        request.Headers.Add("X-Goog-Api-Key", key);
        request.Headers.Add("X-Goog-FieldMask", FieldMask);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Place suggestions failed with {Status}", (int)response.StatusCode);
                return [];
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);
            return Parse(document.RootElement);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException
                                          && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Place suggestions unavailable");
            return [];
        }
    }

    private static List<PlaceSuggestion> Parse(JsonElement root)
    {
        var suggestions = new List<PlaceSuggestion>();
        if (!root.TryGetProperty("suggestions", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return suggestions;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("placePrediction", out var prediction))
            {
                continue;
            }

            var text = TextOf(prediction, "text");
            var name = prediction.TryGetProperty("structuredFormat", out var format)
                ? TextOf(format, "mainText")
                : null;
            var address = format.ValueKind == JsonValueKind.Object ? TextOf(format, "secondaryText") : null;
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var line = (text ?? (address is null ? name! : $"{name}, {address}")).Trim();
            suggestions.Add(new PlaceSuggestion(
                name ?? line,
                address,
                line.Length > Engagement.VenueMaxLength ? line[..Engagement.VenueMaxLength] : line));
        }

        return suggestions;
    }

    private static string? TextOf(JsonElement parent, string property) =>
        parent.TryGetProperty(property, out var value)
        && value.TryGetProperty("text", out var text)
        && text.ValueKind == JsonValueKind.String
            ? text.GetString()
            : null;
}
