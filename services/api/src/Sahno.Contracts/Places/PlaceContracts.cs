namespace Sahno.Contracts.Places;

/// <summary>A venue suggestion (D-102). <c>Text</c> is what fills the field.</summary>
public sealed record PlaceSuggestionResponse(string Name, string? Address, string Text);
