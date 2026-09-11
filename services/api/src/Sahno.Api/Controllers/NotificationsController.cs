using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Notifications;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// The bell (Slice 10, D-042, D-049). Scoped to one organisation, because
/// everything a notification is about lives in one, and switching
/// organisations is meant to be a strict change of context (D-013).
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/notifications")]
[Authorize]
public sealed class NotificationsController(
    EnsureUserService ensureUserService,
    NotificationService notificationService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<NotificationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<NotificationResponse>>> List(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await notificationService.ListAsync(caller, cancellationToken);
        return Ok(rows.Select(ToResponse).ToList());
    }

    /// <summary>The badge. Cheap enough to poll on every focus.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var unread = await notificationService.CountUnreadAsync(caller, cancellationToken);
        return Ok(new UnreadCountResponse(unread));
    }

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(
        Guid organisationId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var found = await notificationService.MarkReadAsync(
            caller,
            notificationId,
            cancellationToken);

        return found ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAllRead(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        await notificationService.MarkAllReadAsync(caller, cancellationToken);
        return NoContent();
    }

    private static NotificationResponse ToResponse(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.Kind.ToString(),
            notification.Title,
            notification.Body,
            notification.EngagementId,
            notification.IsRead,
            notification.CreatedAtUtc);
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
}
