using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

/// <summary>
/// The single place organisation role checks live. Authorisation always
/// derives from Sahno's own membership records (never client input or
/// identity-provider metadata) per D-063 and ROLES_AND_PERMISSIONS.md.
/// </summary>
public sealed class OrganisationAuthorizationService(IMembershipStore memberships)
{
    public Task<Membership?> FindMembershipAsync(
        Guid organisationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return memberships.FindAsync(organisationId, userId, cancellationToken);
    }

    /// <summary>Owner or Admin — the roles allowed to manage invitations and settings.</summary>
    public static bool IsOrganiser(Membership membership)
    {
        return membership.Role is MembershipRole.Owner or MembershipRole.Admin;
    }
}
