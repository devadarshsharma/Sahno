using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sahno.Api.RateLimiting;
using Sahno.Application.Notifications;

namespace Sahno.Api.Controllers;

/// <summary>
/// A phone that finds itself signed out — by Sign out, or because its sign-in
/// ran out — telling the API to stop pushing to it (D-133). It needs no
/// sign-in, which is the point: the one it had may be gone.
/// </summary>
[ApiController]
[Route("api/push-devices")]
[AllowAnonymous]
public sealed class SignedOutPushDevicesController(PushDeviceService pushDeviceService)
    : ControllerBase
{
    [HttpDelete("{token}")]
    [EnableRateLimiting(RateLimitPolicies.ForgetPushDevice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Forget(string token, CancellationToken cancellationToken)
    {
        await pushDeviceService.ForgetAsync(token, cancellationToken);
        return NoContent();
    }
}
