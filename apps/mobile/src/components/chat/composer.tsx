import { Ionicons } from '@expo/vector-icons';
import { useEffect, useRef } from 'react';
import { Pressable, TextInput as NativeTextInput, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import type { DiscussionMessage } from '@/api/discussion';
import { Text } from '@/components/ui';
import { authorName } from '@/lib/chat';
import { colors, fontFamilies, radii, spacing } from '@/theme';

/** What the composer is doing besides writing a fresh message. */
export type ComposerMode =
  | { kind: 'new' }
  | { kind: 'reply'; to: DiscussionMessage }
  | { kind: 'edit'; message: DiscussionMessage };

/**
 * The typing bar, fixed at the bottom of the chat (D-086). Replying shows the
 * quoted message above it; editing shows what is being changed. Either can
 * be cancelled with the cross.
 */
export function Composer({
  value,
  onChange,
  mode,
  onCancelMode,
  onSend,
  sending,
}: {
  value: string;
  onChange: (text: string) => void;
  mode: ComposerMode;
  onCancelMode: () => void;
  onSend: () => void;
  sending: boolean;
}) {
  const insets = useSafeAreaInsets();
  const input = useRef<NativeTextInput>(null);
  const canSend = value.trim().length > 0 && !sending;

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

      <View style={styles.bar}>
        <NativeTextInput
          ref={input}
          value={value}
          onChangeText={onChange}
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
