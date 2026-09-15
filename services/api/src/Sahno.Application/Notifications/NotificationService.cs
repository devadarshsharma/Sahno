using Sahno.Application.Organisations;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Notifications;

/// <summary>
/// What the bell shows (Slice 10, D-042). Scoped to the caller's membership,
/// so a person in two organisations sees each organisation's news only while
/// they are in it (D-013).
/// </summary>
public sealed class NotificationService(
    INotificationStore notifications,
    Notifier notifier)
{
    public const int AnnouncementTitleMaxLength = 120;

    public const int PageSize = 50;

    public Task<IReadOnlyList<Notification>> ListAsync(
        Membership actor,
        CancellationToken cancellationToken)
    {
        return notifications.ListForUserAsync(
            actor.OrganisationId,
            actor.UserId,
            PageSize,
            cancellationToken);
    }

    public Task<int> CountUnreadAsync(
        Membership actor,
        CancellationToken cancellationToken)
    {
        return notifications.CountUnreadAsync(
            actor.OrganisationId,
            actor.UserId,
            cancellationToken);
    }

    /// <summary>
    /// Marks one as read. Only the recipient's own — the store looks it up by
    /// user, so somebody else's id simply is not found.
    /// </summary>
    public async Task<bool> MarkReadAsync(
        Membership actor,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await notifications.FindAsync(
            actor.OrganisationId,
            actor.UserId,
            notificationId,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.MarkRead();
        await notifications.SaveAsync(cancellationToken);
        return true;
    }

    public Task MarkAllReadAsync(
        Membership actor,
        CancellationToken cancellationToken)
    {
        return notifications.MarkAllReadAsync(
            actor.OrganisationId,
            actor.UserId,
            cancellationToken);
    }

    /// <summary>
    /// An organiser writes to everybody (D-080). Reaches every other member
    /// in-app and by push; false when the caller is not an organiser or the
    /// title is empty, and nothing is sent.
    /// </summary>
    public async Task<bool> AnnounceAsync(
        Membership actor,
        string title,
        string? body,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor)
            || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var cleanTitle = title.Trim();
        await notifier.AnnouncementAsync(
            actor.OrganisationId,
            actor.UserId,
            cleanTitle.Length > AnnouncementTitleMaxLength
                ? cleanTitle[..AnnouncementTitleMaxLength]
                : cleanTitle,
            body,
            cancellationToken);
        await notifications.SaveAsync(cancellationToken);
        return true;
    }
}
