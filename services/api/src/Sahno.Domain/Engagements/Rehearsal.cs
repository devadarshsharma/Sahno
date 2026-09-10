namespace Sahno.Domain.Engagements;

/// <summary>
/// A practice session linked to its parent engagement (D-047 §4). Not an
/// engagement of its own: a rehearsal has no customer, no lifecycle, and
/// nothing to confirm — it exists so the group knows when and where they are
/// meeting to prepare for the event it hangs off.
///
/// A date is required. A rehearsal without one is an intention, and the
/// readiness checklist would tick for something nobody could turn up to.
/// </summary>
public sealed class Rehearsal
{
    public const int TitleMaxLength = 120;
    public const int VenueMaxLength = 200;
    public const int NotesMaxLength = 500;

    private Rehearsal(
        Guid id,
        Guid engagementId,
        string? title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? venue,
        string? notes,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        Title = title;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        Venue = venue;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    /// <summary>
    /// Optional, because most rehearsals need no name. One is worth having
    /// when a group runs several — "full run", "harmonium only".
    /// </summary>
    public string? Title { get; private set; }

    public DateOnly Date { get; private set; }

    public TimeOnly? StartTime { get; private set; }

    public TimeOnly? EndTime { get; private set; }

    public string? Venue { get; private set; }

    /// <summary>Participant-facing: what to bring, what is being worked on.</summary>
    public string? Notes { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static Rehearsal Schedule(
        Guid engagementId,
        string? title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? venue,
        string? notes,
        Guid createdByUserId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException(
                "An engagement is required.",
                nameof(engagementId));
        }

        return new Rehearsal(
            Guid.CreateVersion7(),
            engagementId,
            NormalizeOptional(title, TitleMaxLength),
            date,
            startTime,
            NormalizeEndTime(startTime, endTime),
            NormalizeOptional(venue, VenueMaxLength),
            NormalizeOptional(notes, NotesMaxLength),
            createdByUserId,
            DateTimeOffset.UtcNow);
    }

    public void Update(
        string? title,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? venue,
        string? notes)
    {
        Title = NormalizeOptional(title, TitleMaxLength);
        Date = date;
        StartTime = startTime;
        EndTime = NormalizeEndTime(startTime, endTime);
        Venue = NormalizeOptional(venue, VenueMaxLength);
        Notes = NormalizeOptional(notes, NotesMaxLength);
    }

    /// <summary>
    /// An end before its start is a typo, not a rehearsal running backwards.
    /// Dropping it leaves the start time, which is the useful half.
    /// </summary>
    private static TimeOnly? NormalizeEndTime(TimeOnly? startTime, TimeOnly? endTime)
    {
        if (startTime is null || endTime is null)
        {
            return endTime;
        }

        return endTime <= startTime ? null : endTime;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
