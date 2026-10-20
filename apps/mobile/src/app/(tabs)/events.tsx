import { Ionicons } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { Engagement, EngagementStatus } from '@/api/engagements';
import { EngagementCalendar, type CalendarEvent, type CalendarRehearsal } from '@/components/engagement-calendar';
import { OrgBadge } from '@/components/org-badge';
import { Button, Screen, showAlert, Text, toIsoDate } from '@/components/ui';
import {
  FILTER_LABELS,
  formatEngagementDate,
  groupByMonth,
  groupByStatus,
  isPast,
  matchesFilter,
  MEMBER_STATUS_LABELS,
  STATUS_LABELS,
  useEngagements,
  type BookingsFilter,
  type TimeScope,
} from '@/hooks/use-engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useMyCalendar } from '@/hooks/use-my-calendar';
import { colors, fontFamilies, radii, shadows, spacing } from '@/theme';

/**
 * The organiser's pipeline, labelled Bookings for them and Events for Members
 * (D-039). Members cannot see engagements until participant selection exists
 * (Slice 5), so they still get the placeholder.
 */
export default function Events() {
  const router = useRouter();
  const { active, switchTo } = useActiveOrg();
  const engagementsQuery = useEngagements();
  const [view, setView] = useState<'list' | 'calendar'>('list');
  // The list opens on what is coming up; Past is the bookings' history (D-100).
  const [scope, setScope] = useState<TimeScope>('upcoming');

  // Arrived from a Home tile: show only what that tile counted. Clearing it
  // is one tap, and the param goes with it so a re-tap from Home still works.
  const params = useLocalSearchParams<{ filter?: string }>();
  const filter = (params.filter ?? null) as BookingsFilter | null;
  const clearFilter = () => router.setParams({ filter: undefined });

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';
  // The person's whole diary, across every organisation they belong to: the
  // calendar (D-091) and the list (D-098) both show it.
  const myCalendar = useMyCalendar();

  if (engagementsQuery.isPending) {
    return (
      <Screen>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (engagementsQuery.isError) {
    return (
      <Screen>
        <View style={styles.centered}>
          <Text variant="heading" color="error">
            Could not load your bookings
          </Text>
          <Text color="secondary" style={styles.centeredText}>
            Check your connection and try again.
          </Text>
          <Button label="Try again" onPress={() => engagementsQuery.refetch()} />
        </View>
      </Screen>
    );
  }

  const shown = filter
    ? engagementsQuery.data.filter((engagement) => matchesFilter(engagement, filter))
    : engagementsQuery.data;
  // The list, like the calendar, is the person's whole diary (D-098): this
  // organisation's bookings, then what they have with other organisations.
  // A filter from a Home tile is about this organisation, so it shows only
  // this one's.
  const here: ListEvent[] = shown.map((engagement) => ({
    ...engagement,
    organisationId: active?.id,
    organisationName: active?.name ?? null,
    isOrganiser,
  }));
  const elsewhere: ListEvent[] = filter
    ? []
    : (myCalendar.data?.events ?? []).filter((event) => event.organisationId !== active?.id);
  const listed = [...here, ...elsewhere].sort(byDate);
  const multiOrg = elsewhere.length > 0;

  // Upcoming, Past or All (D-100). A filter from a Home tile already says what
  // to show, so it ignores the scope rather than hiding half of what it counted.
  const today = toIsoDate(new Date());
  const past = listed.filter((event) => isPast(event, today));
  const upcoming = listed.filter((event) => !isPast(event, today));
  const scoped = filter ? listed : scope === 'past' ? past : scope === 'upcoming' ? upcoming : listed;
  const scopeCounts: Record<TimeScope, number> = { upcoming: upcoming.length, past: past.length, all: listed.length };
  // History reads by when, not by pipeline stage.
  // Asked and not yet answered goes first, whatever its status — a confirmed
  // event still needs the answer, and filed under "Confirmed" it looked like
  // one already given (D-119).
  const toAnswer = scope === 'past' && !filter ? [] : scoped.filter(needsYourAnswer);
  const rest = scoped.filter((event) => !toAnswer.includes(event));
  const groups =
    !filter && scope === 'past'
      ? groupByMonth(scoped).map((group) => ({ key: group.key, heading: group.label, items: group.items }))
      : [
          ...(toAnswer.length > 0 ? [{ key: 'to-answer', heading: 'Needs your answer', items: toAnswer }] : []),
          ...groupByStatus(rest).map((group) => ({
            key: group.status,
            heading: isOrganiser ? STATUS_LABELS[group.status] : MEMBER_STATUS_LABELS[group.status],
            items: group.items,
          })),
        ];

  // This organisation's bookings as the list has them (the filter applies),
  // then everything the person has with other organisations. Every entry says
  // whose it is, so the calendar can badge them when it mixes groups (D-098).
  const calendarEvents: CalendarEvent[] = [
    ...here,
    ...(myCalendar.data?.events ?? []).filter((event) => event.organisationId !== active?.id),
  ];
  const calendarRehearsals: CalendarRehearsal[] = myCalendar.data?.rehearsals ?? [];

  // Another organisation's event opens there: say so, then switch (D-044).
  const openIn = (organisationId: string | undefined, organisationName: string | null | undefined, open: () => void) => {
    if (!organisationId || organisationId === active?.id) {
      open();
      return;
    }
    showAlert(`Open in ${organisationName}?`, `This is ${organisationName}'s. Sahno switches to ${organisationName} to open it.`, [
      { text: 'Cancel', style: 'cancel' },
      {
        text: `Switch to ${organisationName}`,
        onPress: () => {
          switchTo(organisationId);
          open();
        },
      },
    ]);
  };

  return (
    <Screen
      scroll
      onRefresh={() => engagementsQuery.refetch()}
      refreshing={engagementsQuery.isRefetching}
      hero={{
        title: isOrganiser ? 'Bookings' : 'Events',
        subtitle: isOrganiser
          ? `Everything ${active?.name} is working on, from first enquiry to done.`
          : 'The events you have been asked about or added to.',
      }}
    >

      {isOrganiser ? (
        <Button
          label="New enquiry"
          onPress={() => router.push('/create-engagement')}
          style={styles.newEnquiry}
        />
      ) : null}

      {/* One dataset, two lenses. Calendar lives here rather than as a sixth
          tab: D-042 is provisional and warns about crowding, and a month view
          answers "when" about the same bookings this tab already lists. */}
      <View style={styles.viewToggle}>
        {(['list', 'calendar'] as const).map((option) => {
          const selected = view === option;
          return (
            <Pressable
              key={option}
              accessibilityRole="button"
              accessibilityState={{ selected }}
              accessibilityLabel={option === 'list' ? 'List view' : 'Calendar view'}
              onPress={() => setView(option)}
              style={[styles.viewOption, selected ? styles.viewOptionOn : null]}
            >
              <Text variant="label" color={selected ? 'inverse' : 'secondary'}>
                {option === 'list' ? 'List' : 'Calendar'}
              </Text>
            </Pressable>
          );
        })}
      </View>

      {filter ? (
        <View style={styles.filterBar}>
          <Text variant="bodySmall" color="secondary">
            Showing <Text style={styles.filterName}>{FILTER_LABELS[filter]}</Text>
          </Text>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Show all bookings"
            onPress={clearFilter}
            hitSlop={8}
          >
            <Text variant="bodySmall" color="accent">
              Show all
            </Text>
          </Pressable>
        </View>
      ) : null}

      {view === 'list' && !filter && listed.length > 0 ? (
        <View style={styles.scopeRow}>
          {SCOPES.map((option) => {
            const selected = scope === option;
            return (
              <Pressable
                key={option}
                accessibilityRole="button"
                accessibilityState={{ selected }}
                accessibilityLabel={`${SCOPE_LABELS[option]}, ${scopeCounts[option]}`}
                onPress={() => setScope(option)}
                style={[styles.scopeChip, selected ? styles.scopeChipOn : null]}
              >
                <Text
                  variant="bodySmall"
                  color={selected ? 'primary' : 'secondary'}
                  style={selected ? styles.filterName : null}
                >
                  {SCOPE_LABELS[option]} · {scopeCounts[option]}
                </Text>
              </Pressable>
            );
          })}
        </View>
      ) : null}

      {view === 'calendar' ? (
        <EngagementCalendar
          engagements={calendarEvents}
          onOpen={(event) =>
            openIn(event.organisationId, event.organisationName, () =>
              router.push({
                pathname: '/engagement/[engagementId]',
                params: { engagementId: event.id },
              }),
            )
          }
          rehearsals={calendarRehearsals}
          onOpenRehearsal={(rehearsal) =>
            openIn(rehearsal.organisationId, rehearsal.organisationName, () =>
              router.push({
                pathname: '/engagement/[engagementId]/files',
                params: { engagementId: rehearsal.engagementId },
              }),
            )
          }
        />
      ) : listed.length > 0 && groups.length === 0 ? (
        <View style={styles.empty}>
          <Ionicons name={scope === 'past' ? 'time-outline' : 'calendar-outline'} size={40} color={colors.tealText} />
          <Text variant="subheading" style={styles.centeredText}>
            {scope === 'past' ? 'Nothing in the past yet' : 'Nothing coming up'}
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
            {scope === 'past'
              ? 'Finished bookings, and ones whose date has gone, show here.'
              : 'Earlier bookings are under Past.'}
          </Text>
        </View>
      ) : groups.length === 0 ? (
        <View style={styles.empty}>
          <Ionicons
            name={isOrganiser ? 'clipboard-outline' : 'calendar-outline'}
            size={40}
            color={colors.tealText}
          />
          <Text variant="subheading" style={styles.centeredText}>
            {filter
              ? `Nothing under ${FILTER_LABELS[filter].toLowerCase()}`
              : isOrganiser
                ? 'Nothing in the pipeline yet'
                : 'Nothing on yet'}
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
            {isOrganiser
              ? 'Start an enquiry with just a title — the date, venue, and everything else can come later.'
              : 'Events appear here as soon as an organiser asks whether you are available.'}
          </Text>
        </View>
      ) : (
        groups.map((group) => (
          <View key={group.key} style={styles.group}>
            <Text variant="subheading">{group.heading}</Text>
            {group.items.map((engagement) => (
              <EngagementRow
                key={engagement.id}
                engagement={engagement}
                showOrganisation={multiOrg}
                onPress={() =>
                  openIn(engagement.organisationId, engagement.organisationName, () =>
                    router.push({
                      pathname: '/engagement/[engagementId]',
                      params: { engagementId: engagement.id },
                    }),
                  )
                }
              />
            ))}
          </View>
        ))
      )}

      {view === 'list' && !filter && scope !== 'upcoming' && multiOrg ? (
        <Text variant="caption" color="muted" style={styles.centeredText}>
          Events with other organisations go back twelve months.
        </Text>
      ) : null}
    </Screen>
  );
}

const SCOPES: TimeScope[] = ['upcoming', 'past', 'all'];

const SCOPE_LABELS: Record<TimeScope, string> = {
  upcoming: 'Upcoming',
  past: 'Past',
  all: 'All',
};

/**
 * What a list row needs of an event — met by this organisation's bookings and
 * by the person's events with other organisations alike (D-098).
 */
type ListEvent = Pick<
  Engagement,
  'id' | 'title' | 'status' | 'startDate' | 'endDate' | 'venue' | 'outstandingCount' | 'yourResponse'
> & { organisationId?: string; organisationName?: string | null; isOrganiser?: boolean };

/**
 * You were asked and have not answered, and it is still ahead (D-119). Only
 * as a member: an organiser's own lineup place is not told apart from not
 * being on it, so for them this would be guesswork.
 */
function needsYourAnswer(event: ListEvent): boolean {
  return (
    !event.isOrganiser &&
    event.yourResponse === null &&
    (event.status === 'CheckingAvailability' || event.status === 'Tentative' || event.status === 'Confirmed')
  );
}

/** Soonest first; undated last, as they have no day to sort by. */
function byDate(a: ListEvent, b: ListEvent): number {
  if (a.startDate === b.startDate) {
    return 0;
  }
  if (a.startDate === null) {
    return 1;
  }
  if (b.startDate === null) {
    return -1;
  }
  return a.startDate < b.startDate ? -1 : 1;
}

function EngagementRow({
  engagement,
  onPress,
  showOrganisation,
}: {
  engagement: ListEvent;
  onPress: () => void;
  /** The list mixes organisations: say whose this is. */
  showOrganisation: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${engagement.title}. ${formatEngagementDate(engagement)}.`}
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
    >
      <View style={styles.rowText}>
        <Text variant="subheading" numberOfLines={1}>
          {engagement.title}
        </Text>
        {showOrganisation && engagement.organisationId && engagement.organisationName ? (
          <OrgBadge organisationId={engagement.organisationId} name={engagement.organisationName} />
        ) : null}
        <Text
          variant="bodySmall"
          color={engagement.startDate === null ? 'muted' : 'secondary'}
        >
          {formatEngagementDate(engagement)}
        </Text>
        <View style={styles.rowMeta}>
          {needsYourAnswer(engagement) ? (
            <View style={[styles.chip, styles.chipWaiting]}>
              <Text variant="caption" style={styles.chipText}>
                Needs your answer
              </Text>
            </View>
          ) : null}
          <StatusChip engagement={engagement} />
          {engagement.venue ? (
            <Text variant="caption" color="muted" numberOfLines={1}>
              {engagement.venue}
            </Text>
          ) : null}
        </View>
      </View>
      <Text variant="heading" color="muted">
        ›
      </Text>
    </Pressable>
  );
}


/**
 * What state this booking is in, and — when it is waiting on people — how many
 * have still to answer. The group heading says the same thing, but a row is
 * often read on its own, and the number is the bit that decides what to do
 * next.
 */
function StatusChip({ engagement }: { engagement: ListEvent }) {
  const waiting = engagement.outstandingCount ?? 0;
  const chasing =
    engagement.status === 'CheckingAvailability' ||
    engagement.status === 'Tentative' ||
    engagement.status === 'Confirmed';

  const label =
    chasing && waiting > 0
      ? `${SHORT_STATUS[engagement.status]} · ${waiting} to answer`
      : SHORT_STATUS[engagement.status];

  return (
    <View style={[styles.chip, chipStyle(engagement.status, waiting)]}>
      <Text variant="caption" style={styles.chipText}>
        {label}
      </Text>
    </View>
  );
}

const SHORT_STATUS: Record<EngagementStatus, string> = {
  Draft: 'Enquiry',
  CheckingAvailability: 'Checking',
  Tentative: 'Tentative',
  Confirmed: 'Confirmed',
  Postponed: 'Postponed',
  Completed: 'Done',
  Cancelled: 'Cancelled',
};

function chipStyle(status: EngagementStatus, waiting: number) {
  if (waiting > 0) {
    return styles.chipWaiting;
  }
  if (status === 'Confirmed') {
    return styles.chipConfirmed;
  }
  if (status === 'Cancelled' || status === 'Completed') {
    return styles.chipQuiet;
  }
  return styles.chipNeutral;
}
const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.xl,
  },
  centeredText: {
    textAlign: 'center',
  },
  filterBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: spacing.md,
  },
  filterName: {
    fontFamily: fontFamilies.uiMedium,
    color: colors.text.primary,
  },
  scopeRow: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  scopeChip: {
    paddingHorizontal: spacing.md,
    minHeight: 32,
    justifyContent: 'center',
    borderRadius: radii.full,
    borderWidth: 1,
    borderColor: colors.border.default,
  },
  scopeChipOn: {
    backgroundColor: colors.tealSoft,
    borderColor: colors.tealSoft,
  },
  viewToggle: {
    flexDirection: 'row',
    gap: spacing.xs,
    padding: 4,
    marginBottom: spacing.lg,
    borderRadius: radii.full,
    backgroundColor: colors.surface.subtle,
  },
  viewOption: {
    flex: 1,
    minHeight: 36,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.full,
  },
  viewOptionOn: {
    backgroundColor: colors.interactive.primary,
  },
  newEnquiry: {
    marginBottom: spacing.xl,
  },
  group: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radii.lg,
    backgroundColor: colors.surface.raised,
    ...shadows.sm,
  },
  rowPressed: {
    backgroundColor: colors.surface.subtle,
  },
  rowText: {
    flex: 1,
    gap: 4,
  },
  rowMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flexWrap: 'wrap',
  },
  chip: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: radii.full,
  },
  chipText: {
    color: colors.text.primary,
  },
  chipWaiting: {
    backgroundColor: colors.orangeSoft,
  },
  chipConfirmed: {
    backgroundColor: colors.tealSoft,
  },
  chipNeutral: {
    backgroundColor: colors.surface.subtle,
  },
  chipQuiet: {
    backgroundColor: colors.surface.subtle,
  },
  empty: {
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.xxl,
  },
  illustration: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: colors.surface.subtle,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emoji: {
    fontSize: 40,
    lineHeight: 50,
  },
});
