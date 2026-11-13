using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Users;
using Sahno.Domain.Users;

namespace Sahno.Api.Controllers;

/// <summary>
/// Profile photos (D-129): set and removed by their owner; seen by anyone who
/// shares an organisation with them, and by nobody else.
/// </summary>
[ApiController]
[Authorize]
public sealed class AvatarsController(
    EnsureUserService ensureUserService,
    AvatarService avatarService,
    OrganisationAuthorizationService authorization) : ControllerBase
{
    /// <summary>Your own photo, as a JPEG, PNG or WebP of at most 300 KB (multipart, field "photo").</summary>
    [HttpPut("api/me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UserAvatar.MaxBytes + 16 * 1024)]
    [ProducesResponseType<AvatarVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> SetMine(IFormFile photo, CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (photo.Length > UserAvatar.MaxBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = "That photo is too large." });
        }

        using var buffer = new MemoryStream();
        await photo.CopyToAsync(buffer, cancellationToken);
        var (result, version) = await avatarService.SetAsync(user.Id, buffer.ToArray(), cancellationToken);
        return result switch
        {
            AvatarResult.Saved => Ok(new AvatarVersionResponse(user.Id, version!.Value.ToUnixTimeMilliseconds())),
            AvatarResult.TooLarge => StatusCode(StatusCodes.Status413PayloadTooLarge, new { message = "That photo is too large." }),
            _ => BadRequest(new { message = "That is not a JPEG, PNG or WebP image." }),
        };
    }

    [HttpDelete("api/me/avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMine(CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        await avatarService.RemoveAsync(user.Id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Somebody's photo, to someone who shares an organisation with them; 404
    /// otherwise — whether they have one is not disclosed. Cached hard: the
    /// app asks for it with its version, so a changed photo is a new address.
    /// </summary>
    [HttpGet("api/users/{userId:guid}/avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var avatar = await avatarService.FindForViewerAsync(user.Id, userId, cancellationToken);
        if (avatar is null)
        {
            return NotFound();
        }

        // Private to this viewer's cache, kept a year: the version in the
        // address changes whenever the photo does.
        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return File(avatar.Content, avatar.ContentType);
    }

    /// <summary>Which members of the organisation have a photo, and its version.</summary>
    [HttpGet("api/organisations/{organisationId:guid}/avatars")]
    [ProducesResponseType<IReadOnlyList<AvatarVersionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AvatarVersionResponse>>> ListForOrganisation(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (await authorization.FindMembershipAsync(organisationId, user.Id, cancellationToken) is null)
        {
            return NotFound();
        }

        var rows = await avatarService.ListForOrganisationAsync(organisationId, cancellationToken);
        return Ok(rows.Select(row => new AvatarVersionResponse(row.UserId, row.UpdatedAtUtc.ToUnixTimeMilliseconds())).ToList());
    }

    private async Task<User?> CurrentUserAsync(CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        return identity is null ? null : await ensureUserService.EnsureAsync(identity, cancellationToken);
    }
}
