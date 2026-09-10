namespace Sahno.Domain.Engagements;

/// <summary>Who a note or link is for (D-023).</summary>
public enum ResourceAudience
{
    /// <summary>
    /// Selected participants, plus the Owner and Admins. The default, because
    /// most event material exists to be shared with the people performing.
    /// </summary>
    Participants = 1,

    /// <summary>The Owner and Admins only — internal planning material.</summary>
    AdminsOnly = 2,
}

public enum ResourceKind
{
    /// <summary>Written material — running order, lyrics, a reminder.</summary>
    Note = 1,

    /// <summary>A link out: a recording, a shared folder, a venue page.</summary>
    Link = 2,
}

/// <summary>
/// Repertoire, files, and links attached to an engagement (D-047 §5).
///
/// Uploaded files are not here yet: Sahno has no object storage, so offering
/// an upload would mean promising something that could not be delivered. A
/// link to a file someone already keeps in Drive or Dropbox covers the same
/// need until Spaces exists.
///
/// Financial content stays governed by D-016 whatever audience is chosen. The
/// engagement carries no financial fields until Slice 11, so today that rule
/// binds nothing — when it does, this is where it has to be enforced.
/// </summary>
public sealed class EngagementResource
{
    public const int TitleMaxLength = 150;
    public const int BodyMaxLength = 4000;
    public const int UrlMaxLength = 2000;

    private EngagementResource(
        Guid id,
        Guid engagementId,
        ResourceKind kind,
        string title,
        string? body,
        string? url,
        ResourceAudience audience,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EngagementId = engagementId;
        Kind = kind;
        Title = title;
        Body = body;
        Url = url;
        Audience = audience;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid EngagementId { get; }

    public ResourceKind Kind { get; }

    public string Title { get; private set; }

    /// <summary>The written content of a note. Null on a link.</summary>
    public string? Body { get; private set; }

    /// <summary>Where a link points. Null on a note.</summary>
    public string? Url { get; private set; }

    public ResourceAudience Audience { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public bool IsVisibleToParticipants =>
        Audience == ResourceAudience.Participants;

    public static EngagementResource CreateNote(
        Guid engagementId,
        string title,
        string body,
        ResourceAudience audience,
        Guid createdByUserId)
    {
        var text = body?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            throw new ArgumentException("A note needs something in it.", nameof(body));
        }

        return new EngagementResource(
            Guid.CreateVersion7(),
            RequireEngagement(engagementId),
            ResourceKind.Note,
            NormalizeTitle(title),
            text.Length > BodyMaxLength ? text[..BodyMaxLength] : text,
            url: null,
            audience,
            createdByUserId,
            DateTimeOffset.UtcNow);
    }

    public static EngagementResource CreateLink(
        Guid engagementId,
        string title,
        string url,
        ResourceAudience audience,
        Guid createdByUserId)
    {
        return new EngagementResource(
            Guid.CreateVersion7(),
            RequireEngagement(engagementId),
            ResourceKind.Link,
            NormalizeTitle(title),
            body: null,
            NormalizeUrl(url),
            audience,
            createdByUserId,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Edits the content in place. The kind is fixed at creation: a note that
    /// became a link would leave everyone who read it holding something else.
    /// </summary>
    public void Update(string title, string? body, string? url, ResourceAudience audience)
    {
        Title = NormalizeTitle(title);
        Audience = audience;

        if (Kind == ResourceKind.Note)
        {
            var text = body?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                throw new ArgumentException(
                    "A note needs something in it.",
                    nameof(body));
            }

            Body = text.Length > BodyMaxLength ? text[..BodyMaxLength] : text;
            return;
        }

        Url = NormalizeUrl(url);
    }

    private static Guid RequireEngagement(Guid engagementId)
    {
        if (engagementId == Guid.Empty)
        {
            throw new ArgumentException(
                "An engagement is required.",
                nameof(engagementId));
        }

        return engagementId;
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

    /// <summary>
    /// Only http and https. A link is opened by tapping it, and anything else
    /// — a file:// path, a javascript: URI — either cannot work for the people
    /// receiving it or should not.
    /// </summary>
    private static string NormalizeUrl(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A link needs an address.", nameof(url));
        }

        if (trimmed.Length > UrlMaxLength)
        {
            throw new ArgumentException("That link is too long.", nameof(url));
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "A link must start with http:// or https://.",
                nameof(url));
        }

        return parsed.ToString();
    }
}
