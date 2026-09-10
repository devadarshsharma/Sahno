import { getAuthorizedJson, sendAuthorizedJson } from '@/api/client';

/** The eight checklist items from D-048. */
export type ReadinessItem =
  | 'Lineup'
  | 'Venue'
  | 'CallTime'
  | 'StartTime'
  | 'Responsibilities'
  | 'Dress'
  | 'Rehearsal'
  | 'Resources';

export type ReadinessState = 'Outstanding' | 'Done' | 'NotRequired';

export type ReadinessEntry = {
  item: ReadinessItem;
  state: ReadinessState;
};

function isReadinessList(value: unknown): value is ReadinessEntry[] {
  return (
    Array.isArray(value) &&
    value.every(
      (entry) =>
        typeof entry === 'object' &&
        entry !== null &&
        'item' in entry &&
        'state' in entry,
    )
  );
}

function base(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}/readiness`;
}

export function getReadiness(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<ReadinessEntry[]> {
  return getAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    isReadinessList,
    signal,
  );
}

/** Marks an item as not applying to this event, or puts it back on the list. */
export function setReadiness(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  item: ReadinessItem,
  notRequired: boolean,
): Promise<void> {
  return sendAuthorizedJson(
    base(organisationId, engagementId),
    accessToken,
    'PUT',
    { item, notRequired },
  );
}

/**
 * What each item asks for. Phrased as the thing still to do, since that is
 * what an organiser reads the list for.
 */
export const READINESS_LABELS: Record<ReadinessItem, string> = {
  Lineup: 'Lineup settled',
  Venue: 'Venue added',
  CallTime: 'Call or sound-check time',
  StartTime: 'Start time',
  Responsibilities: 'Responsibilities assigned',
  Dress: 'Dress instructions',
  Rehearsal: 'Rehearsal organised',
  Resources: 'Repertoire or resources ready',
};
