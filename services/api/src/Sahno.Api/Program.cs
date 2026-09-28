using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Sahno.Api.Authentication;
using Sahno.Api.Health;
using Sahno.Api.Live;
using Sahno.Application.Engagements;
using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Application.Repertoire;
using Sahno.Application.Users;
using Sahno.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Hosts such as Render and DigitalOcean App Platform tell the container which
// port to listen on through PORT. Honour it when present; otherwise the usual
// ASP.NET Core settings (launchSettings, ASPNETCORE_URLS/HTTP_PORTS) apply.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("Sahno")
    ?? throw new InvalidOperationException(
        "Connection string 'Sahno' is not configured.");

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<ILiveUpdates, SignalRLiveUpdates>();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<EnsureUserService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<OrganisationService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<RepertoireService>();
builder.Services.AddScoped<SetListService>();
builder.Services.AddScoped<EngagementService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<ReadinessService>();
builder.Services.AddScoped<ResponsibilityService>();
builder.Services.AddScoped<PreparationService>();
builder.Services.AddScoped<DiscussionService>();
builder.Services.AddScoped<CommercialService>();
builder.Services.AddScoped<Notifier>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PushDeviceService>();
builder.Services.AddScoped<OrganisationAuthorizationService>();
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
            // A browser or phone opening a websocket cannot set headers, so
            // SignalR sends the token as a query parameter. Accepted for the
            // hub path only — nowhere else does a token in a URL count.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken)
                        && context.HttpContext.Request.Path.StartsWithSegments(LiveHub.Path))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
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

// Browser origins allowed to call the API. The phone app is not a browser and
// is never subject to CORS, so by default the list is empty and no
// cross-origin browser call is allowed at all. Add an origin (for example the
// Expo web dev server) through Cors__AllowedOrigins__0 and friends.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // SignalR's negotiate request from a browser carries credentials.
        .AllowCredentials()));
}

var app = builder.Build();

if (!auth0Configured)
{
    app.Logger.LogWarning(
        "Auth0 is not configured (missing Auth0:Domain and/or Auth0:Audience). "
        + "Authenticated endpoints will reject all requests. "
        + "See docs/LOCAL_DEVELOPMENT.md for setup.");
}

// Configure the HTTP request pipeline.
// The OpenAPI document describes endpoints and shapes only — no secrets — so
// a test deployment may publish it with OpenApi__Enabled=true. Production
// leaves it off.
if (app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
}

// Behind a TLS-terminating proxy (Render, DigitalOcean) the request reaches
// Kestrel as plain HTTP. Hosted environments set
// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true, which makes ASP.NET Core honour
// X-Forwarded-Proto/For before anything else runs, so the request is seen as
// the HTTPS it was and this redirect never loops.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authorised responses must never be cached by any intermediary or client
// HTTP layer — account data changes with the bearer token, not the URL.
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next();
});

if (allowedOrigins.Length > 0)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LiveHub>(LiveHub.Path);

// Liveness: the process is up and serving. No checks run, so a database blip
// does not make the host restart a healthy container — that is what the
// readiness endpoint below is for. Both answer with a bare status word.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

app.Run();

public partial class Program
{
}
