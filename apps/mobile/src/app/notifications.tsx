import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { Notification, NotificationKind } from '@/api/notifications';
import { Button, Card, Screen, Text } from '@/components/ui';
import {
  useMarkAllRead,
  useMarkRead,
  useNotifications,
} from '@/hooks/use-notifications';
import { colors, radii, spacing } from '@/theme';

/**
 * The bell's list (D-042, D-049). Newest first, unread rows marked, and
 * tapping one goes to the event it is about. Reading happens on the tap: a
 * notification you opened is one you have seen, and there is no separate
 * gesture to learn.
 */
export default function Notifications() {
  const router = useRouter();
  const query = useNotifications();
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();

  const rows = query.data ?? [];
  const unread = rows.filter((row) => !row.isRead).length;

  function open(notification: Notification) {
    if (!notification.isRead) {
      markRead.mutate(notification.id);
    }

    if (notification.engagementId) {
      router.push({
        pathname: '/engagement/[engagementId]',
        params: { engagementId: notification.engagementId },
      });
      return;
    }

    // Organisation-level news — a new member — goes to People.
    if (notification.kind === 'MemberJoined') {
      router.push('/(tabs)/people');
    }
  }

  return (
    <Screen
      scroll
      onRefresh={() => query.refetch()}
      refreshing={query.isRefetching}
      hero={{
        title: 'Notifications',
        subtitle:
          rows.length === 0
            ? 'Nothing yet.'
            : unread === 0
              ? 'All caught up.'
              : `${unread} unread.`,
      }}
    >

      {query.isPending ? (
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      ) : null}

      {rows.length > 0 ? (
        <Card style={styles.list}>
          {rows.map((row) => (
            <Pressable
              key={row.id}
              accessibilityRole="button"
              accessibilityLabel={`${row.title}${row.isRead ? '' : ', unread'}`}
              onPress={() => open(row)}
              style={({ pressed }) => [
                styles.row,
                pressed ? styles.rowPressed : null,
              ]}
            >
              <View style={[styles.dot, row.isRead ? styles.dotRead : null]} />
              <View style={styles.rowBody}>
                <View style={styles.rowHead}>
                  <Text variant="caption" color="muted" style={styles.rowKind}>
                    {kindLabel(row.kind)}
                  </Text>
                  <Text variant="caption" color="muted" style={styles.rowWhen}>
                    {formatWhen(row.createdAtUtc)}
                  </Text>
                </View>
                <Text
                  variant="bodySmall"
                  color={row.isRead ? 'secondary' : 'primary'}
                >
                  {row.title}
                </Text>
                {row.body ? (
                  <Text variant="caption" color="secondary" numberOfLines={2}>
                    {row.body}
                  </Text>
                ) : null}
              </View>
              <Text color="muted">›</Text>
            </Pressable>
          ))}
        </Card>
      ) : null}

      {unread > 0 ? (
        <Button
          label="Mark all as read"
          variant="ghost"
          onPress={() => markAllRead.mutate()}
          loading={markAllRead.isPending}
        />
      ) : null}

    </Screen>
  );
}

/**
 * A short category for the row, since the title already says what happened.
 * Kept to a word or two so the eye can scan for the kind it cares about.
 */
function kindLabel(kind: NotificationKind): string {
  switch (kind) {
    case 'AvailabilityRequested':
    case 'AvailabilityReminder':
      return 'Availability';
    case 'AvailabilityAnswered':
      return 'Answer';
    case 'EngagementConfirmed':
      return 'Confirmed';
    case 'EngagementPostponed':
      return 'Postponed';
    case 'EngagementCancelled':
      return 'Cancelled';
    case 'EngagementReopened':
      return 'Back on';
    case 'EngagementDateChanged':
      return 'New date';
    case 'EngagementDetailsChanged':
      return 'Details changed';
    case 'ResponsibilityAssigned':
      return 'Your job';
    case 'DiscussionMessage':
      return 'Discussion';
    case 'MemberJoined':
      return 'New member';
  }
}

function formatWhen(createdAtUtc: string): string {
  const created = new Date(createdAtUtc);
  const minutes = Math.floor((Date.now() - created.getTime()) / 60000);

  if (minutes < 1) {
    return 'now';
  }
  if (minutes < 60) {
    return `${minutes}m`;
  }
  if (minutes < 24 * 60) {
    return `${Math.floor(minutes / 60)}h`;
  }

  return created.toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
}

const styles = StyleSheet.create({
  centered: {
    paddingVertical: spacing.xl,
    alignItems: 'center',
  },
  list: {
    gap: 0,
    marginBottom: spacing.lg,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border.default,
  },
  rowPressed: {
    backgroundColor: colors.surface.subtle,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: radii.full,
    backgroundColor: colors.orange,
  },
  dotRead: {
    backgroundColor: 'transparent',
  },
  rowBody: {
    flex: 1,
    gap: 2,
  },
  rowHead: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: spacing.sm,
  },
  rowKind: {
    flexShrink: 1,
  },
  // The time never wraps or truncates; the label beside it gives way instead.
  rowWhen: {
    flexShrink: 0,
  },
});
