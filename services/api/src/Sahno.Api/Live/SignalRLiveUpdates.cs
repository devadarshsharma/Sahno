using Microsoft.AspNetCore.SignalR;
using Sahno.Application.Notifications;

namespace Sahno.Api.Live;

/// <summary>
/// Delivers the bare "changed" signal to an organisation's group, and a
/// "notification" — payload and all — to one person's own group.
/// </summary>
public sealed class SignalRLiveUpdates(IHubContext<LiveHub> hub) : ILiveUpdates
{
    public Task OrganisationChangedAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return hub.Clients
            .Group(LiveHub.GroupFor(organisationId))
            .SendAsync(LiveHub.ChangedEvent, cancellationToken);
    }

    public Task NotificationCreatedAsync(
        Guid recipientUserId,
        NotificationPayload payload,
        CancellationToken cancellationToken)
    {
        return hub.Clients
            .Group(LiveHub.GroupForUser(recipientUserId))
            .SendAsync(LiveHub.NotificationEvent, payload, cancellationToken);
    }
}
