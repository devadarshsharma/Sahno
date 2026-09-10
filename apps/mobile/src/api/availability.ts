import {
  getAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

/** Null while someone has not answered — the state organisers chase. */
export type AvailabilityAnswer = 'Available' | 'Maybe' | 'Unavailable';

export type Participant = {
  userId: string;
  displayName: string | null;
  response: AvailabilityAnswer | null;
  /** False once they have been taken off the lineup; their answer is kept. */
  isActive: boolean;
  requestedAtUtc: string;
  respondedAtUtc: string | null;
  remindedAtUtc: string | null;
  removedAtUtc: string | null;
};

export type AvailabilitySummary = {
  selected: number;
  available: number;
  maybe: number;
  unavailable: number;
  outstanding: number;
};

export type Availability = {
  summary: AvailabilitySummary;
  participants: Participant[];
};

/** What a member is told about their own participation, and nothing more. */
export type OwnAvailability = {
  isSelected: boolean;
  response: AvailabilityAnswer | null;
  respondedAtUtc: string | null;
};

function isAvailability(value: unknown): value is Availability {
  return (
    typeof value === 'object' &&
    value !== null &&
    'summary' in value &&
    'participants' in value &&
    Array.isArray(value.participants)
  );
}

function isOwnAvailability(value: unknown): value is OwnAvailability {
  return (
    typeof value === 'object' &&
    value !== null &&
    'isSelected' in value &&
    typeof value.isSelected === 'boolean'
  );
}

function base(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}/availability`;
}

/** Organisers only: every answer plus the totals. */
export function getAvailability(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<Availability> {
  return getAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    isAvailability,
    signal,
  );
}

export function getOwnAvailability(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<OwnAvailability> {
  return getAuthorizedJson(
    `${base(organisationId, engagementId)}/me`,
    accessToken,
    isOwnAvailability,
    signal,
  );
}

/**
 * Asks the named people. Re-selecting someone previously removed restores the
 * answer they already gave rather than asking again.
 */
export function requestAvailability(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  userIds: string[],
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/requests`,
    accessToken,
    'POST',
    { userIds },
  );
}

/** Records the caller's own answer. */
export function respondToAvailability(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  response: AvailabilityAnswer,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/me`,
    accessToken,
    'PUT',
    { response },
  );
}

export function remindParticipant(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  userId: string,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/${userId}/reminders`,
    accessToken,
    'POST',
    {},
  );
}

export function removeParticipant(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  userId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId, engagementId)}/${userId}`,
    accessToken,
    'DELETE',
  );
}

export const ANSWER_LABELS: Record<AvailabilityAnswer, string> = {
  Available: 'Available',
  Maybe: 'Maybe',
  Unavailable: 'Not available',
};
