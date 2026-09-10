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
/// Rehearsals linked to their parent engagement (Slice 8, D-047 §4).
/// Organisers schedule them; everyone on the lineup can see them, because a
/// rehearsal nobody can see is a meeting nobody attends.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/rehearsals")]
[Authorize]
public sealed class RehearsalsController(
    EnsureUserService ensureUserService,
    PreparationService preparationService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<RehearsalResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RehearsalResponse>>> List(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await preparationService.ListRehearsalsAsync(
            caller,
            engagementId,
            cancellationToken);

        return rows is null
            ? NotFound()
            : Ok(rows.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<RehearsalResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RehearsalResponse>> Create(
        Guid organisationId,
        Guid engagementId,
        SaveRehearsalRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, created) = await preparationService.ScheduleRehearsalAsync(
            caller,
            engagementId,
            request.Title,
            request.Date,
            request.StartTime,
            request.EndTime,
            request.Venue,
            request.Notes,
            cancellationToken);

        if (result != EngagementResult.Success || created is null)
        {
            return result switch
            {
                EngagementResult.NotFound => NotFound(),
                EngagementResult.Forbidden => Forbid(),
                _ => ValidationProblem("That rehearsal could not be saved."),
            };
        }

        return CreatedAtAction(
            nameof(List),
            new { organisationId, engagementId },
            ToResponse(created));
    }

    [HttpPut("{rehearsalId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid engagementId,
        Guid rehearsalId,
        SaveRehearsalRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await preparationService.UpdateRehearsalAsync(
            caller,
            engagementId,
            rehearsalId,
            request.Title,
            request.Date,
            request.StartTime,
            request.EndTime,
            request.Venue,
            request.Notes,
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{rehearsalId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organisationId,
        Guid engagementId,
        Guid rehearsalId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await preparationService.DeleteRehearsalAsync(
            caller,
            engagementId,
            rehearsalId,
            cancellationToken);

        return FromResult(result);
    }

    private static RehearsalResponse ToResponse(Rehearsal rehearsal)
    {
        return new RehearsalResponse(
            rehearsal.Id,
            rehearsal.Title,
            rehearsal.Date,
            rehearsal.StartTime,
            rehearsal.EndTime,
            rehearsal.Venue,
            rehearsal.Notes,
            rehearsal.CreatedAtUtc);
    }

    private async Task<Membership?> CallerAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return null;
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);

        return await authorization.FindMembershipAsync(
            organisationId,
            user.Id,
            cancellationToken);
    }

    private IActionResult FromResult(EngagementResult result)
    {
        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.NotFound => NotFound(),
            EngagementResult.Forbidden => Forbid(),
            _ => ValidationProblem("That rehearsal could not be saved."),
        };
    }
}
