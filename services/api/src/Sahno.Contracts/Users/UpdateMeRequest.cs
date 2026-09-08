namespace Sahno.Contracts.Users;

/// <summary>
/// Sets the person's own display name (D-046 — required, and the only
/// identity detail onboarding collects).
/// </summary>
public sealed record UpdateMeRequest(string DisplayName);
