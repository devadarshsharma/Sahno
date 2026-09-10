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
};

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
): Promise<DiscussionMessage> {
  return postAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    { body },
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
