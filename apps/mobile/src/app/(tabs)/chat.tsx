import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, View } from 'react-native';

import { Button, Card, CountBadge, Screen, Text } from '@/components/ui';
import { formatEngagementDate, useEngagements } from '@/hooks/use-engagements';
import { colors, fontFamilies, spacing } from '@/theme';

/**
 * Discussion lives inside each event rather than in an inbox (D-024, D-047
 * §6), so this tab is mostly a signpost. What it adds is the one thing a
 * signpost cannot: which events have messages you have not read, with the
 * count, each a tap away from its conversation. A full cross-event inbox is
 * still a later question, if a real group ever asks for one.
 */
export default function Chat() {
  const router = useRouter();
  const engagementsQuery = useEngagements();

  const withUnread = (engagementsQuery.data ?? []).filter(
    (engagement) => (engagement.unreadMessages ?? 0) > 0,
  );

  const openChat = (engagementId: string) =>
    router.push({
      pathname: '/engagement/[engagementId]/chat',
      params: { engagementId },
    });

  return (
    <Screen
      onRefresh={() => engagementsQuery.refetch()}
      refreshing={engagementsQuery.isRefetching}
      hero={{
        title: 'Chat',
        subtitle: 'Conversations live with their events.',
      }}
    >
      {withUnread.length > 0 ? (
        <View style={styles.list}>
          <Text variant="subheading">New messages</Text>
          {withUnread.map((engagement) => {
            const unread = engagement.unreadMessages ?? 0;
            return (
              <Pressable
                key={engagement.id}
                accessibilityRole="button"
                accessibilityLabel={`${engagement.title}. ${unread} unread.`}
                onPress={() => openChat(engagement.id)}
                style={({ pressed }) => (pressed ? styles.pressed : null)}
              >
                <Card style={styles.row}>
                  <View style={styles.rowIcon}>
                    <Ionicons name="chatbubble-outline" size={18} color={colors.tealText} />
                  </View>
                  <View style={styles.rowText}>
                    <Text style={styles.rowTitle} numberOfLines={1}>
                      {engagement.title}
                    </Text>
                    <Text variant="caption" color="secondary" numberOfLines={1}>
                      {unread === 1 ? '1 new message' : `${unread} new messages`} ·{' '}
                      {formatEngagementDate(engagement)}
                    </Text>
                  </View>
                  <CountBadge count={unread} />
                  <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
                </Card>
              </Pressable>
            );
          })}
        </View>
      ) : (
        <View style={styles.container}>
          <View style={styles.illustration}>
            <Ionicons name="chatbubbles-outline" size={40} color={colors.tealText} />
          </View>
          <Text variant="heading" style={styles.centered}>
            You are all caught up
          </Text>
          <Text color="secondary" variant="bodySmall" style={styles.centered}>
            Every booking has its own discussion, so what was said about
            Saturday stays next to Saturday. New messages show up here.
          </Text>
          <Button label="Go to events" onPress={() => router.push('/(tabs)/events')} />
        </View>
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  list: {
    gap: spacing.md,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
  },
  rowIcon: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  rowTitle: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 16,
    color: colors.text.primary,
  },
  pressed: {
    opacity: 0.85,
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
