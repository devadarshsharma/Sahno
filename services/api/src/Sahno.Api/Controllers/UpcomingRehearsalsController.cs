using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;

namespace Sahno.Api.Controllers;

/// <summary>
/// Rehearsals across events, from today on (D-085), for Home and the calendar.
/// Each event's own rehearsals stay under that event (RehearsalsController).
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/rehearsals")]
[Authorize]
public sealed class UpcomingRehearsalsController(
    EnsureUserService ensureUserService,
    PreparationService preparationService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet("upcoming")]
    [ProducesResponseType<List<UpcomingRehearsalResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<UpcomingRehearsalResponse>>> Upcoming(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return NotFound();
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);
        var caller = await authorization.FindMembershipAsync(organisationId, user.Id, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var upcoming = await preparationService.UpcomingRehearsalsAsync(caller, cancellationToken);

        return Ok(upcoming.Select(row => new UpcomingRehearsalResponse(
            row.Rehearsal.Id,
            row.Engagement.Id,
            row.Engagement.Title,
            row.Rehearsal.Title,
            row.Rehearsal.Date,
            row.Rehearsal.StartTime,
            row.Rehearsal.EndTime,
            row.Rehearsal.Venue)).ToList());
    }
}
