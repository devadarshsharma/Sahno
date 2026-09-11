import { getAuthorizedJson, sendAuthorized } from '@/api/client';

export type NotificationKind =
  | 'AvailabilityRequested'
  | 'AvailabilityReminder'
  | 'AvailabilityAnswered'
  | 'EngagementConfirmed'
  | 'EngagementPostponed'
  | 'EngagementCancelled'
  | 'EngagementReopened'
  | 'EngagementDateChanged'
  | 'EngagementDetailsChanged'
  | 'ResponsibilityAssigned'
  | 'DiscussionMessage'
  | 'MemberJoined';

/**
 * One thing the bell shows (D-049). `engagementId` is where tapping it goes;
 * null for organisation-level news like a new member.
 */
export type Notification = {
  id: string;
  kind: NotificationKind;
  title: string;
  body: string | null;
  engagementId: string | null;
  isRead: boolean;
  createdAtUtc: string;
};

function isNotification(value: unknown): value is Notification {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'kind' in value &&
    'isRead' in value
  );
}

function isNotificationList(value: unknown): value is Notification[] {
  return Array.isArray(value) && value.every(isNotification);
}

function isUnreadCount(value: unknown): value is { unread: number } {
  return (
    typeof value === 'object' &&
    value !== null &&
    'unread' in value &&
    typeof value.unread === 'number'
  );
}

function base(organisationId: string): string {
  return `/api/organisations/${organisationId}/notifications`;
}

export function listNotifications(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<Notification[]> {
  return getAuthorizedJson(
    base(organisationId),
    accessToken,
    isNotificationList,
    signal,
  );
}

export async function getUnreadCount(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<number> {
  const result = await getAuthorizedJson(
    `${base(organisationId)}/unread-count`,
    accessToken,
    isUnreadCount,
    signal,
  );
  return result.unread;
}

export function markNotificationRead(
  accessToken: string,
  organisationId: string,
  notificationId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId)}/${notificationId}/read`,
    accessToken,
    'POST',
  );
}

export function markAllNotificationsRead(
  accessToken: string,
  organisationId: string,
): Promise<void> {
  return sendAuthorized(`${base(organisationId)}/read-all`, accessToken, 'POST');
}
