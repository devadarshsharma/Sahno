namespace Sahno.Domain.Engagements;

public enum EngagementActivityType
{
    Created = 1,
    StatusChanged = 2,
    DateChanged = 3,
}

/// <summary>
/// One entry in an engagement's history. Status changes and significant date
/// changes are kept permanently (Slice 4): cancelling, postponing, reopening,
/// and reversing a completion all stay visible rather than replacing what came
/// before (D-037).
/// </summary>
public sealed class EngagementActivity
{
    private EngagementActivity(
        Guid id,
        Guid engagementId,
        EngagementActivityType type,
        EngagementStatus? fromStatus,
        EngagementStatus? toStatus,
        DateOnly? fromStartDate,
        DateOnly? toStartDate,
        string? reason,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        Type = type;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        FromStartDate = fromStartDate;
        ToStartDate = toStartDate;
        Reason = reason;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
    }

    public const int ReasonMaxLength = 500;

    public Guid Id { get; }

    public Guid EngagementId { get; }

    public EngagementActivityType Type { get; }

    public EngagementStatus? FromStatus { get; }

    public EngagementStatus? ToStatus { get; }

    /// <summary>
    /// The date the engagement was moved away from. Postponement preserves it
    /// here so the original date survives the reschedule (D-035, D-038).
    /// </summary>
    public DateOnly? FromStartDate { get; }

    public DateOnly? ToStartDate { get; }

    /// <summary>
    /// Why, for the transitions that demand an explanation — cancelling,
    /// postponing, reopening, and reversing a completion.
    /// </summary>
    public string? Reason { get; }

    public Guid ActorUserId { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    internal static EngagementActivity Created(Guid engagementId, Guid actorUserId)
    {
        return new EngagementActivity(
            Guid.CreateVersion7(),
            engagementId,
            EngagementActivityType.Created,
            fromStatus: null,
            toStatus: EngagementStatus.Draft,
            fromStartDate: null,
            toStartDate: null,
            reason: null,
            actorUserId,
            DateTimeOffset.UtcNow);
    }

    internal static EngagementActivity StatusChanged(
        Guid engagementId,
        EngagementStatus from,
        EngagementStatus to,
        DateOnly? dateAtTransition,
        string? reason,
        Guid actorUserId)
    {
        return new EngagementActivity(
            Guid.CreateVersion7(),
            engagementId,
            EngagementActivityType.StatusChanged,
            from,
            to,
            // Postponing records the date being left behind, which is what
            // makes the original date recoverable afterwards.
            to == EngagementStatus.Postponed ? dateAtTransition : null,
            toStartDate: null,
            reason,
            actorUserId,
            DateTimeOffset.UtcNow);
    }

    internal static EngagementActivity DateChanged(
        Guid engagementId,
        DateOnly? from,
        DateOnly? to,
        Guid actorUserId)
    {
        return new EngagementActivity(
            Guid.CreateVersion7(),
            engagementId,
            EngagementActivityType.DateChanged,
            fromStatus: null,
            toStatus: null,
            from,
            to,
            reason: null,
            actorUserId,
            DateTimeOffset.UtcNow);
    }
}
