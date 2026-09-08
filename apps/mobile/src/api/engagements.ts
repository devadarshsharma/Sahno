import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

/**
 * The lifecycle states (ENGAGEMENT_STATE_MACHINE.md). "Engagement" is the
 * internal term; people see Enquiry, Booking, or Event by context (D-039).
 */
export type EngagementStatus =
  | 'Draft'
  | 'CheckingAvailability'
  | 'Tentative'
  | 'Confirmed'
  | 'Completed'
  | 'Cancelled'
  | 'Postponed';

export type Engagement = {
  id: string;
  title: string;
  status: EngagementStatus;
  /** ISO date, e.g. "2027-05-20". Null while the date is still unknown. */
  startDate: string | null;
  endDate: string | null;
  /** ISO time, e.g. "19:30:00". */
  startTime: string | null;
  venue: string | null;
  isSharedWithMembers: boolean;
  canChangeDateDirectly: boolean;
  canBeDiscarded: boolean;
  /** What this engagement may move to next, decided by the API. */
  allowedTransitions: EngagementStatus[];
  createdAtUtc: string;
};

export type EngagementActivity = {
  id: string;
  type: 'Created' | 'StatusChanged' | 'DateChanged';
  fromStatus: EngagementStatus | null;
  toStatus: EngagementStatus | null;
  fromStartDate: string | null;
  toStartDate: string | null;
  reason: string | null;
  actorUserId: string;
  occurredAtUtc: string;
};

function isEngagement(value: unknown): value is Engagement {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    typeof value.id === 'string' &&
    'status' in value &&
    typeof value.status === 'string'
  );
}

function isEngagementList(value: unknown): value is Engagement[] {
  return Array.isArray(value) && value.every(isEngagement);
}

function isActivityList(value: unknown): value is EngagementActivity[] {
  return (
    Array.isArray(value) &&
    value.every(
      (entry) =>
        typeof entry === 'object' &&
        entry !== null &&
        'id' in entry &&
        typeof entry.id === 'string',
    )
  );
}

function base(organisationId: string): string {
  return `/api/organisations/${organisationId}/engagements`;
}

export function listEngagements(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<Engagement[]> {
  return getAuthorizedJson(
    base(organisationId),
    accessToken,
    isEngagementList,
    signal,
  );
}

export function listEngagementActivity(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<EngagementActivity[]> {
  return getAuthorizedJson(
    `${base(organisationId)}/${engagementId}/activity`,
    accessToken,
    isActivityList,
    signal,
  );
}

/** Starts a Draft. Only a title is required (D-025). */
export function createEngagement(
  accessToken: string,
  organisationId: string,
  input: {
    title: string;
    startDate?: string | null;
    endDate?: string | null;
    startTime?: string | null;
    venue?: string | null;
  },
): Promise<Engagement> {
  return postAuthorizedJson(
    base(organisationId),
    accessToken,
    input,
    isEngagement,
  );
}

export function updateEngagement(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  update: { title?: string | null; startTime?: string | null; venue?: string | null },
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId)}/${engagementId}`,
    accessToken,
    'PATCH',
    update,
  );
}

/**
 * Sets the proposed date. Only accepted while the engagement says
 * canChangeDateDirectly — otherwise the route is postponement (D-038).
 */
export function setEngagementDates(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  dates: { startDate: string | null; endDate: string | null },
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId)}/${engagementId}/dates`,
    accessToken,
    'PUT',
    dates,
  );
}

export function transitionEngagement(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  status: EngagementStatus,
  reason?: string | null,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId)}/${engagementId}/transition`,
    accessToken,
    'POST',
    { status, reason: reason ?? null },
  );
}

/** Discards a Draft. Anything Members have seen is cancelled instead. */
export function discardEngagement(
  accessToken: string,
  organisationId: string,
  engagementId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId)}/${engagementId}`,
    accessToken,
    'DELETE',
  );
}

/**
 * Mirrors the server rule: cancelling, postponing, reopening, and reversing a
 * completion each need an explanation, so the UI asks for one before sending.
 */
export function transitionNeedsReason(
  from: EngagementStatus,
  to: EngagementStatus,
): boolean {
  return (
    to === 'Cancelled' ||
    to === 'Postponed' ||
    from === 'Cancelled' ||
    from === 'Completed'
  );
}
