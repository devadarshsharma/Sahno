using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

/// <summary>
/// Organisation creation and listing (D-044, D-056, D-057).
/// </summary>
public sealed class OrganisationService(
    IOrganisationStore organisations,
    IMembershipStore memberships)
{
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

        return new OrganisationMembership(organisation, membership);
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
