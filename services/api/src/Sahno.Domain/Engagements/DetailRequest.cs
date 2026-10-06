namespace Sahno.Domain.Engagements;

/// <summary>
/// A performer asking the organisers for a detail of the event that is not
/// set yet — the venue, the sound-check or start time, what to wear (D-089).
/// One open request per person per detail, so asking twice is not a second
/// ping; the request closes when the detail is filled in.
/// </summary>
public sealed class DetailRequest
{
    /// <summary>The details a performer plans their day around, and so may ask for.</summary>
    public static readonly IReadOnlyList<ReadinessItem> Askable =
    [
        ReadinessItem.Venue,
        ReadinessItem.CallTime,
        ReadinessItem.StartTime,
        ReadinessItem.Dress,
    ];

    private DetailRequest(Guid engagementId, Guid userId, ReadinessItem item, DateTimeOffset requestedAtUtc)
    {
        EngagementId = engagementId;
        UserId = userId;
        Item = item;
        RequestedAtUtc = requestedAtUtc;
    }

    public Guid EngagementId { get; }

    public Guid UserId { get; }

    public ReadinessItem Item { get; }

    public DateTimeOffset RequestedAtUtc { get; }

    public static DetailRequest Ask(Guid engagementId, Guid userId, ReadinessItem item)
    {
        if (!Askable.Contains(item))
        {
            throw new ArgumentException($"{item} is not a detail that can be asked for.", nameof(item));
        }

        return new DetailRequest(engagementId, userId, item, DateTimeOffset.UtcNow);
    }

    /// <summary>"sound-check time" — as it reads in a sentence.</summary>
    public static string Describe(ReadinessItem item) => item switch
    {
        ReadinessItem.Venue => "venue",
        ReadinessItem.CallTime => "sound-check time",
        ReadinessItem.StartTime => "start time",
        ReadinessItem.Dress => "outfit",
        _ => item.ToString(),
    };
}
