using Sahno.Domain.Engagements;
using Sahno.Domain.Repertoire;

namespace Sahno.Application.Repertoire;

/// <summary>A booking a piece was on, as a line in the piece's history.</summary>
public sealed record PieceEngagement(
    Guid EngagementId,
    string Title,
    string Status,
    DateOnly? StartDate);

public interface IPieceStore
{
    /// <summary>The whole repertoire of one organisation, by title.</summary>
    Task<IReadOnlyList<Piece>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    Task<Piece?> FindAsync(
        Guid organisationId,
        Guid pieceId,
        CancellationToken cancellationToken);

    /// <summary>How many bookings each piece has been on — one query for the list.</summary>
    Task<IReadOnlyDictionary<Guid, int>> UseCountsAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    /// <summary>The bookings one piece has been on, newest first.</summary>
    Task<IReadOnlyList<PieceEngagement>> ListEngagementsAsync(
        Guid pieceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PieceLink>> ListLinksAsync(
        Guid pieceId,
        CancellationToken cancellationToken);

    /// <summary>Links for many pieces at once, keyed by piece.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PieceLink>>> ListLinksAsync(
        IReadOnlyCollection<Guid> pieceIds,
        CancellationToken cancellationToken);

    Task<PieceLink?> FindLinkAsync(
        Guid pieceId,
        Guid linkId,
        CancellationToken cancellationToken);

    Task AddAsync(Piece piece, CancellationToken cancellationToken);

    Task AddLinkAsync(PieceLink link, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);

    /// <summary>Removes the piece, its links, and every set list entry pointing at it.</summary>
    Task RemoveAsync(Guid pieceId, CancellationToken cancellationToken);

    Task RemoveLinkAsync(Guid linkId, CancellationToken cancellationToken);
}

/// <summary>A set list entry with its piece resolved.</summary>
public sealed record SetListRow(SetListEntry Entry, Piece Piece);

public interface ISetListStore
{
    /// <summary>The set list in running order, pieces resolved.</summary>
    Task<IReadOnlyList<SetListRow>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken);

    Task<SetListEntry?> FindAsync(
        Guid engagementId,
        Guid entryId,
        CancellationToken cancellationToken);

    Task AddAsync(SetListEntry entry, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);

    Task RemoveAsync(Guid entryId, CancellationToken cancellationToken);

    /// <summary>Which engagements of one organisation have a set list.</summary>
    Task<IReadOnlySet<Guid>> EngagementIdsWithSetListAsync(
        Guid organisationId,
        CancellationToken cancellationToken);
}
