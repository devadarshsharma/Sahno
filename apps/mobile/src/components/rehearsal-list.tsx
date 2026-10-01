import { Ionicons } from '@expo/vector-icons';
import { Pressable, StyleSheet, View } from 'react-native';

import type { UpcomingRehearsal } from '@/api/upcoming-rehearsals';
import { formatClock } from '@/components/engagement-card';
import { Text } from '@/components/ui';
import { colors, fontFamilies, radii, shadows, spacing } from '@/theme';

/** How many Home shows; the rest are on the events and the calendar. */
const HOME_LIMIT = 3;

/**
 * The next rehearsals, soonest first (D-085), so nobody learns about one by
 * opening the right event at the right moment. Hidden when there are none —
 * Home says nothing about what is not there.
 */
export function RehearsalList({
  rehearsals,
  onOpen,
}: {
  rehearsals: UpcomingRehearsal[];
  onOpen: (rehearsal: UpcomingRehearsal) => void;
}) {
  if (rehearsals.length === 0) {
    return null;
  }

  const shown = rehearsals.slice(0, HOME_LIMIT);
  const more = rehearsals.length - shown.length;

  return (
    <View style={styles.section}>
      <Text variant="subheading">Rehearsals</Text>
      <View style={styles.cards}>
        {shown.map((rehearsal) => (
          <RehearsalCard
            key={rehearsal.id}
            rehearsal={rehearsal}
            onPress={() => onOpen(rehearsal)}
          />
        ))}
      </View>
      {more > 0 ? (
        <Text variant="caption" color="muted">
          {more === 1 ? '1 more' : `${more} more`} on the calendar.
        </Text>
      ) : null}
    </View>
  );
}

function RehearsalCard({
  rehearsal,
  onPress,
}: {
  rehearsal: UpcomingRehearsal;
  onPress: () => void;
}) {
  const date = new Date(`${rehearsal.date}T00:00:00`);
  const name = rehearsal.title ?? 'Rehearsal';
  const when = rehearsalTimes(rehearsal);

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={[
        `${name} for ${rehearsal.engagementTitle}`,
        date.toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'long' }),
        when,
        rehearsal.venue,
      ]
        .filter(Boolean)
        .join('. ')}
      onPress={onPress}
      style={({ pressed }) => [styles.card, pressed ? styles.pressed : null]}
    >
      <View style={styles.dateBlock}>
        <Text style={styles.dateDay}>{date.getDate()}</Text>
        <Text style={styles.dateMonth}>
          {date.toLocaleDateString(undefined, { month: 'short' })}
        </Text>
      </View>
      <View style={styles.text}>
        <Text style={styles.title} numberOfLines={1}>
          {name}
        </Text>
        <Text variant="caption" color="secondary" numberOfLines={1}>
          {[
            date.toLocaleDateString(undefined, { weekday: 'short' }),
            when,
            rehearsal.venue,
          ]
            .filter(Boolean)
            .join(' · ')}
        </Text>
        <Text variant="caption" color="muted" numberOfLines={1}>
          For {rehearsal.engagementTitle}
        </Text>
      </View>
      <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
    </Pressable>
  );
}

/** "7:00 PM–9:00 PM", "7:00 PM", or nothing when no time was set. */
export function rehearsalTimes(rehearsal: UpcomingRehearsal): string | null {
  if (!rehearsal.startTime) {
    return null;
  }
  return rehearsal.endTime
    ? `${formatClock(rehearsal.startTime)}–${formatClock(rehearsal.endTime)}`
    : formatClock(rehearsal.startTime);
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.md,
  },
  cards: {
    gap: spacing.sm,
  },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    backgroundColor: colors.surface.raised,
    borderRadius: radii.lg,
    padding: spacing.md,
    ...shadows.md,
  },
  pressed: {
    opacity: 0.85,
  },
  dateBlock: {
    width: 48,
    paddingVertical: spacing.xs,
    borderRadius: radii.md,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
  },
  dateDay: {
    fontFamily: fontFamilies.bold,
    fontSize: 18,
    lineHeight: 22,
    color: colors.tealText,
  },
  dateMonth: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 11,
    color: colors.tealText,
    textTransform: 'uppercase',
  },
  text: {
    flex: 1,
    gap: 2,
  },
  title: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 16,
    color: colors.text.primary,
  },
});
