using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sahno.Api.Authentication;
using Sahno.Api.Health;
using Sahno.Application.Users;
using Sahno.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("Sahno")
    ?? throw new InvalidOperationException(
        "Connection string 'Sahno' is not configured.");

builder.Services.AddControllers();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<EnsureUserService>();
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql");
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Auth0-issued JWT bearer authentication. Real tenant values come from user
// secrets or environment variables — never from committed configuration.
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];
var auth0Configured =
    !string.IsNullOrWhiteSpace(auth0Domain)
    && !string.IsNullOrWhiteSpace(auth0Audience);

if (auth0Configured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            // Keep raw JWT claim names ("sub", "email", "name").
            options.MapInboundClaims = false;
            // Issuer, audience, signature, and lifetime validation are all on
            // by default; stated explicitly so a future change is a visible,
            // reviewable decision.
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = auth0Audience,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
            };
        });
}
else
{
    // Fail closed: without Auth0 configuration no token is ever accepted,
    // while unauthenticated endpoints (health) keep working.
    builder.Services
        .AddAuthentication(UnconfiguredAuthenticationDefaults.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(
            UnconfiguredAuthenticationDefaults.SchemeName,
            displayName: null,
            configureOptions: null);
}

builder.Services.AddAuthorization();

var app = builder.Build();

if (!auth0Configured)
{
    app.Logger.LogWarning(
        "Auth0 is not configured (missing Auth0:Domain and/or Auth0:Audience). "
        + "Authenticated endpoints will reject all requests. "
        + "See docs/LOCAL_DEVELOPMENT.md for setup.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program
{
}
