using Sahno.Application.Notifications;
using Sahno.Application.Organisations;
using Sahno.Domain.Engagements;
using Sahno.Domain.Notifications;
using Sahno.Domain.Organisations;

namespace Sahno.Application.Engagements;

/// <summary>One message with its author's name resolved, and whose it is.</summary>
public sealed record DiscussionRow(
    DiscussionMessage Message,
    string? AuthorDisplayName,
    bool IsYours);

/// <summary>One conversation in the Chat inbox: its event, latest message, and unread count.</summary>
public sealed record ChatInboxRow(
    Engagement Engagement,
    DiscussionRow Latest,
    int UnreadMessages);

/// <summary>
/// Discussion inside an engagement (Slice 9, D-024, D-047 §6).
///
/// Three rules carry the whole thing. Access follows the engagement, so
/// nothing here is reachable by somebody who could not open the event itself.
/// A message belongs to whoever wrote it, so only they can rewrite it. And an
/// organiser can take any message down but never change one — moderation is
/// removal, not authorship.
/// </summary>
public sealed class DiscussionService(
    IEngagementStore engagements,
    IEngagementParticipantStore participants,
    IDiscussionStore messages,
    IMembershipStore memberships,
    INotificationStore notifications,
    Notifier notifier)
{
    /// <summary>
    /// How long a finished (completed or cancelled) event's chat stays in the
    /// inbox after its last message: people talk after a gig, and once the
    /// thread has been quiet this long it drops out (D-084).
    /// </summary>
    public static readonly TimeSpan FinishedChatQuietPeriod = TimeSpan.FromDays(7);

    /// <summary>
    /// The Chat tab (D-084): every active conversation the caller can open,
    /// newest activity first. Only threads with at least one message — an
    /// event nobody has written in is not a conversation. Access is exactly
    /// the thread's own rule: organisers see every event, a member the ones
    /// they are on.
    /// </summary>
    public async Task<IReadOnlyList<ChatInboxRow>> InboxAsync(
        Membership actor,
        CancellationToken cancellationToken)
    {
        var all = await engagements.ListForOrganisationAsync(
            actor.OrganisationId,
            cancellationToken);

        IReadOnlyList<Engagement> visible = all;
        if (!OrganisationAuthorizationService.IsOrganiser(actor))
        {
            var mine = (await participants.ListEngagementIdsForUserAsync(
                actor.OrganisationId,
                actor.UserId,
                cancellationToken)).ToHashSet();
            visible = all.Where(engagement => mine.Contains(engagement.Id)).ToList();
        }

        var latest = await messages.LatestForEngagementsAsync(
            visible.Select(engagement => engagement.Id).ToList(),
            cancellationToken);
        if (latest.Count == 0)
        {
            return [];
        }

        var unread = await notifications.CountUnreadByEngagementAsync(
            actor.OrganisationId,
            actor.UserId,
            NotificationKind.DiscussionMessage,
            cancellationToken);

        var names = (await memberships.ListForOrganisationAsync(
                actor.OrganisationId,
                cancellationToken))
            .ToDictionary(row => row.Membership.UserId, row => row.DisplayName);

        var quietSince = DateTimeOffset.UtcNow - FinishedChatQuietPeriod;

        return visible
            .Where(engagement => latest.ContainsKey(engagement.Id))
            .Where(engagement => IsActive(engagement, latest[engagement.Id], quietSince))
            .Select(engagement =>
            {
                var message = latest[engagement.Id];
                return new ChatInboxRow(
                    engagement,
                    new DiscussionRow(
                        message,
                        names.GetValueOrDefault(message.AuthorUserId),
                        message.AuthorUserId == actor.UserId),
                    unread.GetValueOrDefault(engagement.Id));
            })
            .OrderByDescending(row => row.Latest.Message.PostedAtUtc)
            .ToList();
    }

    /// <summary>
    /// Active: the event is not finished, or it finished but its chat is still
    /// going. A finished event drops out once its thread has been quiet for
    /// <see cref="FinishedChatQuietPeriod"/>; its chat stays on the event.
    /// </summary>
    private static bool IsActive(
        Engagement engagement,
        DiscussionMessage latest,
        DateTimeOffset quietSince)
    {
        var finished = engagement.Status is EngagementStatus.Completed or EngagementStatus.Cancelled;
        return !finished || latest.PostedAtUtc >= quietSince;
    }

    /// <summary>
    /// The thread, oldest first. Removed messages come back as tombstones with
    /// no text: the fact that something was taken down stays visible, and the
    /// words do not.
    /// </summary>
    public async Task<IReadOnlyList<DiscussionRow>?> ListAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(actor, engagementId, cancellationToken))
        {
            return null;
        }

        var thread = await messages.ListForEngagementAsync(
            engagementId,
            cancellationToken);

        var directory = await memberships.ListForOrganisationAsync(
            actor.OrganisationId,
            cancellationToken);
        var names = directory.ToDictionary(
            row => row.Membership.UserId,
            row => row.DisplayName);

        return thread
            .Select(message => new DiscussionRow(
                message,
                names.GetValueOrDefault(message.AuthorUserId),
                message.AuthorUserId == actor.UserId))
            .ToList();
    }

    public async Task<(EngagementResult Result, DiscussionMessage? Posted)> PostAsync(
        Membership actor,
        Guid engagementId,
        string body,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(actor, engagementId, cancellationToken))
        {
            return (EngagementResult.NotFound, null);
        }

        var message = DiscussionMessage.Post(engagementId, actor.UserId, body);

        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        var lineup = await participants.ListForEngagementAsync(
            engagementId,
            cancellationToken);
        await notifier.DiscussionMessageAsync(
            engagement!,
            lineup
                .Where(participant => participant.IsActive)
                .Select(participant => participant.UserId)
                .ToList(),
            actor.UserId,
            message.Body!,
            cancellationToken);

        await messages.AddAsync(message, cancellationToken);
        return (EngagementResult.Success, message);
    }

    /// <summary>
    /// Rewrites a message. Its author and nobody else — an organiser with
    /// moderation powers still cannot put words in somebody's mouth.
    /// </summary>
    public async Task<EngagementResult> EditAsync(
        Membership actor,
        Guid engagementId,
        Guid messageId,
        string body,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var message = await messages.FindAsync(
            engagementId,
            messageId,
            cancellationToken);
        if (message is null)
        {
            return EngagementResult.NotFound;
        }

        if (message.AuthorUserId != actor.UserId)
        {
            return EngagementResult.Forbidden;
        }

        if (message.IsDeleted)
        {
            return EngagementResult.Invalid;
        }

        message.Edit(body);
        await messages.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Takes a message down. Its author may take their own back; an organiser
    /// may remove anybody's, which is the moderation route in D-024. Everyone
    /// else is refused.
    /// </summary>
    public async Task<EngagementResult> RemoveAsync(
        Membership actor,
        Guid engagementId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(actor, engagementId, cancellationToken))
        {
            return EngagementResult.NotFound;
        }

        var message = await messages.FindAsync(
            engagementId,
            messageId,
            cancellationToken);
        if (message is null)
        {
            return EngagementResult.NotFound;
        }

        var isOwnMessage = message.AuthorUserId == actor.UserId;
        if (!isOwnMessage && !OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return EngagementResult.Forbidden;
        }

        message.Remove(actor.UserId);
        await messages.SaveAsync(cancellationToken);
        return EngagementResult.Success;
    }

    /// <summary>
    /// Access follows the engagement exactly (D-024). There is no separate
    /// notion of who may read a thread: if you can open the event, you can read
    /// and post; if you cannot, the discussion does not exist as far as you are
    /// concerned.
    /// </summary>
    private async Task<bool> CanAccessAsync(
        Membership actor,
        Guid engagementId,
        CancellationToken cancellationToken)
    {
        var engagement = await engagements.FindByIdAsync(
            actor.OrganisationId,
            engagementId,
            cancellationToken);
        if (engagement is null)
        {
            return false;
        }

        if (OrganisationAuthorizationService.IsOrganiser(actor))
        {
            return true;
        }

        var participant = await participants.FindAsync(
            engagementId,
            actor.UserId,
            cancellationToken);

        return participant is { IsActive: true };
    }
}
