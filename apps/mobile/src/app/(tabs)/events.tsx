import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { Button, Screen, Text } from '@/components/ui';
import {
  formatEngagementDate,
  groupByStatus,
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

  if (!isOrganiser) {
    return <MemberPlaceholder />;
  }

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
        <Text variant="title">Bookings</Text>
        <Text color="secondary" variant="bodySmall">
          Everything {active?.name} is working on, from first enquiry to done.
        </Text>
      </View>

      <Button
        label="New enquiry"
        onPress={() => router.push('/create-engagement')}
        style={styles.newEnquiry}
      />

      {groups.length === 0 ? (
        <View style={styles.empty}>
          <Text style={styles.emptyEmoji}>📋</Text>
          <Text variant="subheading" style={styles.centeredText}>
            Nothing in the pipeline yet
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
            Start an enquiry with just a title — the date, venue, and everything
            else can come later.
          </Text>
        </View>
      ) : (
        groups.map((group) => (
          <View key={group.status} style={styles.group}>
            <Text variant="subheading">{STATUS_LABELS[group.status]}</Text>
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
        {engagement.venue ? (
          <Text variant="caption" color="muted" numberOfLines={1}>
            {engagement.venue}
          </Text>
        ) : null}
      </View>
      <Text variant="heading" color="muted">
        ›
      </Text>
    </Pressable>
  );
}

function MemberPlaceholder() {
  return (
    <Screen>
      <View style={styles.centered}>
        <View style={styles.illustration}>
          <Text style={styles.emoji}>📅</Text>
        </View>
        <Text variant="heading" style={styles.centeredText}>
          Events
        </Text>
        <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
          Your events and schedules arrive here once your organiser starts
          adding you to them.
        </Text>
      </View>
    </Screen>
  );
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
    gap: 2,
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
