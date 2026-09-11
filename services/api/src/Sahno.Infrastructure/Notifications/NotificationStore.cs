using Microsoft.EntityFrameworkCore;
using Sahno.Application.Notifications;
using Sahno.Domain.Notifications;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Notifications;

public sealed class NotificationStore(SahnoDbContext dbContext) : INotificationStore
{
    public async Task<IReadOnlyList<Notification>> ListForUserAsync(
        Guid organisationId,
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.OrganisationId == organisationId
                && notification.RecipientUserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Notifications.CountAsync(
            notification =>
                notification.OrganisationId == organisationId
                && notification.RecipientUserId == userId
                && notification.ReadAtUtc == null,
            cancellationToken);
    }

    public Task<Notification?> FindAsync(
        Guid organisationId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Notifications.FirstOrDefaultAsync(
            notification =>
                notification.Id == notificationId
                && notification.OrganisationId == organisationId
                && notification.RecipientUserId == userId,
            cancellationToken);
    }

    public void Stage(IReadOnlyList<Notification> notifications)
    {
        dbContext.Notifications.AddRange(notifications);
    }

    public Task MarkAllReadAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return dbContext.Notifications
            .Where(notification =>
                notification.OrganisationId == organisationId
                && notification.RecipientUserId == userId
                && notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    notification => notification.ReadAtUtc,
                    now),
                cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
