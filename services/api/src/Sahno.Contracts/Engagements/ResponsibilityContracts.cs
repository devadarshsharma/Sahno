namespace Sahno.Contracts.Engagements;

/// <summary>
/// One job on an engagement (Slice 8, D-047 §3). Members and organisers get
/// the same shape: the list is shared so the group can see the whole picture,
/// and <see cref="IsYours"/> is what a member's own screen sorts on.
/// </summary>
public sealed record ResponsibilityResponse(
    Guid Id,
    string Title,
    string? Detail,
    Guid? AssignedUserId,
    string? AssignedDisplayName,
    bool IsYours,
    bool IsDone,
    string? Note,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Organisers only. <see cref="AssignedUserId"/> may be null: a job can be
/// written down before anyone has taken it on.
/// </summary>
public sealed record CreateResponsibilityRequest(
    string Title,
    string? Detail,
    Guid? AssignedUserId);

/// <summary>Organisers only — what the job is, and whose it is.</summary>
public sealed record UpdateResponsibilityRequest(
    string Title,
    string? Detail,
    Guid? AssignedUserId);

/// <summary>
/// The assignee's own update, and the only part of a responsibility a Member
/// may change (ROLES_AND_PERMISSIONS: "update own assignments").
/// </summary>
public sealed record SetResponsibilityProgressRequest(bool IsDone, string? Note);
