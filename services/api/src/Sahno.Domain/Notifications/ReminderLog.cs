namespace Sahno.Domain.Notifications;

/// <summary>
/// What a scheduled reminder is about (D-085). Kept apart from
/// <see cref="NotificationKind"/> because one reminder fans out into several
/// kinds of notification (the day-of reminder is plain for some people and
/// carries their open jobs for others) but is sent, and logged, once.
/// </summary>
public enum ReminderKind
{
    /// <summary>The day of a confirmed engagement. Subject: the engagement.</summary>
    EngagementDay = 1,

    /// <summary>The day of a rehearsal. Subject: the rehearsal.</summary>
    RehearsalDay = 2,
}

/// <summary>
/// A scheduled reminder that has gone out (D-085). The scheduler checks every
/// few minutes for reminders that are due; this row is how it knows one has
/// already been sent, across restarts and redeploys. It is written in the same
/// save as the notifications it produced, so a reminder is either sent and
/// logged or neither — never sent twice, never logged unsent.
///
/// Unique on (kind, subject, occasion): one reminder per rehearsal or event
/// per local day.
/// </summary>
public sealed class ReminderLog
{
    private ReminderLog(
        Guid id,
        ReminderKind kind,
        Guid subjectId,
        DateOnly occasionDate,
        DateTimeOffset sentAtUtc)
    {
        Id = id;
        Kind = kind;
        SubjectId = subjectId;
        OccasionDate = occasionDate;
        SentAtUtc = sentAtUtc;
    }

    public Guid Id { get; }

    public ReminderKind Kind { get; }

    /// <summary>The engagement or rehearsal the reminder was about.</summary>
    public Guid SubjectId { get; }

    /// <summary>The organisation-local day it was for.</summary>
    public DateOnly OccasionDate { get; }

    public DateTimeOffset SentAtUtc { get; }

    public static ReminderLog Sent(
        ReminderKind kind,
        Guid subjectId,
        DateOnly occasionDate,
        DateTimeOffset sentAtUtc)
    {
        return new ReminderLog(
            Guid.CreateVersion7(),
            kind,
            subjectId,
            occasionDate,
            sentAtUtc);
    }
}
