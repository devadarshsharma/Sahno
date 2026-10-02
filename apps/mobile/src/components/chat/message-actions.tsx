import { Ionicons } from '@expo/vector-icons';
import type { ComponentProps } from 'react';
import { Modal, Pressable, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { REACTIONS, type DiscussionMessage } from '@/api/discussion';
import { Text } from '@/components/ui';
import { authorName } from '@/lib/chat';
import { colors, fontFamilies, radii, spacing } from '@/theme';

type IconName = ComponentProps<typeof Ionicons>['name'];

export type MessageAction = 'reply' | 'copy' | 'edit' | 'deleteForMe' | 'deleteForEveryone';

/**
 * What a long-press offers (D-086): a reaction row, then the actions this
 * person may take on this message — and only those. Edit is the author's;
 * delete for everyone is the author's or an organiser's; delete for me is
 * anybody's. A removed message keeps only "delete for me".
 */
export function MessageActions({
  message,
  isOrganiser,
  onClose,
  onReact,
  onAction,
}: {
  message: DiscussionMessage | null;
  isOrganiser: boolean;
  onClose: () => void;
  onReact: (emoji: string) => void;
  onAction: (action: MessageAction) => void;
}) {
  const insets = useSafeAreaInsets();

  if (!message) {
    return null;
  }

  const live = !message.isDeleted;
  const yourReaction = message.reactions?.find((reaction) => reaction.includesYou)?.emoji;

  const actions: { action: MessageAction; label: string; icon: IconName; destructive?: boolean }[] = [
    ...(live ? [{ action: 'reply' as const, label: 'Reply', icon: 'arrow-undo-outline' as IconName }] : []),
    ...(live ? [{ action: 'copy' as const, label: 'Copy', icon: 'copy-outline' as IconName }] : []),
    ...(live && message.isYours ? [{ action: 'edit' as const, label: 'Edit', icon: 'create-outline' as IconName }] : []),
    { action: 'deleteForMe', label: 'Delete for me', icon: 'eye-off-outline', destructive: true },
    ...(live && (message.isYours || isOrganiser)
      ? [{
          action: 'deleteForEveryone' as const,
          label: message.isYours ? 'Delete for everyone' : 'Remove for everyone (organiser)',
          icon: 'trash-outline' as IconName,
          destructive: true,
        }]
      : []),
  ];

  return (
    <Modal transparent animationType="fade" visible onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose} accessibilityLabel="Close">
        <Pressable style={[styles.sheet, { paddingBottom: insets.bottom + spacing.md }]}>
          <Text variant="caption" color="muted" numberOfLines={1} style={styles.context}>
            {authorName(message)}
            {message.body ? `: ${message.body}` : ''}
          </Text>

          {live ? (
            <View style={styles.reactions}>
              {REACTIONS.map((emoji) => (
                <Pressable
                  key={emoji}
                  accessibilityRole="button"
                  accessibilityLabel={yourReaction === emoji ? `Take back ${emoji}` : `React ${emoji}`}
                  onPress={() => onReact(emoji)}
                  style={[styles.reaction, yourReaction === emoji ? styles.reactionChosen : null]}
                >
                  <Text style={styles.reactionEmoji}>{emoji}</Text>
                </Pressable>
              ))}
            </View>
          ) : null}

          <View style={styles.list}>
            {actions.map((item) => (
              <Pressable
                key={item.action}
                accessibilityRole="button"
                onPress={() => onAction(item.action)}
                style={({ pressed }) => [styles.action, pressed ? styles.actionPressed : null]}
              >
                <Ionicons
                  name={item.icon}
                  size={20}
                  color={item.destructive ? colors.text.error : colors.text.primary}
                />
                <Text style={[styles.actionLabel, item.destructive ? styles.destructive : null]}>
                  {item.label}
                </Text>
              </Pressable>
            ))}
          </View>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    justifyContent: 'flex-end',
    backgroundColor: 'rgba(11,27,42,0.4)',
  },
  sheet: {
    backgroundColor: colors.surface.raised,
    borderTopLeftRadius: radii.lg,
    borderTopRightRadius: radii.lg,
    paddingTop: spacing.md,
    paddingHorizontal: spacing.lg,
    gap: spacing.md,
  },
  context: {
    textAlign: 'center',
  },
  reactions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    backgroundColor: colors.surface.subtle,
    borderRadius: 28,
    paddingHorizontal: spacing.sm,
    paddingVertical: 6,
  },
  reaction: {
    width: 44,
    height: 44,
    borderRadius: 22,
    alignItems: 'center',
    justifyContent: 'center',
  },
  reactionChosen: {
    backgroundColor: colors.tealSoft,
  },
  reactionEmoji: {
    fontSize: 26,
  },
  list: {
    gap: 2,
  },
  action: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderRadius: radii.md,
    paddingHorizontal: spacing.sm,
  },
  actionPressed: {
    backgroundColor: colors.surface.subtle,
  },
  actionLabel: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 16,
    color: colors.text.primary,
  },
  destructive: {
    color: colors.text.error,
  },
});
