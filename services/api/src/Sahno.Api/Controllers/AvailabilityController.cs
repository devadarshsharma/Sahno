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
/// Availability collection (Slice 5). Who sees what is the whole point here:
/// organisers get every answer and the totals, while a Member is only ever
/// handed their own (D-021).
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/availability")]
[Authorize]
public sealed class AvailabilityController(
    EnsureUserService ensureUserService,
    AvailabilityService availabilityService,
    EngagementService engagementService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    /// <summary>
    /// The lineup and its totals. Organisers only — a Member asking about
    /// everyone else's answers gets nothing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<EngagementAvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngagementAvailabilityResponse>> Get(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (!OrganisationAuthorizationService.IsOrganiser(caller))
        {
            return Forbid();
        }

        var engagement = await engagementService.FindVisibleAsync(
            caller,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return NotFound();
        }

        var rows = await availabilityService.ListAsync(
            organisationId,
            engagementId,
            cancellationToken);

        var summary = AvailabilityService.Summarise(
            rows.Select(row => row.Participant));

        return Ok(new EngagementAvailabilityResponse(
            new AvailabilitySummaryResponse(
                summary.Selected,
                summary.Available,
                summary.Maybe,
                summary.Unavailable,
                summary.Outstanding),
            rows.Select(ToResponse).ToList()));
    }

    /// <summary>
    /// The caller's own participation and answer. This is the only
    /// availability a Member can read (D-021).
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<OwnAvailabilityResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnAvailabilityResponse>> GetOwn(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var engagement = await engagementService.FindVisibleAsync(
            caller,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return NotFound();
        }

        var participant = await availabilityService.FindOwnAsync(
            engagementId,
            caller.UserId,
            cancellationToken);

        var selected = participant is { IsActive: true };

        return Ok(new OwnAvailabilityResponse(
            selected,
            selected ? participant!.Response?.ToString() : null,
            selected ? participant!.RespondedAtUtc : null));
    }

    /// <summary>
    /// Selects Members and asks them. The first request moves a Draft into
    /// Checking Availability; later ones leave the state alone, so adjusting a
    /// lineup never drags the engagement backwards (D-027, D-028).
    /// </summary>
    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Request(
        Guid organisationId,
        Guid engagementId,
        RequestAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await availabilityService.RequestAsync(
            caller,
            engagementId,
            request.UserIds ?? [],
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Records the caller's own answer. There is no route for answering on
    /// someone else's behalf, by design.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Respond(
        Guid organisationId,
        Guid engagementId,
        RespondAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<AvailabilityResponse>(
                request.Response,
                out var response))
        {
            ModelState.AddModelError(
                nameof(request.Response),
                "Answer Available, Maybe, or Unavailable.");
            return ValidationProblem(ModelState);
        }

        var result = await availabilityService.RespondAsync(
            caller,
            engagementId,
            response,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>Chases one person who has not answered.</summary>
    [HttpPost("{userId:guid}/reminders")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remind(
        Guid organisationId,
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await availabilityService.RemindAsync(
            caller,
            engagementId,
            userId,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Takes someone off the lineup. Their answer is kept in the organisation's
    /// internal history rather than erased (D-027).
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParticipant(
        Guid organisationId,
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await availabilityService.RemoveAsync(
            caller,
            engagementId,
            userId,
            cancellationToken);

        return FromResult(result);
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
            _ => ValidationProblem("That change is not allowed for this engagement."),
        };
    }

    private static ParticipantResponse ToResponse(ParticipantRow row)
    {
        return new ParticipantResponse(
            row.Participant.UserId,
            row.DisplayName,
            row.Participant.Response?.ToString(),
            row.Participant.IsActive,
            row.Participant.RequestedAtUtc,
            row.Participant.RespondedAtUtc,
            row.Participant.RemindedAtUtc,
            row.Participant.RemovedAtUtc);
    }
}
