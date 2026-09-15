using Sahno.Domain.Notifications;

namespace Sahno.Application.Notifications;

public interface INotificationStore
{
    /// <summary>Newest first, scoped to one person in one organisation.</summary>
    Task<IReadOnlyList<Notification>> ListForUserAsync(
        Guid organisationId,
        Guid userId,
        int limit,
        CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<Notification?> FindAsync(
        Guid organisationId,
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds rows to the current unit of work WITHOUT saving. The caller's next
    /// save commits them alongside the change they describe — that is what
    /// makes a confirmation and its notifications land together or not at all.
    /// </summary>
    void Stage(IReadOnlyList<Notification> notifications);

    Task MarkAllReadAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IOutboxStore
{
    /// <summary>Stages without saving, for the same reason as notifications.</summary>
    void Stage(IReadOnlyList<OutboxMessage> messages);

    /// <summary>
    /// Messages ready to try: unsent, not abandoned, and past their backoff.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ListDueAsync(
        int limit,
        CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IPushDeviceStore
{
    Task<PushDevice?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>Every device a person is signed in on, active ones only.</summary>
    Task<IReadOnlyList<PushDevice>> ListActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Active devices for many people at once — one query for a notification
    /// going to a whole lineup.
    /// </summary>
    Task<IReadOnlyList<PushDevice>> ListActiveForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);

    void Add(PushDevice device);

    Task RemoveAsync(Guid deviceId, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The delivery adapter. Resend in production, a logger in development; the
/// worker does not know which.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string subject,
        string textBody,
        CancellationToken cancellationToken);
}

/// <summary>What the push service said about one message.</summary>
public enum PushSendOutcome
{
    Sent,

    /// <summary>
    /// The token is dead — the app was uninstalled, or the token rotated.
    /// Retrying will never help; the device should be disabled.
    /// </summary>
    DeviceNotRegistered,

    /// <summary>Something else went wrong; worth another try later.</summary>
    Failed,
}

public sealed record PushSendResult(PushSendOutcome Outcome, string? Error = null);

/// <summary>
/// The push adapter (TECHNICAL_ARCHITECTURE: Expo push as a delivery
/// adapter). One message to one device; the dispatcher owns retries.
/// </summary>
public interface IPushSender
{
    Task<PushSendResult> SendAsync(
        string pushToken,
        string title,
        string body,
        string? dataJson,
        CancellationToken cancellationToken);
}
