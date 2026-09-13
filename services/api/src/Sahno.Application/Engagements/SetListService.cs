using Sahno.Application.Organisations;
using Sahno.Application.Repertoire;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>
/// A booking's set list: pieces from the repertoire, in running order, each
/// with a note for this booking (D-079). Read by everyone on the lineup, like
/// a note addressed to participants (D-023); arranged by organisers, like
/// everything else about how the event is put together.
/// </summary>
public sealed class SetListService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    ISetListStore setLists,
    IPieceStore pieces)
{
    public async Task<IReadOnlyList<SetListRow>?> ListAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!await CanSeeAsync(actor, engagementId, cancellationToken))
        {
            return null;
        }

        return await setLists.ListForEngagementAsync(engagementId, cancellationToken);
    }

    /// <summary>Appends a piece. The same piece can appear twice — an encore is a real thing.</summary>
    public async Task<(EngagementResult Result, SetListEntry? Created)> AddAsync(
        Membership actor,
        Guid engagementId,
        Guid pieceId,
        string? note,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return (EngagementResult.Forbidden, null);
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return (EngagementResult.NotFound, null);
        }

        // A piece from another organisation's repertoire is Invalid, not
        // NotFound: the booking exists; the request is wrong.
        if (await pieces.FindAsync(actor.OrganisationId, pieceId, cancellationToken) is null)
        {
            return (EngagementResult.Invalid, null);
        }

        var current = await setLists.ListForEngagementAsync(engagementId, cancellationToken);
        var entry = SetListEntry.Create(
            engagementId,
            pieceId,
            current.Count,
            note,
            actor.UserId);

        await setLists.AddAsync(entry, cancellationToken);
        return (EngagementResult.Success, entry);
    }

    public async Task<EngagementResult> UpdateNoteAsync(
        Membership actor,
        Guid engagementId,
        Guid entryId,
        string? note,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var entry = await setLists.FindAsync(engagementId, entryId, cancellationToken);
        if (entry is null)
        {
            return EngagementResult.NotFound;
        }

        entry.UpdateNote(note);
        await setLists.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Rewrites the running order from a full list of entry ids. Every entry
    /// must be named exactly once, so a stale client cannot silently drop one.
    /// </summary>
    public async Task<EngagementResult> ReorderAsync(
        Membership actor,
        Guid engagementId,
        IReadOnlyList<Guid> orderedEntryIds,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var current = await setLists.ListForEngagementAsync(engagementId, cancellationToken);
        var byId = current.ToDictionary(row => row.Entry.Id, row => row.Entry);

        if (orderedEntryIds.Count != byId.Count
            || orderedEntryIds.Distinct().Count() != orderedEntryIds.Count
            || orderedEntryIds.Any(id => !byId.ContainsKey(id)))
        {
            return EngagementResult.Invalid;
        }

        for (var position = 0; position < orderedEntryIds.Count; position++)
        {
            byId[orderedEntryIds[position]].MoveTo(position);
        }

        await setLists.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    public async Task<EngagementResult> RemoveAsync(
        Membership actor,
        Guid engagementId,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var entry = await setLists.FindAsync(engagementId, entryId, cancellationToken);
        if (entry is null)
        {
            return EngagementResult.NotFound;
        }

        await setLists.RemoveAsync(entryId, cancellationToken);

        // Close the gap so positions stay dense; a later reorder expects that.
        var remaining = await setLists.ListForEngagementAsync(engagementId, cancellationToken);
        for (var position = 0; position < remaining.Count; position++)
        {
            remaining[position].Entry.MoveTo(position);
        }

        await setLists.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    private async Task<bool> ExistsAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);

        return engagement is not null;
    }

    private async Task<bool> CanSeeAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(actor, engagementId, cancellationToken))
        {
            return false;
        }

        if (OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return true;
        }

        var participant = await participants.FindAsync(
            engagementId,
            actor.UserId,
            cancellationToken);

        return participant is { IsActive: true };
    }
}
