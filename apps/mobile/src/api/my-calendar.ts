import type { AvailabilityAnswer } from '@/api/availability';
import { getAuthorizedJson } from '@/api/client';
import type { EngagementStatus } from '@/api/engagements';
import type { UpcomingRehearsal } from '@/api/upcoming-rehearsals';

/**
 * One event on the caller's own calendar (D-091), from any of their
 * organisations: what they already see in each, labelled with whose it is.
 */
export type MyCalendarEvent = {
  organisationId: string;
  organisationName: string;
  /** Whether the caller organises in that organisation. */
  isOrganiser: boolean;
  id: string;
  title: string;
  status: EngagementStatus;
  startDate: string | null;
  endDate: string | null;
  startTime: string | null;
  venue: string | null;
  yourResponse: AvailabilityAnswer | null;
  outstandingCount: number | null;
};

export type MyCalendarRehearsal = UpcomingRehearsal & {
  organisationId: string;
  organisationName: string;
};

export type MyCalendar = {
  events: MyCalendarEvent[];
  rehearsals: MyCalendarRehearsal[];
};

function isMyCalendar(value: unknown): value is MyCalendar {
  return (
    typeof value === 'object' &&
    value !== null &&
    Array.isArray((value as MyCalendar).events) &&
    Array.isArray((value as MyCalendar).rehearsals)
  );
}

/** Everything from `fromDate` ("YYYY-MM-DD") on, across every organisation. */
export function getMyCalendar(accessToken: string, fromDate: string, signal?: AbortSignal): Promise<MyCalendar> {
  return getAuthorizedJson(`/api/me/calendar?from=${fromDate}`, accessToken, isMyCalendar, signal);
}
