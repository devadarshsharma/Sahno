namespace Sahno.Application.Users;

/// <summary>
/// The authenticated external identity presented to the application layer.
/// The API layer translates the validated security principal into this input;
/// nothing here depends on a specific identity provider.
/// <see cref="EmailVerified"/> says the provider has proved the person owns
/// <see cref="Email"/> — the only case in which a new sign-in may open an
/// existing account with that address (D-131).
/// </summary>
public sealed record ExternalIdentity(
    string Subject,
    string? Email,
    string? DisplayName,
    bool EmailVerified = false);
