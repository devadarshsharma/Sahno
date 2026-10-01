using Microsoft.EntityFrameworkCore;
using Sahno.Application.Notifications;
using Sahno.Domain.Notifications;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Notifications;

public sealed class ReminderLogStore(SahnoDbContext dbContext) : IReminderLogStore
{
    public Task<bool> WasSentAsync(
        ReminderKind kind,
        Guid subjectId,
        DateOnly occasionDate,
        CancellationToken cancellationToken)
    {
        return dbContext.ReminderLogs.AnyAsync(
            log => log.Kind == kind
                && log.SubjectId == subjectId
                && log.OccasionDate == occasionDate,
            cancellationToken);
    }

    public void Stage(ReminderLog log)
    {
        dbContext.ReminderLogs.Add(log);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
