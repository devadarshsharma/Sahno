namespace Sahno.Domain.Engagements;

/// <summary>
/// The readiness checklist from D-048. A checklist, not a score: it tells an
/// organiser what action remains, where a percentage would imply a precision
/// nobody has.
/// </summary>
public enum ReadinessItem
{
    /// <summary>Everyone selected has answered and a lineup is settled.</summary>
    Lineup = 1,
    Venue = 2,

    /// <summary>Call or sound-check time.</summary>
    CallTime = 3,

    /// <summary>Performance or start time.</summary>
    StartTime = 4,
    Responsibilities = 5,
    Dress = 6,
    Rehearsal = 7,

    /// <summary>Repertoire or resources ready.</summary>
    Resources = 8,
}

public enum ReadinessState
{
    /// <summary>Still to do. This is what an organiser is chasing.</summary>
    Outstanding = 1,

    Done = 2,

    /// <summary>Marked as not applying to this event (D-048).</summary>
    NotRequired = 3,
}

public sealed record ReadinessEntry(ReadinessItem Item, ReadinessState State);

/// <summary>
/// What the rest of the system knows about an engagement's preparation, so the
/// checklist can be derived rather than maintained. Everything here is a fact
/// about other records — the lineup, the responsibility list, the rehearsals,
/// the resources — which is what stops the checklist drifting out of step with
/// the event it describes (D-048).
/// </summary>
public sealed record ReadinessFacts(
    bool LineupResolved,
    bool ResponsibilitiesAssigned,
    bool RehearsalOrganised,
    bool ResourcesReady)
{
    public static readonly ReadinessFacts None = new(false, false, false, false);
}



/// <summary>
/// An organiser's decision that a checklist item does not apply to this event
/// — no dress code for a rehearsal-room session, no sound check for a small
/// acoustic set. Stored per item rather than as a blanket dismissal so the
/// rest of the checklist keeps working.
/// </summary>
public sealed class ReadinessWaiver
{
    private ReadinessWaiver(
        Guid id,
        Guid engagementId,
        ReadinessItem item,
        Guid waivedByUserId,
        DateTimeOffset waivedAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        Item = item;
        WaivedByUserId = waivedByUserId;
        WaivedAtUtc = waivedAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    public ReadinessItem Item { get; }

    public Guid WaivedByUserId { get; }

    public DateTimeOffset WaivedAtUtc { get; }

    public static ReadinessWaiver Create(
        Guid engagementId,
        ReadinessItem item,
        Guid waivedByUserId)
    {
        return new ReadinessWaiver(
            Guid.CreateVersion7(),
            engagementId,
            item,
            waivedByUserId,
            DateTimeOffset.UtcNow);
    }
}
