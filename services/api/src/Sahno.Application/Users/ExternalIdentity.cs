namespace Sahno.Application.Users;

/// <summary>
/// The authenticated external identity presented to the application layer.
/// The API layer translates the validated security principal into this input;
/// nothing here depends on a specific identity provider.
/// </summary>
public sealed record ExternalIdentity(
    string Subject,
    string? Email,
    string? DisplayName);
