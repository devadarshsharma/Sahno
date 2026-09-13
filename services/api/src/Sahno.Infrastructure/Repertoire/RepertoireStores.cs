using Microsoft.EntityFrameworkCore;
using Sahno.Application.Repertoire;
using Sahno.Domain.Engagements;
using Sahno.Domain.Repertoire;
using Sahno.Infrastructure.Persistence;

namespace Sahno.Infrastructure.Repertoire;

public sealed class PieceStore(SahnoDbContext dbContext) : IPieceStore
{
    public async Task<IReadOnlyList<Piece>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Pieces
            .AsNoTracking()
            .Where(piece => piece.OrganisationId == organisationId)
            .OrderBy(piece => piece.Title)
            .ToListAsync(cancellationToken);
    }

    public Task<Piece?> FindAsync(
        Guid organisationId,
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        return dbContext.Pieces.FirstOrDefaultAsync(
            piece => piece.Id == pieceId && piece.OrganisationId == organisationId,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> UseCountsAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        // Distinct bookings per piece: an encore is still one booking.
        var rows = await dbContext.SetListEntries
            .AsNoTracking()
            .Join(
                dbContext.Pieces.Where(piece => piece.OrganisationId == organisationId),
                entry => entry.PieceId,
                piece => piece.Id,
                (entry, _) => new { entry.PieceId, entry.EngagementId })
            .Distinct()
            .GroupBy(row => row.PieceId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Key, row => row.Count);
    }

    public async Task<IReadOnlyList<PieceEngagement>> ListEngagementsAsync(
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.SetListEntries
            .AsNoTracking()
            .Where(entry => entry.PieceId == pieceId)
            .Join(
                dbContext.Engagements,
                entry => entry.EngagementId,
                engagement => engagement.Id,
                (_, engagement) => engagement)
            .Distinct()
            .OrderByDescending(engagement => engagement.StartDate)
            .ThenByDescending(engagement => engagement.CreatedAtUtc)
            .Select(engagement => new PieceEngagement(
                engagement.Id,
                engagement.Title,
                engagement.Status.ToString(),
                engagement.StartDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PieceLink>> ListLinksAsync(
        Guid pieceId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PieceLinks
            .AsNoTracking()
            .Where(link => link.PieceId == pieceId)
            .OrderBy(link => link.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PieceLink>>> ListLinksAsync(
        IReadOnlyCollection<Guid> pieceIds,
        CancellationToken cancellationToken)
    {
        if (pieceIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<PieceLink>>();
        }

        var links = await dbContext.PieceLinks
            .AsNoTracking()
            .Where(link => pieceIds.Contains(link.PieceId))
            .OrderBy(link => link.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(link => link.PieceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PieceLink>)group.ToList());
    }

    public Task<PieceLink?> FindLinkAsync(
        Guid pieceId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        return dbContext.PieceLinks.FirstOrDefaultAsync(
            link => link.Id == linkId && link.PieceId == pieceId,
            cancellationToken);
    }

    public Task AddAsync(Piece piece, CancellationToken cancellationToken)
    {
        dbContext.Pieces.Add(piece);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task AddLinkAsync(PieceLink link, CancellationToken cancellationToken)
    {
        dbContext.PieceLinks.Add(link);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid pieceId, CancellationToken cancellationToken)
    {
        // Links and set list entries cascade from the piece; spelled out here
        // too so the intent survives a provider that does not cascade.
        await dbContext.SetListEntries
            .Where(entry => entry.PieceId == pieceId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.PieceLinks
            .Where(link => link.PieceId == pieceId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Pieces
            .Where(piece => piece.Id == pieceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task RemoveLinkAsync(Guid linkId, CancellationToken cancellationToken)
    {
        return dbContext.PieceLinks
            .Where(link => link.Id == linkId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class SetListStore(SahnoDbContext dbContext) : ISetListStore
{
    public async Task<IReadOnlyList<SetListRow>> ListForEngagementAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        // Entries stay tracked: the service renumbers them after a removal.
        // (AsNoTracking is query-wide, so the pieces ride along tracked too.)
        var rows = await dbContext.SetListEntries
            .Where(entry => entry.EngagementId == engagementId)
            .Join(
                dbContext.Pieces,
                entry => entry.PieceId,
                piece => piece.Id,
                (entry, piece) => new { entry, piece })
            .OrderBy(row => row.entry.Position)
            .ThenBy(row => row.entry.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new SetListRow(row.entry, row.piece)).ToList();
    }

    public Task<SetListEntry?> FindAsync(
        Guid engagementId,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        return dbContext.SetListEntries.FirstOrDefaultAsync(
            entry => entry.Id == entryId && entry.EngagementId == engagementId,
            cancellationToken);
    }

    public Task AddAsync(SetListEntry entry, CancellationToken cancellationToken)
    {
        dbContext.SetListEntries.Add(entry);
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var tracked = dbContext.SetListEntries.Local.FirstOrDefault(entry => entry.Id == entryId);
        if (tracked is not null)
        {
            dbContext.SetListEntries.Remove(tracked);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await dbContext.SetListEntries
            .Where(entry => entry.Id == entryId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> EngagementIdsWithSetListAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var ids = await dbContext.SetListEntries
            .AsNoTracking()
            .Join(
                dbContext.Engagements.AsNoTracking()
                    .Where(engagement => engagement.OrganisationId == organisationId),
                entry => entry.EngagementId,
                engagement => engagement.Id,
                (_, engagement) => engagement.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
