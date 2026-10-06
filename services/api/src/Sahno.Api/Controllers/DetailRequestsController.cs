using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// Performers asking organisers for an event's missing details (D-089):
/// venue, sound-check time, start time, outfit. Somebody who cannot see the
/// event gets the same 404 here that they get there.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/detail-requests")]
[Authorize]
public sealed class DetailRequestsController(
    EnsureUserService ensureUserService,
    DetailRequestService detailRequestService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DetailRequestsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetailRequestsResponse>> List(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var view = await detailRequestService.ListAsync(caller, engagementId, cancellationToken);
        if (view is null)
        {
            return NotFound();
        }

        return Ok(new DetailRequestsResponse(
            view.Missing.Select(item => item.ToString()).ToList(),
            view.Asked.Select(item => item.ToString()).ToList(),
            view.Askers?
                .Select(askers => new DetailAskersResponse(askers.Item.ToString(), askers.Names))
                .ToList()));
    }

    /// <summary>
    /// Asks the organisers for these details. Asking again for something
    /// already asked for is a quiet success, so a double tap is not a second
    /// notification.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ask(
        Guid organisationId,
        Guid engagementId,
        AskForDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var items = new List<ReadinessItem>();
        foreach (var name in request.Items ?? [])
        {
            if (!Enum.TryParse<ReadinessItem>(name, ignoreCase: true, out var item)
                || !DetailRequest.Askable.Contains(item))
            {
                ModelState.AddModelError(nameof(request.Items), $"\"{name}\" is not a detail that can be asked for.");
                return ValidationProblem(ModelState);
            }

            items.Add(item);
        }

        var result = await detailRequestService.AskAsync(caller, engagementId, items, cancellationToken);
        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.NotFound => NotFound(),
            EngagementResult.Forbidden => Forbid(),
            _ => ValidationProblem("Choose at least one detail to ask for."),
        };
    }

    private async Task<Membership?> CallerAsync(Guid organisationId, CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return null;
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);
        return await authorization.FindMembershipAsync(organisationId, user.Id, cancellationToken);
    }
}
