import { useEffect, useRef } from 'react';

import { useMyCalendar, historyStart } from '@/hooks/use-my-calendar';
import { useActiveOrg } from '@/hooks/use-organisations';
import { syncPersonalCalendar } from '@/lib/personal-calendar';
import { useCalendarLinks } from '@/stores/calendar-links';

/**
 * Keeps the events Sahno put in this phone's calendar right (D-105). Every
 * time the person's calendar is fetched — opening the app, coming back to it,
 * a live change — each entry Sahno added is rewritten if its booking moved,
 * and taken out if it was cancelled or postponed. Renders nothing.
 */
export function CalendarSync() {
  const hydrated = useCalendarLinks((state) => state.hydrated);
  const linkCount = useCalendarLinks((state) => Object.keys(state.links).length);
  const { organisations } = useActiveOrg();
  const myCalendar = useMyCalendar(hydrated && linkCount > 0);
  const running = useRef(false);

  const updatedAt = myCalendar.dataUpdatedAt;
  const events = myCalendar.data?.events;

  useEffect(() => {
    // Only on a successful fetch: an entry is never removed because a
    // request failed.
    if (!hydrated || linkCount === 0 || !events || organisations.length === 0 || running.current) {
      return;
    }
    running.current = true;
    void syncPersonalCalendar(events, organisations, historyStart()).finally(() => {
      running.current = false;
    });
    // Keyed on the fetch time: each new answer from the server is one pass.
  }, [hydrated, linkCount, updatedAt, events, organisations]);

  return null;
}
