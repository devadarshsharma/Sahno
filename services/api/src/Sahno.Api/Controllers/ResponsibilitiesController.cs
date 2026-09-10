using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// Who is doing or bringing what (Slice 8, D-047 §3).
///
/// The routes divide along the line the roles document draws: creating,
/// describing, and assigning belong to organisers, while <c>progress</c> is
/// open to the person the job is assigned to. That is the only write a Member
/// has here, and it reaches only their own row.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/responsibilities")]
[Authorize]
public sealed class ResponsibilitiesController(
    EnsureUserService ensureUserService,
    ResponsibilityService responsibilityService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    /// <summary>
    /// The whole list for this engagement. Participants see it too: a job list
    /// only prevents duplicated and dropped work if the group can read it.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<ResponsibilityResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResponsibilityResponse>>> List(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await responsibilityService.ListAsync(
            caller,
            engagementId,
            cancellationToken);

        return rows is null
            ? NotFound()
            : Ok(rows.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<ResponsibilityResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponsibilityResponse>> Create(
        Guid organisationId,
        Guid engagementId,
        CreateResponsibilityRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError(
                nameof(request.Title),
                "A responsibility needs a title.");
            return ValidationProblem(ModelState);
        }

        var (result, created) = await responsibilityService.CreateAsync(
            caller,
            engagementId,
            request.Title,
            request.Detail,
            request.AssignedUserId,
            cancellationToken);

        if (result != EngagementResult.Success || created is null)
        {
            return result switch
            {
                EngagementResult.NotFound => NotFound(),
                EngagementResult.Forbidden => Forbid(),
                _ => ValidationProblem(
                    "That person is not on this event, so the job would not reach them."),
            };
        }

        return CreatedAtAction(
            nameof(List),
            new { organisationId, engagementId },
            ToResponse(new ResponsibilityRow(created, null, false)));
    }

    [HttpPut("{responsibilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid engagementId,
        Guid responsibilityId,
        UpdateResponsibilityRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            ModelState.AddModelError(
                nameof(request.Title),
                "A responsibility needs a title.");
            return ValidationProblem(ModelState);
        }

        var result = await responsibilityService.UpdateAsync(
            caller,
            engagementId,
            responsibilityId,
            request.Title,
            request.Detail,
            request.AssignedUserId,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// The assignee's own update. An organiser may also record it, because
    /// they are frequently the one who gets told.
    /// </summary>
    [HttpPut("{responsibilityId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetProgress(
        Guid organisationId,
        Guid engagementId,
        Guid responsibilityId,
        SetResponsibilityProgressRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await responsibilityService.SetProgressAsync(
            caller,
            engagementId,
            responsibilityId,
            request.IsDone,
            request.Note,
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{responsibilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organisationId,
        Guid engagementId,
        Guid responsibilityId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await responsibilityService.DeleteAsync(
            caller,
            engagementId,
            responsibilityId,
            cancellationToken);

        return FromResult(result);
    }

    private static ResponsibilityResponse ToResponse(ResponsibilityRow row)
    {
        return new ResponsibilityResponse(
            row.Responsibility.Id,
            row.Responsibility.Title,
            row.Responsibility.Detail,
            row.Responsibility.AssignedUserId,
            row.AssignedDisplayName,
            row.IsYours,
            row.Responsibility.IsDone,
            row.Responsibility.Note,
            row.Responsibility.CompletedAtUtc,
            row.Responsibility.CreatedAtUtc);
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
            _ => ValidationProblem(
                "That person is not on this event, so the job would not reach them."),
        };
    }
}
