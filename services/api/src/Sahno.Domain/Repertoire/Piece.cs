namespace Sahno.Domain.Repertoire;

/// <summary>
/// One piece the group performs — a qawwali, a song, a set — kept once for
/// the organisation and picked onto bookings, so the lyrics are typed in one
/// time and the same piece is the same piece on every set list (D-079).
///
/// The repertoire is the group's shared knowledge, so any member may add or
/// edit a piece and its lyrics; who last touched it is recorded. Deleting one
/// is an organiser's call. The organiser notes are the one part a member does
/// not see.
/// </summary>
public sealed class Piece
{
    public const int TitleMaxLength = 200;
    public const int AttributionMaxLength = 200;
    public const int LanguageMaxLength = 60;
    public const int KeyMaxLength = 60;
    public const int LyricsMaxLength = 20000;
    public const int NotesMaxLength = 4000;
    public const int DurationMaxMinutes = 600;

    private Piece(
        Guid id,
        Guid organisationId,
        string title,
        string? attribution,
        string? language,
        string? key,
        int? durationMinutes,
        string? lyrics,
        string? notes,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganisationId = organisationId;
        Title = title;
        Attribution = attribution;
        Language = language;
        Key = key;
        DurationMinutes = durationMinutes;
        Lyrics = lyrics;
        Notes = notes;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public Guid OrganisationId { get; }

    public string Title { get; private set; }

    /// <summary>Composer, poet, or whose version it is — "Nusrat", "Amir Khusrau".</summary>
    public string? Attribution { get; private set; }

    public string? Language { get; private set; }

    /// <summary>Key or raag, however the group thinks of it.</summary>
    public string? Key { get; private set; }

    public int? DurationMinutes { get; private set; }

    /// <summary>Written however the group sings it; one block of text.</summary>
    public string? Lyrics { get; private set; }

    /// <summary>
    /// The organiser's own notes — "drop the third verse on a short set".
    /// Organisers only; never on a member's copy of the piece.
    /// </summary>
    public string? Notes { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public Guid UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool HasLyrics => Lyrics is not null;

    public static Piece Create(
        Guid organisationId,
        string title,
        string? attribution,
        string? language,
        string? key,
        int? durationMinutes,
        string? lyrics,
        Guid createdByUserId)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException("An organisation is required.", nameof(organisationId));
        }

        var now = DateTimeOffset.UtcNow;
        return new Piece(
            Guid.CreateVersion7(),
            organisationId,
            RequireTitle(title),
            Normalize(attribution, AttributionMaxLength),
            Normalize(language, LanguageMaxLength),
            Normalize(key, KeyMaxLength),
            NormalizeDuration(durationMinutes),
            Normalize(lyrics, LyricsMaxLength),
            notes: null,
            createdByUserId,
            now,
            createdByUserId,
            now);
    }

    /// <summary>Any member: the piece and its lyrics belong to the group.</summary>
    public void Update(
        string title,
        string? attribution,
        string? language,
        string? key,
        int? durationMinutes,
        string? lyrics,
        Guid updatedByUserId)
    {
        Title = RequireTitle(title);
        Attribution = Normalize(attribution, AttributionMaxLength);
        Language = Normalize(language, LanguageMaxLength);
        Key = Normalize(key, KeyMaxLength);
        DurationMinutes = NormalizeDuration(durationMinutes);
        Lyrics = Normalize(lyrics, LyricsMaxLength);
        Touch(updatedByUserId);
    }

    /// <summary>Organisers only; kept apart from Update so a member's edit cannot reach it.</summary>
    public void UpdateNotes(string? notes, Guid updatedByUserId)
    {
        Notes = Normalize(notes, NotesMaxLength);
        Touch(updatedByUserId);
    }

    private void Touch(Guid userId)
    {
        UpdatedByUserId = userId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string RequireTitle(string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A piece needs a title.", nameof(title));
        }

        return trimmed.Length > TitleMaxLength ? trimmed[..TitleMaxLength] : trimmed;
    }

    private static int? NormalizeDuration(int? minutes)
    {
        if (minutes is null)
        {
            return null;
        }

        if (minutes <= 0 || minutes > DurationMaxMinutes)
        {
            throw new ArgumentException("A duration is between 1 and 600 minutes.", nameof(minutes));
        }

        return minutes;
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}

/// <summary>A link that goes with a piece: a recording, a reference, a folder.</summary>
public sealed class PieceLink
{
    public const int TitleMaxLength = 150;
    public const int UrlMaxLength = 2000;

    private PieceLink(
        Guid id,
        Guid pieceId,
        string title,
        string url,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        PieceId = pieceId;
        Title = title;
        Url = url;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid PieceId { get; }

    public string Title { get; }

    public string Url { get; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static PieceLink Create(Guid pieceId, string title, string url, Guid createdByUserId)
    {
        var trimmedUrl = url?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || trimmedUrl.Length > UrlMaxLength)
        {
            throw new ArgumentException("A link needs a web address.", nameof(url));
        }

        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            trimmedTitle = uri.Host;
        }

        return new PieceLink(
            Guid.CreateVersion7(),
            pieceId,
            trimmedTitle.Length > TitleMaxLength ? trimmedTitle[..TitleMaxLength] : trimmedTitle,
            trimmedUrl,
            createdByUserId,
            DateTimeOffset.UtcNow);
    }
}
