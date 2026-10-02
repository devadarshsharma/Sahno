import { Ionicons } from '@expo/vector-icons';
import { Pressable, StyleSheet, View } from 'react-native';

import type { DiscussionMessage } from '@/api/discussion';
import { Text } from '@/components/ui';
import { authorName, messageTime, nameColour, removedText } from '@/lib/chat';
import { colors, fontFamilies, radii, spacing } from '@/theme';

/**
 * One message as a chat bubble (D-086): yours on the right in soft teal,
 * everyone else's on the left in white with their name in their colour at
 * the start of a run. A reply carries its quote; reactions sit under it.
 * Long-press opens the actions; a tap on the quote jumps to the original.
 */
export function MessageBubble({
  message,
  startsRun,
  highlighted,
  onLongPress,
  onQuotePress,
  onReactionPress,
}: {
  message: DiscussionMessage;
  startsRun: boolean;
  highlighted: boolean;
  onLongPress: () => void;
  onQuotePress: (messageId: string) => void;
  onReactionPress: (emoji: string) => void;
}) {
  const yours = message.isYours;
  const reactions = message.reactions ?? [];

  return (
    <View
      style={[
        styles.row,
        yours ? styles.rowYours : styles.rowTheirs,
        startsRun ? styles.rowStartsRun : null,
      ]}
    >
      <Pressable
        onLongPress={onLongPress}
        delayLongPress={300}
        accessibilityRole="button"
        accessibilityHint="Long press for reply, react, copy, edit and delete"
        style={({ pressed }) => [
          styles.bubble,
          yours ? styles.bubbleYours : styles.bubbleTheirs,
          startsRun ? (yours ? styles.tailYours : styles.tailTheirs) : null,
          highlighted ? styles.highlighted : null,
          pressed ? styles.pressed : null,
        ]}
      >
        {!yours && startsRun ? (
          <Text style={[styles.name, { color: nameColour(message.authorUserId) }]} numberOfLines={1}>
            {authorName(message)}
          </Text>
        ) : null}

        {message.replyTo ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Reply to ${message.replyTo.isYours ? 'you' : (message.replyTo.authorDisplayName ?? 'someone')}. Go to the original.`}
            onPress={() => onQuotePress(message.replyTo!.id)}
            style={[styles.quote, yours ? styles.quoteOnYours : null]}
          >
            <View style={[styles.quoteBar, { backgroundColor: quoteColour(message) }]} />
            <View style={styles.quoteText}>
              <Text style={[styles.quoteName, { color: quoteColour(message) }]} numberOfLines={1}>
                {message.replyTo.isYours ? 'You' : (message.replyTo.authorDisplayName ?? 'Someone')}
              </Text>
              <Text variant="caption" color="secondary" numberOfLines={2} style={message.replyTo.isDeleted ? styles.italic : null}>
                {message.replyTo.isDeleted ? 'This message was deleted' : message.replyTo.body}
              </Text>
            </View>
          </Pressable>
        ) : null}

        {message.isDeleted ? (
          <View style={styles.removed}>
            <Ionicons name="ban-outline" size={14} color={colors.text.muted} />
            <Text variant="bodySmall" color="muted" style={styles.italic}>
              {removedText(message)}
            </Text>
          </View>
        ) : (
          <Text style={styles.body}>{message.body}</Text>
        )}

        <Text style={styles.meta}>
          {message.isEdited && !message.isDeleted ? 'edited · ' : ''}
          {messageTime(message.postedAtUtc)}
        </Text>
      </Pressable>

      {reactions.length > 0 ? (
        <View style={[styles.reactions, yours ? styles.reactionsYours : styles.reactionsTheirs]}>
          {reactions.map((reaction) => (
            <Pressable
              key={reaction.emoji}
              accessibilityRole="button"
              accessibilityLabel={`${reaction.emoji} ${reaction.count}${reaction.includesYou ? ', including you. Tap to take yours back.' : '. Tap to add yours.'}`}
              onPress={() => onReactionPress(reaction.emoji)}
              style={[styles.reaction, reaction.includesYou ? styles.reactionMine : null]}
            >
              <Text style={styles.reactionEmoji}>{reaction.emoji}</Text>
              {reaction.count > 1 ? <Text style={styles.reactionCount}>{reaction.count}</Text> : null}
            </Pressable>
          ))}
        </View>
      ) : null}
    </View>
  );
}

function quoteColour(message: DiscussionMessage): string {
  return message.replyTo?.isYours ? colors.tealText : colors.orange;
}

const styles = StyleSheet.create({
  row: {
    paddingHorizontal: spacing.md,
    marginTop: 2,
    maxWidth: '100%',
  },
  rowStartsRun: {
    marginTop: spacing.sm,
  },
  rowYours: {
    alignItems: 'flex-end',
  },
  rowTheirs: {
    alignItems: 'flex-start',
  },
  bubble: {
    maxWidth: '82%',
    paddingHorizontal: spacing.md,
    paddingTop: spacing.sm,
    paddingBottom: 6,
    borderRadius: radii.lg,
    gap: 2,
  },
  bubbleYours: {
    backgroundColor: colors.tealSoft,
  },
  bubbleTheirs: {
    backgroundColor: colors.surface.raised,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border.default,
  },
  tailYours: {
    borderTopRightRadius: 4,
  },
  tailTheirs: {
    borderTopLeftRadius: 4,
  },
  highlighted: {
    backgroundColor: colors.orangeSoft,
  },
  pressed: {
    opacity: 0.8,
  },
  name: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 13,
  },
  quote: {
    flexDirection: 'row',
    borderRadius: radii.sm,
    backgroundColor: colors.surface.subtle,
    overflow: 'hidden',
    marginVertical: 2,
  },
  quoteOnYours: {
    backgroundColor: 'rgba(255,255,255,0.6)',
  },
  quoteBar: {
    width: 4,
  },
  quoteText: {
    flexShrink: 1,
    paddingHorizontal: spacing.sm,
    paddingVertical: 6,
  },
  quoteName: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 12,
  },
  body: {
    fontSize: 16,
    lineHeight: 22,
    color: colors.text.primary,
  },
  removed: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  italic: {
    fontStyle: 'italic',
  },
  meta: {
    alignSelf: 'flex-end',
    fontSize: 11,
    color: colors.text.muted,
  },
  reactions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 4,
    marginTop: -6,
    maxWidth: '82%',
  },
  reactionsYours: {
    marginRight: spacing.sm,
  },
  reactionsTheirs: {
    marginLeft: spacing.sm,
  },
  reaction: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 12,
    backgroundColor: colors.surface.raised,
    borderWidth: 1,
    borderColor: colors.border.default,
  },
  reactionMine: {
    borderColor: colors.tealText,
    backgroundColor: colors.tealSoft,
  },
  reactionEmoji: {
    fontSize: 14,
  },
  reactionCount: {
    fontSize: 12,
    color: colors.text.secondary,
    fontFamily: fontFamilies.uiMedium,
  },
});
