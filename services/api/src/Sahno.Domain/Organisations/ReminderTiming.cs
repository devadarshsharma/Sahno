namespace Sahno.Domain.Organisations;

/// <summary>
/// When an organisation's day-of reminders go out (D-118), chosen separately
/// for events and rehearsals. Stored as one of these codes; anything else is
/// refused.
/// </summary>
public static class ReminderTiming
{
    /// <summary>08:00 on the day, or two hours before when it starts earlier (D-085). The default.</summary>
    public const string Morning = "morning";

    public const string OneHourBefore = "1h";

    public const string TwoHoursBefore = "2h";

    public const string ThreeHoursBefore = "3h";

    /// <summary>18:00 the evening before.</summary>
    public const string DayBefore = "day-before";

    public const string Off = "off";

    public const int MaxLength = 16;

    public static readonly IReadOnlyList<string> All =
        [Morning, OneHourBefore, TwoHoursBefore, ThreeHoursBefore, DayBefore, Off];

    public static readonly TimeOnly MorningSendTime = new(8, 0);

    public static readonly TimeSpan MorningLeadTime = TimeSpan.FromHours(2);

    public static readonly TimeOnly EveningBeforeSendTime = new(18, 0);

    public static bool IsValid(string? timing) => timing is not null && All.Contains(timing);

    /// <summary>
    /// The local moment a reminder for something on <paramref name="day"/> at
    /// <paramref name="at"/> is due, or null when reminders are off. "Hours
    /// before" something with no time falls back to the morning: there is no
    /// hour to count back from.
    /// </summary>
    public static DateTime? DueAt(string timing, DateOnly day, TimeOnly? at)
    {
        return timing switch
        {
            Off => null,
            DayBefore => day.AddDays(-1).ToDateTime(EveningBeforeSendTime),
            OneHourBefore when at is { } start => day.ToDateTime(start).AddHours(-1),
            TwoHoursBefore when at is { } start => day.ToDateTime(start).AddHours(-2),
            ThreeHoursBefore when at is { } start => day.ToDateTime(start).AddHours(-3),
            _ => day.ToDateTime(MorningSendTimeFor(at)),
        };
    }

    /// <summary>
    /// The latest a reminder still makes sense: once the thing has begun (or,
    /// with no time, once its day is over) it is not sent at all — telling
    /// somebody at noon that a 10:00 rehearsal is today helps nobody.
    /// </summary>
    public static DateTime Deadline(DateOnly day, TimeOnly? at) =>
        at is { } start ? day.ToDateTime(start) : day.AddDays(1).ToDateTime(TimeOnly.MinValue);

    /// <summary>
    /// 08:00, or two hours before <paramref name="at"/> when that is earlier —
    /// a 9:00 rehearsal is reminded about at 7:00, not as it starts. Never
    /// before midnight: an early start still gets its reminder on the day.
    /// </summary>
    public static TimeOnly MorningSendTimeFor(TimeOnly? at)
    {
        if (at is not { } time || time >= MorningSendTime.Add(MorningLeadTime))
        {
            return MorningSendTime;
        }

        return time.ToTimeSpan() >= MorningLeadTime
            ? time.Add(-MorningLeadTime)
            : TimeOnly.MinValue;
    }
}
