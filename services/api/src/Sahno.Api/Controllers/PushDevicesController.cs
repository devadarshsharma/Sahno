using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Notifications;
using Sahno.Application.Users;
using Sahno.Contracts.Notifications;

namespace Sahno.Api.Controllers;

/// <summary>
/// The phones the signed-in person can be pushed to (D-080). Not scoped to
/// an organisation: a phone is the person's, and it receives every
/// organisation's news for them.
/// </summary>
[ApiController]
[Route("api/me/push-devices")]
[Authorize]
public sealed class PushDevicesController(
    EnsureUserService ensureUserService,
    PushDeviceService pushDeviceService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PushDeviceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PushDeviceResponse>>> List(
        CancellationToken cancellationToken)
    {
        var userId = await UserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        var devices = await pushDeviceService.ListAsync(userId.Value, cancellationToken);
        return Ok(devices
            .Select(device => new PushDeviceResponse(
                device.Token,
                device.Platform,
                device.DeviceName,
                device.LastSeenAtUtc))
            .ToList());
    }

    /// <summary>
    /// Registers (or refreshes) this phone. Idempotent: the app calls it on
    /// every sign-in and whenever the token changes.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register(
        RegisterPushDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await UserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        var registered = await pushDeviceService.RegisterAsync(
            userId.Value,
            request.Token,
            request.Platform,
            request.DeviceName,
            cancellationToken);

        return registered
            ? NoContent()
            : ValidationProblem("That is not an Expo push token.");
    }

    /// <summary>Signing out: this phone stops receiving this person's news.</summary>
    [HttpDelete("{token}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unregister(
        string token,
        CancellationToken cancellationToken)
    {
        var userId = await UserIdAsync(cancellationToken);
        if (userId is null)
        {
            return Unauthorized();
        }

        await pushDeviceService.UnregisterAsync(userId.Value, token, cancellationToken);
        return NoContent();
    }

    private async Task<Guid?> UserIdAsync(CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return null;
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);
        return user.Id;
    }
}
