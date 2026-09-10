namespace Sahno.Domain.Engagements;

/// <summary>
/// A piece of work on one engagement — who is doing or bringing what
/// (D-047 §3). Organisers create and assign it; the person it lands on says
/// whether it is done (ROLES_AND_PERMISSIONS: "update own assignments").
///
/// Assignment is deliberately optional. An organiser writing out the jobs the
/// night before a gig knows the list long before they know who is taking each
/// one, and forcing a name up front would either stop them writing it down or
/// produce assignments nobody meant.
/// </summary>
public sealed class Responsibility
{
    public const int TitleMaxLength = 120;
    public const int DetailMaxLength = 500;
    public const int NoteMaxLength = 300;

    private Responsibility(
        Guid id,
        Guid engagementId,
        string title,
        string? detail,
        Guid? assignedUserId,
        bool isDone,
        string? note,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        Title = title;
        Detail = detail;
        AssignedUserId = assignedUserId;
        IsDone = isDone;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    public string Title { get; private set; }

    /// <summary>Anything the title cannot carry on its own.</summary>
    public string? Detail { get; private set; }

    /// <summary>
    /// The person, not their membership — the same reason availability answers
    /// hold a user id. Null while the job is written down but unclaimed.
    /// </summary>
    public Guid? AssignedUserId { get; private set; }

    public bool IsDone { get; private set; }

    /// <summary>
    /// The assignee's own word on it — "borrowed Imran's tabla", "still
    /// chasing the harmonium". Theirs to write, which is what makes marking a
    /// job done more useful than a tick alone.
    /// </summary>
    public string? Note { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public bool IsAssigned => AssignedUserId is not null;

    public static Responsibility Create(
        Guid engagementId,
        string title,
        string? detail,
        Guid? assignedUserId,
        Guid createdByUserId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException(
                "An engagement is required.",
                nameof(engagementId));
        }

        return new Responsibility(
            Guid.CreateVersion7(),
            engagementId,
            NormalizeTitle(title),
            NormalizeOptional(detail, DetailMaxLength),
            assignedUserId,
            isDone: false,
            note: null,
            createdByUserId,
            DateTimeOffset.UtcNow,
            completedAtUtc: null);
    }

    /// <summary>What the job is. The organiser's to describe, not the assignee's.</summary>
    public void Describe(string title, string? detail)
    {
        Title = NormalizeTitle(title);
        Detail = NormalizeOptional(detail, DetailMaxLength);
    }

    /// <summary>
    /// Hands the job to someone, or puts it back on the pile. Reassigning
    /// clears the previous assignee's note: it was their account of their
    /// progress, and leaving it against a new name would misattribute it.
    /// </summary>
    public void AssignTo(Guid? userId)
    {
        if (userId == AssignedUserId)
        {
            return;
        }

        AssignedUserId = userId;
        Note = null;
    }

    /// <summary>
    /// How the person doing it says it is going. Reopening clears the
    /// completion time rather than keeping a stale one.
    /// </summary>
    public void SetProgress(bool isDone, string? note)
    {
        if (isDone && !IsDone)
        {
            CompletedAtUtc = DateTimeOffset.UtcNow;
        }
        else if (!isDone)
        {
            CompletedAtUtc = null;
        }

        IsDone = isDone;
        Note = NormalizeOptional(note, NoteMaxLength);
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
