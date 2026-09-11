using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Users;

namespace Sahno.Api.Live;

/// <summary>
/// One connection per app session, one group per organisation
/// (TECHNICAL_ARCHITECTURE: accepted realtime architecture).
///
/// The only thing sent down it is "changed", and the only thing checked on
/// the way in is membership — a connection asks to join an organisation, and
/// is refused unless the caller belongs to it. There are no hub methods to
/// call: the client listens, refetches over REST, and REST decides what it may
/// see. Nothing here needs to know what the change was.
/// </summary>
[Authorize]
public sealed class LiveHub(
    EnsureUserService ensureUserService,
    OrganisationAuthorizationService authorization,
    ILogger<LiveHub> logger) : Hub
{
    public const string Path = "/hubs/live";
    public const string ChangedEvent = "changed";

    public static string GroupFor(Guid organisationId) => $"org:{organisationId}";

    public override async Task OnConnectedAsync()
    {
        var organisationId = ParseOrganisation();
        var identity = Context.User?.ToExternalIdentity();
        if (organisationId is null || identity is null)
        {
            Context.Abort();
            return;
        }

        var user = await ensureUserService.EnsureAsync(identity, Context.ConnectionAborted);
        var membership = await authorization.FindMembershipAsync(
            organisationId.Value,
            user.Id,
            Context.ConnectionAborted);

        if (membership is null)
        {
            // Not a member. The same silence a non-member gets from every
            // other endpoint of this organisation.
            logger.LogInformation(
                "Live connection refused: user {UserId} is not in organisation {OrganisationId}",
                user.Id,
                organisationId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupFor(organisationId.Value),
            Context.ConnectionAborted);

        // One line per join is worth having: it is how an operator sees that
        // live updates are reaching devices at all.
        logger.LogInformation(
            "Live connection joined organisation {OrganisationId} for user {UserId}",
            organisationId,
            user.Id);

        await base.OnConnectedAsync();
    }

    private Guid? ParseOrganisation()
    {
        var raw = Context.GetHttpContext()?.Request.Query["organisationId"].ToString();
        return Guid.TryParse(raw, out var organisationId) ? organisationId : null;
    }
}
