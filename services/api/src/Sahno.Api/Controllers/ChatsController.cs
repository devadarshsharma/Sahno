using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;

namespace Sahno.Api.Controllers;

/// <summary>
/// The Chat tab's inbox (D-084): an index of the event conversations the
/// caller can open. The conversations themselves stay on their events
/// (DiscussionController); this only lists them.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/chats")]
[Authorize]
public sealed class ChatsController(
    EnsureUserService ensureUserService,
    DiscussionService discussionService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    private const int PreviewLength = 140;

    [HttpGet]
    [ProducesResponseType<List<ChatInboxEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ChatInboxEntryResponse>>> Inbox(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var identity = User.ToExternalIdentity();
        if (identity is null)
        {
            return NotFound();
        }

        var user = await ensureUserService.EnsureAsync(identity, cancellationToken);
        var caller = await authorization.FindMembershipAsync(
            organisationId,
            user.Id,
            cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await discussionService.InboxAsync(caller, cancellationToken);

        return Ok(rows.Select(row =>
        {
            var message = row.Latest.Message;
            var body = message.Body;
            return new ChatInboxEntryResponse(
                row.Engagement.Id,
                row.Engagement.Title,
                row.Engagement.Status.ToString(),
                row.Engagement.StartDate,
                row.Latest.AuthorDisplayName,
                row.Latest.IsYours,
                body is null
                    ? null
                    : body.Length > PreviewLength ? body[..PreviewLength] + "…" : body,
                message.IsDeleted,
                message.PostedAtUtc,
                row.UnreadMessages);
        }).ToList());
    }
}
