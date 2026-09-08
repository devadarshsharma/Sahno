namespace Sahno.Contracts.Users;

/// <summary>
/// Updates the caller's own account. A null field is left as it is; a blank
/// <see cref="PhoneNumber"/> clears it. The display name is required by D-046
/// and so cannot be cleared, only replaced.
/// </summary>
public sealed record UpdateMeRequest(string? DisplayName, string? PhoneNumber);
