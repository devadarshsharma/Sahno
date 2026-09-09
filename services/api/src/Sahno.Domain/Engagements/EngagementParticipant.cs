namespace Sahno.Domain.Engagements;

/// <summary>
/// A Member's answer to an availability request. Absent until they reply —
/// "no answer yet" is a real state, and the one organisers chase (Slice 5).
/// </summary>
public enum AvailabilityResponse
{
    Available = 1,
    Maybe = 2,
    Unavailable = 3,
}

/// <summary>
/// One Member selected for one engagement, and their availability answer.
///
/// Responses live per participant rather than as a batch, which is what lets a
/// lineup change mid-collection without disturbing anyone else: adding someone
/// adds a row, and everyone else's answer is untouched (D-027).
///
/// Removal is recorded, not erased. A removed Member loses access, but their
/// answer stays in the organisation's internal history so the record of who
/// said what remains true (D-027).
/// </summary>
public sealed class EngagementParticipant
{
    private EngagementParticipant(
        Guid id,
        Guid engagementId,
        Guid userId,
        DateTimeOffset requestedAtUtc,
        AvailabilityResponse? response,
        DateTimeOffset? respondedAtUtc,
        DateTimeOffset? remindedAtUtc,
        DateTimeOffset? removedAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        UserId = userId;
        RequestedAtUtc = requestedAtUtc;
        Response = response;
        RespondedAtUtc = respondedAtUtc;
        RemindedAtUtc = remindedAtUtc;
        RemovedAtUtc = removedAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    /// <summary>
    /// The person, not their membership. Memberships can be removed outright;
    /// an availability answer has to outlive that to stay honest.
    /// </summary>
    public Guid UserId { get; }

    public DateTimeOffset RequestedAtUtc { get; }

    public AvailabilityResponse? Response { get; private set; }

    public DateTimeOffset? RespondedAtUtc { get; private set; }

    /// <summary>When they were last chased. Null if they never have been.</summary>
    public DateTimeOffset? RemindedAtUtc { get; private set; }

    public DateTimeOffset? RemovedAtUtc { get; private set; }

    public bool IsActive => RemovedAtUtc is null;

    /// <summary>
    /// Outstanding means selected, still on the lineup, and yet to answer.
    /// This is what the confirmation warning counts (D-029).
    /// </summary>
    public bool IsOutstanding => IsActive && Response is null;

    public static EngagementParticipant Request(Guid engagementId, Guid userId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException(
                "An engagement is required.",
                nameof(engagementId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user is required.", nameof(userId));
        }

        return new EngagementParticipant(
            Guid.CreateVersion7(),
            engagementId,
            userId,
            DateTimeOffset.UtcNow,
            response: null,
            respondedAtUtc: null,
            remindedAtUtc: null,
            removedAtUtc: null);
    }

    /// <summary>
    /// Records this person's answer. They may change it while they are still
    /// on the lineup: availability changes, and a stale answer is worse than a
    /// corrected one.
    /// </summary>
    public void Respond(AvailabilityResponse response)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                "A removed participant cannot answer.");
        }

        Response = response;
        RespondedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkReminded()
    {
        if (!IsOutstanding)
        {
            throw new InvalidOperationException(
                "Only someone who has not answered can be reminded.");
        }

        RemindedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Remove()
    {
        RemovedAtUtc ??= DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Puts someone back on the lineup. Their previous answer is deliberately
    /// kept: they answered about this date, and that has not stopped being
    /// true because they were briefly off the list.
    /// </summary>
    public void Restore()
    {
        RemovedAtUtc = null;
    }
}
