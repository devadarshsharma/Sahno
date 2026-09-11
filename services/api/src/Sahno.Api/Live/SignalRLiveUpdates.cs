using Microsoft.AspNetCore.SignalR;
using Sahno.Application.Notifications;

namespace Sahno.Api.Live;

/// <summary>Delivers the bare "changed" signal to an organisation's group.</summary>
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
}
