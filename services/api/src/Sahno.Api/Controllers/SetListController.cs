using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Engagements;
using Sahno.Application.Organisations;
using Sahno.Application.Repertoire;
using Sahno.Application.Users;
using Sahno.Contracts.Repertoire;
using Sahno.Domain.Organisations;

namespace Sahno.Api.Controllers;

/// <summary>
/// A booking's set list (D-079): read by everyone on the lineup, arranged by
/// organisers. A member not on the event gets 404, as for the rest of the
/// workspace — from outside the lineup, the event does not exist.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/engagements/{engagementId:guid}/setlist")]
[Authorize]
public sealed class SetListController(
    EnsureUserService ensureUserService,
    SetListService setListService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SetListEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SetListEntryResponse>>> List(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await setListService.ListAsync(caller, engagementId, cancellationToken);
        if (rows is null)
        {
            return NotFound();
        }

        return Ok(rows.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType<SetListEntryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SetListEntryResponse>> Add(
        Guid organisationId,
        Guid engagementId,
        AddSetListEntryRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, created) = await setListService.AddAsync(
            caller,
            engagementId,
            request.PieceId,
            request.Note,
            cancellationToken);

        if (result != EngagementResult.Success)
        {
            return FromResult(
                result,
                "That piece is not in this organisation's repertoire.");
        }

        // Re-read so the response carries the piece, the same shape as the list.
        var rows = await setListService.ListAsync(caller, engagementId, cancellationToken);
        var row = rows!.Single(candidate => candidate.Entry.Id == created!.Id);
        return StatusCode(StatusCodes.Status201Created, ToResponse(row));
    }

    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        Guid organisationId,
        Guid engagementId,
        ReorderSetListRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await setListService.ReorderAsync(
            caller,
            engagementId,
            request.EntryIds,
            cancellationToken);

        return FromResult(result, "The new order must name every entry exactly once.");
    }

    [HttpPut("{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNote(
        Guid organisationId,
        Guid engagementId,
        Guid entryId,
        UpdateSetListEntryRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await setListService.UpdateNoteAsync(
            caller,
            engagementId,
            entryId,
            request.Note,
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid organisationId,
        Guid engagementId,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await setListService.RemoveAsync(caller, engagementId, entryId, cancellationToken);
        return FromResult(result);
    }

    private static SetListEntryResponse ToResponse(SetListRow row)
    {
        return new SetListEntryResponse(
            row.Entry.Id,
            row.Piece.Id,
            row.Entry.Position,
            row.Piece.Title,
            row.Piece.Attribution,
            row.Piece.DurationMinutes,
            row.Piece.HasLyrics,
            row.Entry.Note);
    }

    private ActionResult FromResult(EngagementResult result, string invalidMessage = "That change is not allowed.")
    {
        return result switch
        {
            EngagementResult.Success => NoContent(),
            EngagementResult.NotFound => NotFound(),
            EngagementResult.Forbidden => Forbid(),
            _ => ValidationProblem(invalidMessage),
        };
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
