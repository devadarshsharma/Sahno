import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { Engagement, EngagementStatus } from '@/api/engagements';
import { Button, Screen, Text } from '@/components/ui';
import {
  formatEngagementDate,
  groupByStatus,
  MEMBER_STATUS_LABELS,
  STATUS_LABELS,
  useEngagements,
} from '@/hooks/use-engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, radii, shadows, spacing } from '@/theme';

/**
 * The organiser's pipeline, labelled Bookings for them and Events for Members
 * (D-039). Members cannot see engagements until participant selection exists
 * (Slice 5), so they still get the placeholder.
 */
export default function Events() {
  const router = useRouter();
  const { active } = useActiveOrg();
  const engagementsQuery = useEngagements();

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

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

  const groups = groupByStatus(engagementsQuery.data);

  return (
    <Screen
      scroll
      onRefresh={() => engagementsQuery.refetch()}
      refreshing={engagementsQuery.isRefetching}
    >
      <View style={styles.header}>
        <Text variant="title">{isOrganiser ? 'Bookings' : 'Events'}</Text>
        <Text color="secondary" variant="bodySmall">
          {isOrganiser
            ? `Everything ${active?.name} is working on, from first enquiry to done.`
            : 'The events you have been asked about or added to.'}
        </Text>
      </View>

      {isOrganiser ? (
        <Button
          label="New enquiry"
          onPress={() => router.push('/create-engagement')}
          style={styles.newEnquiry}
        />
      ) : null}

      {groups.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyEmoji}>{isOrganiser ? '📋' : '📅'}</Text>
          <Text variant="subheading" style={styles.centeredText}>
            {isOrganiser ? 'Nothing in the pipeline yet' : 'Nothing on yet'}
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
            {isOrganiser
              ? 'Start an enquiry with just a title — the date, venue, and everything else can come later.'
              : 'Events appear here as soon as an organiser asks whether you are available.'}
          </Text>
        </View>
      ) : (
        groups.map((group) => (
          <View key={group.status} style={styles.group}>
            <Text variant="subheading">
              {isOrganiser
                ? STATUS_LABELS[group.status]
                : MEMBER_STATUS_LABELS[group.status]}
            </Text>
            {group.items.map((engagement) => (
              <EngagementRow
                key={engagement.id}
                engagement={engagement}
                onPress={() =>
                  router.push({
                    pathname: '/engagement/[engagementId]',
                    params: { engagementId: engagement.id },
                  })
                }
              />
            ))}
          </View>
        ))
      )}
    </Screen>
  );
}

function EngagementRow({
  engagement,
  onPress,
}: {
  engagement: Engagement;
  onPress: () => void;
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
        <Text
          variant="bodySmall"
          color={engagement.startDate === null ? 'muted' : 'secondary'}
        >
          {formatEngagementDate(engagement)}
        </Text>
        <View style={styles.rowMeta}>
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
function StatusChip({ engagement }: { engagement: Engagement }) {
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
  header: {
    gap: spacing.xs,
    marginBottom: spacing.lg,
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
  emptyEmoji: {
    fontSize: 44,
    lineHeight: 56,
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
