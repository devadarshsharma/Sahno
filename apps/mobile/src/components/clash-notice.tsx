import { Ionicons } from '@expo/vector-icons';
import { StyleSheet, View } from 'react-native';

import type { AvailabilityAnswer } from '@/api/availability';
import type { MyCalendarEvent } from '@/api/my-calendar';
import { Text } from '@/components/ui';
import { useEngagements } from '@/hooks/use-engagements';
import { useMyCalendar } from '@/hooks/use-my-calendar';
import { useActiveOrg } from '@/hooks/use-organisations';
import { clashesWith } from '@/lib/clashes';
import { colors, fontFamilies, radii, spacing } from '@/theme';

type Day = { id?: string; startDate: string | null; endDate?: string | null };

/**
 * For an organiser choosing a date (D-091): other bookings in this group on
 * that day, and the organiser's own commitments with other groups. A heads-up
 * only — nothing stops the booking.
 */
export function BookingClashNotice({ day }: { day: Day }) {
  const { active } = useActiveOrg();
  const engagements = useEngagements();
  const calendar = useMyCalendar(day.startDate !== null);
  const target = { id: day.id, startDate: day.startDate, endDate: day.endDate ?? null };

  if (day.startDate === null) {
    return null;
  }

  const here = clashesWith(target, engagements.data ?? []).map((engagement) => ({
    key: engagement.id,
    text: engagement.title,
    detail: 'already booked in this group',
  }));
  const elsewhere = yours(target, calendar.data?.events ?? [])
    .filter((event) => event.organisationId !== active?.id)
    .map((event) => describe(event));

  return <Notice title="Something else is on this day" lines={[...here, ...elsewhere]} />;
}

/**
 * For a performer asked about an event (D-091): what else they have that day,
 * in this group or any other they belong to. Only they see it — no organiser
 * is told what somebody has on with another group.
 */
export function YourClashNotice({ day }: { day: Day & { id: string } }) {
  const { active } = useActiveOrg();
  const calendar = useMyCalendar(day.startDate !== null);
  const target = { id: day.id, startDate: day.startDate, endDate: day.endDate ?? null };

  const lines = yours(target, calendar.data?.events ?? []).map((event) =>
    describe(event, event.organisationId === active?.id),
  );

  return <Notice title="You have something else on this day" lines={lines} />;
}

/** The caller's own commitments that clash: everything except what they turned down. */
function yours(target: Day & { endDate: string | null }, events: MyCalendarEvent[]): MyCalendarEvent[] {
  return clashesWith(target, events).filter((event) => event.yourResponse !== 'Unavailable');
}

const ANSWERED: Record<AvailabilityAnswer, string> = {
  Available: 'you said available',
  Maybe: 'you said maybe',
  Unavailable: 'you said not available',
};

function describe(event: MyCalendarEvent, sameGroup = false) {
  const where = sameGroup ? 'in this group' : `with ${event.organisationName}`;
  const standing = event.yourResponse
    ? ANSWERED[event.yourResponse]
    : event.isOrganiser
      ? 'you organise it'
      : 'not answered yet';
  return { key: `${event.organisationId}-${event.id}`, text: event.title, detail: `${where} · ${standing}` };
}

function Notice({ title, lines }: { title: string; lines: { key: string; text: string; detail: string }[] }) {
  if (lines.length === 0) {
    return null;
  }

  return (
    <View style={styles.notice} accessibilityRole="alert">
      <View style={styles.heading}>
        <Ionicons name="warning-outline" size={16} color={colors.orange} />
        <Text style={styles.title}>{title}</Text>
      </View>
      {lines.map((line) => (
        <Text key={line.key} variant="bodySmall" color="secondary">
          <Text variant="bodySmall" style={styles.strong}>
            {line.text}
          </Text>
          {` — ${line.detail}`}
        </Text>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  notice: {
    gap: 4,
    padding: spacing.md,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.orange,
    backgroundColor: colors.orangeSoft,
  },
  heading: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  title: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 14,
    color: colors.text.primary,
  },
  strong: {
    fontFamily: fontFamilies.uiMedium,
    color: colors.text.primary,
  },
});
