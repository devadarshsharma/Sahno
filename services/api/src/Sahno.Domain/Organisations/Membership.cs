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
    public const int FunctionMaxLength = 80;

    public const int InternalNotesMaxLength = 2000;
    private Membership(
        Guid id,
        Guid organisationId,
        Guid userId,
        MembershipRole role,
        bool canManageFinances,
        string? function,
        bool sharesContactDetails,
        string? internalNotes,
        DateTimeOffset joinedAtUtc,
        DateTimeOffset? setupChecklistDismissedAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        UserId = userId;
        Role = role;
        CanManageFinances = canManageFinances;
        Function = function;
        SharesContactDetails = sharesContactDetails;
        InternalNotes = internalNotes;
        JoinedAtUtc = joinedAtUtc;
        SetupChecklistDismissedAtUtc = setupChecklistDismissedAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public Guid UserId { get; }

    public MembershipRole Role { get; private set; }

    public bool CanManageFinances { get; private set; }

    /// <summary>
    /// What this person does in this organisation — singer, tabla player,
    /// volunteer (D-046). Per organisation rather than per account: the same
    /// person can hold a different function in each.
    /// </summary>
    public string? Function { get; private set; }

    /// <summary>
    /// Whether this person has chosen to show their contact details to
    /// ordinary Members of this organisation (D-018). Off by default, theirs
    /// alone to change, and per organisation — sharing with one group is not
    /// sharing with another.
    /// </summary>
    public bool SharesContactDetails { get; private set; }

    /// <summary>
    /// Organiser-only notes about this member (D-046). Never shown to
    /// Members, including the person they are about.
    /// </summary>
    public string? InternalNotes { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; }

    /// <summary>
    /// When the new-Owner setup checklist (D-058) was dismissed; null while it
    /// is still shown. Only meaningful for the Owner membership.
    /// </summary>
    public DateTimeOffset? SetupChecklistDismissedAtUtc { get; private set; }

    /// <summary>
    /// Whether this membership may see and manage protected financial data
    /// (D-016). The Owner always may; an Admin only once the Owner has granted
    /// it; a Member never. Read this rather than <see cref="CanManageFinances"/>,
    /// which is only the Admin grant flag.
    /// </summary>
    public bool HasFinancialAccess =>
        Role == MembershipRole.Owner
        || (Role == MembershipRole.Admin && CanManageFinances);

    public static Membership CreateOwner(Guid organisationId, Guid userId)
    {
        return Create(organisationId, userId, MembershipRole.Owner);
    }

    public static Membership CreateMember(Guid organisationId, Guid userId)
    {
        return Create(organisationId, userId, MembershipRole.Member);
    }


    /// <summary>
    /// Updates what this person shows about themselves in this organisation.
    /// Theirs to set: the function others see, and whether ordinary Members
    /// may see their contact details (D-018).
    /// </summary>
    public void SetOwnProfile(string? function, bool sharesContactDetails)
    {
        Function = Normalize(function, FunctionMaxLength);
        SharesContactDetails = sharesContactDetails;
    }

    /// <summary>Records organiser-only notes about this member (D-046).</summary>
    public void SetInternalNotes(string? internalNotes)
    {
        InternalNotes = Normalize(internalNotes, InternalNotesMaxLength);
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
    public void DismissSetupChecklist()
    {
        SetupChecklistDismissedAtUtc ??= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Moves this membership between Admin and Member. Ownership is not
    /// reachable here: it moves only through <see cref="TransferOwnership"/>,
    /// which keeps the single-Owner rule (D-013) and makes transfer a
    /// deliberate act rather than an ordinary role edit.
    /// </summary>
    public void ChangeRoleTo(MembershipRole role)
    {
        if (role == MembershipRole.Owner)
        {
            throw new InvalidOperationException(
                "Ownership moves only through an explicit transfer.");
        }

        if (Role == MembershipRole.Owner)
        {
            throw new InvalidOperationException(
                "The Owner's role changes only by transferring ownership.");
        }

        Role = role;

        // Financial access is a grant to a specific Admin. Someone who stops
        // being an Admin loses it rather than carrying it into a later
        // reappointment.
        if (Role != MembershipRole.Admin)
        {
            CanManageFinances = false;
        }
    }

    /// <summary>
    /// Grants or revokes the per-Admin financial permission (D-016). Only
    /// Admins carry the flag: the Owner always has access and a Member never
    /// does.
    /// </summary>
    public void SetFinancialAccess(bool canManageFinances)
    {
        if (Role != MembershipRole.Admin)
        {
            throw new InvalidOperationException(
                "Financial access is granted to Admins only.");
        }

        CanManageFinances = canManageFinances;
    }

    /// <summary>
    /// Hands ownership to another membership of the same organisation. The
    /// outgoing Owner stays on as an Admin — the organisation is never left
    /// without an Owner, and never briefly has two. The outgoing Owner's
    /// financial access is not assumed: it returns to the D-016 default of off
    /// for the new Owner to grant deliberately.
    /// </summary>
    public static void TransferOwnership(
        Membership outgoingOwner,
        Membership incomingOwner)
    {
        ArgumentNullException.ThrowIfNull(outgoingOwner);
        ArgumentNullException.ThrowIfNull(incomingOwner);

        if (outgoingOwner.Role != MembershipRole.Owner)
        {
            throw new InvalidOperationException(
                "Only the current Owner can transfer ownership.");
        }

        if (outgoingOwner.OrganisationId != incomingOwner.OrganisationId)
        {
            throw new InvalidOperationException(
                "Ownership can only be transferred within one organisation.");
        }

        if (outgoingOwner.Id == incomingOwner.Id)
        {
            throw new InvalidOperationException(
                "Ownership cannot be transferred to the current Owner.");
        }

        outgoingOwner.Role = MembershipRole.Admin;
        outgoingOwner.CanManageFinances = false;

        incomingOwner.Role = MembershipRole.Owner;
        incomingOwner.CanManageFinances = false;
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
            function: null,
            sharesContactDetails: false,
            internalNotes: null,
            DateTimeOffset.UtcNow,
            setupChecklistDismissedAtUtc: null);
    }
}
