import * as Calendar from 'expo-calendar';
import { Platform } from 'react-native';

import type { Engagement } from '@/api/engagements';

export type AddToCalendarResult =
  | { ok: true; tentative: boolean }
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

/** The device calendar Sahno should write to. */
async function defaultCalendarAsync() {
  const calendars = await Calendar.getCalendarsAsync(
    Calendar.EntityTypes.EVENT,
  );

  const writable = calendars.filter(
    (calendar) => calendar.allowsModifications,
  );
  if (writable.length === 0) {
    return null;
  }

  if (Platform.OS === 'ios') {
    const preferred = await Calendar.getDefaultCalendarAsync();
    return (
      writable.find((calendar) => calendar.id === preferred?.id) ?? writable[0]
    );
  }

  // Android has no single "default", so prefer the primary local account.
  return (
    writable.find((calendar) => calendar.isPrimary) ??
    writable.find((calendar) => calendar.accessLevel === Calendar.CalendarAccessLevel.OWNER) ??
    writable[0]
  );
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

  const permission = await Calendar.requestCalendarPermissionsAsync();
  if (permission.status !== 'granted') {
    return { ok: false, reason: 'permission' };
  }

  try {
    const calendar = await defaultCalendarAsync();
    if (calendar === null) {
      return { ok: false, reason: 'no-calendar' };
    }

    const start = new Date(`${engagement.startDate}T00:00:00`);
    const end = new Date(
      `${engagement.endDate ?? engagement.startDate}T00:00:00`,
    );
    // An all-day event ends at the start of the following day.
    end.setDate(end.getDate() + 1);

    await Calendar.createEventAsync(calendar.id, {
      title: personalCalendarTitle(engagement),
      startDate: start,
      endDate: end,
      allDay: true,
      location: engagement.venue ?? undefined,
      notes: notesFor(engagement, organisationName),
    });

    return { ok: true, tentative: engagement.status === 'Tentative' };
  } catch {
    return { ok: false, reason: 'failed' };
  }
}
