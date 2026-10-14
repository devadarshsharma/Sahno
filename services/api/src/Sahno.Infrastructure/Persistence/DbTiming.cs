using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Sahno.Infrastructure.Persistence;

/// <summary>
/// What one request (one DI scope) spent in the database: how many commands
/// and how long they took, and how many connections it opened and how long
/// that took. Logged per request (D-104), so a slow screen says whether the
/// time went on queries, on connecting, or elsewhere.
/// </summary>
public sealed class DbTiming
{
    public int Commands { get; private set; }

    public TimeSpan CommandTime { get; private set; }

    public int Opens { get; private set; }

    public TimeSpan OpenTime { get; private set; }

    internal void Command(TimeSpan duration)
    {
        Commands++;
        CommandTime += duration;
    }

    internal void Open(TimeSpan duration)
    {
        Opens++;
        OpenTime += duration;
    }
}

public sealed class DbCommandTimingInterceptor(DbTiming timing) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        timing.Command(eventData.Duration);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        timing.Command(eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        timing.Command(eventData.Duration);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        timing.Command(eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        timing.Command(eventData.Duration);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        timing.Command(eventData.Duration);
        return ValueTask.FromResult(result);
    }
}

public sealed class DbConnectionTimingInterceptor(DbTiming timing) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        timing.Open(eventData.Duration);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        timing.Open(eventData.Duration);
        return Task.CompletedTask;
    }
}
