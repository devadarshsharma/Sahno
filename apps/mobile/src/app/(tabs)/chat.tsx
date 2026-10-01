import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import type { ChatInboxEntry } from '@/api/chats';
import { Button, Card, CountBadge, Screen, Text } from '@/components/ui';
import { useChatInbox } from '@/hooks/use-chats';
import { colors, fontFamilies, spacing } from '@/theme';

/**
 * The Chat inbox (D-084). Conversations still belong to their events
 * (D-024): this is the index across them, newest activity first, so catching
 * up does not mean opening events one at a time. Only threads where something
 * has been said, and only active ones — a finished event's chat drops out a
 * week after it goes quiet, and stays on the event itself.
 */
export default function Chat() {
  const router = useRouter();
  const inbox = useChatInbox();

  const openChat = (engagementId: string) =>
    router.push({
      pathname: '/engagement/[engagementId]/chat',
      params: { engagementId },
    });

  const entries = inbox.data ?? [];

  return (
    <Screen
      onRefresh={() => inbox.refetch()}
      refreshing={inbox.isRefetching}
      hero={{
        title: 'Chat',
        subtitle: 'Every conversation, newest first.',
      }}
    >
      {inbox.isPending ? (
        <View style={styles.container}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      ) : inbox.isError ? (
        <View style={styles.container}>
          <Text color="error" style={styles.centered}>
            Could not load your conversations.
          </Text>
          <Button label="Try again" variant="secondary" onPress={() => inbox.refetch()} />
        </View>
      ) : entries.length === 0 ? (
        <View style={styles.container}>
          <View style={styles.illustration}>
            <Ionicons name="chatbubbles-outline" size={40} color={colors.tealText} />
          </View>
          <Text variant="heading" style={styles.centered}>
            No conversations yet
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centered}>
            Every event has its own chat. Once somebody writes in one, it shows
            up here.
          </Text>
          <Button label="Go to events" onPress={() => router.push('/(tabs)/events')} />
        </View>
      ) : (
        <Card style={styles.list}>
          {entries.map((entry, index) => (
            <InboxRow
              key={entry.engagementId}
              entry={entry}
              last={index === entries.length - 1}
              onPress={() => openChat(entry.engagementId)}
            />
          ))}
        </Card>
      )}
    </Screen>
  );
}

function InboxRow({
  entry,
  last,
  onPress,
}: {
  entry: ChatInboxEntry;
  last: boolean;
  onPress: () => void;
}) {
  const unread = entry.unreadMessages > 0;
  const preview = previewOf(entry);
  const when = formatInboxTime(entry.lastMessageAtUtc);

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={[
        entry.title,
        preview,
        when,
        unread ? `${entry.unreadMessages} unread` : null,
      ]
        .filter(Boolean)
        .join('. ')}
      onPress={onPress}
      style={({ pressed }) => [
        styles.row,
        last ? styles.rowLast : null,
        pressed ? styles.rowPressed : null,
      ]}
    >
      <View style={styles.avatar}>
        <Ionicons name="chatbubble-outline" size={18} color={colors.tealText} />
      </View>
      <View style={styles.rowText}>
        <View style={styles.rowTop}>
          <Text
            style={[styles.title, unread ? styles.titleUnread : null]}
            numberOfLines={1}
          >
            {entry.title}
          </Text>
          <Text
            variant="caption"
            color={unread ? 'accent' : 'muted'}
            style={unread ? styles.timeUnread : null}
          >
            {when}
          </Text>
        </View>
        <View style={styles.rowBottom}>
          <Text
            variant="bodySmall"
            color={unread ? 'primary' : 'secondary'}
            numberOfLines={1}
            style={styles.preview}
          >
            {preview}
          </Text>
          <CountBadge count={entry.unreadMessages} />
        </View>
      </View>
    </Pressable>
  );
}

/** "Priya: running late", "You: on my way", or a removal said plainly. */
function previewOf(entry: ChatInboxEntry): string {
  const who = entry.lastIsYours
    ? 'You'
    : (entry.lastAuthorDisplayName ?? 'Someone');
  const what = entry.lastMessageRemoved
    ? 'Message removed'
    : (entry.lastMessagePreview ?? '');
  return `${who}: ${what}`;
}

/**
 * The time a chat list shows: the clock today, "Yesterday", the weekday this
 * week, and the date before that.
 */
function formatInboxTime(isoUtc: string): string {
  const at = new Date(isoUtc);
  const now = new Date();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const dayMs = 24 * 60 * 60 * 1000;

  if (at >= startOfToday) {
    return at.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
  }
  if (at >= new Date(startOfToday.getTime() - dayMs)) {
    return 'Yesterday';
  }
  if (at >= new Date(startOfToday.getTime() - 6 * dayMs)) {
    return at.toLocaleDateString(undefined, { weekday: 'short' });
  }
  return at.toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
}

const styles = StyleSheet.create({
  list: {
    paddingVertical: 0,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border.default,
  },
  rowLast: {
    borderBottomWidth: 0,
  },
  rowPressed: {
    opacity: 0.7,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  rowTop: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  rowBottom: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  title: {
    flex: 1,
    fontFamily: fontFamilies.uiMedium,
    fontSize: 16,
    color: colors.text.primary,
  },
  titleUnread: {
    fontFamily: fontFamilies.bold,
  },
  timeUnread: {
    fontFamily: fontFamilies.uiMedium,
  },
  preview: {
    flex: 1,
  },
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.xl,
  },
  illustration: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  centered: {
    textAlign: 'center',
  },
});
