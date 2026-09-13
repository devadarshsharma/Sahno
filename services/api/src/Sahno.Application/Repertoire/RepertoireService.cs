using Sahno.Application.Organisations;
using Sahno.Domain.Organisations;
using Sahno.Domain.Repertoire;

namespace Sahno.Application.Repertoire;

public enum RepertoireResult
{
    Success,
    NotFound,
    Forbidden,
    Invalid,
}

/// <summary>A repertoire row: the piece, its links, and how often it has been performed.</summary>
public sealed record PieceRow(Piece Piece, IReadOnlyList<PieceLink> Links, int UseCount);

/// <summary>
/// The organisation's repertoire (D-079). Every member of the organisation
/// can read it, add to it, and edit a piece's lyrics — it is the group's
/// shared knowledge, and the tabla player knows the taal better than the
/// organiser does. Deleting a piece, and the organiser notes on it, are
/// organiser-only.
/// </summary>
public sealed class RepertoireService(IPieceStore pieces)
{
    public async Task<IReadOnlyList<PieceRow>> ListAsync(
        Membership actor,
        CancellationToken cancellationToken)
    {
        var all = await pieces.ListForOrganisationAsync(actor.OrganisationId, cancellationToken);
        var counts = await pieces.UseCountsAsync(actor.OrganisationId, cancellationToken);
        var links = await pieces.ListLinksAsync(
            all.Select(piece => piece.Id).ToList(),
            cancellationToken);

        return all
            .Select(piece => new PieceRow(
                piece,
                links.GetValueOrDefault(piece.Id) ?? [],
                counts.GetValueOrDefault(piece.Id)))
            .ToList();
    }

    public async Task<(RepertoireResult Result, PieceRow? Row, IReadOnlyList<PieceEngagement> History)> GetAsync(
        Membership actor,
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        var piece = await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken);
        if (piece is null)
        {
            return (RepertoireResult.NotFound, null, []);
        }

        var links = await pieces.ListLinksAsync(pieceId, cancellationToken);
        var history = await pieces.ListEngagementsAsync(pieceId, cancellationToken);
        return (RepertoireResult.Success, new PieceRow(piece, links, history.Count), history);
    }

    public async Task<(RepertoireResult Result, Piece? Created)> CreateAsync(
        Membership actor,
        string title,
        string? attribution,
        string? language,
        string? key,
        int? durationMinutes,
        string? lyrics,
        CancellationToken cancellationToken)
    {
        Piece piece;
        try
        {
            piece = Piece.Create(
                actor.OrganisationId,
                title,
                attribution,
                language,
                key,
                durationMinutes,
                lyrics,
                actor.UserId);
        }
        catch (ArgumentException)
        {
            return (RepertoireResult.Invalid, null);
        }

        await pieces.AddAsync(piece, cancellationToken);
        return (RepertoireResult.Success, piece);
    }

    public async Task<RepertoireResult> UpdateAsync(
        Membership actor,
        Guid pieceId,
        string title,
        string? attribution,
        string? language,
        string? key,
        int? durationMinutes,
        string? lyrics,
        CancellationToken cancellationToken)
    {
        var piece = await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken);
        if (piece is null)
        {
            return RepertoireResult.NotFound;
        }

        try
        {
            piece.Update(title, attribution, language, key, durationMinutes, lyrics, actor.UserId);
        }
        catch (ArgumentException)
        {
            return RepertoireResult.Invalid;
        }

        await pieces.SaveAsync(cancellationToken);
        return RepertoireResult.Success;
    }

    /// <summary>Organisers only (D-022-style: internal planning material).</summary>
    public async Task<RepertoireResult> UpdateNotesAsync(
        Membership actor,
        Guid pieceId,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return RepertoireResult.Forbidden;
        }

        var piece = await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken);
        if (piece is null)
        {
            return RepertoireResult.NotFound;
        }

        piece.UpdateNotes(notes, actor.UserId);
        await pieces.SaveAsync(cancellationToken);
        return RepertoireResult.Success;
    }

    /// <summary>
    /// Organisers only. Takes the piece off every set list it was on — a
    /// piece the group no longer performs should not linger on old bookings
    /// as a dangling reference.
    /// </summary>
    public async Task<RepertoireResult> DeleteAsync(
        Membership actor,
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return RepertoireResult.Forbidden;
        }

        var piece = await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken);
        if (piece is null)
        {
            return RepertoireResult.NotFound;
        }

        await pieces.RemoveAsync(pieceId, cancellationToken);
        return RepertoireResult.Success;
    }

    public async Task<(RepertoireResult Result, PieceLink? Created)> AddLinkAsync(
        Membership actor,
        Guid pieceId,
        string title,
        string url,
        CancellationToken cancellationToken)
    {
        var piece = await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken);
        if (piece is null)
        {
            return (RepertoireResult.NotFound, null);
        }

        PieceLink link;
        try
        {
            link = PieceLink.Create(pieceId, title, url, actor.UserId);
        }
        catch (ArgumentException)
        {
            return (RepertoireResult.Invalid, null);
        }

        await pieces.AddLinkAsync(link, cancellationToken);
        return (RepertoireResult.Success, link);
    }

    public async Task<RepertoireResult> RemoveLinkAsync(
        Membership actor,
        Guid pieceId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var piece = await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken);
        if (piece is null)
        {
            return RepertoireResult.NotFound;
        }

        var link = await pieces.FindLinkAsync(pieceId, linkId, cancellationToken);
        if (link is null)
        {
            return RepertoireResult.NotFound;
        }

        await pieces.RemoveLinkAsync(linkId, cancellationToken);
        return RepertoireResult.Success;
    }
}
