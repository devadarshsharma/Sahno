using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

/// <summary>An organisation together with the caller's role in it.</summary>
public sealed record OrganisationMembership(
    Organisation Organisation,
    Membership Membership);

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

    Task<IReadOnlyList<OrganisationMembership>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists a new membership. Returns <c>false</c> when a concurrent
    /// request created a membership for the same person first (uniqueness is
    /// enforced by the database); the caller should re-read.
    /// </summary>
    Task<bool> AddAsync(Membership membership, CancellationToken cancellationToken);

    Task SaveAsync(Membership membership, CancellationToken cancellationToken);
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
