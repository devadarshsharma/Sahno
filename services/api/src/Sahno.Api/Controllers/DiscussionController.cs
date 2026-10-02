using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Application.Users;
using Sahno.Contracts.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// Discussion inside an engagement (Slice 9, D-024, D-047 §6).
///
/// Access follows the engagement, so there is no separate permission to get
/// wrong: somebody who cannot open the event gets the same 404 here that they
/// get there. Editing is the author's alone; removal is the author's or an
/// organiser's, which is what moderation means in D-024.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/discussion")]
[Authorize]
public sealed class DiscussionController(
    EnsureUserService ensureUserService,
    DiscussionService discussionService,
    NotificationService notificationService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<DiscussionMessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DiscussionMessageResponse>>> List(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var thread = await discussionService.ListAsync(
            caller,
            engagementId,
            cancellationToken);

        return thread is null
            ? NotFound()
            : Ok(thread.Select(ToResponse).ToList());
    }

    /// <summary>
    /// The caller has read this event's chat: its unread bubble clears, and
    /// the matching rows on the bell with it. Idempotent; only ever touches
    /// the caller's own notifications.
    /// </summary>
    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        await notificationService.MarkDiscussionReadAsync(
            caller,
            engagementId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType<DiscussionMessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscussionMessageResponse>> Post(
        Guid organisationId,
        Guid engagementId,
        PostDiscussionMessageRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            ModelState.AddModelError(
                nameof(request.Body),
                "A message needs something in it.");
            return ValidationProblem(ModelState);
        }

        var (result, posted) = await discussionService.PostAsync(
            caller,
            engagementId,
            request.Body,
            cancellationToken,
            request.ReplyToMessageId);

        if (result == EngagementResult.Invalid)
        {
            ModelState.AddModelError(
                nameof(request.ReplyToMessageId),
                "That message cannot be replied to.");
            return ValidationProblem(ModelState);
        }

        if (result != EngagementResult.Success || posted is null)
        {
            return result == EngagementResult.Forbidden ? Forbid() : NotFound();
        }

        return CreatedAtAction(
            nameof(List),
            new { organisationId, engagementId },
            ToResponse(new DiscussionRow(posted, null, true)));
    }

    /// <summary>
    /// The author's own rewrite. An organiser moderating can remove a message
    /// but never change words attributed to somebody else, so there is no
    /// route here that reaches another person's text.
    /// </summary>
    [HttpPut("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit(
        Guid organisationId,
        Guid engagementId,
        Guid messageId,
        EditDiscussionMessageRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            ModelState.AddModelError(
                nameof(request.Body),
                "A message needs something in it.");
            return ValidationProblem(ModelState);
        }

        var result = await discussionService.EditAsync(
            caller,
            engagementId,
            messageId,
            request.Body,
            cancellationToken);

        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.NotFound => NotFound(),
            EngagementResult.Forbidden => Forbid(),
            _ => ValidationProblem("That message has been removed."),
        };
    }

    /// <summary>
    /// Takes a message down — the author taking their own back, or an
    /// organiser moderating.
    /// </summary>
    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid organisationId,
        Guid engagementId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await discussionService.RemoveAsync(
            caller,
            engagementId,
            messageId,
            cancellationToken);

        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.NotFound => NotFound(),
            EngagementResult.Forbidden => Forbid(),
            _ => ValidationProblem("That message could not be removed."),
        };
    }

    /// <summary>
    /// Reacts to a message (D-086). The same reaction again takes it back;
    /// anything else replaces it.
    /// </summary>
    [HttpPut("{messageId:guid}/reaction")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> React(
        Guid organisationId,
        Guid engagementId,
        Guid messageId,
        SetReactionRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await discussionService.ReactAsync(
            caller,
            engagementId,
            messageId,
            request.Emoji,
            cancellationToken);

        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.Invalid => ValidationProblem("That reaction cannot be added."),
            _ => NotFound(),
        };
    }

    /// <summary>Takes the caller's reaction back, whatever it was.</summary>
    [HttpDelete("{messageId:guid}/reaction")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unreact(
        Guid organisationId,
        Guid engagementId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await discussionService.ReactAsync(
            caller,
            engagementId,
            messageId,
            emoji: null,
            cancellationToken);

        return result == EngagementResult.NotFound ? NotFound() : NoContent();
    }

    /// <summary>"Delete for me" (D-086): the caller stops seeing this message; nobody else is affected.</summary>
    [HttpPost("{messageId:guid}/hide")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hide(
        Guid organisationId,
        Guid engagementId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await discussionService.HideAsync(caller, engagementId, messageId, cancellationToken);
        return result == EngagementResult.Success ? NoContent() : NotFound();
    }

    private static DiscussionMessageResponse ToResponse(DiscussionRow row)
    {
        return new DiscussionMessageResponse(
            row.Message.Id,
            row.Message.AuthorUserId,
            row.AuthorDisplayName,
            row.IsYours,
            row.Message.Body,
            row.Message.IsEdited,
            row.Message.IsDeleted,
            row.Message.WasModerated,
            row.Message.PostedAtUtc,
            row.Message.EditedAtUtc,
            row.ReplyTo is { } quote
                ? new DiscussionQuoteResponse(
                    quote.Message.Id,
                    quote.AuthorDisplayName,
                    quote.IsYours,
                    quote.Message.Body,
                    quote.Message.IsDeleted)
                : null,
            (row.Reactions ?? [])
                .Select(reaction => new DiscussionReactionResponse(
                    reaction.Emoji,
                    reaction.Count,
                    reaction.IncludesYou))
                .ToList());
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
