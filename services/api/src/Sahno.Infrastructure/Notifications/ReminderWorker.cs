using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sahno.Application.Notifications;

namespace Sahno.Infrastructure.Notifications;

/// <summary>
/// Runs the reminder scheduler (D-085) every few minutes in the background of
/// the API process. Five minutes is close enough for "8 am on the day" and
/// light enough to be invisible; a missed pass costs nothing, because the next
/// one sends whatever is still due and the log stops anything going twice.
/// </summary>
public sealed class ReminderWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();
                var sent = await reminders.SendDueAsync(stoppingToken);
                if (sent > 0)
                {
                    logger.LogInformation("Sent {Count} scheduled reminder(s)", sent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // Usually the database being away. The next pass tries again.
                logger.LogError(exception, "Reminder pass failed");
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
