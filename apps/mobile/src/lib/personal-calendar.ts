import {
  EntityTypes,
  ExpoCalendarEvent,
  getCalendarPermissions,
  getCalendars,
  requestCalendarPermissions,
} from 'expo-calendar';

import type { EngagementStatus } from '@/api/engagements';
import { useCalendarLinks } from '@/stores/calendar-links';

/**
 * What a phone-calendar entry is written from (D-105). Met by a booking on
 * its own page and by any event in the person's calendar across their
 * organisations, so adding and keeping it up to date write the same entry.
 */
export type CalendarSource = {
  id: string;
  title: string;
  status: EngagementStatus;
  startDate: string | null;
  endDate: string | null;
  startTime: string | null;
  venue: string | null;
};

export type CalendarOrganisation = { id: string; name: string; timeZoneId: string };

export type AddToCalendarResult =
  | { ok: true; tentative: boolean }
  | { ok: false; reason: 'permission' | 'no-calendar' | 'no-date' | 'failed' };

/** An event with no finishing time is entered as this long (D-105). */
const DEFAULT_LENGTH_HOURS = 3;

/**
 * Only events that are actually on go into someone's own calendar. A draft or
 * an availability request is not a commitment, and putting one there would
 * have people holding a date the group has not taken.
 */
export function canAddToPersonalCalendar(event: CalendarSource): boolean {
  return event.startDate !== null && (event.status === 'Confirmed' || event.status === 'Tentative');
}

/**
 * Whether an entry Sahno added still belongs in the calendar. A finished
 * event stays — it happened — but one moved back to being asked about, or
 * cancelled, or postponed, comes out.
 */
function stillBelongs(event: CalendarSource): boolean {
  return (
    event.startDate !== null &&
    (event.status === 'Confirmed' || event.status === 'Tentative' || event.status === 'Completed')
  );
}

/**
 * A tentative booking says so in the calendar entry itself, not just in Sahno
 * (Slice 6). Someone glancing at a busy week sees the title and nothing else,
 * so the title is where the word has to be — otherwise a maybe reads exactly
 * like a commitment.
 */
export function personalCalendarTitle(event: Pick<CalendarSource, 'title' | 'status'>): string {
  return event.status === 'Tentative' ? `[Tentative] ${event.title}` : event.title;
}

function notesFor(event: CalendarSource, organisation: CalendarOrganisation): string {
  const lines = [`${organisation.name} · via Sahno`];
  if (event.status === 'Tentative') {
    lines.push('Not confirmed yet. Sahno updates this entry if it changes.');
  } else {
    lines.push('Sahno updates this entry if the booking changes.');
  }
  if (event.startTime && !event.endDate) {
    lines.push('No finishing time is set in Sahno, so this shows as three hours.');
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

/** Minutes the zone is ahead of UTC at that instant. */
function offsetMinutes(instant: Date, timeZone: string): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone,
    hourCycle: 'h23',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).formatToParts(instant);
  const part = (type: string) => Number(parts.find((entry) => entry.type === type)?.value);
  const asUtc = Date.UTC(part('year'), part('month') - 1, part('day'), part('hour'), part('minute'));
  return Math.round((asUtc - instant.getTime()) / 60_000);
}

/**
 * A day and a time as the organisation means them — in its own time zone —
 * as the instant it is everywhere. Measured twice so a booking on the day the
 * clocks change still lands on the right hour.
 */
function zonedInstant(isoDay: string, time: string, timeZone: string): Date {
  const [year, month, day] = isoDay.split('-').map(Number);
  const [hours, minutes] = time.split(':').map(Number);
  const wallClock = Date.UTC(year, month - 1, day, hours, minutes);
  try {
    const first = wallClock - offsetMinutes(new Date(wallClock), timeZone) * 60_000;
    return new Date(wallClock - offsetMinutes(new Date(first), timeZone) * 60_000);
  } catch {
    // An unknown zone: the phone's own is the best guess left.
    return new Date(year, month - 1, day, hours, minutes);
  }
}

type EntryDetails = {
  title: string;
  startDate: Date;
  endDate: Date;
  allDay: boolean;
  location: string | null;
  notes: string;
  timeZone: string;
};

/**
 * The entry for an event. Timed from its start time when it has one and is a
 * single day, so the phone shows it at the hour and its alerts mean
 * something; otherwise all-day, because inventing a start would put people
 * somewhere at an hour nobody agreed.
 */
function entryFor(event: CalendarSource, organisation: CalendarOrganisation): EntryDetails | null {
  if (event.startDate === null) {
    return null;
  }

  const base = {
    title: personalCalendarTitle(event),
    location: event.venue,
    notes: notesFor(event, organisation),
  };

  if (event.startTime && (event.endDate === null || event.endDate === event.startDate)) {
    const start = zonedInstant(event.startDate, event.startTime, organisation.timeZoneId);
    return {
      ...base,
      startDate: start,
      endDate: new Date(start.getTime() + DEFAULT_LENGTH_HOURS * 60 * 60 * 1000),
      allDay: false,
      timeZone: organisation.timeZoneId,
    };
  }

  const start = utcMidnight(event.startDate);
  const end = utcMidnight(event.endDate ?? event.startDate);
  // An all-day event ends at the start of the following day.
  end.setUTCDate(end.getUTCDate() + 1);
  // UTC to match the anchoring above; otherwise the provider re-reads the
  // bounds in the device zone and shifts the day back again.
  return { ...base, startDate: start, endDate: end, allDay: true, timeZone: 'UTC' };
}

/** What the entry was written from: when this changes, it needs rewriting. */
function signatureOf(event: CalendarSource, organisation: CalendarOrganisation): string {
  return JSON.stringify([
    event.title,
    event.status === 'Tentative',
    event.startDate,
    event.endDate,
    event.startTime,
    event.venue,
    organisation.name,
    organisation.timeZoneId,
  ]);
}

function lastDayOf(event: CalendarSource): string {
  return event.endDate ?? event.startDate ?? '';
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
 * Adds one event to the person's own calendar, and remembers it so Sahno can
 * keep it right afterwards (D-105). Asks for full calendar access, not
 * write-only: keeping an entry up to date means finding it again.
 *
 * An entry this event already has — added by an older version of the app,
 * which matched on its title — is taken over rather than duplicated.
 */
export async function addToPersonalCalendar(
  event: CalendarSource,
  organisation: CalendarOrganisation,
): Promise<AddToCalendarResult> {
  const entry = entryFor(event, organisation);
  if (entry === null) {
    return { ok: false, reason: 'no-date' };
  }

  const permission = await requestCalendarPermissions(false);
  if (permission.status !== 'granted') {
    return { ok: false, reason: 'permission' };
  }

  try {
    const calendar = await defaultCalendarAsync();
    if (calendar === null) {
      return { ok: false, reason: 'no-calendar' };
    }

    const dayStart = utcMidnight(event.startDate!);
    const dayEnd = utcMidnight(lastDayOf(event));
    dayEnd.setUTCDate(dayEnd.getUTCDate() + 2);
    const existing = (await calendar.listEvents(dayStart, dayEnd)).find(
      (candidate) => candidate.title === event.title || candidate.title === `[Tentative] ${event.title}`,
    );

    let eventId: string;
    if (existing) {
      // An empty location clears a venue that was removed in Sahno.
      await existing.update({ ...entry, location: entry.location ?? '' });
      eventId = existing.id;
    } else {
      const created = await calendar.createEvent({ ...entry, location: entry.location ?? undefined });
      eventId = created.id;
    }

    useCalendarLinks.getState().setLink(event.id, {
      eventId,
      organisationId: organisation.id,
      signature: signatureOf(event, organisation),
      lastDay: lastDayOf(event),
    });
    return { ok: true, tentative: event.status === 'Tentative' };
  } catch {
    return { ok: false, reason: 'failed' };
  }
}

/** Takes an event Sahno added back out of the person's calendar. */
export async function removeFromPersonalCalendar(engagementId: string): Promise<boolean> {
  const link = useCalendarLinks.getState().links[engagementId];
  if (!link) {
    return true;
  }
  try {
    const entry = await ExpoCalendarEvent.get(link.eventId);
    await entry.delete();
  } catch {
    // Already gone — deleted by hand in the calendar app.
  }
  useCalendarLinks.getState().removeLink(engagementId);
  return true;
}

/**
 * Brings every entry Sahno added on this phone into line with Sahno (D-105).
 * `events` is the person's whole calendar across organisations, reaching back
 * to `listedFrom`: an event missing from it that should be there was
 * cancelled, postponed, or the person was taken off it, and its entry comes
 * out. One older than `listedFrom` simply is not listed any more, and is left
 * alone. Entries deleted by hand are forgotten, never re-added.
 *
 * Runs only with calendar access already granted; it never asks.
 */
export async function syncPersonalCalendar(
  events: (CalendarSource & { organisationId: string })[],
  organisations: CalendarOrganisation[],
  listedFrom: string,
): Promise<void> {
  const { links, setLink, removeLink } = useCalendarLinks.getState();
  if (Object.keys(links).length === 0) {
    return;
  }

  const permission = await getCalendarPermissions(false);
  if (permission.status !== 'granted') {
    return;
  }

  const byId = new Map(events.map((event) => [event.id, event]));
  const organisationsById = new Map(organisations.map((organisation) => [organisation.id, organisation]));

  for (const [engagementId, link] of Object.entries(links)) {
    const event = byId.get(engagementId);

    if (!event) {
      if (link.lastDay < listedFrom) {
        removeLink(engagementId);
      } else {
        await removeFromPersonalCalendar(engagementId);
      }
      continue;
    }

    if (!stillBelongs(event)) {
      await removeFromPersonalCalendar(engagementId);
      continue;
    }

    const organisation = organisationsById.get(event.organisationId);
    if (!organisation) {
      continue;
    }

    const signature = signatureOf(event, organisation);
    if (signature === link.signature) {
      continue;
    }

    const entry = entryFor(event, organisation);
    if (entry === null) {
      await removeFromPersonalCalendar(engagementId);
      continue;
    }

    try {
      const existing = await ExpoCalendarEvent.get(link.eventId);
      // An empty location clears a venue that was removed in Sahno.
      await existing.update({ ...entry, location: entry.location ?? '' });
      setLink(engagementId, { ...link, signature, lastDay: lastDayOf(event) });
    } catch {
      // Deleted by hand in the calendar app: that was their choice.
      removeLink(engagementId);
    }
  }
}
