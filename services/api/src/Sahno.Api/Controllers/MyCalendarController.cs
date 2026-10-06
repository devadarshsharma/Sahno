using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;

namespace Sahno.Api.Controllers;

/// <summary>
/// The signed-in person's calendar across all their organisations (D-091).
/// Not under an organisation: it is theirs, not any one group's.
/// </summary>
[ApiController]
[Route("api/me/calendar")]
[Authorize]
public sealed class MyCalendarController(
    EnsureUserService ensureUserService,
    MyCalendarService myCalendarService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MyCalendarResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MyCalendarResponse>> Get(
        [FromQuery] DateOnly from,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return Unauthorized();
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);
        var (events, rehearsals) = await myCalendarService.ForUserAsync(user.Id, from, cancellationToken);

        return Ok(new MyCalendarResponse(
            events.Select(item => new MyCalendarEventResponse(
                item.Organisation.Id,
                item.Organisation.Name,
                item.IsOrganiser,
                item.View.Engagement.Id,
                item.View.Engagement.Title,
                item.View.Engagement.Status.ToString(),
                item.View.Engagement.StartDate,
                item.View.Engagement.EndDate,
                item.View.Engagement.StartTime,
                item.View.Engagement.Venue,
                item.View.YourResponse?.ToString(),
                item.View.Lineup?.Outstanding)).ToList(),
            rehearsals.Select(item => new MyCalendarRehearsalResponse(
                item.Organisation.Id,
                item.Organisation.Name,
                item.Row.Rehearsal.Id,
                item.Row.Engagement.Id,
                item.Row.Engagement.Title,
                item.Row.Rehearsal.Title,
                item.Row.Rehearsal.Date,
                item.Row.Rehearsal.StartTime,
                item.Row.Rehearsal.EndTime,
                item.Row.Rehearsal.Venue)).ToList()));
    }
}
