import { Ionicons } from '@expo/vector-icons';
import { useEffect, useRef, useState } from 'react';
import { Pressable, TextInput as NativeTextInput, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import type { DiscussionMessage } from '@/api/discussion';
import { Text } from '@/components/ui';
import { authorName } from '@/lib/chat';
import { insertMention, mentionQueryAt } from '@/lib/mentions';
import { colors, fontFamilies, radii, spacing } from '@/theme';

/** What the composer is doing besides writing a fresh message. */
export type ComposerMode =
  | { kind: 'new' }
  | { kind: 'reply'; to: DiscussionMessage }
  | { kind: 'edit'; message: DiscussionMessage };

/** Somebody the composer can offer after an "@" (D-112). */
export type MentionOption = { userId: string; name: string; detail: string | null };

/** How many people the "@" list shows at once. */
const MAX_SUGGESTIONS = 5;

/**
 * The typing bar, fixed at the bottom of the chat (D-086). Replying shows the
 * quoted message above it; editing shows what is being changed. Either can
 * be cancelled with the cross.
 *
 * Typing "@" offers the people who can be mentioned (D-112), narrowed as you
 * type; tapping one writes "@Name " and tells the chat who was picked.
 */
export function Composer({
  value,
  onChange,
  mode,
  onCancelMode,
  onSend,
  sending,
  mentionOptions = [],
  onMention,
}: {
  value: string;
  onChange: (text: string) => void;
  mode: ComposerMode;
  onCancelMode: () => void;
  onSend: () => void;
  sending: boolean;
  mentionOptions?: MentionOption[];
  onMention?: (option: MentionOption) => void;
}) {
  const insets = useSafeAreaInsets();
  const input = useRef<NativeTextInput>(null);
  const [cursor, setCursor] = useState(value.length);
  const canSend = value.trim().length > 0 && !sending;

  // Mentions belong to new messages and replies; an edit keeps its own.
  const typing = onMention && mode.kind !== 'edit' ? mentionQueryAt(value, Math.min(cursor, value.length)) : null;
  const needle = typing?.query.toLowerCase() ?? '';
  const suggestions = typing
    ? mentionOptions
        .filter((option) => option.name.toLowerCase().includes(needle))
        .sort((a, b) => Number(!a.name.toLowerCase().startsWith(needle)) - Number(!b.name.toLowerCase().startsWith(needle)))
        .slice(0, MAX_SUGGESTIONS)
    : [];

  const pick = (option: MentionOption) => {
    if (!typing) {
      return;
    }
    const next = insertMention(value, typing.start, Math.min(cursor, value.length), option.name);
    onChange(next.text);
    setCursor(next.cursor);
    onMention?.(option);
    // After the new text lands, put the cursor after the name.
    setTimeout(() => input.current?.setSelection(next.cursor, next.cursor), 0);
  };

  // Choosing Reply or Edit puts the cursor straight in the box.
  useEffect(() => {
    if (mode.kind !== 'new') {
      input.current?.focus();
    }
  }, [mode]);

  return (
    <View style={[styles.wrap, { paddingBottom: Math.max(insets.bottom, spacing.sm) }]}>
      {mode.kind !== 'new' ? (
        <View style={styles.context}>
          <View style={styles.contextBar} />
          <View style={styles.contextText}>
            <Text style={styles.contextTitle} numberOfLines={1}>
              {mode.kind === 'reply' ? `Replying to ${authorName(mode.to)}` : 'Editing your message'}
            </Text>
            <Text variant="caption" color="secondary" numberOfLines={1}>
              {mode.kind === 'reply' ? mode.to.body : mode.message.body}
            </Text>
          </View>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={mode.kind === 'reply' ? 'Cancel reply' : 'Cancel edit'}
            onPress={onCancelMode}
            hitSlop={10}
          >
            <Ionicons name="close" size={20} color={colors.text.muted} />
          </Pressable>
        </View>
      ) : null}

      {suggestions.length > 0 ? (
        <View style={styles.suggestions} accessibilityRole="list" accessibilityLabel="People to mention">
          {suggestions.map((option, index) => (
            <Pressable
              key={option.userId}
              accessibilityRole="button"
              accessibilityLabel={`Mention ${option.name}`}
              onPress={() => pick(option)}
              style={({ pressed }) => [
                styles.suggestion,
                index > 0 ? styles.suggestionDivider : null,
                pressed ? styles.suggestionPressed : null,
              ]}
            >
              <View style={styles.initial}>
                <Text style={styles.initialText}>{option.name.trim().charAt(0).toUpperCase()}</Text>
              </View>
              <Text variant="bodySmall" numberOfLines={1} style={styles.suggestionName}>
                {option.name}
              </Text>
              {option.detail ? (
                <Text variant="caption" color="muted" numberOfLines={1}>
                  {option.detail}
                </Text>
              ) : null}
            </Pressable>
          ))}
        </View>
      ) : null}

      <View style={styles.bar}>
        <NativeTextInput
          ref={input}
          value={value}
          onChangeText={onChange}
          onSelectionChange={(event) => setCursor(event.nativeEvent.selection.end)}
          placeholder="Message"
          placeholderTextColor={colors.text.muted}
          multiline
          maxLength={4000}
          style={styles.input}
          accessibilityLabel="Message"
        />
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={mode.kind === 'edit' ? 'Save edit' : 'Send'}
          accessibilityState={{ disabled: !canSend }}
          disabled={!canSend}
          onPress={onSend}
          style={[styles.send, canSend ? null : styles.sendDisabled]}
        >
          <Ionicons
            name={mode.kind === 'edit' ? 'checkmark' : 'send'}
            size={18}
            color={colors.offWhite}
          />
        </Pressable>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    backgroundColor: colors.surface.raised,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border.default,
    paddingTop: spacing.sm,
    paddingHorizontal: spacing.md,
    gap: spacing.sm,
  },
  context: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: colors.surface.subtle,
    borderRadius: radii.sm,
    overflow: 'hidden',
    paddingRight: spacing.sm,
  },
  contextBar: {
    width: 4,
    alignSelf: 'stretch',
    backgroundColor: colors.tealText,
  },
  contextText: {
    flex: 1,
    paddingVertical: 6,
  },
  contextTitle: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 13,
    color: colors.tealText,
  },
  suggestions: {
    borderRadius: radii.md,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border.default,
    backgroundColor: colors.surface.raised,
    overflow: 'hidden',
  },
  suggestion: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    minHeight: 44,
    paddingHorizontal: spacing.md,
  },
  suggestionDivider: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border.default,
  },
  suggestionPressed: {
    backgroundColor: colors.surface.subtle,
  },
  suggestionName: {
    flexShrink: 1,
  },
  initial: {
    width: 26,
    height: 26,
    borderRadius: 13,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  initialText: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 13,
    color: colors.tealText,
  },
  bar: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: spacing.sm,
  },
  input: {
    flex: 1,
    minHeight: 40,
    maxHeight: 120,
    paddingHorizontal: spacing.md,
    paddingTop: 10,
    paddingBottom: 10,
    borderRadius: 20,
    backgroundColor: colors.surface.subtle,
    color: colors.text.primary,
    fontSize: 16,
  },
  send: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.tealText,
    alignItems: 'center',
    justifyContent: 'center',
  },
  sendDisabled: {
    opacity: 0.4,
  },
});
