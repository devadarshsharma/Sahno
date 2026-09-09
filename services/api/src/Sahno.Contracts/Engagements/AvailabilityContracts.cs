namespace Sahno.Contracts.Engagements;

/// <summary>
/// One selected Member as an organiser sees them, answer included (D-021).
/// Members never receive these rows — only their own answer.
/// </summary>
public sealed record ParticipantResponse(
    Guid UserId,
    string? DisplayName,
    /// <summary>Null while they have not answered — the state organisers chase.</summary>
    string? Response,
    bool IsActive,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? RespondedAtUtc,
    DateTimeOffset? RemindedAtUtc,
    DateTimeOffset? RemovedAtUtc);

/// <summary>Totals for the lineup, and the count behind the confirmation warning.</summary>
public sealed record AvailabilitySummaryResponse(
    int Selected,
    int Available,
    int Maybe,
    int Unavailable,
    int Outstanding);

/// <summary>The organiser's view: everyone asked, plus the totals.</summary>
public sealed record EngagementAvailabilityResponse(
    AvailabilitySummaryResponse Summary,
    IReadOnlyList<ParticipantResponse> Participants);

/// <summary>Selects Members and asks them. Re-selecting someone restores them.</summary>
public sealed record RequestAvailabilityRequest(IReadOnlyList<Guid> UserIds);

/// <summary>Available, Maybe, or Unavailable.</summary>
public sealed record RespondAvailabilityRequest(string Response);

/// <summary>
/// What a Member is told about their own participation. Deliberately just
/// their own answer: nobody else's is theirs to see (D-021).
/// </summary>
public sealed record OwnAvailabilityResponse(
    bool IsSelected,
    string? Response,
    DateTimeOffset? RespondedAtUtc);
