import { useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { Notification, NotificationKind } from '@/api/notifications';
import { Button, Card, Screen, Text, TextAction } from '@/components/ui';
import {
  useMarkAllRead,
  useMarkRead,
  useReadNotifications,
  useUnreadNotifications,
} from '@/hooks/use-notifications';
import { colors, radii, spacing } from '@/theme';

/**
 * The bell (D-042, D-049, D-111). What is unread, newest first, with **Mark
 * all as read** at the top; tapping one goes to the event it is about and
 * reads it. Once read, a notification leaves this list — the bell is for what
 * still needs a look — and waits under **Earlier**, fetched twenty at a time
 * only when asked for.
 */
export default function Notifications() {
  const router = useRouter();
  const unreadQuery = useUnreadNotifications();
  const [showEarlier, setShowEarlier] = useState(false);
  const readQuery = useReadNotifications(showEarlier);
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();

  const unread = unreadQuery.data ?? [];
  const earlier = (readQuery.data?.pages ?? []).flat();

  function open(notification: Notification) {
    if (!notification.isRead) {
      markRead.mutate(notification.id);
    }

    // The API names where each kind goes (D-080), so the bell, the banner
    // and the tray all land in one place. An announcement points here.
    if (notification.route !== '/notifications') {
      router.push(notification.route as never);
    }
  }

  return (
    <Screen
      scroll
      onRefresh={() => {
        void unreadQuery.refetch();
        if (showEarlier) {
          void readQuery.refetch();
        }
      }}
      refreshing={unreadQuery.isRefetching}
      hero={{
        title: 'Notifications',
        subtitle: unreadQuery.isPending
          ? undefined
          : unread.length === 0
            ? 'All caught up.'
            : `${unread.length} unread.`,
      }}
    >
      {unread.length > 0 ? (
        <View style={styles.toolbar}>
          <TextAction
            label="Mark all as read"
            color="accent"
            busy={markAllRead.isPending}
            onPress={() => markAllRead.mutate()}
          />
        </View>
      ) : null}

      {unreadQuery.isPending ? (
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      ) : unread.length > 0 ? (
        <Card style={styles.list}>
          {unread.map((row) => (
            <NotificationRow key={row.id} notification={row} onPress={() => open(row)} />
          ))}
        </Card>
      ) : (
        <Text color="muted" variant="bodySmall" style={styles.empty}>
          Nothing new. Read notifications are under Earlier.
        </Text>
      )}

      {showEarlier ? (
        <>
          <Text variant="subheading" style={styles.sectionHeading}>
            Earlier
          </Text>
          {readQuery.isPending ? (
            <View style={styles.centered}>
              <ActivityIndicator color={colors.tealText} />
            </View>
          ) : earlier.length === 0 ? (
            <Text color="muted" variant="bodySmall" style={styles.empty}>
              Nothing earlier.
            </Text>
          ) : (
            <Card style={styles.list}>
              {earlier.map((row) => (
                <NotificationRow key={row.id} notification={row} onPress={() => open(row)} />
              ))}
            </Card>
          )}
          {readQuery.hasNextPage ? (
            <Button
              label="Show more"
              variant="secondary"
              loading={readQuery.isFetchingNextPage}
              onPress={() => void readQuery.fetchNextPage()}
            />
          ) : null}
          <Button label="Hide earlier notifications" variant="ghost" onPress={() => setShowEarlier(false)} />
        </>
      ) : (
        <Button label="Show earlier notifications" variant="ghost" onPress={() => setShowEarlier(true)} />
      )}
    </Screen>
  );
}

function NotificationRow({ notification, onPress }: { notification: Notification; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${notification.title}${notification.isRead ? '' : ', unread'}`}
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
    >
      <View style={[styles.dot, notification.isRead ? styles.dotRead : null]} />
      <View style={styles.rowBody}>
        <View style={styles.rowHead}>
          <Text variant="caption" color="muted" style={styles.rowKind}>
            {kindLabel(notification.kind)}
          </Text>
          <Text variant="caption" color="muted" style={styles.rowWhen}>
            {formatWhen(notification.createdAtUtc)}
          </Text>
        </View>
        <Text variant="bodySmall" color={notification.isRead ? 'secondary' : 'primary'}>
          {notification.title}
        </Text>
        {notification.body ? (
          <Text variant="caption" color="secondary" numberOfLines={2}>
            {notification.body}
          </Text>
        ) : null}
      </View>
      <Text color="muted">›</Text>
    </Pressable>
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
    case 'RemovedFromEngagement':
      return 'Lineup change';
    case 'EngagementConfirmed':
      return 'Confirmed';
    case 'EngagementPostponed':
      return 'Postponed';
    case 'EngagementCancelled':
      return 'Cancelled';
    case 'EngagementReopened':
      return 'Back on';
    case 'EngagementUnconfirmed':
      return 'No longer confirmed';
    case 'EngagementDateChanged':
      return 'New date';
    case 'EngagementDetailsChanged':
      return 'Details changed';
    case 'EngagementDayReminder':
      return 'Today';
    case 'DetailsRequested':
      return 'Question';
    case 'ResponsibilityAssigned':
      return 'Your job';
    case 'ResponsibilityReminder':
      return 'Still to do';
    case 'RehearsalScheduled':
      return 'Rehearsal';
    case 'RehearsalReminder':
      return 'Rehearsal today';
    case 'DiscussionMessage':
      return 'Discussion';
    case 'DiscussionReaction':
      return 'Reaction';
    case 'MemberJoined':
      return 'New member';
    case 'OrganiserAnnouncement':
      return 'Announcement';
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
  toolbar: {
    alignItems: 'flex-end',
    marginBottom: spacing.sm,
  },
  empty: {
    marginBottom: spacing.lg,
  },
  sectionHeading: {
    marginBottom: spacing.sm,
  },
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
