import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { Text } from '@/components/ui';
import { colors, radii, spacing } from '@/theme';

/**
 * What a day's dot means. Slice 6 asks the calendar to distinguish three
 * things, and they are ranked: a date that needs an answer outranks one that
 * is merely provisional, which outranks one that is settled — because that is
 * the order in which they need the reader to do something.
 */
type DayState = 'needsResponse' | 'tentative' | 'confirmed';

const STATE_RANK: Record<DayState, number> = {
  needsResponse: 0,
  tentative: 1,
  confirmed: 2,
};

export const CALENDAR_KEY: { state: DayState; label: string }[] = [
  { state: 'needsResponse', label: 'Needs a response' },
  { state: 'tentative', label: 'Tentative' },
  { state: 'confirmed', label: 'Confirmed' },
];

export function stateOf(engagement: Engagement): DayState | null {
  if (engagement.status === 'Confirmed') {
    return 'confirmed';
  }
  if (engagement.status === 'Tentative') {
    return 'tentative';
  }
  if (engagement.status === 'CheckingAvailability') {
    // For a member this is their own answer; for an organiser it is the
    // lineup's. Either way the day is unresolved.
    const unanswered =
      engagement.yourResponse === null || (engagement.outstandingCount ?? 0) > 0;
    return unanswered ? 'needsResponse' : 'tentative';
  }
  return null;
}

function isoDay(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

/**
 * A month at a glance. Days carry a dot in the colour of the most urgent thing
 * happening on them; choosing a day lists what is actually on.
 */
export function EngagementCalendar({
  engagements,
  onOpen,
}: {
  engagements: Engagement[];
  onOpen: (engagementId: string) => void;
}) {
  const today = useMemo(() => new Date(), []);
  const [month, setMonth] = useState(
    () => new Date(today.getFullYear(), today.getMonth(), 1),
  );
  const [chosen, setChosen] = useState<string | null>(isoDay(today));

  // One pass to bucket engagements by day, so the grid does not scan the whole
  // list for each of its cells.
  const byDay = useMemo(() => {
    const map = new Map<string, Engagement[]>();
    for (const engagement of engagements) {
      if (engagement.startDate === null || stateOf(engagement) === null) {
        continue;
      }
      const existing = map.get(engagement.startDate);
      if (existing) {
        existing.push(engagement);
      } else {
        map.set(engagement.startDate, [engagement]);
      }
    }
    return map;
  }, [engagements]);

  const firstOfMonth = new Date(month.getFullYear(), month.getMonth(), 1);
  const daysInMonth = new Date(
    month.getFullYear(),
    month.getMonth() + 1,
    0,
  ).getDate();

  // Monday-first, which is how a week reads for people planning gigs.
  const leadingBlanks = (firstOfMonth.getDay() + 6) % 7;
  const cells: (Date | null)[] = [
    ...Array.from({ length: leadingBlanks }, () => null),
    ...Array.from(
      { length: daysInMonth },
      (_, index) => new Date(month.getFullYear(), month.getMonth(), index + 1),
    ),
  ];

  const chosenEvents = chosen ? (byDay.get(chosen) ?? []) : [];

  return (
    <View style={styles.wrap}>
      <View style={styles.monthBar}>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Previous month"
          onPress={() =>
            setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))
          }
          style={styles.monthButton}
        >
          <Text variant="subheading" color="accent">
            ‹
          </Text>
        </Pressable>

        <Text variant="subheading">
          {month.toLocaleDateString(undefined, {
            month: 'long',
            year: 'numeric',
          })}
        </Text>

        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Next month"
          onPress={() =>
            setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))
          }
          style={styles.monthButton}
        >
          <Text variant="subheading" color="accent">
            ›
          </Text>
        </Pressable>
      </View>

      <View style={styles.weekRow}>
        {['M', 'T', 'W', 'T', 'F', 'S', 'S'].map((day, index) => (
          <Text
            key={`${day}-${index}`}
            variant="caption"
            color="muted"
            style={styles.weekday}
          >
            {day}
          </Text>
        ))}
      </View>

      <View style={styles.grid}>
        {cells.map((date, index) => {
          if (date === null) {
            return <View key={`blank-${index}`} style={styles.cell} />;
          }

          const key = isoDay(date);
          const events = byDay.get(key) ?? [];
          const state = events
            .map(stateOf)
            .filter((value): value is DayState => value !== null)
            .sort((a, b) => STATE_RANK[a] - STATE_RANK[b])[0];

          const isToday = key === isoDay(today);
          const isChosen = key === chosen;

          return (
            <Pressable
              key={key}
              accessibilityRole="button"
              accessibilityState={{ selected: isChosen }}
              accessibilityLabel={`${date.getDate()} ${month.toLocaleDateString(undefined, { month: 'long' })}${
                events.length > 0 ? `, ${events.length} on` : ''
              }`}
              onPress={() => setChosen(key)}
              style={[styles.cell, isChosen ? styles.cellChosen : null]}
            >
              <Text
                variant="bodySmall"
                color={isChosen ? 'inverse' : isToday ? 'accent' : 'primary'}
              >
                {date.getDate()}
              </Text>
              <View style={styles.dotRow}>
                {state ? (
                  <View style={[styles.dot, dotStyle(state)]} />
                ) : (
                  <View style={styles.dotSpacer} />
                )}
              </View>
            </Pressable>
          );
        })}
      </View>

      <View style={styles.key}>
        {CALENDAR_KEY.map((entry) => (
          <View key={entry.state} style={styles.keyItem}>
            <View style={[styles.dot, dotStyle(entry.state)]} />
            <Text variant="caption" color="muted">
              {entry.label}
            </Text>
          </View>
        ))}
      </View>

      <View style={styles.dayList}>
        <Text variant="label" color="secondary">
          {chosen
            ? new Date(`${chosen}T00:00:00`).toLocaleDateString(undefined, {
                weekday: 'long',
                day: 'numeric',
                month: 'long',
              })
            : 'Pick a day'}
        </Text>

        {chosenEvents.length === 0 ? (
          <Text color="muted" variant="bodySmall">
            Nothing on this day.
          </Text>
        ) : (
          chosenEvents.map((engagement) => (
            <Pressable
              key={engagement.id}
              accessibilityRole="button"
              accessibilityLabel={engagement.title}
              onPress={() => onOpen(engagement.id)}
              style={styles.dayRow}
            >
              <View
                style={[
                  styles.dayStripe,
                  dotStyle(stateOf(engagement) ?? 'confirmed'),
                ]}
              />
              <View style={styles.dayText}>
                <Text numberOfLines={1}>{engagement.title}</Text>
                {engagement.venue ? (
                  <Text variant="caption" color="muted" numberOfLines={1}>
                    {engagement.venue}
                  </Text>
                ) : null}
              </View>
              <Text color="muted">›</Text>
            </Pressable>
          ))
        )}
      </View>
    </View>
  );
}

function dotStyle(state: DayState) {
  if (state === 'needsResponse') {
    return styles.dotNeedsResponse;
  }
  return state === 'tentative' ? styles.dotTentative : styles.dotConfirmed;
}

const styles = StyleSheet.create({
  wrap: {
    gap: spacing.md,
  },
  monthBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  monthButton: {
    minWidth: 44,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
  },
  weekRow: {
    flexDirection: 'row',
  },
  weekday: {
    flex: 1,
    textAlign: 'center',
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  cell: {
    width: `${100 / 7}%`,
    aspectRatio: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 2,
    borderRadius: radii.md,
  },
  cellChosen: {
    backgroundColor: colors.interactive.primary,
  },
  dotRow: {
    height: 6,
    justifyContent: 'center',
  },
  dot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  dotSpacer: {
    width: 6,
    height: 6,
  },
  dotNeedsResponse: {
    backgroundColor: colors.orange,
  },
  dotTentative: {
    backgroundColor: colors.teal,
  },
  dotConfirmed: {
    backgroundColor: colors.tealText,
  },
  key: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.md,
  },
  keyItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
  },
  dayList: {
    gap: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.border.default,
  },
  dayRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.sm,
  },
  dayStripe: {
    width: 4,
    height: 32,
    borderRadius: 2,
  },
  dayText: {
    flex: 1,
    gap: 2,
  },
});
