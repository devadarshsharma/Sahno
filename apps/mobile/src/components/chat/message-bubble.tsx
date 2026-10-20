import { Ionicons } from '@expo/vector-icons';
import { Pressable, StyleSheet, View } from 'react-native';

import type { DiscussionMessage, DiscussionReader } from '@/api/discussion';
import { Text } from '@/components/ui';
import { authorName, initials, messageTime, nameColour, removedText, type ChatMessage } from '@/lib/chat';
import { splitMentions } from '@/lib/mentions';
import { colors, fontFamilies, radii, spacing } from '@/theme';

/**
 * One message as a chat bubble (D-086): yours on the right in soft teal,
 * everyone else's on the left in white with their name in their colour at
 * the start of a run. A reply carries its quote; reactions sit under it.
 * Long-press opens the actions; a tap on the quote jumps to the original.
 * Your own messages carry where they are (D-087): a clock while sending, a
 * tick once the server has them, and "Not sent" — tap to retry — if not.
 */
export function MessageBubble({
  message,
  startsRun,
  highlighted,
  onLongPress,
  onQuotePress,
  onReactionPress,
  onRetry,
  mentionNames = [],
  mentionsYou = false,
}: {
  /** Names of the people this message mentions, to pick out in the text (D-112). */
  mentionNames?: string[];
  /** It mentions the reader: it stands out in the thread. */
  mentionsYou?: boolean;
  message: ChatMessage;
  startsRun: boolean;
  highlighted: boolean;
  onLongPress: () => void;
  onQuotePress: (messageId: string) => void;
  onReactionPress: (emoji: string) => void;
  onRetry: () => void;
}) {
  const yours = message.isYours;
  const reactions = message.reactions ?? [];
  const failed = message.delivery === 'failed';

  return (
    <View
      style={[
        styles.row,
        yours ? styles.rowYours : styles.rowTheirs,
        startsRun ? styles.rowStartsRun : null,
      ]}
    >
      <Pressable
        onPress={failed ? onRetry : undefined}
        onLongPress={onLongPress}
        delayLongPress={300}
        accessibilityRole="button"
        accessibilityHint={
          failed
            ? 'Not sent. Tap to try again, long press to delete'
            : 'Long press for reply, react, copy, edit and delete'
        }
        style={({ pressed }) => [
          styles.bubble,
          yours ? styles.bubbleYours : styles.bubbleTheirs,
          startsRun ? (yours ? styles.tailYours : styles.tailTheirs) : null,
          mentionsYou && !yours ? styles.mentionsYou : null,
          highlighted ? styles.highlighted : null,
          failed ? styles.failed : null,
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
          <Text style={styles.body}>
            {splitMentions(message.body ?? '', mentionNames).map((segment, index) =>
              segment.mention ? (
                <Text key={index} style={styles.mention}>
                  {segment.text}
                </Text>
              ) : (
                segment.text
              ),
            )}
          </Text>
        )}

        <View style={styles.metaRow}>
          <Text style={styles.meta}>
            {message.isEdited && !message.isDeleted ? 'edited · ' : ''}
            {messageTime(message.postedAtUtc)}
          </Text>
          {yours && !message.isDeleted ? <DeliveryMark delivery={message.delivery} /> : null}
        </View>
      </Pressable>

      {failed ? (
        <Pressable accessibilityRole="button" onPress={onRetry} hitSlop={8} style={styles.notSent}>
          <Text style={styles.notSentText}>Not sent · Tap to retry</Text>
        </Pressable>
      ) : null}

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

      <SeenBy readers={message.seenBy ?? []} yours={yours} />
    </View>
  );
}

/** How many initials show before the rest become "+N". */
const SEEN_SHOWN = 5;

/**
 * Who has read up to here (D-096): a row of initials, each in that person's
 * colour, five at most and then "+N". It sits under the last message each
 * of them has seen, and moves down as they read on.
 */
function SeenBy({ readers, yours }: { readers: DiscussionReader[]; yours: boolean }) {
  if (readers.length === 0) {
    return null;
  }

  const shown = readers.slice(0, SEEN_SHOWN);
  const more = readers.length - shown.length;
  const names = readers.map((reader) => reader.displayName ?? 'Someone');

  return (
    <View
      style={[styles.seen, yours ? styles.seenYours : styles.seenTheirs]}
      accessible
      accessibilityLabel={`Seen by ${names.join(', ')}`}
    >
      {shown.map((reader) => (
        <View key={reader.userId} style={[styles.seenDot, { backgroundColor: nameColour(reader.userId) }]}>
          <Text style={styles.seenInitials}>{initials(reader.displayName)}</Text>
        </View>
      ))}
      {more > 0 ? <Text style={styles.seenMore}>+{more}</Text> : null}
    </View>
  );
}

function DeliveryMark({ delivery }: { delivery: ChatMessage['delivery'] }) {
  switch (delivery) {
    case 'sending':
      return <Ionicons name="time-outline" size={13} color={colors.text.muted} accessibilityLabel="Sending" />;
    case 'failed':
      return <Ionicons name="alert-circle" size={14} color={colors.text.error} accessibilityLabel="Not sent" />;
    default:
      return <Ionicons name="checkmark" size={14} color={colors.tealText} accessibilityLabel="Sent" />;
  }
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
  // A message that mentions you, among everyone else's (D-112).
  mentionsYou: {
    borderLeftWidth: 3,
    borderLeftColor: colors.orange,
  },
  mention: {
    fontFamily: fontFamilies.uiMedium,
    color: colors.tealText,
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
  metaRow: {
    alignSelf: 'flex-end',
    flexDirection: 'row',
    alignItems: 'center',
    gap: 3,
  },
  meta: {
    fontSize: 11,
    color: colors.text.muted,
  },
  failed: {
    opacity: 0.75,
  },
  notSent: {
    marginTop: 2,
    marginRight: spacing.xs,
  },
  notSentText: {
    fontSize: 12,
    fontFamily: fontFamilies.uiMedium,
    color: colors.text.error,
  },
  seen: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
    marginTop: 4,
  },
  seenYours: {
    justifyContent: 'flex-end',
    marginRight: spacing.xs,
  },
  seenTheirs: {
    marginLeft: spacing.xs,
  },
  seenDot: {
    width: 18,
    height: 18,
    borderRadius: 9,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.surface.canvas,
  },
  seenInitials: {
    fontSize: 8,
    lineHeight: 10,
    fontFamily: fontFamilies.uiMedium,
    color: colors.surface.raised,
  },
  seenMore: {
    fontSize: 11,
    lineHeight: 14,
    color: colors.text.muted,
    marginLeft: 2,
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
    lineHeight: 20,
  },
  reactionCount: {
    fontSize: 12,
    color: colors.text.secondary,
    fontFamily: fontFamilies.uiMedium,
  },
});
