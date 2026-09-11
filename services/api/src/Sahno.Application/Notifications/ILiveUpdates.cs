namespace Sahno.Application.Notifications;

/// <summary>
/// Tells connected clients that something in an organisation changed.
///
/// Deliberately carries nothing else. REST stays authoritative
/// (TECHNICAL_ARCHITECTURE: accepted realtime architecture): the client's only
/// job on hearing this is to refetch what it already holds, through endpoints
/// that already enforce who may see what. A payload of ids or content would be
/// a second data path with its own authorisation to get wrong; a bare signal
/// cannot leak anything, because there is nothing in it.
/// </summary>
public interface ILiveUpdates
{
    Task OrganisationChangedAsync(Guid organisationId, CancellationToken cancellationToken);
}
