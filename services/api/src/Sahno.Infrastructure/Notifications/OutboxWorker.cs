using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sahno.Application.Notifications;

namespace Sahno.Infrastructure.Notifications;

/// <summary>
/// Drains the outbox in the background of the API process. Polls rather than
/// listens: at pilot scale a few seconds of latency on an email is invisible,
/// and polling survives every restart, deploy, and dropped connection without
/// any of them being handled.
///
/// Each pass gets its own scope, so the DbContext it uses is as short-lived as
/// a request's. A pass that finds work runs again straight away; an empty one
/// waits.
/// </summary>
public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            int attempted;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                attempted = await dispatcher.DispatchDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // The database being away is the usual reason. Log it, wait a
                // little longer than usual, and try again — the rows are still
                // there.
                logger.LogError(exception, "Outbox pass failed");
                await Delay(FailureDelay, stoppingToken);
                continue;
            }

            if (attempted == 0)
            {
                await Delay(IdleDelay, stoppingToken);
            }
        }
    }

    private static async Task Delay(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down; the loop condition ends things.
        }
    }
}
