import { getAuthorizedJson } from '@/api/client';
import type { EngagementStatus } from '@/api/engagements';

/** One conversation in the Chat inbox (D-084). */
export type ChatInboxEntry = {
  engagementId: string;
  title: string;
  status: EngagementStatus;
  startDate: string | null;
  lastAuthorDisplayName: string | null;
  lastIsYours: boolean;
  /** Clipped. Null when the latest message was removed. */
  lastMessagePreview: string | null;
  lastMessageRemoved: boolean;
  lastMessageAtUtc: string;
  unreadMessages: number;
};

function isInboxEntry(value: unknown): value is ChatInboxEntry {
  return (
    typeof value === 'object' &&
    value !== null &&
    'engagementId' in value &&
    typeof value.engagementId === 'string' &&
    'lastMessageAtUtc' in value &&
    typeof value.lastMessageAtUtc === 'string'
  );
}

function isInbox(value: unknown): value is ChatInboxEntry[] {
  return Array.isArray(value) && value.every(isInboxEntry);
}

/** Every active conversation the caller can open, newest activity first. */
export function listChats(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<ChatInboxEntry[]> {
  return getAuthorizedJson(
    `/api/organisations/${organisationId}/chats`,
    accessToken,
    isInbox,
    signal,
  );
}
