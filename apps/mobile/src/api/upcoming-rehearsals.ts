import { getAuthorizedJson } from '@/api/client';

/** A rehearsal with the event it is for (D-085). Times are "HH:mm:ss". */
export type UpcomingRehearsal = {
  id: string;
  engagementId: string;
  engagementTitle: string;
  title: string | null;
  date: string;
  startTime: string | null;
  endTime: string | null;
  venue: string | null;
};

function isUpcoming(value: unknown): value is UpcomingRehearsal {
  return (
    typeof value === 'object' &&
    value !== null &&
    'engagementId' in value &&
    typeof value.engagementId === 'string' &&
    'date' in value &&
    typeof value.date === 'string'
  );
}

function isUpcomingList(value: unknown): value is UpcomingRehearsal[] {
  return Array.isArray(value) && value.every(isUpcoming);
}

/**
 * Rehearsals from today on, soonest first, for every event the caller is
 * expected at — organisers, every event's.
 */
export function listUpcomingRehearsals(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<UpcomingRehearsal[]> {
  return getAuthorizedJson(
    `/api/organisations/${organisationId}/rehearsals/upcoming`,
    accessToken,
    isUpcomingList,
    signal,
  );
}
