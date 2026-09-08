using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Users;
using Sahno.Contracts.Users;
using Sahno.Domain.Users;

namespace Sahno.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Get(
        [FromServices] EnsureUserService ensureUserService,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return Unauthorized();
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);

        return Ok(ToResponse(user));
    }

    /// <summary>
    /// Sets the person's own display name. Required by D-046 and asked for
    /// during onboarding, because the passwordless email connection supplies
    /// no usable name of its own.
    /// </summary>
    [HttpPatch]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Update(
        [FromBody] UpdateMeRequest request,
        [FromServices] EnsureUserService ensureUserService,
        [FromServices] UserProfileService userProfileService,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return Unauthorized();
        }

        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName))
        {
            ModelState.AddModelError(
                nameof(request.DisplayName),
                "A display name is required.");
            return ValidationProblem(ModelState);
        }

        if (displayName.Length > UserProfileService.MaxDisplayNameLength)
        {
            ModelState.AddModelError(
                nameof(request.DisplayName),
                $"Keep the name under {UserProfileService.MaxDisplayNameLength} characters.");
            return ValidationProblem(ModelState);
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);

        await userProfileService.SetDisplayNameAsync(
            user,
            displayName,
            cancellationToken);

        return Ok(ToResponse(user));
    }

    private static MeResponse ToResponse(User user)
    {
        return new MeResponse(
            user.Id,
            user.Email,
            // Presentable, not raw: an email-address "name" hint is reported
            // as absent so the client asks for a real one.
            user.PresentableDisplayName,
            user.CreatedAtUtc);
    }
}
