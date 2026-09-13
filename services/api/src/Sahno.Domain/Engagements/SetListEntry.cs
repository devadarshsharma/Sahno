namespace Sahno.Domain.Engagements;

/// <summary>
/// One piece on a booking's set list, in order, with whatever the organiser
/// wants said about it for this booking in particular — "open with this,
/// bride's request" (D-079). The piece itself is a repertoire record; this
/// is the link and the running order.
///
/// Participant-facing in full, like a note addressed to participants (D-023):
/// a set list the performers cannot see is not much of a set list.
/// </summary>
public sealed class SetListEntry
{
    public const int NoteMaxLength = 500;

    private SetListEntry(
        Guid id,
        Guid engagementId,
        Guid pieceId,
        int position,
        string? note,
        Guid addedByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        PieceId = pieceId;
        Position = position;
        Note = note;
        AddedByUserId = addedByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    public Guid PieceId { get; }

    /// <summary>Zero-based running order. Reordering rewrites every position.</summary>
    public int Position { get; private set; }

    public string? Note { get; private set; }

    public Guid AddedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static SetListEntry Create(
        Guid engagementId,
        Guid pieceId,
        int position,
        string? note,
        Guid addedByUserId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException("An engagement is required.", nameof(engagementId));
        }

        if (pieceId == Guid.Empty)
        {
            throw new ArgumentException("A piece is required.", nameof(pieceId));
        }

        return new SetListEntry(
            Guid.CreateVersion7(),
            engagementId,
            pieceId,
            Math.Max(0, position),
            NormalizeNote(note),
            addedByUserId,
            DateTimeOffset.UtcNow);
    }

    public void MoveTo(int position) => Position = Math.Max(0, position);

    public void UpdateNote(string? note) => Note = NormalizeNote(note);

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length > NoteMaxLength ? trimmed[..NoteMaxLength] : trimmed;
    }
}
