import type { EngagementStatus } from '@/api/engagements';

/** Anything with a day or a run of days. Dates are "YYYY-MM-DD", which compare as text. */
type Dated = {
  id: string;
  status: EngagementStatus;
  startDate: string | null;
  endDate: string | null;
};

/**
 * Statuses that hold a day (D-091). A cancelled event gave its day back, a
 * postponed one's date is no longer its date, and a done one is over.
 */
const HOLDS_THE_DAY: EngagementStatus[] = ['Draft', 'CheckingAvailability', 'Tentative', 'Confirmed'];

function overlaps(
  a: { startDate: string | null; endDate: string | null },
  b: { startDate: string | null; endDate: string | null },
): boolean {
  if (a.startDate === null || b.startDate === null) {
    return false;
  }
  const aEnd = a.endDate ?? a.startDate;
  const bEnd = b.endDate ?? b.startDate;
  return a.startDate <= bEnd && b.startDate <= aEnd;
}

/**
 * Other events on the same day(s) as `target` that still hold that day. A
 * warning, never a block: two bookings on one day are often fine (a
 * ceremony and a reception), and the organiser knows which.
 */
export function clashesWith<T extends Dated>(
  target: { id?: string; startDate: string | null; endDate: string | null },
  others: T[],
): T[] {
  return others.filter(
    (other) => other.id !== target.id && HOLDS_THE_DAY.includes(other.status) && overlaps(target, other),
  );
}
