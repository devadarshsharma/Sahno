import { Ionicons } from '@expo/vector-icons';
import { useEffect, useState } from 'react';
import { Animated, Pressable, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import type { NotificationKind, NotificationPayload } from '@/api/notifications';
import { Text } from '@/components/ui';
import { colors, radii, shadows, spacing } from '@/theme';

const SHOW_FOR_MS = 5000;

/**
 * The in-app notification: a card that drops in under the status bar while
 * the app is open, says what happened, and takes you there on a tap. Gone on
 * its own after five seconds or on a swipe up — it is a nudge, not a modal.
 */
export function NotificationBanner({
  payload,
  onOpen,
  onDismiss,
}: {
  payload: NotificationPayload;
  onOpen: () => void;
  onDismiss: () => void;
}) {
  const insets = useSafeAreaInsets();
  const [translate] = useState(() => new Animated.Value(-160));

  useEffect(() => {
    Animated.spring(translate, {
      toValue: 0,
      useNativeDriver: true,
      damping: 18,
      stiffness: 180,
    }).start();

    const timer = setTimeout(() => {
      Animated.timing(translate, {
        toValue: -160,
        duration: 220,
        useNativeDriver: true,
      }).start(onDismiss);
    }, SHOW_FOR_MS);

    return () => clearTimeout(timer);
  }, [translate, onDismiss, payload.notificationId]);

  return (
    <Animated.View
      pointerEvents="box-none"
      style={[styles.host, { paddingTop: insets.top + spacing.xs }, { transform: [{ translateY: translate }] }]}
    >
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${payload.title}. ${payload.body ?? ''} Open.`}
        onPress={onOpen}
        style={({ pressed }) => [styles.card, pressed ? styles.pressed : null]}
      >
        <View style={styles.icon}>
          <Ionicons name={iconFor(payload.notificationType)} size={18} color={colors.tealText} />
        </View>
        <View style={styles.text}>
          <Text style={styles.title} numberOfLines={1}>
            {payload.title}
          </Text>
          {payload.body ? (
            <Text variant="bodySmall" color="secondary" numberOfLines={2}>
              {payload.body}
            </Text>
          ) : null}
        </View>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Dismiss"
          onPress={onDismiss}
          hitSlop={10}
          style={styles.close}
        >
          <Ionicons name="close" size={18} color={colors.text.muted} />
        </Pressable>
      </Pressable>
    </Animated.View>
  );
}

function iconFor(kind: NotificationKind): React.ComponentProps<typeof Ionicons>['name'] {
  switch (kind) {
    case 'ResponsibilityAssigned':
      return 'clipboard-outline';
    case 'DiscussionMessage':
      return 'chatbubble-outline';
    case 'AvailabilityRequested':
    case 'AvailabilityReminder':
    case 'AvailabilityAnswered':
      return 'people-outline';
    case 'EngagementCancelled':
    case 'EngagementPostponed':
      return 'alert-circle-outline';
    case 'EngagementConfirmed':
      return 'checkmark-circle-outline';
    case 'OrganiserAnnouncement':
      return 'megaphone-outline';
    case 'MemberJoined':
      return 'person-add-outline';
    default:
      return 'notifications-outline';
  }
}

const styles = StyleSheet.create({
  host: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    paddingHorizontal: spacing.md,
    zIndex: 1000,
    elevation: 1000,
  },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radii.lg,
    backgroundColor: colors.surface.raised,
    borderWidth: 1,
    borderColor: colors.border.default,
    ...shadows.md,
  },
  pressed: {
    opacity: 0.85,
  },
  icon: {
    width: 36,
    height: 36,
    borderRadius: radii.full,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  text: {
    flex: 1,
    gap: 2,
  },
  title: {
    fontWeight: '600',
  },
  close: {
    padding: spacing.xs,
  },
});
