using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Users;
using Sahno.Contracts.Users;

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

        return Ok(new MeResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.CreatedAtUtc));
    }
}
