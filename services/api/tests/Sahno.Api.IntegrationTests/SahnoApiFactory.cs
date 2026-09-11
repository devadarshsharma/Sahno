using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sahno.Api.IntegrationTests.Authentication;
using Sahno.Application.Notifications;
using Sahno.Infrastructure.Notifications;
using Sahno.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Sahno.Api.IntegrationTests;

/// <summary>
/// Boots the real API against a disposable PostgreSQL container (per D-070)
/// with the test-only authentication scheme as the default. Migrations are
/// applied programmatically; the application itself never auto-migrates.
/// </summary>
public sealed class SahnoApiFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SahnoDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:Sahno",
            _postgres.GetConnectionString());

        builder.ConfigureServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthDefaults.SchemeName;
                    options.DefaultChallengeScheme = TestAuthDefaults.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthDefaults.SchemeName,
                    displayName: null,
                    configureOptions: null);

            // Email is recorded rather than sent, and the background worker is
            // removed so tests drive the outbox themselves. Otherwise a test
            // asserting "queued but not yet sent" races a timer.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<RecordingEmailSender>();
            services.AddSingleton<IEmailSender>(provider =>
                provider.GetRequiredService<RecordingEmailSender>());

            var worker = services.FirstOrDefault(descriptor =>
                descriptor.ImplementationType == typeof(OutboxWorker));
            if (worker is not null)
            {
                services.Remove(worker);
            }
        });
    }
}
