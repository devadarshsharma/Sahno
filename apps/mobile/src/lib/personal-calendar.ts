import {
  EntityTypes,
  getCalendars,
  requestCalendarPermissions,
} from 'expo-calendar';

import type { Engagement } from '@/api/engagements';

export type AddToCalendarResult =
  | { ok: true; tentative: boolean }
  | { ok: false; reason: 'already-added' }
  | { ok: false; reason: 'permission' | 'no-calendar' | 'no-date' | 'failed' };

/**
 * Only events that are actually on go into someone's own calendar. A draft or
 * an availability request is not a commitment, and putting one there would
 * have people holding a date the group has not taken.
 */
export function canAddToPersonalCalendar(engagement: Engagement): boolean {
  return (
    engagement.startDate !== null &&
    (engagement.status === 'Confirmed' || engagement.status === 'Tentative')
  );
}

/**
 * A tentative booking says so in the calendar entry itself, not just in Sahno
 * (Slice 6). Someone glancing at a busy week sees the title and nothing else,
 * so the title is where the word has to be — otherwise a maybe reads exactly
 * like a commitment.
 */
export function personalCalendarTitle(engagement: Engagement): string {
  return engagement.status === 'Tentative'
    ? `[Tentative] ${engagement.title}`
    : engagement.title;
}

function notesFor(engagement: Engagement, organisationName: string): string {
  const lines = [`${organisationName} · via Sahno`];

  if (engagement.status === 'Tentative') {
    lines.push(
      'This booking is not confirmed yet. Sahno will still be the place it changes.',
    );
  }

  return lines.join('\n');
}


/**
 * All-day entries are anchored to UTC midnight, not local midnight.
 *
 * Calendar providers read an all-day event's bounds as UTC. Local midnight in
 * a positive offset is the previous afternoon in UTC, so an event on the 26th
 * saved from Sydney would appear as the 25th and 26th — a booking that reads
 * as spanning two days, one of them wrong.
 */
function utcMidnight(isoDay: string): Date {
  const [year, month, day] = isoDay.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

/**
 * Whether this engagement is already in the calendar. Matched on either title
 * it could have been saved under, since a booking may have been added while
 * tentative and confirmed since.
 */
function matchesEngagement(title: string | undefined, engagement: Engagement) {
  return (
    title === engagement.title || title === `[Tentative] ${engagement.title}`
  );
}
/** The device calendar Sahno should write to. */
async function defaultCalendarAsync() {
  const calendars = await getCalendars(EntityTypes.EVENT);

  const writable = calendars.filter((calendar) => calendar.allowsModifications);
  if (writable.length === 0) {
    return null;
  }

  // Neither platform guarantees a single obvious target, so prefer the
  // person's primary account and fall back to whatever can be written to.
  return writable.find((calendar) => calendar.isPrimary) ?? writable[0];
}

/**
 * Adds one engagement to the person's own calendar. All-day, because Sahno
 * does not yet know a start time for most bookings and inventing one would put
 * people somewhere at an hour nobody agreed.
 */
export async function addToPersonalCalendar(
  engagement: Engagement,
  organisationName: string,
): Promise<AddToCalendarResult> {
  if (engagement.startDate === null) {
    return { ok: false, reason: 'no-date' };
  }

  // writeOnly: Sahno only ever adds an entry, so it does not ask to read
  // someone's diary to do it.
  const permission = await requestCalendarPermissions(true);
  if (permission.status !== 'granted') {
    return { ok: false, reason: 'permission' };
  }

  try {
    const calendar = await defaultCalendarAsync();
    if (calendar === null) {
      return { ok: false, reason: 'no-calendar' };
    }

    const start = utcMidnight(engagement.startDate);
    const end = utcMidnight(engagement.endDate ?? engagement.startDate);
    // An all-day event ends at the start of the following day.
    end.setUTCDate(end.getUTCDate() + 1);

    // Adding the same booking twice leaves two entries and no way to tell
    // which is current, so say it is already there instead.
    const existing = await calendar.listEvents(start, end);
    if (existing.some((event) => matchesEngagement(event.title, engagement))) {
      return { ok: false, reason: 'already-added' };
    }

    await calendar.createEvent({
      title: personalCalendarTitle(engagement),
      startDate: start,
      endDate: end,
      allDay: true,
      location: engagement.venue ?? undefined,
      notes: notesFor(engagement, organisationName),
      // UTC to match the anchoring above; otherwise the provider re-reads the
      // bounds in the device zone and shifts the day back again.
      timeZone: 'UTC',
    });

    return { ok: true, tentative: engagement.status === 'Tentative' };
  } catch {
    return { ok: false, reason: 'failed' };
  }
}
