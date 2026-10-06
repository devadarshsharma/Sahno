import * as Notifications from 'expo-notifications';
import * as TaskManager from 'expo-task-manager';
import Auth0 from 'react-native-auth0';

import { markDiscussionRead, postDiscussionMessage } from '@/api/discussion';
import { environment } from '@/config/environment';
import { payloadOf } from '@/notifications/push';
import { useChatOutbox } from '@/stores/chat-outbox';

/**
 * Reply and Mark as read, straight from a chat notification (D-088), without
 * opening the app. The API sends chat pushes under this category; the
 * buttons are defined here.
 *
 * This module is imported first thing (see index.ts), because on Android a
 * button pressed while the app is closed starts the JS bundle just to run
 * the task below — there is no React tree, no session provider, nothing but
 * what this file sets up.
 */
export const CHAT_MESSAGE_CATEGORY = 'chat-message';
const REPLY = 'reply';
const MARK_READ = 'mark-read';
const TASK = 'sahno-chat-notification-actions';

function ensureChatActions(): Promise<unknown> {
  return Notifications.setNotificationCategoryAsync(CHAT_MESSAGE_CATEGORY, [
    {
      identifier: REPLY,
      buttonTitle: 'Reply',
      textInput: { submitButtonTitle: 'Send', placeholder: 'Message' },
      options: { opensAppToForeground: false },
    },
    {
      identifier: MARK_READ,
      buttonTitle: 'Mark as read',
      options: { opensAppToForeground: false },
    },
  ]);
}

/** True for a press on one of the chat buttons, rather than a tap on the notification. */
export function isChatAction(response: Notifications.NotificationResponse): boolean {
  return response.actionIdentifier === REPLY || response.actionIdentifier === MARK_READ;
}

// The same press can reach both the task and the listener (Android, app in
// the background); one handling is enough.
const handled = new Set<string>();

export async function handleChatAction(response: Notifications.NotificationResponse): Promise<void> {
  const payload = payloadOf(response.notification);
  if (!payload?.engagementId || !isChatAction(response)) {
    return;
  }

  const key = responseKey(response);
  if (handled.has(key)) {
    return;
  }
  handled.add(key);

  const text = response.userText?.trim() ?? '';
  const replying = response.actionIdentifier === REPLY && text.length > 0;
  // Named from the press itself, so if it is handled twice anyway — in two
  // processes — the server sees the same message and posts it once (D-087).
  const clientId = uuidFrom(key);

  try {
    const accessToken = await accessTokenOutsideReact();
    if (replying) {
      await postDiscussionMessage(accessToken, payload.organisationId, payload.engagementId, text, null, clientId);
    }
    // Replying means you have read what you replied to.
    await markDiscussionRead(accessToken, payload.organisationId, payload.engagementId);
  } catch {
    if (replying) {
      // Not lost: it waits in the chat as "Not sent" and goes again when the
      // chat is next open.
      await useChatOutbox.persist.rehydrate();
      if (!useChatOutbox.getState().messages.some((message) => message.clientId === clientId)) {
        useChatOutbox.getState().add({
          clientId,
          organisationId: payload.organisationId,
          engagementId: payload.engagementId,
          body: text,
          replyTo: null,
          queuedAtUtc: new Date().toISOString(),
          state: 'failed',
          retryable: true,
        });
      }
      await Notifications.scheduleNotificationAsync({
        content: {
          title: 'Reply not sent',
          body: 'It is waiting in the chat. Open it to send.',
          data: payload,
        },
        trigger: null,
      });
    }
  } finally {
    // Android keeps a spinner on a replied notification until it goes, and a
    // chat you just read has nothing left to tell you.
    await Notifications.dismissNotificationAsync(response.notification.request.identifier).catch(() => undefined);
  }
}

/** Everything in the tray about one chat goes once it has been read in the app. */
export async function dismissChatNotifications(engagementId: string): Promise<void> {
  const presented = await Notifications.getPresentedNotificationsAsync();
  await Promise.all(
    presented
      .filter((notification) => payloadOf(notification)?.engagementId === engagementId)
      .map((notification) => Notifications.dismissNotificationAsync(notification.request.identifier)),
  );
}

let auth0: Auth0 | null = null;

/** The stored session, read without the React provider — the app may not be running. */
async function accessTokenOutsideReact(): Promise<string> {
  if (!environment.auth0) {
    throw new Error('Auth0 is not configured.');
  }
  auth0 ??= new Auth0({ domain: environment.auth0.domain, clientId: environment.auth0.clientId });
  const credentials = await auth0.credentialsManager.getCredentials();
  return credentials.accessToken;
}

function responseKey(response: Notifications.NotificationResponse): string {
  return [
    response.notification.request.identifier,
    response.notification.date,
    response.actionIdentifier,
    response.userText ?? '',
  ].join('|');
}

/** A UUID-shaped id that is always the same for the same text. */
function uuidFrom(text: string): string {
  let h1 = 0x811c9dc5;
  let h2 = 0x01000193;
  let h3 = 0x9e3779b9;
  let h4 = 0x85ebca6b;
  for (let index = 0; index < text.length; index++) {
    const code = text.charCodeAt(index);
    h1 = Math.imul(h1 ^ code, 0x01000193);
    h2 = Math.imul(h2 ^ code, 0x5bd1e995);
    h3 = Math.imul(h3 ^ code, 0x27d4eb2d);
    h4 = Math.imul(h4 ^ code, 0x165667b1);
  }
  const hex = [h1, h2, h3, h4].map((part) => (part >>> 0).toString(16).padStart(8, '0')).join('');
  const variant = ((parseInt(hex[16], 16) & 0x3) | 0x8).toString(16);
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-${variant}${hex.slice(17, 20)}-${hex.slice(20, 32)}`;
}

// ---- Registration, at import ------------------------------------------------

// The buttons exist before the first chat push can arrive.
void ensureChatActions().catch(() => undefined);

// Android, app in the background or closed: the press runs this task.
TaskManager.defineTask<Notifications.NotificationTaskPayload>(TASK, async ({ data }) => {
  if (data && 'actionIdentifier' in data) {
    await handleChatAction(data);
  }
});
void Notifications.registerTaskAsync(TASK).catch(() => undefined);

// iOS, and Android with the app open: the press arrives as a response.
Notifications.addNotificationResponseReceivedListener((response) => {
  if (isChatAction(response)) {
    void handleChatAction(response);
  }
});
