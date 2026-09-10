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
/// Repertoire, notes, and links (Slice 8, D-047 §5, D-023).
///
/// The audience is the substance here. Participants is the default, because
/// most event material exists to be shared; anything marked Admins-only never
/// leaves the service for a Member, rather than being filtered out on the
/// client where a mistake would be a privacy failure.
///
/// Uploaded files are absent by necessity: Sahno has no object storage yet, so
/// an upload button would promise something that could not be delivered.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/resources")]
[Authorize]
public sealed class ResourcesController(
    EnsureUserService ensureUserService,
    PreparationService preparationService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<EngagementResourceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EngagementResourceResponse>>> List(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await preparationService.ListResourcesAsync(
            caller,
            engagementId,
            cancellationToken);

        return rows is null
            ? NotFound()
            : Ok(rows.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<EngagementResourceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngagementResourceResponse>> Create(
        Guid organisationId,
        Guid engagementId,
        CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<ResourceKind>(request.Kind, out var kind))
        {
            ModelState.AddModelError(
                nameof(request.Kind),
                "A resource is either a Note or a Link.");
            return ValidationProblem(ModelState);
        }

        if (!TryParseAudience(request.Audience, out var audience))
        {
            ModelState.AddModelError(
                nameof(request.Audience),
                "An audience is either Participants or AdminsOnly.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var (result, created) = await preparationService.AddResourceAsync(
                caller,
                engagementId,
                kind,
                request.Title,
                request.Body,
                request.Url,
                audience,
                cancellationToken);

            if (result != EngagementResult.Success || created is null)
            {
                return result switch
                {
                    EngagementResult.NotFound => NotFound(),
                    EngagementResult.Forbidden => Forbid(),
                    _ => ValidationProblem("That resource could not be saved."),
                };
            }

            return CreatedAtAction(
                nameof(List),
                new { organisationId, engagementId },
                ToResponse(created));
        }
        catch (ArgumentException exception)
        {
            // The domain refuses an empty note, a missing address, or a link
            // that is not http(s). Its wording is what the person needs to
            // read, so it is passed through rather than replaced.
            ModelState.AddModelError(string.Empty, exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpPut("{resourceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid engagementId,
        Guid resourceId,
        UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (!TryParseAudience(request.Audience, out var audience))
        {
            ModelState.AddModelError(
                nameof(request.Audience),
                "An audience is either Participants or AdminsOnly.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await preparationService.UpdateResourceAsync(
                caller,
                engagementId,
                resourceId,
                request.Title,
                request.Body,
                request.Url,
                audience,
                cancellationToken);

            return FromResult(result);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpDelete("{resourceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organisationId,
        Guid engagementId,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await preparationService.DeleteResourceAsync(
            caller,
            engagementId,
            resourceId,
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// An absent audience means Participants (D-023). An unrecognised one is
    /// refused rather than defaulted: guessing at what somebody meant to
    /// protect is how private material ends up shared.
    /// </summary>
    private static bool TryParseAudience(string? value, out ResourceAudience audience)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            audience = ResourceAudience.Participants;
            return true;
        }

        return Enum.TryParse(value, out audience);
    }

    private static EngagementResourceResponse ToResponse(EngagementResource resource)
    {
        return new EngagementResourceResponse(
            resource.Id,
            resource.Kind.ToString(),
            resource.Title,
            resource.Body,
            resource.Url,
            resource.Audience.ToString(),
            resource.CreatedAtUtc);
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
            _ => ValidationProblem("That resource could not be saved."),
        };
    }
}
