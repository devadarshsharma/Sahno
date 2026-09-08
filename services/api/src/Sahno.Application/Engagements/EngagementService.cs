using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

public enum EngagementResult
{
    Success,

    /// <summary>No such engagement in this organisation.</summary>
    NotFound,

    /// <summary>The caller's role does not permit it.</summary>
    Forbidden,

    /// <summary>The lifecycle does not allow it, whoever asked.</summary>
    Invalid,
}

/// <summary>
/// Engagement creation and lifecycle (Slice 4). Creating and moving
/// engagements belongs to Owners and Admins (D-019); Members see the ones they
/// are part of, which is Slice 5's business once selection exists.
///
/// The lifecycle rules themselves live in <see cref="Engagement"/>. This layer
/// answers who may ask, and makes sure the history entry a change produces is
/// saved with it.
/// </summary>
public sealed class EngagementService(IEngagementStore engagements)
{
    public Task<IReadOnlyList<Engagement>> ListAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return engagements.ListForOrganisationAsync(organisationId, cancellationToken);
    }

    public Task<Engagement?> FindAsync(
        Guid organisationId,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return engagements.FindByIdAsync(organisationId, engagementId, cancellationToken);
    }

    public Task<IReadOnlyList<EngagementActivity>> ListActivityAsync(
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        return engagements.ListActivityAsync(engagementId, cancellationToken);
    }

    /// <summary>
    /// Starts a Draft. Only a title is required — the rest is what the
    /// organiser does not know yet (D-025).
    /// </summary>
    public async Task<Engagement> CreateDraftAsync(
        Membership actor,
        string title,
        DateOnly? startDate,
        DateOnly? endDate,
        TimeOnly? startTime,
        string? venue,
        CancellationToken cancellationToken)
    {
        var (engagement, activity) = Engagement.CreateDraft(
            actor.OrganisationId,
            title,
            startDate,
            endDate,
            startTime,
            venue,
            actor.UserId);

        await engagements.AddAsync(engagement, activity, cancellationToken);
        return engagement;
    }

    public async Task<EngagementResult> UpdateDetailsAsync(
        Membership actor,
        Guid engagementId,
        string? title,
        TimeOnly? startTime,
        string? venue,
        CancellationToken cancellationToken)
    {
        var engagement = await FindForOrganiserAsync(actor, engagementId, cancellationToken);
        if (engagement is null)
        {
            return MissingOrForbidden(actor);
        }

        try
        {
            engagement.UpdateDetails(title, startTime, venue);
        }
        catch (Exception exception)
            when (exception is InvalidOperationException or ArgumentException)
        {
            return EngagementResult.Invalid;
        }

        await engagements.SaveAsync(engagement, null, cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Sets the proposed date. Refused once Members hold the old one — that
    /// route is postponement, so their availability is never left describing a
    /// date that has quietly moved (D-038).
    /// </summary>
    public async Task<EngagementResult> SetDatesAsync(
        Membership actor,
        Guid engagementId,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        var engagement = await FindForOrganiserAsync(actor, engagementId, cancellationToken);
        if (engagement is null)
        {
            return MissingOrForbidden(actor);
        }

        EngagementActivity? activity;
        try
        {
            activity = engagement.SetDates(startDate, endDate, actor.UserId);
        }
        catch (InvalidOperationException)
        {
            return EngagementResult.Invalid;
        }

        await engagements.SaveAsync(engagement, activity, cancellationToken);
        return EngagementResult.Success;
    }

    public async Task<EngagementResult> TransitionAsync(
        Membership actor,
        Guid engagementId,
        EngagementStatus target,
        string? reason,
        CancellationToken cancellationToken)
    {
        var engagement = await FindForOrganiserAsync(actor, engagementId, cancellationToken);
        if (engagement is null)
        {
            return MissingOrForbidden(actor);
        }

        EngagementActivity activity;
        try
        {
            activity = engagement.TransitionTo(target, actor.UserId, reason);
        }
        catch (InvalidOperationException)
        {
            return EngagementResult.Invalid;
        }

        await engagements.SaveAsync(engagement, activity, cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Discards a Draft outright. Anything Members have seen is cancelled with
    /// a reason instead, and kept (D-034).
    /// </summary>
    public async Task<EngagementResult> DiscardDraftAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await FindForOrganiserAsync(actor, engagementId, cancellationToken);
        if (engagement is null)
        {
            return MissingOrForbidden(actor);
        }

        if (!engagement.CanBeDiscarded)
        {
            return EngagementResult.Invalid;
        }

        await engagements.RemoveAsync(engagement, cancellationToken);
        return EngagementResult.Success;
    }

    private async Task<Engagement?> FindForOrganiserAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return null;
        }

        return await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
    }

    /// <summary>
    /// A Member is told they may not; an organiser looking at something that
    /// is not there is told it is not there.
    /// </summary>
    private static EngagementResult MissingOrForbidden(Membership actor)
    {
        return OrganisationAuthorizationService.IsOrganiser(actor)
            ? EngagementResult.NotFound
            : EngagementResult.Forbidden;
    }
}
