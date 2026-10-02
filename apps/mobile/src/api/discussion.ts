import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

/**
 * One message in an engagement's discussion (D-024).
 *
 * A removed message still arrives, with `body` null. A thread with silent gaps
 * in it stops making sense, and `wasModerated` says which kind of removal it
 * was — an author taking their own words back reads differently from an
 * organiser taking them down.
 */
export type DiscussionMessage = {
  id: string;
  authorUserId: string;
  authorDisplayName: string | null;
  isYours: boolean;
  body: string | null;
  isEdited: boolean;
  isDeleted: boolean;
  wasModerated: boolean;
  postedAtUtc: string;
  editedAtUtc: string | null;
  /** What this replies to, as the original stands now (D-086). Absent from older APIs. */
  replyTo?: DiscussionQuote | null;
  /** One entry per emoji, in the picker's order. Absent from older APIs. */
  reactions?: DiscussionReaction[];
};

/** A reply's quote. `body` is null once the original was removed. */
export type DiscussionQuote = {
  id: string;
  authorDisplayName: string | null;
  isYours: boolean;
  body: string | null;
  isDeleted: boolean;
};

export type DiscussionReaction = {
  emoji: string;
  count: number;
  includesYou: boolean;
};

/** The reactions on offer, in the server's order (DiscussionReaction.Allowed). */
export const REACTIONS = ['👍', '❤️', '😂', '😮', '😢', '🙏'] as const;

function isMessage(value: unknown): value is DiscussionMessage {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'authorUserId' in value &&
    'postedAtUtc' in value
  );
}

function isThread(value: unknown): value is DiscussionMessage[] {
  return Array.isArray(value) && value.every(isMessage);
}

function base(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}/discussion`;
}

export function listDiscussion(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<DiscussionMessage[]> {
  return getAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    isThread,
    signal,
  );
}

export function postDiscussionMessage(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  body: string,
  replyToMessageId?: string | null,
): Promise<DiscussionMessage> {
  return postAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    replyToMessageId ? { body, replyToMessageId } : { body },
    isMessage,
  );
}

/** The author's own rewrite. Nobody else can reach it, organisers included. */
export function editDiscussionMessage(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  messageId: string,
  body: string,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/${messageId}`,
    accessToken,
    'PUT',
    { body },
  );
}

/** The author taking their own back, or an organiser moderating. */
export function removeDiscussionMessage(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  messageId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId, engagementId)}/${messageId}`,
    accessToken,
    'DELETE',
  );
}

/**
 * The caller has the chat open: everything in it is read. Clears the event's
 * unread bubble and the matching bell rows; safe to call as often as needed.
 */
export function markDiscussionRead(
  accessToken: string,
  organisationId: string,
  engagementId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId, engagementId)}/read`,
    accessToken,
    'POST',
  );
}

/**
 * Reacts to a message. The same emoji you already gave takes it back; a
 * different one replaces it — one reaction each (D-086).
 */
export function reactToMessage(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  messageId: string,
  emoji: string,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/${messageId}/reaction`,
    accessToken,
    'PUT',
    { emoji },
  );
}

/** "Delete for me": the caller stops seeing this message; nobody else is affected. */
export function hideMessage(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  messageId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId, engagementId)}/${messageId}/hide`,
    accessToken,
    'POST',
  );
}
