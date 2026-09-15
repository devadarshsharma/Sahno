namespace Sahno.Application.Notifications;

/// <summary>
/// Tells connected clients that something changed.
///
/// The organisation signal deliberately carries nothing. REST stays
/// authoritative (TECHNICAL_ARCHITECTURE: accepted realtime architecture):
/// the client's only job on hearing it is to refetch what it already holds,
/// through endpoints that already enforce who may see what. A payload of ids
/// or content would be a second data path with its own authorisation to get
/// wrong; a bare signal cannot leak anything, because there is nothing in it.
///
/// The notification signal is the one exception, and it is not really one:
/// it goes to the recipient alone and carries only that person's own
/// notification — the row the bell would show them anyway — so the app can
/// put a banner up the moment it lands rather than after a refetch (D-080).
/// </summary>
public interface ILiveUpdates
{
    Task OrganisationChangedAsync(Guid organisationId, CancellationToken cancellationToken);

    Task NotificationCreatedAsync(
        Guid recipientUserId,
        NotificationPayload payload,
        CancellationToken cancellationToken);
}
