using System.Text.Json;
using System.Text.Json.Serialization;
using Sahno.Domain.Notifications;

namespace Sahno.Application.Notifications;

/// <summary>
/// Where a notification takes you, as the path the app opens. Decided once,
/// here, so the bell, the push payload, and the in-app banner all agree —
/// a job assignment lands on the Jobs page whether you tapped it in the tray
/// or in the list.
/// </summary>
public static class NotificationRoutes
{
    public static string For(NotificationKind kind, Guid? engagementId)
    {
        if (engagementId is not { } id)
        {
            return kind switch
            {
                NotificationKind.MemberJoined => "/(tabs)/people",
                _ => "/notifications",
            };
        }

        // Taken off the event: it is no longer theirs to open, so the bell.
        if (kind == NotificationKind.RemovedFromEngagement)
        {
            return "/notifications";
        }

        var engagement = $"/engagement/{id}";
        return kind switch
        {
            // Organisers are told about answers: the lineup is where they act.
            NotificationKind.AvailabilityAnswered => $"{engagement}/people",
            // Asked for a detail: straight to where it is filled in.
            NotificationKind.DetailsRequested => $"{engagement}/details",
            // Closing it is a status change; that page is where it is done.
            NotificationKind.EngagementWrapUp => $"{engagement}/status",
            NotificationKind.ResponsibilityAssigned
                or NotificationKind.ResponsibilityReminder => $"{engagement}/jobs",
            NotificationKind.RehearsalScheduled
                or NotificationKind.RehearsalReminder => $"{engagement}/files",
            NotificationKind.DiscussionMessage
                or NotificationKind.DiscussionReaction => $"{engagement}/chat",
            // A request, a reminder, or changed details lands on the event
            // itself: its header carries the date, times, venue and dress,
            // and a member answers there — they have no separate People or
            // Details page any more.
            _ => engagement,
        };
    }
}

/// <summary>
/// How the phone shows a push beyond its words (D-088). A chat's messages
/// share a <see cref="Tag"/>, so on Android each new one replaces the last and
/// the chat is one notification, the way messaging apps do it; on iOS they
/// share a <see cref="ThreadId"/> and stack together. Different chats never
/// share either. <see cref="CategoryId"/> puts Reply and Mark as read on a
/// chat message — the app defines the buttons under that name.
/// </summary>
public sealed record PushPresentation(string? Tag, string? ThreadId, string? CategoryId)
{
    public const string ChatMessageCategory = "chat-message";

    private static readonly PushPresentation Plain = new(null, null, null);

    public static PushPresentation For(string? notificationType, Guid? engagementId)
    {
        if (engagementId is not { } id)
        {
            return Plain;
        }

        var chat = $"chat-{id}";
        return notificationType switch
        {
            nameof(NotificationKind.DiscussionMessage) => new PushPresentation(chat, chat, ChatMessageCategory),
            // Stacks with the chat, but must not replace its unread messages.
            nameof(NotificationKind.DiscussionReaction) => new PushPresentation(null, chat, null),
            _ => Plain,
        };
    }
}

/// <summary>
/// The structured payload a push carries and a live "notification" event
/// sends: enough for the app to show a banner and to open the right screen,
/// and nothing that is not already the recipient's own notification.
/// </summary>
public sealed record NotificationPayload(
    Guid NotificationId,
    string NotificationType,
    Guid OrganisationId,
    Guid? EngagementId,
    string Title,
    string? Body,
    string Route)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static NotificationPayload From(Notification notification)
    {
        return new NotificationPayload(
            notification.Id,
            notification.Kind.ToString(),
            notification.OrganisationId,
            notification.EngagementId,
            notification.Title,
            notification.Body,
            NotificationRoutes.For(notification.Kind, notification.EngagementId));
    }

    public string ToJson() => JsonSerializer.Serialize(this, Json);
}
