import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

/** A practice session linked to its engagement (D-047 §4). */
export type Rehearsal = {
  id: string;
  title: string | null;
  /** ISO date, e.g. "2027-08-07". Always present — see the domain entity. */
  date: string;
  startTime: string | null;
  endTime: string | null;
  venue: string | null;
  notes: string | null;
  createdAtUtc: string;
};

/** Who a note or link is for (D-023). */
export type ResourceAudience = 'Participants' | 'AdminsOnly';

export type ResourceKind = 'Note' | 'Link';

/**
 * Repertoire, notes, and links (D-047 §5). A member is never handed an
 * Admins-only row at all, so anything in this list is safe to show.
 */
export type EngagementResource = {
  id: string;
  kind: ResourceKind;
  title: string;
  body: string | null;
  url: string | null;
  audience: ResourceAudience;
  createdAtUtc: string;
};

export type SaveRehearsalInput = {
  title?: string | null;
  date: string;
  startTime?: string | null;
  endTime?: string | null;
  venue?: string | null;
  notes?: string | null;
};

function isRehearsal(value: unknown): value is Rehearsal {
  return (
    typeof value === 'object' && value !== null && 'id' in value && 'date' in value
  );
}

function isRehearsalList(value: unknown): value is Rehearsal[] {
  return Array.isArray(value) && value.every(isRehearsal);
}

function isResource(value: unknown): value is EngagementResource {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'kind' in value &&
    'audience' in value
  );
}

function isResourceList(value: unknown): value is EngagementResource[] {
  return Array.isArray(value) && value.every(isResource);
}

function engagement(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}`;
}

export function listRehearsals(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<Rehearsal[]> {
  return getAuthorizedJson(
    `${engagement(organisationId, engagementId)}/rehearsals`,
    accessToken,
    isRehearsalList,
    signal,
  );
}

export function createRehearsal(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: SaveRehearsalInput,
): Promise<Rehearsal> {
  return postAuthorizedJson(
    `${engagement(organisationId, engagementId)}/rehearsals`,
    accessToken,
    toRehearsalBody(input),
    isRehearsal,
  );
}

export function updateRehearsal(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  rehearsalId: string,
  input: SaveRehearsalInput,
): Promise<void> {
  return sendAuthorizedJson(
    `${engagement(organisationId, engagementId)}/rehearsals/${rehearsalId}`,
    accessToken,
    'PUT',
    toRehearsalBody(input),
  );
}

export function deleteRehearsal(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  rehearsalId: string,
): Promise<void> {
  return sendAuthorized(
    `${engagement(organisationId, engagementId)}/rehearsals/${rehearsalId}`,
    accessToken,
    'DELETE',
  );
}

export function listResources(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<EngagementResource[]> {
  return getAuthorizedJson(
    `${engagement(organisationId, engagementId)}/resources`,
    accessToken,
    isResourceList,
    signal,
  );
}

export function createResource(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: {
    kind: ResourceKind;
    title: string;
    body?: string | null;
    url?: string | null;
    audience: ResourceAudience;
  },
): Promise<EngagementResource> {
  return postAuthorizedJson(
    `${engagement(organisationId, engagementId)}/resources`,
    accessToken,
    {
      kind: input.kind,
      title: input.title,
      body: input.body ?? null,
      url: input.url ?? null,
      audience: input.audience,
    },
    isResource,
  );
}

export function updateResource(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  resourceId: string,
  input: {
    title: string;
    body?: string | null;
    url?: string | null;
    audience: ResourceAudience;
  },
): Promise<void> {
  return sendAuthorizedJson(
    `${engagement(organisationId, engagementId)}/resources/${resourceId}`,
    accessToken,
    'PUT',
    {
      title: input.title,
      body: input.body ?? null,
      url: input.url ?? null,
      audience: input.audience,
    },
  );
}

export function deleteResource(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  resourceId: string,
): Promise<void> {
  return sendAuthorized(
    `${engagement(organisationId, engagementId)}/resources/${resourceId}`,
    accessToken,
    'DELETE',
  );
}

function toRehearsalBody(input: SaveRehearsalInput) {
  return {
    title: input.title ?? null,
    date: input.date,
    startTime: input.startTime ?? null,
    endTime: input.endTime ?? null,
    venue: input.venue ?? null,
    notes: input.notes ?? null,
  };
}
