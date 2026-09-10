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
/// Engagements and their lifecycle (Slice 4). Creating and moving them is
/// Owner/Admin work (D-019). Members reach only the ones they are part of,
/// which needs participant selection and so arrives with Slice 5 — until then
/// this whole surface is organisers only.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements")]
[Authorize]
public sealed class EngagementsController(
    EnsureUserService ensureUserService,
    EngagementService engagementService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EngagementResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EngagementResponse>>> List(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        // Members see the engagements they are on the lineup for, and nothing
        // else (D-020); organisers see everything.
        var rows = await engagementService.ListVisibleWithContextAsync(
            caller,
            cancellationToken);

        return Ok(rows
            .Select(row => ToResponse(row.Engagement, row.Lineup, row.YourResponse))
            .ToList());
    }

    [HttpGet("{engagementId:guid}")]
    [ProducesResponseType<EngagementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngagementResponse>> GetById(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var view = await engagementService.FindVisibleWithContextAsync(
            caller,
            engagementId,
            cancellationToken);

        return view is null
            ? NotFound()
            : Ok(ToResponse(view.Engagement, view.Lineup, view.YourResponse));
    }

    /// <summary>The engagement's history, newest first.</summary>
    [HttpGet("{engagementId:guid}/activity")]
    [ProducesResponseType<IReadOnlyList<EngagementActivityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EngagementActivityResponse>>> Activity(
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

        var engagement = await engagementService.FindAsync(
            organisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return NotFound();
        }

        var rows = await engagementService.ListActivityAsync(engagementId, cancellationToken);
        return Ok(rows.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<EngagementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngagementResponse>> Create(
        Guid organisationId,
        CreateEngagementRequest request,
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

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError(nameof(request.Title), "A title is required.");
            return ValidationProblem(ModelState);
        }

        var engagement = await engagementService.CreateDraftAsync(
            caller,
            request.Title,
            request.StartDate,
            request.EndDate,
            request.StartTime,
            request.Venue,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(engagement));
    }

    [HttpPatch("{engagementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid engagementId,
        UpdateEngagementRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await engagementService.UpdateDetailsAsync(
            caller,
            engagementId,
            request.Title,
            request.StartTime,
            request.Venue,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Sets the proposed date. Refused once Members hold the old one: that
    /// change goes through postponement so nobody is left holding a date that
    /// quietly moved (D-038).
    /// </summary>
    [HttpPut("{engagementId:guid}/dates")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDates(
        Guid organisationId,
        Guid engagementId,
        SetEngagementDatesRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await engagementService.SetDatesAsync(
            caller,
            engagementId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Moves the engagement along its lifecycle. The allowed targets for the
    /// current state are on the engagement itself, so a client can offer only
    /// the moves that exist.
    /// </summary>
    [HttpPost("{engagementId:guid}/transition")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transition(
        Guid organisationId,
        Guid engagementId,
        TransitionEngagementRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<EngagementStatus>(request.Status, out var target))
        {
            ModelState.AddModelError(nameof(request.Status), "Unknown status.");
            return ValidationProblem(ModelState);
        }

        var result = await engagementService.TransitionAsync(
            caller,
            engagementId,
            target,
            request.Reason,
            request.AcknowledgeOutstanding,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Discards a Draft. Anything Members have seen is cancelled with a reason
    /// instead, and kept in history (D-034).
    /// </summary>
    [HttpDelete("{engagementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Discard(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await engagementService.DiscardDraftAsync(
            caller,
            engagementId,
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
            EngagementResult.OutstandingAcknowledgementRequired => Conflict(
                new ProblemDetails
                {
                    Title = "Availability is still outstanding",
                    Detail =
                        "Some selected members have not answered yet. Confirm again "
                        + "with acknowledgeOutstanding to record the booking anyway.",
                    Status = StatusCodes.Status409Conflict,
                }),
            _ => ValidationProblem("That change is not allowed for this engagement."),
        };
    }

    private static EngagementResponse ToResponse(
        Engagement engagement,
        EngagementLineup? lineup = null,
        AvailabilityResponse? ownResponse = null)
    {
        var allowed = Enum.GetValues<EngagementStatus>()
            .Where(status =>
                Engagement.IsTransitionAllowed(engagement.Status, status))
            .Select(status => status.ToString())
            .ToList();

        return new EngagementResponse(
            engagement.Id,
            engagement.Title,
            engagement.Status.ToString(),
            engagement.StartDate,
            engagement.EndDate,
            engagement.StartTime,
            engagement.Venue,
            engagement.IsSharedWithMembers,
            engagement.CanChangeDateDirectly,
            engagement.CanBeDiscarded,
            allowed,
            lineup?.Selected,
            lineup?.Outstanding,
            ownResponse?.ToString(),
            engagement.CreatedAtUtc);
    }

    private static EngagementActivityResponse ToResponse(EngagementActivity activity)
    {
        return new EngagementActivityResponse(
            activity.Id,
            activity.Type.ToString(),
            activity.FromStatus?.ToString(),
            activity.ToStatus?.ToString(),
            activity.FromStartDate,
            activity.ToStartDate,
            activity.Reason,
            activity.ActorUserId,
            activity.OccurredAtUtc);
    }
}
