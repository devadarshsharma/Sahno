using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahno.Api.Authentication;
using Sahno.Application.Organisations;
using Sahno.Application.Repertoire;
using Sahno.Application.Users;
using Sahno.Contracts.Repertoire;
using Sahno.Domain.Organisations;
using Sahno.Domain.Repertoire;

namespace Sahno.Api.Controllers;

/// <summary>
/// The organisation's repertoire (D-079). Every member reads and edits it;
/// deleting a piece and the organiser notes are organiser-only.
/// </summary>
[ApiController]
[Route("api/organisations/{organisationId:guid}/repertoire")]
[Authorize]
public sealed class RepertoireController(
    EnsureUserService ensureUserService,
    RepertoireService repertoireService,
    OrganisationAuthorizationService authorization)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PieceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PieceResponse>>> List(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var rows = await repertoireService.ListAsync(caller, cancellationToken);

        // The list travels without lyrics; a hundred pieces of twenty
        // thousand characters is not what a scroll needs. Detail has them.
        return Ok(rows.Select(row => ToResponse(row, caller, includeLyrics: false)).ToList());
    }

    [HttpGet("{pieceId:guid}")]
    [ProducesResponseType<PieceDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PieceDetailResponse>> Get(
        Guid organisationId,
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, row, history) = await repertoireService.GetAsync(caller, pieceId, cancellationToken);
        if (result != RepertoireResult.Success)
        {
            return NotFound();
        }

        return Ok(new PieceDetailResponse(
            ToResponse(row!, caller, includeLyrics: true),
            history
                .Select(booking => new PieceBookingResponse(
                    booking.EngagementId,
                    booking.Title,
                    booking.Status,
                    booking.StartDate))
                .ToList()));
    }

    [HttpPost]
    [ProducesResponseType<PieceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PieceResponse>> Create(
        Guid organisationId,
        SavePieceRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, created) = await repertoireService.CreateAsync(
            caller,
            request.Title,
            request.Attribution,
            request.Language,
            request.Key,
            request.DurationMinutes,
            request.Lyrics,
            cancellationToken);

        return result switch
        {
            RepertoireResult.Success => StatusCode(
                StatusCodes.Status201Created,
                ToResponse(new PieceRow(created!, [], 0), caller, includeLyrics: true)),
            RepertoireResult.Invalid => ValidationProblem("A piece needs a title, and a duration between 1 and 600 minutes."),
            _ => NotFound(),
        };
    }

    [HttpPut("{pieceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid organisationId,
        Guid pieceId,
        SavePieceRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await repertoireService.UpdateAsync(
            caller,
            pieceId,
            request.Title,
            request.Attribution,
            request.Language,
            request.Key,
            request.DurationMinutes,
            request.Lyrics,
            cancellationToken);

        return FromResult(result, "A piece needs a title, and a duration between 1 and 600 minutes.");
    }

    [HttpPut("{pieceId:guid}/notes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotes(
        Guid organisationId,
        Guid pieceId,
        UpdatePieceNotesRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await repertoireService.UpdateNotesAsync(
            caller,
            pieceId,
            request.Notes,
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{pieceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organisationId,
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await repertoireService.DeleteAsync(caller, pieceId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{pieceId:guid}/links")]
    [ProducesResponseType<PieceLinkResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PieceLinkResponse>> AddLink(
        Guid organisationId,
        Guid pieceId,
        AddPieceLinkRequest request,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var (result, created) = await repertoireService.AddLinkAsync(
            caller,
            pieceId,
            request.Title ?? string.Empty,
            request.Url,
            cancellationToken);

        return result switch
        {
            RepertoireResult.Success => StatusCode(
                StatusCodes.Status201Created,
                new PieceLinkResponse(created!.Id, created.Title, created.Url)),
            RepertoireResult.Invalid => ValidationProblem("A link needs a web address."),
            _ => NotFound(),
        };
    }

    [HttpDelete("{pieceId:guid}/links/{linkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLink(
        Guid organisationId,
        Guid pieceId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var caller = await CallerAsync(organisationId, cancellationToken);
        if (caller is null)
        {
            return NotFound();
        }

        var result = await repertoireService.RemoveLinkAsync(caller, pieceId, linkId, cancellationToken);
        return FromResult(result);
    }

    private static PieceResponse ToResponse(PieceRow row, Membership caller, bool includeLyrics)
    {
        var piece = row.Piece;
        return new PieceResponse(
            piece.Id,
            piece.Title,
            piece.Attribution,
            piece.Language,
            piece.Key,
            piece.DurationMinutes,
            piece.HasLyrics,
            includeLyrics ? piece.Lyrics : null,
            // The organiser's notes never reach a member's copy.
            OrganisationAuthorizationService.IsOrganiser(caller) ? piece.Notes : null,
            row.Links.Select(link => new PieceLinkResponse(link.Id, link.Title, link.Url)).ToList(),
            row.UseCount,
            piece.UpdatedByUserId,
            piece.UpdatedAtUtc);
    }

    private IActionResult FromResult(RepertoireResult result, string invalidMessage = "That change is not allowed.")
    {
        return result switch
        {
            RepertoireResult.Success => NoContent(),
            RepertoireResult.NotFound => NotFound(),
            RepertoireResult.Forbidden => Forbid(),
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
