namespace Sahno.Application.Places;

/// <summary>
/// One place offered while somebody types a venue (D-102): "Hamer Hall" and
/// "100 St Kilda Rd, Southbank VIC, Australia", and the one line the venue
/// field is filled with when it is picked.
/// </summary>
public sealed record PlaceSuggestion(string Name, string? Address, string Text);

/// <summary>
/// Suggests real places for a venue as it is typed (D-102). Suggestions only:
/// the venue stays free text, so "Ali's place" or "TBC" are as valid as a
/// picked address. Answers nothing, rather than failing, when it is not
/// configured or the provider is unavailable.
/// </summary>
public interface IPlaceSuggestions
{
    Task<IReadOnlyList<PlaceSuggestion>> SuggestAsync(
        string input,
        string? sessionToken,
        string? regionCode,
        CancellationToken cancellationToken);
}
