namespace Sahno.Contracts.Engagements;

/// <summary>A practice session linked to its engagement (D-047 §4).</summary>
public sealed record RehearsalResponse(
    Guid Id,
    string? Title,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Venue,
    string? Notes,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Organisers only. The date is required — a rehearsal without one is an
/// intention, and the readiness checklist would tick for something nobody
/// could turn up to.
/// </summary>
public sealed record SaveRehearsalRequest(
    string? Title,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Venue,
    string? Notes);

/// <summary>
/// Repertoire, notes, and links (D-047 §5). <see cref="Audience"/> is
/// "Participants" or "AdminsOnly"; a Member is never handed the latter at all
/// (D-023).
/// </summary>
public sealed record EngagementResourceResponse(
    Guid Id,
    string Kind,
    string Title,
    string? Body,
    string? Url,
    string Audience,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Organisers only. <see cref="Kind"/> is "Note" or "Link" — a note carries
/// <see cref="Body"/>, a link carries <see cref="Url"/>. Uploaded files are
/// not here: Sahno has no object storage yet, and a link to a file someone
/// already keeps elsewhere covers the same need honestly.
/// </summary>
public sealed record CreateResourceRequest(
    string Kind,
    string Title,
    string? Body,
    string? Url,
    string? Audience);

/// <summary>
/// The kind is fixed at creation, so it is absent here: a note that became a
/// link would leave everyone who read it holding something else.
/// </summary>
public sealed record UpdateResourceRequest(
    string Title,
    string? Body,
    string? Url,
    string? Audience);
