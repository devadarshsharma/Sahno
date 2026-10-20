using Sahno.Application.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

public enum OrganisationDeleteResult
{
    Deleted,
    NotFound,
    NotOwner,
    NameMismatch,
}

public enum OrganisationSettingResult
{
    Saved,
    NotFound,
    NotOrganiser,
    Invalid,
}

public enum OrganisationRestoreResult
{
    Restored,
    NotFound,
    TooLate,
}

/// <summary>
/// Organisation creation, listing, deletion and restoring (D-044, D-056, D-057, D-101).
/// </summary>
public sealed class OrganisationService(
    IOrganisationStore organisations,
    IMembershipStore memberships,
    Notifier notifier,
    TimeProvider clock)
{
    /// <summary>
    /// Deletes the organisation (D-101): it disappears for everyone at once,
    /// and its Owner can restore it for <see cref="Organisation.RestoreWindow"/>
    /// before it is purged. The Owner's alone — one Admin should not be able
    /// to end it for everybody — and only with its name typed out, so a
    /// stray request cannot do it either.
    /// </summary>
    public async Task<OrganisationDeleteResult> DeleteAsync(
        Guid organisationId,
        Guid userId,
        string? confirmName,
        CancellationToken cancellationToken)
    {
        var membership = await memberships.FindAsync(organisationId, userId, cancellationToken);
        var organisation = membership is null
            ? null
            : await organisations.FindByIdAsync(organisationId, cancellationToken);
        if (membership is null || organisation is null)
        {
            return OrganisationDeleteResult.NotFound;
        }

        if (membership.Role != MembershipRole.Owner)
        {
            return OrganisationDeleteResult.NotOwner;
        }

        if (!string.Equals(confirmName?.Trim(), organisation.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return OrganisationDeleteResult.NameMismatch;
        }

        organisation.MarkDeleted(userId, clock.GetUtcNow());

        var everyoneElse = (await organisations.ListMembersIncludingDeletedAsync(organisationId, cancellationToken))
            .Select(member => member.UserId)
            .Where(memberId => memberId != userId);
        await notifier.OrganisationDeletedAsync(organisation, everyoneElse, cancellationToken);

        // One save: the deletion and the pushes about it commit together, and
        // connected phones are told it changed (LiveUpdateInterceptor).
        await organisations.SaveAsync(organisation, cancellationToken);
        return OrganisationDeleteResult.Deleted;
    }

    /// <summary>
    /// Brings a deleted organisation back exactly as it was, while the
    /// restore window is open (D-101). The Owner's alone; anyone else, or an
    /// organisation not deleted, is NotFound.
    /// </summary>
    public async Task<OrganisationRestoreResult> RestoreAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var organisation = await organisations.FindIncludingDeletedAsync(organisationId, cancellationToken);
        var members = organisation?.DeletedAtUtc is null
            ? []
            : await organisations.ListMembersIncludingDeletedAsync(organisationId, cancellationToken);
        if (organisation is null || !members.Any(member => member.UserId == userId && member.Role == MembershipRole.Owner))
        {
            return OrganisationRestoreResult.NotFound;
        }

        if (organisation.PurgeAfterUtc <= clock.GetUtcNow())
        {
            return OrganisationRestoreResult.TooLate;
        }

        organisation.Restore();
        await notifier.OrganisationRestoredAsync(
            organisation,
            members.Select(member => member.UserId).Where(memberId => memberId != userId),
            cancellationToken);
        await organisations.SaveAsync(organisation, cancellationToken);
        return OrganisationRestoreResult.Restored;
    }

    /// <summary>
    /// When the organisation's event and rehearsal reminders go out (D-118).
    /// Owner and Admins: they run the events the reminders are about.
    /// </summary>
    public async Task<OrganisationSettingResult> SetRemindersAsync(
        Guid organisationId,
        Guid userId,
        string? eventReminder,
        string? rehearsalReminder,
        string? availabilityNudge,
        CancellationToken cancellationToken)
    {
        var membership = await memberships.FindAsync(organisationId, userId, cancellationToken);
        var organisation = membership is null
            ? null
            : await organisations.FindByIdAsync(organisationId, cancellationToken);
        if (membership is null || organisation is null)
        {
            return OrganisationSettingResult.NotFound;
        }

        if (!OrganisationAuthorizationService.IsOrganiser(membership))
        {
            return OrganisationSettingResult.NotOrganiser;
        }

        if (!ReminderTiming.IsValid(eventReminder) || !ReminderTiming.IsValid(rehearsalReminder)
            || (availabilityNudge is not null && !AvailabilityNudge.IsValid(availabilityNudge)))
        {
            return OrganisationSettingResult.Invalid;
        }

        organisation.SetReminders(eventReminder!, rehearsalReminder!, availabilityNudge);
        await organisations.SaveAsync(organisation, cancellationToken);
        return OrganisationSettingResult.Saved;
    }

    /// <summary>What this person deleted and can still restore (D-101).</summary>
    public async Task<IReadOnlyList<Organisation>> ListRestorableAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return (await organisations.ListDeletedOwnedByAsync(userId, cancellationToken))
            .Where(organisation => organisation.PurgeAfterUtc > now)
            .ToList();
    }

    /// <summary>
    /// Removes for good every organisation whose restore window has passed
    /// (D-101). Run by the background worker; returns how many went.
    /// </summary>
    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var expired = await organisations.ListDeletedBeforeAsync(
            clock.GetUtcNow() - Organisation.RestoreWindow,
            cancellationToken);
        foreach (var organisationId in expired)
        {
            await organisations.PurgeAsync(organisationId, cancellationToken);
        }

        return expired.Count;
    }

    /// <summary>Creates an organisation with the caller as its single Owner.</summary>
    public async Task<OrganisationMembership> CreateAsync(
        Guid userId,
        string name,
        string? groupType,
        string? timeZoneId,
        CancellationToken cancellationToken)
    {
        var organisation = Organisation.Create(name, groupType, timeZoneId);
        var ownerMembership = Membership.CreateOwner(organisation.Id, userId);

        await organisations.AddWithOwnerAsync(
            organisation,
            ownerMembership,
            cancellationToken);

        return new OrganisationMembership(organisation, ownerMembership);
    }

    public Task<IReadOnlyList<OrganisationMembership>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return memberships.ListForUserAsync(userId, cancellationToken);
    }

    /// <summary>Returns the organisation only when the caller is a member.</summary>
    public async Task<OrganisationMembership?> GetForMemberAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await memberships.FindAsync(
            organisationId,
            userId,
            cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var organisation = await organisations.FindByIdAsync(
            organisationId,
            cancellationToken);
        if (organisation is null)
        {
            return null;
        }

        var memberCount = await memberships.CountForOrganisationAsync(
            organisationId,
            cancellationToken);

        return new OrganisationMembership(organisation, membership, memberCount);
    }

    /// <summary>
    /// Makes this organisation the one the app opens in for this person
    /// (D-099), and no other. False when they are not a member of it.
    /// </summary>
    public async Task<bool> SetDefaultAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var chosen = await memberships.FindAsync(organisationId, userId, cancellationToken);
        if (chosen is null)
        {
            return false;
        }

        foreach (var row in await memberships.ListForUserAsync(userId, cancellationToken))
        {
            if (row.Membership.IsDefault && row.Organisation.Id != organisationId
                && await memberships.FindAsync(row.Organisation.Id, userId, cancellationToken) is { } previous)
            {
                previous.ClearDefault();
                await memberships.SaveAsync(previous, cancellationToken);
            }
        }

        chosen.MakeDefault();
        await memberships.SaveAsync(chosen, cancellationToken);
        return true;
    }

    public async Task<bool> DismissSetupChecklistAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var membership = await memberships.FindAsync(
            organisationId,
            userId,
            cancellationToken);
        if (membership is null)
        {
            return false;
        }

        membership.DismissSetupChecklist();
        await memberships.SaveAsync(membership, cancellationToken);
        return true;
    }
}
