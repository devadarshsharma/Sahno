namespace Sahno.Domain.Organisations;

/// <summary>
/// Automatic nudges for availability nobody has answered (D-120): one some
/// days after the person was asked, and a last call the day before the event.
/// The organisation chooses how soon the first comes, or turns both off.
/// </summary>
public static class AvailabilityNudge
{
    public const string Off = "off";

    public const string OneDayAfter = "1d";

    public const string TwoDaysAfter = "2d";

    /// <summary>The default: a request answered within two days is the norm, not a chase.</summary>
    public const string Default = TwoDaysAfter;

    public const int MaxLength = 16;

    public static readonly IReadOnlyList<string> All = [TwoDaysAfter, OneDayAfter, Off];

    /// <summary>Nudges never land overnight: before 9:00 they wait, after 20:00 they go next morning.</summary>
    public static readonly TimeOnly EarliestSend = new(9, 0);

    public static readonly TimeOnly LatestSend = new(20, 0);

    /// <summary>The day-before last call goes at 10:00, clear of the 18:00 event reminder.</summary>
    public static readonly TimeOnly LastCallSend = new(10, 0);

    public static bool IsValid(string? nudge) => nudge is not null && All.Contains(nudge);

    /// <summary>
    /// When the first nudge is due, in local time, for somebody asked at
    /// <paramref name="askedLocal"/>; null when nudges are off.
    /// </summary>
    public static DateTime? FirstDueAt(string nudge, DateTime askedLocal)
    {
        var days = nudge switch
        {
            OneDayAfter => 1,
            TwoDaysAfter => 2,
            _ => 0,
        };
        if (days == 0)
        {
            return null;
        }

        var due = askedLocal.AddDays(days);
        var time = TimeOnly.FromDateTime(due);
        if (time < EarliestSend)
        {
            return DateOnly.FromDateTime(due).ToDateTime(EarliestSend);
        }

        return time >= LatestSend
            ? DateOnly.FromDateTime(due).AddDays(1).ToDateTime(EarliestSend)
            : due;
    }

    /// <summary>The day-before last call, in local time; null when nudges are off.</summary>
    public static DateTime? LastCallAt(string nudge, DateOnly eventDay) =>
        nudge == Off ? null : eventDay.AddDays(-1).ToDateTime(LastCallSend);
}
