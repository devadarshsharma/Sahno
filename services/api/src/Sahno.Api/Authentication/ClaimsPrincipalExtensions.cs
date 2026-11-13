using System.Security.Claims;
using Sahno.Application.Users;

namespace Sahno.Api.Authentication;

/// <summary>
/// The single place token claims are translated into the application-level
/// external identity. The subject always comes from the validated token —
/// never from client-supplied values. Email and name prefer the namespaced
/// claims added by the Auth0 post-login Action (access tokens carry no plain
/// profile claims), falling back to standard claim names for the test scheme.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string Namespace = "https://sahno.app/";

    /// <summary>
    /// Connections that only ever hand over an address the person has proved
    /// they own: the emailed code is that proof, and Google signs in only with
    /// an address Google has verified. Used when the token carries no
    /// email_verified claim of its own (D-131).
    /// </summary>
    private static readonly string[] VerifiedEmailConnections = ["email|", "google-oauth2|"];

    public static ExternalIdentity? ToExternalIdentity(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var verifiedClaim = principal.FindFirst(Namespace + "email_verified")?.Value
            ?? principal.FindFirst("email_verified")?.Value;
        var emailVerified = verifiedClaim is not null
            ? string.Equals(verifiedClaim, "true", StringComparison.OrdinalIgnoreCase)
            : VerifiedEmailConnections.Any(prefix => subject.StartsWith(prefix, StringComparison.Ordinal));

        return new ExternalIdentity(
            subject,
            principal.FindFirst(Namespace + "email")?.Value
                ?? principal.FindFirst("email")?.Value,
            principal.FindFirst(Namespace + "name")?.Value
                ?? principal.FindFirst("name")?.Value,
            emailVerified);
    }
}
