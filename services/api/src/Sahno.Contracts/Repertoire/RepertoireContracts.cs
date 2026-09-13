namespace Sahno.Contracts.Repertoire;

public sealed record PieceLinkResponse(
    Guid Id,
    string Title,
    string Url);

/// <summary>
/// One piece in the repertoire (D-079). Every member sees it, lyrics
/// included; <c>Notes</c> is the organiser's and comes back null to anyone
/// else. <c>UseCount</c> is how many bookings it has been on.
/// </summary>
public sealed record PieceResponse(
    Guid Id,
    string Title,
    string? Attribution,
    string? Language,
    string? Key,
    int? DurationMinutes,
    bool HasLyrics,
    string? Lyrics,
    string? Notes,
    IReadOnlyList<PieceLinkResponse> Links,
    int UseCount,
    Guid UpdatedByUserId,
    DateTimeOffset UpdatedAtUtc);

/// <summary>A booking a piece was on.</summary>
public sealed record PieceBookingResponse(
    Guid EngagementId,
    string Title,
    string Status,
    DateOnly? StartDate);

public sealed record PieceDetailResponse(
    PieceResponse Piece,
    IReadOnlyList<PieceBookingResponse> Bookings);

public sealed record SavePieceRequest(
    string Title,
    string? Attribution,
    string? Language,
    string? Key,
    int? DurationMinutes,
    string? Lyrics);

public sealed record UpdatePieceNotesRequest(string? Notes);

public sealed record AddPieceLinkRequest(string? Title, string Url);

/// <summary>One line of a booking's set list, with the piece resolved.</summary>
public sealed record SetListEntryResponse(
    Guid Id,
    Guid PieceId,
    int Position,
    string Title,
    string? Attribution,
    int? DurationMinutes,
    bool HasLyrics,
    string? Note);

public sealed record AddSetListEntryRequest(Guid PieceId, string? Note);

public sealed record UpdateSetListEntryRequest(string? Note);

/// <summary>Every entry id, in the new running order.</summary>
public sealed record ReorderSetListRequest(IReadOnlyList<Guid> EntryIds);
