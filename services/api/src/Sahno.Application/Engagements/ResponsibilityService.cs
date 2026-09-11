using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>One job, with the assignee's name resolved for display.</summary>
public sealed record ResponsibilityRow(
    Responsibility Responsibility,
    string? AssignedDisplayName,
    bool IsYours);

/// <summary>
/// Who is doing or bringing what (Slice 8, D-047 §3).
///
/// The split of authority is the whole point of this service. Organisers own
/// what the job is and whose it is; the person it lands on owns whether it is
/// done and what they want to say about it. Neither can do the other's part —
/// a member renaming a job would change it for everyone, and an organiser
/// ticking someone else's work off would make the list a fiction.
/// </summary>
public sealed class ResponsibilityService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IResponsibilityStore responsibilities,
    IMembershipStore memberships,
    Notifier notifier)
{
    /// <summary>
    /// Everything on this engagement. Members see the whole list rather than
    /// only their own: knowing that someone else is bringing the harmonium is
    /// exactly what stops two people bringing one and nobody bringing the
    /// tabla. Names come from the organisation directory, so a member never
    /// learns anything about a colleague they could not already see (D-022).
    /// </summary>
    public async Task<IReadOnlyList<ResponsibilityRow>?> ListAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!await CanSeeAsync(actor, engagementId, cancellationToken))
        {
            return null;
        }

        var jobs = await responsibilities.ListForEngagementAsync(
            engagementId,
            cancellationToken);

        var directory = await memberships.ListForOrganisationAsync(
            actor.OrganisationId,
            cancellationToken);
        var names = directory.ToDictionary(
            row => row.Membership.UserId,
            row => row.DisplayName);

        return jobs
            .Select(job => new ResponsibilityRow(
                job,
                job.AssignedUserId is { } userId
                    ? names.GetValueOrDefault(userId)
                    : null,
                job.AssignedUserId == actor.UserId))
            .ToList();
    }

    public async Task<(EngagementResult Result, Responsibility? Created)> CreateAsync(
        Membership actor,
        Guid engagementId,
        string title,
        string? detail,
        Guid? assignedUserId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return (EngagementResult.Forbidden, null);
        }

        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return (EngagementResult.NotFound, null);
        }

        if (assignedUserId is { } userId
            && !await CanHoldWorkAsync(actor, engagementId, userId, cancellationToken))
        {
            return (EngagementResult.Invalid, null);
        }

        var responsibility = Responsibility.Create(
            engagementId,
            title,
            detail,
            assignedUserId,
            actor.UserId);

        // Being handed a job is news to the person it lands on. Giving it to
        // yourself is not.
        if (assignedUserId is { } assignee && assignee != actor.UserId)
        {
            await notifier.ResponsibilityAssignedAsync(
                engagement,
                assignee,
                responsibility.Title,
                cancellationToken);
        }

        await responsibilities.AddAsync(responsibility, cancellationToken);
        return (EngagementResult.Success, responsibility);
    }

    /// <summary>Changes what the job is, or whose it is. Organisers only.</summary>
    public async Task<EngagementResult> UpdateAsync(
        Membership actor,
        Guid engagementId,
        Guid responsibilityId,
        string title,
        string? detail,
        Guid? assignedUserId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        var responsibility = await FindInOrganisationAsync(
            actor,
            engagementId,
            responsibilityId,
            cancellationToken);
        if (responsibility is null)
        {
            return EngagementResult.NotFound;
        }

        if (assignedUserId is { } userId
            && !await CanHoldWorkAsync(actor, engagementId, userId, cancellationToken))
        {
            return EngagementResult.Invalid;
        }

        var previousAssignee = responsibility.AssignedUserId;
        responsibility.Describe(title, detail);
        responsibility.AssignTo(assignedUserId);

        // Only a new holder is told. Retitling a job somebody already has is
        // not a handover, and telling them again would be noise.
        if (assignedUserId is { } assignee
            && assignee != previousAssignee
            && assignee != actor.UserId)
        {
            var engagement = await engagements.FindByIdAsync(
                actor.OrganisationId,
                engagementId,
                cancellationToken);
            await notifier.ResponsibilityAssignedAsync(
                engagement!,
                assignee,
                responsibility.Title,
                cancellationToken);
        }

        await responsibilities.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// How it is going, from the person doing it. An organiser may also record
    /// progress — they are often told in person and are the ones keeping the
    /// list honest — but nobody else can touch another person's job.
    /// </summary>
    public async Task<EngagementResult> SetProgressAsync(
        Membership actor,
        Guid engagementId,
        Guid responsibilityId,
        bool isDone,
        string? note,
        CancellationToken cancellationToken)
    {
        var responsibility = await FindInOrganisationAsync(
            actor,
            engagementId,
            responsibilityId,
            cancellationToken);
        if (responsibility is null)
        {
            return EngagementResult.NotFound;
        }

        var isOwnWork = responsibility.AssignedUserId == actor.UserId;
        if (!isOwnWork && !OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        responsibility.SetProgress(isDone, note);
        await responsibilities.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    public async Task<EngagementResult> DeleteAsync(
        Membership actor,
        Guid engagementId,
        Guid responsibilityId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        var responsibility = await FindInOrganisationAsync(
            actor,
            engagementId,
            responsibilityId,
            cancellationToken);
        if (responsibility is null)
        {
            return EngagementResult.NotFound;
        }

        await responsibilities.RemoveAsync(responsibilityId, cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// A job can only be given to someone who can see the event it belongs to
    /// — anyone on the lineup, or an organiser. Assigning work to a Member who
    /// was never selected would leave them holding a task they cannot open.
    /// </summary>
    private async Task<bool> CanHoldWorkAsync(
        Membership actor,
        Guid engagementId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var participant = await participants.FindAsync(
            engagementId,
            userId,
            cancellationToken);
        if (participant is { IsActive: true })
        {
            return true;
        }

        var membership = await memberships.FindAsync(
            actor.OrganisationId,
            userId,
            cancellationToken);

        return membership is not null
            && OrganisationAuthorizationService.IsOrganiser(membership);
    }

    private async Task<Responsibility?> FindInOrganisationAsync(
        Membership actor,
        Guid engagementId,
        Guid responsibilityId,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);

        return engagement is null
            ? null
            : await responsibilities.FindAsync(
                engagementId,
                responsibilityId,
                cancellationToken);
    }

    private async Task<bool> CanSeeAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
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
