namespace Sahno.Domain.Organisations;

/// <summary>
/// Organisation roles per D-015. There is exactly one Owner per organisation
/// (D-013); the uniqueness is enforced by the database alongside application
/// checks.
/// </summary>
public enum MembershipRole
{
    Owner = 1,
    Admin = 2,
    Member = 3,
}

/// <summary>
/// A person's membership of one organisation. One account can hold different
/// roles in different organisations (D-044). <see cref="CanManageFinances"/>
/// is the Owner-controlled per-Admin financial permission from D-016 — off by
/// default and meaningless for Members.
/// </summary>
public sealed class Membership
{
    private Membership(
        Guid id,
        Guid organisationId,
        Guid userId,
        MembershipRole role,
        bool canManageFinances,
        DateTimeOffset joinedAtUtc,
        DateTimeOffset? setupChecklistDismissedAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        UserId = userId;
        Role = role;
        CanManageFinances = canManageFinances;
        JoinedAtUtc = joinedAtUtc;
        SetupChecklistDismissedAtUtc = setupChecklistDismissedAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public Guid UserId { get; }

    public MembershipRole Role { get; }

    public bool CanManageFinances { get; }

    public DateTimeOffset JoinedAtUtc { get; }

    /// <summary>
    /// When the new-Owner setup checklist (D-058) was dismissed; null while it
    /// is still shown. Only meaningful for the Owner membership.
    /// </summary>
    public DateTimeOffset? SetupChecklistDismissedAtUtc { get; private set; }

    public static Membership CreateOwner(Guid organisationId, Guid userId)
    {
        return Create(organisationId, userId, MembershipRole.Owner);
    }

    public static Membership CreateMember(Guid organisationId, Guid userId)
    {
        return Create(organisationId, userId, MembershipRole.Member);
    }

    public void DismissSetupChecklist()
    {
        SetupChecklistDismissedAtUtc ??= DateTimeOffset.UtcNow;
    }

    private static Membership Create(
        Guid organisationId,
        Guid userId,
        MembershipRole role)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException(
                "An organisation is required.",
                nameof(organisationId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        return new Membership(
            Guid.CreateVersion7(),
            organisationId,
            userId,
            role,
            canManageFinances: false,
            DateTimeOffset.UtcNow,
            setupChecklistDismissedAtUtc: null);
    }
}
