using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

/// <summary>
/// Why a membership change was refused. Distinguishing "not found" from
/// "forbidden" matters here: a caller who is not a member of the organisation
/// learns nothing about it, while a member who lacks the authority is told
/// plainly.
/// </summary>
public enum MembershipChangeResult
{
    Success,

    /// <summary>No such membership in this organisation.</summary>
    NotFound,

    /// <summary>The caller's role does not permit the change.</summary>
    Forbidden,

    /// <summary>The change is not a legal move regardless of who asked.</summary>
    Invalid,
}

/// <summary>
/// Membership management and its safeguards (Slice 3, D-013 to D-017).
///
/// Every rule here answers the same question — can this actor make this change
/// to this target — and the answers are deliberately narrow:
///
/// - Nobody edits their own membership, which is what stops a role change
///   being used to escalate one's own authority.
/// - Only the Owner appoints or removes Admins, so an Admin cannot grow the
///   set of people who share their authority.
/// - Admins act on ordinary Members only; the Owner and fellow Admins are out
///   of reach.
/// - The Owner is never demoted or removed here. Ownership moves only by
///   explicit transfer, which is why an organisation always has exactly one.
/// </summary>
public sealed class MembershipService(IMembershipStore memberships)
{
    public Task<IReadOnlyList<OrganisationMember>> ListAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return memberships.ListForOrganisationAsync(organisationId, cancellationToken);
    }

    /// <summary>
    /// Moves a member between Admin and Member. Appointing or removing an
    /// Admin is the Owner's alone (D-014); an Admin may only act on ordinary
    /// Members, and so cannot promote anyone to their own level.
    /// </summary>
    public async Task<MembershipChangeResult> ChangeRoleAsync(
        Membership actor,
        Guid targetMembershipId,
        MembershipRole role,
        CancellationToken cancellationToken)
    {
        if (role is not (MembershipRole.Admin or MembershipRole.Member))
        {
            return MembershipChangeResult.Invalid;
        }

        var target = await memberships.FindByIdAsync(
            actor.OrganisationId,
            targetMembershipId,
            cancellationToken);
        if (target is null)
        {
            return MembershipChangeResult.NotFound;
        }

        if (target.Id == actor.Id)
        {
            return MembershipChangeResult.Forbidden;
        }

        if (target.Role == MembershipRole.Owner)
        {
            return MembershipChangeResult.Forbidden;
        }

        // Only the Owner may appoint an Admin, or take the role back.
        var touchesAdminAuthority =
            role == MembershipRole.Admin || target.Role == MembershipRole.Admin;
        if (touchesAdminAuthority && actor.Role != MembershipRole.Owner)
        {
            return MembershipChangeResult.Forbidden;
        }

        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return MembershipChangeResult.Forbidden;
        }

        target.ChangeRoleTo(role);
        await memberships.SaveAsync(target, cancellationToken);
        return MembershipChangeResult.Success;
    }

    /// <summary>
    /// Grants or revokes an Admin's financial permission. The Owner alone
    /// controls it and it is off by default (D-016).
    /// </summary>
    public async Task<MembershipChangeResult> SetFinancialAccessAsync(
        Membership actor,
        Guid targetMembershipId,
        bool canManageFinances,
        CancellationToken cancellationToken)
    {
        if (actor.Role != MembershipRole.Owner)
        {
            return MembershipChangeResult.Forbidden;
        }

        var target = await memberships.FindByIdAsync(
            actor.OrganisationId,
            targetMembershipId,
            cancellationToken);
        if (target is null)
        {
            return MembershipChangeResult.NotFound;
        }

        // The Owner already has access and a Member cannot be given it, so
        // there is no meaningful flag to set on either.
        if (target.Role != MembershipRole.Admin)
        {
            return MembershipChangeResult.Invalid;
        }

        target.SetFinancialAccess(canManageFinances);
        await memberships.SaveAsync(target, cancellationToken);
        return MembershipChangeResult.Success;
    }

    /// <summary>
    /// Removes someone from the organisation. The Owner cannot be removed —
    /// ownership must be transferred first — and Admins may remove ordinary
    /// Members only. Leaving of one's own accord is a separate action and is
    /// not reachable here.
    /// </summary>
    public async Task<MembershipChangeResult> RemoveAsync(
        Membership actor,
        Guid targetMembershipId,
        CancellationToken cancellationToken)
    {
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return MembershipChangeResult.Forbidden;
        }

        var target = await memberships.FindByIdAsync(
            actor.OrganisationId,
            targetMembershipId,
            cancellationToken);
        if (target is null)
        {
            return MembershipChangeResult.NotFound;
        }

        if (target.Id == actor.Id)
        {
            return MembershipChangeResult.Forbidden;
        }

        if (target.Role == MembershipRole.Owner)
        {
            return MembershipChangeResult.Forbidden;
        }

        if (target.Role == MembershipRole.Admin && actor.Role != MembershipRole.Owner)
        {
            return MembershipChangeResult.Forbidden;
        }

        await memberships.RemoveAsync(target, cancellationToken);
        return MembershipChangeResult.Success;
    }

    /// <summary>
    /// Hands ownership to another member (D-014). Deliberately separate from
    /// an ordinary role edit, and the only way the Owner's own role ever
    /// changes.
    /// </summary>
    public async Task<MembershipChangeResult> TransferOwnershipAsync(
        Membership actor,
        Guid targetMembershipId,
        CancellationToken cancellationToken)
    {
        if (actor.Role != MembershipRole.Owner)
        {
            return MembershipChangeResult.Forbidden;
        }

        var target = await memberships.FindByIdAsync(
            actor.OrganisationId,
            targetMembershipId,
            cancellationToken);
        if (target is null)
        {
            return MembershipChangeResult.NotFound;
        }

        if (target.Id == actor.Id)
        {
            return MembershipChangeResult.Invalid;
        }

        Membership.TransferOwnership(actor, target);
        await memberships.TransferOwnershipAsync(actor, target, cancellationToken);
        return MembershipChangeResult.Success;
    }
}
