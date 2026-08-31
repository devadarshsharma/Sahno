using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Sahno.Api.Authentication;

public static class UnconfiguredAuthenticationDefaults
{
    public const string SchemeName = "Unconfigured";
}

/// <summary>
/// Fail-closed authentication scheme used only when Auth0 configuration is
/// absent (for example a fresh local checkout). Every request that requires
/// authentication is rejected with a bare 401 — no token is ever accepted —
/// while unauthenticated endpoints such as health checks keep working.
/// </summary>
public sealed class UnconfiguredAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(
            AuthenticateResult.Fail("Authentication is not configured."));
    }
}
