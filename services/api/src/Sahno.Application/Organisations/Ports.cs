using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

/// <summary>An organisation together with the caller's role in it.</summary>
public sealed record OrganisationMembership(
    Organisation Organisation,
    Membership Membership,
    int MemberCount = 1);

/// <summary>
/// A directory row: one membership with the identity details from its account.
/// Contact details travel with the row so the caller's own role can decide
/// what to reveal (D-018); they are never revealed by the store itself.
/// </summary>
public sealed record OrganisationMember(
    Membership Membership,
    string? DisplayName,
    string? Email,
    string? PhoneNumber);

public interface IOrganisationStore
{
    Task<Organisation?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Persists a new organisation and its Owner membership atomically.</summary>
    Task AddWithOwnerAsync(
        Organisation organisation,
        Membership ownerMembership,
        CancellationToken cancellationToken);
}

public interface IMembershipStore
{
    Task<Membership?> FindAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>One membership of an organisation, addressed by membership id.</summary>
    Task<Membership?> FindByIdAsync(
        Guid organisationId,
        Guid membershipId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganisationMembership>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>The member directory for one organisation, oldest first.</summary>
    Task<IReadOnlyList<OrganisationMember>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    Task<int> CountForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new membership. Returns <c>false</c> when a concurrent
    /// request created a membership for the same person first (uniqueness is
    /// enforced by the database); the caller should re-read.
    /// </summary>
    Task<bool> AddAsync(Membership membership, CancellationToken cancellationToken);

    Task SaveAsync(Membership membership, CancellationToken cancellationToken);

    Task RemoveAsync(Membership membership, CancellationToken cancellationToken);

    /// <summary>
    /// Persists both sides of an ownership transfer. The outgoing Owner is
    /// demoted before the incoming Owner is promoted, so the single-Owner
    /// index (D-013) is never transiently violated, and both moves land in one
    /// transaction so the organisation cannot be left without an Owner.
    /// </summary>
    Task TransferOwnershipAsync(
        Membership outgoingOwner,
        Membership incomingOwner,
        CancellationToken cancellationToken);
}

public interface IInvitationStore
{
    Task<Invitation?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    Task<Invitation?> FindByIdAsync(
        Guid organisationId,
        Guid invitationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Invitation>> ListForOrganisationAsync(
        Guid organisationId,
        CancellationToken cancellationToken);

    Task AddAsync(Invitation invitation, CancellationToken cancellationToken);

    Task SaveAsync(Invitation invitation, CancellationToken cancellationToken);
}
