import { Ionicons } from '@expo/vector-icons';
import * as Clipboard from 'expo-clipboard';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Alert, FlatList, Pressable, StyleSheet, View } from 'react-native';
import { KeyboardAvoidingView } from 'react-native-keyboard-controller';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import {
  editDiscussionMessage,
  hideMessage,
  postDiscussionMessage,
  reactToMessage,
  removeDiscussionMessage,
  type DiscussionMessage,
} from '@/api/discussion';
import { Composer, type ComposerMode } from '@/components/chat/composer';
import { MessageActions, type MessageAction } from '@/components/chat/message-actions';
import { MessageBubble } from '@/components/chat/message-bubble';
import { Button, Text } from '@/components/ui';
import { useDiscussion, useDiscussionMutation, useMarkDiscussionRead } from '@/hooks/use-discussion';
import { useEngagements } from '@/hooks/use-engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import { chatItems, type ChatItem } from '@/lib/chat';
import { colors, fontFamilies, spacing } from '@/theme';

/**
 * An event's chat, laid out like a phone's messaging app (D-086): the whole
 * screen, newest at the bottom, the typing bar fixed above the keyboard.
 * Long-press a message to react, reply, copy, edit or delete; tap a quote to
 * jump to what it answers. Still the event's own conversation (D-024) — the
 * header says which event.
 */
export default function Chat() {
  const { engagementId } = useLocalSearchParams<{ engagementId: string }>();
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const { active } = useActiveOrg();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';
  const engagement = useEngagements().data?.find((row) => row.id === engagementId);

  const thread = useDiscussion(engagementId);
  useMarkDiscussionRead(engagementId);

  const [draft, setDraft] = useState('');
  const [mode, setMode] = useState<ComposerMode>({ kind: 'new' });
  const [selected, setSelected] = useState<DiscussionMessage | null>(null);
  const [highlightedId, setHighlightedId] = useState<string | null>(null);
  const list = useRef<FlatList<ChatItem>>(null);

  const items = useMemo(() => chatItems(thread.data ?? []), [thread.data]);

  const post = useDiscussionMutation<{ body: string; replyTo: string | null }>(
    engagementId,
    (token, organisationId, { body, replyTo }) =>
      postDiscussionMessage(token, organisationId, engagementId, body, replyTo),
  );
  const edit = useDiscussionMutation<{ id: string; body: string }>(engagementId, (token, organisationId, { id, body }) =>
    editDiscussionMessage(token, organisationId, engagementId, id, body),
  );
  const remove = useDiscussionMutation<string>(engagementId, (token, organisationId, id) =>
    removeDiscussionMessage(token, organisationId, engagementId, id),
  );
  const hide = useDiscussionMutation<string>(engagementId, (token, organisationId, id) =>
    hideMessage(token, organisationId, engagementId, id),
  );
  const react = useDiscussionMutation<{ id: string; emoji: string }>(engagementId, (token, organisationId, { id, emoji }) =>
    reactToMessage(token, organisationId, engagementId, id, emoji),
  );

  const failed = (what: string) => () => Alert.alert(`Could not ${what}`, 'Check your connection and try again.');

  const send = () => {
    const body = draft.trim();
    if (!body) {
      return;
    }

    if (mode.kind === 'edit') {
      edit.mutate(
        { id: mode.message.id, body },
        { onSuccess: () => { setDraft(''); setMode({ kind: 'new' }); }, onError: failed('save your edit') },
      );
      return;
    }

    post.mutate(
      { body, replyTo: mode.kind === 'reply' ? mode.to.id : null },
      {
        onSuccess: () => {
          setDraft('');
          setMode({ kind: 'new' });
          list.current?.scrollToOffset({ offset: 0, animated: true });
        },
        onError: failed('send your message'),
      },
    );
  };

  const onAction = (action: MessageAction) => {
    const message = selected;
    setSelected(null);
    if (!message) {
      return;
    }

    switch (action) {
      case 'reply':
        setMode({ kind: 'reply', to: message });
        break;
      case 'copy':
        if (message.body) {
          void Clipboard.setStringAsync(message.body);
        }
        break;
      case 'edit':
        setMode({ kind: 'edit', message });
        setDraft(message.body ?? '');
        break;
      case 'deleteForMe':
        hide.mutate(message.id, { onError: failed('delete the message') });
        break;
      case 'deleteForEveryone':
        Alert.alert(
          message.isYours ? 'Delete for everyone?' : 'Remove for everyone?',
          message.isYours
            ? 'Everyone in this chat will see that you deleted a message.'
            : 'Everyone will see that an organiser removed this message.',
          [
            { text: 'Cancel', style: 'cancel' },
            {
              text: message.isYours ? 'Delete' : 'Remove',
              style: 'destructive',
              onPress: () => remove.mutate(message.id, { onError: failed('delete the message') }),
            },
          ],
        );
        break;
    }
  };

  // Tapping a quote brings the original into view and flashes it.
  const jumpTo = useCallback(
    (messageId: string) => {
      const index = items.findIndex((item) => item.kind === 'message' && item.message.id === messageId);
      if (index < 0) {
        Alert.alert('Not in this chat', 'That message is no longer in your view of the chat.');
        return;
      }
      list.current?.scrollToIndex({ index, animated: true, viewPosition: 0.5 });
      setHighlightedId(messageId);
    },
    [items],
  );

  useEffect(() => {
    if (!highlightedId) {
      return;
    }
    const timer = setTimeout(() => setHighlightedId(null), 1500);
    return () => clearTimeout(timer);
  }, [highlightedId]);

  return (
    <View style={styles.screen}>
      <StatusBar style="dark" />
      <View style={[styles.header, { paddingTop: insets.top + spacing.sm }]}>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Back"
          onPress={() => router.back()}
          hitSlop={10}
          style={styles.back}
        >
          <Ionicons name="chevron-back" size={24} color={colors.text.primary} />
        </Pressable>
        <View style={styles.headerText}>
          <Text style={styles.headerTitle} numberOfLines={1}>
            {engagement?.title ?? 'Chat'}
          </Text>
          <Text variant="caption" color="muted" numberOfLines={1}>
            Event chat
          </Text>
        </View>
      </View>

      <KeyboardAvoidingView behavior="padding" style={styles.body}>
        {thread.isPending ? (
          <View style={styles.centered}>
            <ActivityIndicator color={colors.tealText} />
          </View>
        ) : thread.isError ? (
          <View style={styles.centered}>
            <Text color="error">Could not load this chat.</Text>
            <Button label="Try again" variant="secondary" onPress={() => thread.refetch()} />
          </View>
        ) : items.length === 0 ? (
          <View style={styles.centered}>
            <Ionicons name="chatbubbles-outline" size={40} color={colors.tealText} />
            <Text color="secondary" style={styles.centeredText}>
              No messages yet. Say hello to everyone on this event.
            </Text>
          </View>
        ) : (
          <FlatList
            ref={list}
            inverted
            data={items}
            keyExtractor={(item) => item.key}
            keyboardShouldPersistTaps="handled"
            contentContainerStyle={styles.listContent}
            onScrollToIndexFailed={(info) => {
              // Not yet measured: get close, then try again.
              list.current?.scrollToOffset({ offset: info.averageItemLength * info.index, animated: false });
              setTimeout(() => list.current?.scrollToIndex({ index: info.index, animated: true, viewPosition: 0.5 }), 100);
            }}
            renderItem={({ item }) =>
              item.kind === 'day' ? (
                <View style={styles.day}>
                  <Text style={styles.dayText}>{item.label}</Text>
                </View>
              ) : (
                <MessageBubble
                  message={item.message}
                  startsRun={item.startsRun}
                  highlighted={highlightedId === item.message.id}
                  onLongPress={() => setSelected(item.message)}
                  onQuotePress={jumpTo}
                  onReactionPress={(emoji) =>
                    react.mutate({ id: item.message.id, emoji }, { onError: failed('react') })
                  }
                />
              )
            }
          />
        )}

        <Composer
          value={draft}
          onChange={setDraft}
          mode={mode}
          onCancelMode={() => {
            if (mode.kind === 'edit') {
              setDraft('');
            }
            setMode({ kind: 'new' });
          }}
          onSend={send}
          sending={post.isPending || edit.isPending}
        />
      </KeyboardAvoidingView>

      <MessageActions
        message={selected}
        isOrganiser={isOrganiser}
        onClose={() => setSelected(null)}
        onReact={(emoji) => {
          const message = selected;
          setSelected(null);
          if (message) {
            react.mutate({ id: message.id, emoji }, { onError: failed('react') });
          }
        }}
        onAction={onAction}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.surface.canvas,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingHorizontal: spacing.md,
    paddingBottom: spacing.sm,
    backgroundColor: colors.surface.raised,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border.default,
  },
  back: {
    width: 36,
    height: 36,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerText: {
    flex: 1,
  },
  headerTitle: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 17,
    color: colors.text.primary,
  },
  body: {
    flex: 1,
  },
  listContent: {
    paddingVertical: spacing.md,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.xl,
  },
  centeredText: {
    textAlign: 'center',
  },
  day: {
    alignSelf: 'center',
    marginVertical: spacing.md,
    paddingHorizontal: spacing.md,
    paddingVertical: 4,
    borderRadius: 12,
    backgroundColor: colors.surface.subtle,
  },
  dayText: {
    fontSize: 12,
    fontFamily: fontFamilies.uiMedium,
    color: colors.text.secondary,
  },
});
