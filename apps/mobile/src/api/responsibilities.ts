import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

/**
 * One job on an engagement — who is doing or bringing what (D-047 §3).
 * Everyone on the event sees the whole list; `isYours` is what a member's own
 * screen sorts on.
 */
export type Responsibility = {
  id: string;
  title: string;
  detail: string | null;
  /** Null while the job is written down but unclaimed. */
  assignedUserId: string | null;
  assignedDisplayName: string | null;
  isYours: boolean;
  isDone: boolean;
  /** The assignee's own word on how it is going. */
  note: string | null;
  completedAtUtc: string | null;
  createdAtUtc: string;
};

function isResponsibility(value: unknown): value is Responsibility {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'title' in value &&
    'isDone' in value
  );
}

function isResponsibilityList(value: unknown): value is Responsibility[] {
  return Array.isArray(value) && value.every(isResponsibility);
}

function base(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}/responsibilities`;
}

export function listResponsibilities(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<Responsibility[]> {
  return getAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    isResponsibilityList,
    signal,
  );
}

/** Organisers only. The assignee may be left out until somebody takes it on. */
export function createResponsibility(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: { title: string; detail?: string | null; assignedUserId?: string | null },
): Promise<Responsibility> {
  return postAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    {
      title: input.title,
      detail: input.detail ?? null,
      assignedUserId: input.assignedUserId ?? null,
    },
    isResponsibility,
  );
}

/** Organisers only — what the job is, and whose it is. */
export function updateResponsibility(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  responsibilityId: string,
  input: { title: string; detail?: string | null; assignedUserId?: string | null },
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/${responsibilityId}`,
    accessToken,
    'PUT',
    {
      title: input.title,
      detail: input.detail ?? null,
      assignedUserId: input.assignedUserId ?? null,
    },
  );
}

/**
 * How it is going, from the person doing it. The one write a member has here,
 * and it reaches only their own row.
 */
export function setResponsibilityProgress(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  responsibilityId: string,
  isDone: boolean,
  note: string | null,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/${responsibilityId}/progress`,
    accessToken,
    'PUT',
    { isDone, note },
  );
}

export function deleteResponsibility(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  responsibilityId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId, engagementId)}/${responsibilityId}`,
    accessToken,
    'DELETE',
  );
}
