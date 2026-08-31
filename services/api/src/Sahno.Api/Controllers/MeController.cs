using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        // The only place token claims are read. The subject comes from the
        // validated token — never from client-supplied values — and email and
        // name are optional profile hints.
        var subject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized();
        }

        var identity = new ExternalIdentity(
            subject,
            User.FindFirst("email")?.Value,
            User.FindFirst("name")?.Value);

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);

        return Ok(new MeResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.CreatedAtUtc));
    }
}
