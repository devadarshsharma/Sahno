using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sahno.Api.IntegrationTests.Authentication;

public static class TestAuthDefaults
{
    public const string SchemeName = "IntegrationTest";
    public const string SubjectHeader = "X-Test-Subject";
    public const string EmailHeader = "X-Test-Email";
    public const string NameHeader = "X-Test-Name";
}

/// <summary>
/// Test-only authentication scheme. It exists solely in this test project and
/// is registered only by <see cref="SahnoApiFactory"/>'s test host, so it can
/// never become active through normal application configuration. Identity is
/// taken from request headers, keeping every test local and deterministic —
/// no identity provider is ever contacted.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers[TestAuthDefaults.SubjectHeader].ToString();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new("sub", subject) };

        var email = Request.Headers[TestAuthDefaults.EmailHeader].ToString();
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim("email", email));
        }

        var name = Request.Headers[TestAuthDefaults.NameHeader].ToString();
        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim("name", name));
        }

        var identity = new ClaimsIdentity(claims, TestAuthDefaults.SchemeName);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            TestAuthDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
