namespace Sahno.Domain.Engagements;

/// <summary>
/// One opportunity through its whole life, from an early enquiry to a
/// completed event (ENGAGEMENT_STATE_MACHINE.md).
///
/// Almost everything here is optional on purpose: a Draft can be saved with
/// only a title, and venue, time, and customer details may stay TBC while an
/// organiser is still finding out (D-025). The lifecycle carries the rules
/// instead — what may follow what, and what must be explained.
/// </summary>
public sealed class Engagement
{
    public const int TitleMaxLength = 200;
    public const int VenueMaxLength = 200;

    private Engagement(
        Guid id,
        Guid organisationId,
        string title,
        EngagementStatus status,
        DateOnly? startDate,
        DateOnly? endDate,
        TimeOnly? startTime,
        string? venue,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        Title = title;
        Status = status;
        StartDate = startDate;
        EndDate = endDate;
        StartTime = startTime;
        Venue = venue;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public string Title { get; private set; }

    public EngagementStatus Status { get; private set; }

    /// <summary>
    /// The proposed date, or the first day of a proposed range. Null while the
    /// date is still unknown — allowed in Draft, but availability cannot be
    /// requested without it.
    /// </summary>
    public DateOnly? StartDate { get; private set; }

    /// <summary>The last day of a proposed range; null for a single day.</summary>
    public DateOnly? EndDate { get; private set; }

    public TimeOnly? StartTime { get; private set; }

    public string? Venue { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Whether Members have been told this exists. Everything outside Draft
    /// has been shared, which is what makes silent date changes unacceptable
    /// from then on (D-038).
    /// </summary>
    public bool IsSharedWithMembers => Status != EngagementStatus.Draft;

    /// <summary>
    /// A Draft is private, so it can simply be discarded. Anything Members
    /// have seen is cancelled instead, with a reason, and kept (D-034).
    /// </summary>
    public bool CanBeDiscarded => Status == EngagementStatus.Draft;

    public static (Engagement Engagement, EngagementActivity Activity) CreateDraft(
        Guid organisationId,
        string title,
        DateOnly? startDate,
        DateOnly? endDate,
        TimeOnly? startTime,
        string? venue,
        Guid createdByUserId)
    {
        var engagement = new Engagement(
            Guid.CreateVersion7(),
            organisationId,
            NormalizeTitle(title),
            EngagementStatus.Draft,
            startDate,
            NormalizeEndDate(startDate, endDate),
            startTime,
            NormalizeOptional(venue, VenueMaxLength),
            createdByUserId,
            DateTimeOffset.UtcNow);

        return (engagement, EngagementActivity.Created(engagement.Id, createdByUserId));
    }

    /// <summary>
    /// What may follow what (ENGAGEMENT_STATE_MACHINE.md "Accepted
    /// transition"). Anything absent here is refused, so the normal path can
    /// skip a step when reality does — a booking that arrives already
    /// confirmed goes straight from Draft — while nothing can move backwards
    /// except through the deliberate reopen and resume routes.
    /// </summary>
    private static readonly Dictionary<EngagementStatus, EngagementStatus[]> Allowed =
        new()
        {
            [EngagementStatus.Draft] =
            [
                EngagementStatus.CheckingAvailability,
                EngagementStatus.Confirmed,
            ],
            [EngagementStatus.CheckingAvailability] =
            [
                EngagementStatus.Tentative,
                EngagementStatus.Confirmed,
                EngagementStatus.Cancelled,
                EngagementStatus.Postponed,
            ],
            [EngagementStatus.Tentative] =
            [
                EngagementStatus.Confirmed,
                EngagementStatus.Cancelled,
                EngagementStatus.Postponed,
            ],
            [EngagementStatus.Confirmed] =
            [
                EngagementStatus.Completed,
                EngagementStatus.Cancelled,
                EngagementStatus.Postponed,
            ],
            // Resuming picks the state that matches reality (D-036).
            [EngagementStatus.Postponed] =
            [
                EngagementStatus.CheckingAvailability,
                EngagementStatus.Tentative,
                EngagementStatus.Confirmed,
            ],
            // Reopening a cancellation, likewise (D-037).
            [EngagementStatus.Cancelled] =
            [
                EngagementStatus.CheckingAvailability,
                EngagementStatus.Tentative,
                EngagementStatus.Confirmed,
            ],
            // Only to undo a completion recorded by mistake (D-037).
            [EngagementStatus.Completed] = [EngagementStatus.Confirmed],
        };

    /// <summary>
    /// Transitions that must be explained. Each either tells Members something
    /// changed or walks back something already recorded, so an unexplained one
    /// would leave the history unreadable later.
    /// </summary>
    public static bool RequiresReason(EngagementStatus from, EngagementStatus to)
    {
        return to is EngagementStatus.Cancelled or EngagementStatus.Postponed
            || from is EngagementStatus.Cancelled or EngagementStatus.Completed;
    }

    public static bool IsTransitionAllowed(EngagementStatus from, EngagementStatus to)
    {
        return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    /// <summary>
    /// Moves the engagement, or throws if the move is not one the lifecycle
    /// permits. Returns the history entry the caller must persist: a
    /// transition that left no trace would defeat the point of keeping one.
    /// </summary>
    public EngagementActivity TransitionTo(
        EngagementStatus target,
        Guid actorUserId,
        string? reason)
    {
        if (target == Status)
        {
            throw new InvalidOperationException(
                $"The engagement is already {Status}.");
        }

        if (!IsTransitionAllowed(Status, target))
        {
            throw new InvalidOperationException(
                $"An engagement cannot move from {Status} to {target}.");
        }

        // Availability is a question about a date, so there has to be one to
        // ask about (Slice 4 acceptance criteria, D-025).
        if (target == EngagementStatus.CheckingAvailability && StartDate is null)
        {
            throw new InvalidOperationException(
                "A proposed date is required before requesting availability.");
        }

        var trimmedReason = NormalizeOptional(reason, EngagementActivity.ReasonMaxLength);
        if (RequiresReason(Status, target) && trimmedReason is null)
        {
            throw new InvalidOperationException(
                $"Moving from {Status} to {target} requires a reason.");
        }

        var activity = EngagementActivity.StatusChanged(
            Id,
            Status,
            target,
            StartDate,
            trimmedReason,
            actorUserId);

        Status = target;
        return activity;
    }

    /// <summary>
    /// Edits the details that may stay TBC. Dates are handled separately,
    /// because once Members have been told, moving a date is a reschedule
    /// rather than an edit.
    /// </summary>
    public void UpdateDetails(string? title, TimeOnly? startTime, string? venue)
    {
        if (Status is EngagementStatus.Completed or EngagementStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"A {Status} engagement cannot be edited.");
        }

        if (title is not null)
        {
            Title = NormalizeTitle(title);
        }

        StartTime = startTime;
        Venue = NormalizeOptional(venue, VenueMaxLength);
    }

    /// <summary>
    /// Whether the date can be set directly. A Draft is private so it may be
    /// changed freely; a Postponed engagement is being rescheduled, which is
    /// exactly when a replacement date is entered. In every other state
    /// Members hold the old date, so it moves only through postponement
    /// (D-038).
    /// </summary>
    public bool CanChangeDateDirectly =>
        Status is EngagementStatus.Draft or EngagementStatus.Postponed;

    /// <summary>
    /// Sets the proposed date or range. Returns a history entry once Members
    /// are involved; a Draft's date moves without ceremony because nobody has
    /// been told it yet.
    /// </summary>
    public EngagementActivity? SetDates(
        DateOnly? startDate,
        DateOnly? endDate,
        Guid actorUserId)
    {
        if (!CanChangeDateDirectly)
        {
            throw new InvalidOperationException(
                $"A {Status} engagement's date changes by postponing it, so that "
                + "Members are told and their availability is not left misleading.");
        }

        var previous = StartDate;
        StartDate = startDate;
        EndDate = NormalizeEndDate(startDate, endDate);

        if (Status == EngagementStatus.Draft || previous == startDate)
        {
            return null;
        }

        return EngagementActivity.DateChanged(Id, previous, startDate, actorUserId);
    }

    private static string NormalizeTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A title is required.", nameof(title));
        }

        return trimmed.Length > TitleMaxLength ? trimmed[..TitleMaxLength] : trimmed;
    }

    /// <summary>An end before the start is not a range; treat it as one day.</summary>
    private static DateOnly? NormalizeEndDate(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate is null || endDate is null || endDate <= startDate)
        {
            return null;
        }

        return endDate;
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
