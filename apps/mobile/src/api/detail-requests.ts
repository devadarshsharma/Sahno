import { getAuthorizedJson, sendAuthorizedJson } from '@/api/client';

/** The details of an event a performer can ask the organisers for (D-089). */
export type AskableDetail = 'Venue' | 'CallTime' | 'StartTime' | 'Dress';

/**
 * What is still to come on an event, for the caller. `askers` is for
 * organisers only — who is waiting on each detail — and null for everyone else.
 */
export type DetailRequests = {
  missing: AskableDetail[];
  asked: AskableDetail[];
  askers: { item: AskableDetail; names: string[] }[] | null;
};

function isDetailRequests(value: unknown): value is DetailRequests {
  return (
    typeof value === 'object' &&
    value !== null &&
    Array.isArray((value as DetailRequests).missing) &&
    Array.isArray((value as DetailRequests).asked)
  );
}

function base(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}/detail-requests`;
}

export function getDetailRequests(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<DetailRequests> {
  return getAuthorizedJson(base(organisationId, engagementId), accessToken, isDetailRequests, signal);
}

/** Asks the organisers. Asking again for the same thing does not ping them twice. */
export function askForDetails(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  items: AskableDetail[],
): Promise<void> {
  return sendAuthorizedJson(base(organisationId, engagementId), accessToken, 'POST', { items });
}

/** How a detail reads on its own ("Sound-check time") and in a sentence. */
export const DETAIL_LABELS: Record<AskableDetail, string> = {
  Venue: 'Venue',
  CallTime: 'Sound-check time',
  StartTime: 'Start time',
  Dress: 'Outfit',
};
