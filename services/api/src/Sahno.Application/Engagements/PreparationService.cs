using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>
/// Rehearsals and resources (Slice 8, D-047 §4 and §5, D-023).
///
/// Both hang off an engagement and share one rule: a participant may read what
/// is meant for participants, and organisers may read and write everything.
/// The audience setting on a resource is the only place in Sahno where the
/// person creating something chooses who sees it, so the default matters —
/// Participants, because most event material exists to be shared, and the
/// person who needs to hide something will say so deliberately.
/// </summary>
public sealed class PreparationService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IRehearsalStore rehearsals,
    IEngagementResourceStore resources)
{
    /// <summary>
    /// Rehearsals for this engagement. Participant-facing in full: a rehearsal
    /// nobody on the lineup can see is a meeting nobody attends.
    /// </summary>
    public async Task<IReadOnlyList<Rehearsal>?> ListRehearsalsAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!await CanSeeAsync(actor, engagementId, cancellationToken))
        {
            return null;
        }

        return await rehearsals.ListForEngagementAsync(engagementId, cancellationToken);
    }

    public async Task<(EngagementResult Result, Rehearsal? Created)> ScheduleRehearsalAsync(
        Membership actor,
        Guid engagementId,
        string? title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? venue,
        string? notes,
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

        var rehearsal = Rehearsal.Schedule(
            engagementId,
            title,
            date,
            startTime,
            endTime,
            venue,
            notes,
            actor.UserId);

        await rehearsals.AddAsync(rehearsal, cancellationToken);
        return (EngagementResult.Success, rehearsal);
    }

    public async Task<EngagementResult> UpdateRehearsalAsync(
        Membership actor,
        Guid engagementId,
        Guid rehearsalId,
        string? title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? venue,
        string? notes,
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

        var rehearsal = await rehearsals.FindAsync(
            engagementId,
            rehearsalId,
            cancellationToken);
        if (rehearsal is null)
        {
            return EngagementResult.NotFound;
        }

        rehearsal.Update(title, date, startTime, endTime, venue, notes);
        await rehearsals.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    public async Task<EngagementResult> DeleteRehearsalAsync(
        Membership actor,
        Guid engagementId,
        Guid rehearsalId,
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

        var rehearsal = await rehearsals.FindAsync(
            engagementId,
            rehearsalId,
            cancellationToken);
        if (rehearsal is null)
        {
            return EngagementResult.NotFound;
        }

        await rehearsals.RemoveAsync(rehearsalId, cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Notes and links. A Member is handed only the ones addressed to
    /// participants — the Admins-only ones are not filtered out of a longer
    /// list on the client, they never leave here (D-023).
    /// </summary>
    public async Task<IReadOnlyList<EngagementResource>?> ListResourcesAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!await CanSeeAsync(actor, engagementId, cancellationToken))
        {
            return null;
        }

        var attached = await resources.ListForEngagementAsync(
            engagementId,
            cancellationToken);

        if (OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return attached;
        }

        return attached
            .Where(resource => resource.IsVisibleToParticipants)
            .ToList();
    }

    public async Task<(EngagementResult Result, EngagementResource? Created)> AddResourceAsync(
        Membership actor,
        Guid engagementId,
        ResourceKind kind,
        string title,
        string? body,
        string? url,
        ResourceAudience audience,
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

        var resource = kind == ResourceKind.Note
            ? EngagementResource.CreateNote(
                engagementId,
                title,
                body ?? string.Empty,
                audience,
                actor.UserId)
            : EngagementResource.CreateLink(
                engagementId,
                title,
                url ?? string.Empty,
                audience,
                actor.UserId);

        await resources.AddAsync(resource, cancellationToken);
        return (EngagementResult.Success, resource);
    }

    public async Task<EngagementResult> UpdateResourceAsync(
        Membership actor,
        Guid engagementId,
        Guid resourceId,
        string title,
        string? body,
        string? url,
        ResourceAudience audience,
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

        var resource = await resources.FindAsync(
            engagementId,
            resourceId,
            cancellationToken);
        if (resource is null)
        {
            return EngagementResult.NotFound;
        }

        resource.Update(title, body, url, audience);
        await resources.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    public async Task<EngagementResult> DeleteResourceAsync(
        Membership actor,
        Guid engagementId,
        Guid resourceId,
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

        var resource = await resources.FindAsync(
            engagementId,
            resourceId,
            cancellationToken);
        if (resource is null)
        {
            return EngagementResult.NotFound;
        }

        await resources.RemoveAsync(resourceId, cancellationToken);
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
