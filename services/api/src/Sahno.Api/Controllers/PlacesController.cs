using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sahno.Api.RateLimiting;
using Sahno.Application.Places;
using Sahno.Contracts.Places;

namespace Sahno.Api.Controllers;

/// <summary>
/// Venue suggestions while typing (D-102). Signed-in people only, and rate
/// limited per account: every call is billed by Google.
/// </summary>
[ApiController]
[Route("api/places")]
[Authorize]
public sealed class PlacesController(IPlaceSuggestions places) : ControllerBase
{
    private const int MinimumInput = 3;
    private const int MaximumInput = 200;

    [HttpGet("autocomplete")]
    [EnableRateLimiting(RateLimitPolicies.PlaceSuggestions)]
    [ProducesResponseType<IReadOnlyList<PlaceSuggestionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IReadOnlyList<PlaceSuggestionResponse>>> Autocomplete(
        [FromQuery] string? input,
        [FromQuery] string? sessionToken,
        [FromQuery] string? regionCode,
        CancellationToken cancellationToken)
    {
        var typed = input?.Trim() ?? string.Empty;
        // Too short to be worth a billed lookup, or too long to be a venue.
        if (typed.Length < MinimumInput || typed.Length > MaximumInput)
        {
            return Ok(Array.Empty<PlaceSuggestionResponse>());
        }

        var suggestions = await places.SuggestAsync(
            typed,
            sessionToken is { Length: > 0 and <= 64 } ? sessionToken : null,
            regionCode is { Length: 2 } && regionCode.All(char.IsAsciiLetter) ? regionCode.ToUpperInvariant() : null,
            cancellationToken);

        return Ok(suggestions
            .Select(place => new PlaceSuggestionResponse(place.Name, place.Address, place.Text))
            .ToList());
    }
}
