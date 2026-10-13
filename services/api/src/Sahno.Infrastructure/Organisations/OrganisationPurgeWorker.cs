using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sahno.Application.Organisations;

namespace Sahno.Infrastructure.Organisations;

/// <summary>
/// Removes for good the organisations whose restore window has passed
/// (D-101). Hourly is plenty: the window is thirty days, and a pass that
/// fails is simply caught by the next.
/// </summary>
public sealed class OrganisationPurgeWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OrganisationPurgeWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<OrganisationService>();
                var purged = await service.PurgeExpiredAsync(stoppingToken);
                if (purged > 0)
                {
                    logger.LogInformation("Purged {Count} deleted organisation(s)", purged);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Organisation purge pass failed");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
