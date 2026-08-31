using Sahno.Domain.Organisations;

namespace Sahno.Application.Organisations;

public sealed record InvitationPreview(
    Organisation Organisation,
    Invitation Invitation);

public enum AcceptInvitationResult
{
    Accepted,
    AlreadyMember,
    NotUsable,
}

/// <summary>
/// Shareable-link invitations (D-045, D-077): multi-use until revoked or
/// expired; joining always creates a Member. Email-bound invitations are
/// modelled in the domain but not issued until email delivery exists.
/// </summary>
public sealed class InvitationService(
    IInvitationStore invitations,
    IMembershipStore memberships,
    IOrganisationStore organisations)
{
    public async Task<Invitation> CreateLinkAsync(
        Guid organisationId,
        Guid createdByUserId,
        DateTimeOffset? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var invitation = Invitation.CreateLink(
            organisationId,
            createdByUserId,
            expiresAtUtc);
        await invitations.AddAsync(invitation, cancellationToken);
        return invitation;
    }

    public Task<IReadOnlyList<Invitation>> ListAsync(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        return invitations.ListForOrganisationAsync(organisationId, cancellationToken);
    }

    public async Task<bool> RevokeAsync(
        Guid organisationId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var invitation = await invitations.FindByIdAsync(
            organisationId,
            invitationId,
            cancellationToken);
        if (invitation is null)
        {
            return false;
        }

        invitation.Revoke();
        await invitations.SaveAsync(invitation, cancellationToken);
        return true;
    }

    /// <summary>
    /// The organisation identity shown before joining (D-056). Returns null
    /// for unknown or unusable tokens — the caller learns nothing else.
    /// </summary>
    public async Task<InvitationPreview?> PreviewAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var invitation = await invitations.FindByTokenAsync(token, cancellationToken);
        if (invitation is null || !invitation.IsUsable(DateTimeOffset.UtcNow))
        {
            return null;
        }

        var organisation = await organisations.FindByIdAsync(
            invitation.OrganisationId,
            cancellationToken);
        if (organisation is null)
        {
            return null;
        }

        return new InvitationPreview(organisation, invitation);
    }

    /// <summary>
    /// Joins the caller as a Member (D-045). Idempotent for existing members;
    /// safe under concurrent acceptance via the database uniqueness constraint.
    /// </summary>
    public async Task<(AcceptInvitationResult Result, Membership? Membership)> AcceptAsync(
        string token,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var invitation = await invitations.FindByTokenAsync(token, cancellationToken);
        if (invitation is null || !invitation.IsUsable(DateTimeOffset.UtcNow))
        {
            return (AcceptInvitationResult.NotUsable, null);
        }

        var existing = await memberships.FindAsync(
            invitation.OrganisationId,
            userId,
            cancellationToken);
        if (existing is not null)
        {
            return (AcceptInvitationResult.AlreadyMember, existing);
        }

        var membership = Membership.CreateMember(invitation.OrganisationId, userId);
        var added = await memberships.AddAsync(membership, cancellationToken);
        if (!added)
        {
            // Lost a concurrent-join race; the winner's membership stands.
            var winner = await memberships.FindAsync(
                invitation.OrganisationId,
                userId,
                cancellationToken);
            return (AcceptInvitationResult.AlreadyMember, winner);
        }

        invitation.MarkAccepted(userId);
        await invitations.SaveAsync(invitation, cancellationToken);

        return (AcceptInvitationResult.Accepted, membership);
    }
}
