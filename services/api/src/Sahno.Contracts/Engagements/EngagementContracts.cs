namespace Sahno.Contracts.Engagements;

/// <summary>
/// One engagement. Almost every field is nullable because a Draft may be
/// saved with only a title, and venue, time, and dates can stay TBC while an
/// organiser is still finding out (D-025).
/// </summary>
public sealed record EngagementResponse(
    Guid Id,
    string Title,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TimeOnly? StartTime,
    string? Venue,
    /// <summary>Whether Members have been told this exists.</summary>
    bool IsSharedWithMembers,
    /// <summary>Whether the date can still be set without postponing.</summary>
    bool CanChangeDateDirectly,
    /// <summary>Whether it can be discarded outright rather than cancelled.</summary>
    bool CanBeDiscarded,
    /// <summary>The states it may move to next, so a client can offer only those.</summary>
    IReadOnlyList<string> AllowedTransitions,
    /// <summary>How many members are on the lineup. Null for members, who do not see it.</summary>
    int? SelectedCount,
    /// <summary>How many have still to answer. Null for members (D-021).</summary>
    int? OutstandingCount,
    /// <summary>The caller's own answer, when they are on the lineup themselves.</summary>
    string? YourResponse,
    DateTimeOffset CreatedAtUtc);

/// <summary>Only a title is required (D-025).</summary>
public sealed record CreateEngagementRequest(
    string Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TimeOnly? StartTime,
    string? Venue);

/// <summary>The details that may stay TBC. Dates move on their own route.</summary>
public sealed record UpdateEngagementRequest(
    string? Title,
    TimeOnly? StartTime,
    string? Venue);

/// <summary>
/// Sets the proposed date or range. Accepted for a Draft, or for a Postponed
/// engagement being rescheduled; otherwise the answer is to postpone (D-038).
/// </summary>
public sealed record SetEngagementDatesRequest(DateOnly? StartDate, DateOnly? EndDate);

/// <summary>
/// Moves the engagement. Cancelling, postponing, reopening, and reversing a
/// completion each require a reason. Confirming while selected Members have
/// not answered requires AcknowledgeOutstanding (D-029).
/// </summary>
public sealed record TransitionEngagementRequest(
    string Status,
    string? Reason,
    bool AcknowledgeOutstanding = false);


/// <summary>One entry of the engagement's history.</summary>
public sealed record EngagementActivityResponse(
    Guid Id,
    string Type,
    string? FromStatus,
    string? ToStatus,
    DateOnly? FromStartDate,
    DateOnly? ToStartDate,
    string? Reason,
    Guid ActorUserId,
    DateTimeOffset OccurredAtUtc);
