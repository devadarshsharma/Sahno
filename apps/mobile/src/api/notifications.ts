import { getAuthorizedJson, sendAuthorized, sendAuthorizedJson } from '@/api/client';

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
  | 'MemberJoined'
  | 'OrganiserAnnouncement';

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
  /** Where tapping it goes, as the API decides (D-080). */
  route: string;
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

/**
 * What a push carries and what the live "notification" event sends (D-080):
 * enough to show a banner and open the right screen. `route` is decided by
 * the API so the tray, the banner, and the bell all land in one place.
 */
export type NotificationPayload = {
  notificationId: string;
  notificationType: NotificationKind;
  organisationId: string;
  engagementId: string | null;
  title: string;
  body: string | null;
  route: string;
};

export function isNotificationPayload(value: unknown): value is NotificationPayload {
  return (
    typeof value === 'object' &&
    value !== null &&
    'notificationId' in value &&
    'route' in value &&
    'organisationId' in value &&
    typeof (value as NotificationPayload).route === 'string'
  );
}

/** Registers (or refreshes) this phone's Expo push token for the signed-in person. */
export function registerPushDevice(
  accessToken: string,
  input: { token: string; platform: 'ios' | 'android'; deviceName: string | null },
): Promise<void> {
  return sendAuthorizedJson('/api/me/push-devices', accessToken, 'PUT', input);
}

/** Signing out: this phone stops receiving this person's news. */
export function unregisterPushDevice(accessToken: string, token: string): Promise<void> {
  return sendAuthorized(
    `/api/me/push-devices/${encodeURIComponent(token)}`,
    accessToken,
    'DELETE',
  );
}

/** An organiser writing to everybody in the organisation (D-080). */
export function sendAnnouncement(
  accessToken: string,
  organisationId: string,
  input: { title: string; body: string | null },
): Promise<void> {
  return sendAuthorizedJson(`${base(organisationId)}/announcements`, accessToken, 'POST', input);
}
