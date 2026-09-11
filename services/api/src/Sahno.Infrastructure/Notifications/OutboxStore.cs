using Microsoft.EntityFrameworkCore;
using Sahno.Application.Notifications;
using Sahno.Domain.Notifications;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Notifications;

public sealed class OutboxStore(SahnoDbContext dbContext) : IOutboxStore
{
    public void Stage(IReadOnlyList<OutboxMessage> messages)
    {
        dbContext.OutboxMessages.AddRange(messages);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListDueAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        // Backoff is a function of two columns, so the "not before" test is
        // done here after a cheap index scan on the unsent rows, rather than
        // taught to the database.
        var unsent = await dbContext.OutboxMessages
            .Where(message =>
                message.SentAtUtc == null
                && message.AttemptCount < OutboxMessage.MaxAttempts)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(limit * 4)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return unsent
            .Where(message => message.NotBeforeUtc <= now)
            .Take(limit)
            .ToList();
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
