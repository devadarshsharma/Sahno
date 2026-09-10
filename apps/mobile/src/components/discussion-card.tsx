import { useState } from 'react';
import { Alert, Pressable, StyleSheet, View } from 'react-native';

import {
  editDiscussionMessage,
  postDiscussionMessage,
  removeDiscussionMessage,
  type DiscussionMessage,
} from '@/api/discussion';
import { Button, Card, Text, TextInput } from '@/components/ui';
import { useDiscussion, useDiscussionMutation } from '@/hooks/use-discussion';
import { colors, radii, spacing } from '@/theme';

/**
 * Conversation about one event, kept with the event (D-047 §6, D-024).
 *
 * Access follows the engagement, so this card simply appears for anyone who
 * can open the booking. What differs by person is what they may do to a given
 * message: rewrite their own, take their own back, and — for organisers —
 * remove anybody's.
 */
export function DiscussionCard({
  engagementId,
  isOrganiser,
}: {
  engagementId: string;
  isOrganiser: boolean;
}) {
  const threadQuery = useDiscussion(engagementId);
  const [draft, setDraft] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const post = useDiscussionMutation<string>(
    engagementId,
    (accessToken, organisationId, body) =>
      postDiscussionMessage(accessToken, organisationId, engagementId, body),
  );

  const edit = useDiscussionMutation<{ messageId: string; body: string }>(
    engagementId,
    (accessToken, organisationId, args) =>
      editDiscussionMessage(
        accessToken,
        organisationId,
        engagementId,
        args.messageId,
        args.body,
      ),
  );

  const remove = useDiscussionMutation<string>(
    engagementId,
    (accessToken, organisationId, messageId) =>
      removeDiscussionMessage(
        accessToken,
        organisationId,
        engagementId,
        messageId,
      ),
  );

  if (!threadQuery.isSuccess) {
    return null;
  }

  const thread = threadQuery.data;

  function confirmRemoval(message: DiscussionMessage) {
    // Removing somebody else's message is moderation and shows as such to the
    // whole group, so it asks first. Taking your own back does not need a
    // ceremony.
    if (message.isYours) {
      remove.mutate(message.id, {
        onError: () => setError('Could not remove that.'),
      });
      return;
    }

    Alert.alert(
      'Remove this message?',
      'Everyone on the event will see that an organiser removed it.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Remove',
          style: 'destructive',
          onPress: () =>
            remove.mutate(message.id, {
              onError: () => setError('Could not remove that.'),
            }),
        },
      ],
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Discussion</Text>
      <Text color="secondary" variant="bodySmall">
        {thread.length === 0
          ? 'Nothing said yet. Anything here stays with this event.'
          : 'Everyone on this event can read this.'}
      </Text>

      {thread.length > 0 ? (
        <View style={styles.thread}>
          {thread.map((message) =>
            editingId === message.id ? (
              <EditMessage
                key={message.id}
                message={message}
                busy={edit.isPending}
                onCancel={() => setEditingId(null)}
                onSave={(body) =>
                  edit.mutate(
                    { messageId: message.id, body },
                    {
                      onSuccess: () => setEditingId(null),
                      onError: () => setError('Could not save that.'),
                    },
                  )
                }
              />
            ) : (
              <MessageRow
                key={message.id}
                message={message}
                canRemove={message.isYours || isOrganiser}
                busy={remove.isPending}
                onEdit={() => setEditingId(message.id)}
                onRemove={() => confirmRemoval(message)}
              />
            ),
          )}
        </View>
      ) : null}

      <View style={styles.composer}>
        <TextInput
          label="Say something"
          placeholder="Call time has moved to six."
          value={draft}
          onChangeText={setDraft}
          multiline
        />
        <Button
          label="Send"
          loading={post.isPending}
          onPress={() => {
            const body = draft.trim();
            if (body.length === 0) {
              setError('A message needs something in it.');
              return;
            }

            post.mutate(body, {
              onSuccess: () => {
                setDraft('');
                setError(null);
              },
              onError: () => setError('Could not send that.'),
            });
          }}
        />
      </View>

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function MessageRow({
  message,
  canRemove,
  busy,
  onEdit,
  onRemove,
}: {
  message: DiscussionMessage;
  canRemove: boolean;
  busy: boolean;
  onEdit: () => void;
  onRemove: () => void;
}) {
  // A removed message keeps its place. The words are gone — the API does not
  // send them — but a thread with holes in it leaves the replies around them
  // answering nothing.
  if (message.isDeleted) {
    return (
      <View style={[styles.bubble, styles.tombstone]}>
        <Text variant="caption" color="muted">
          {message.wasModerated
            ? 'Removed by an organiser'
            : `${message.isYours ? 'You' : (message.authorDisplayName ?? 'Someone')} removed this`}
        </Text>
      </View>
    );
  }

  return (
    <View
      style={[
        styles.bubble,
        message.isYours ? styles.bubbleYours : styles.bubbleTheirs,
      ]}
    >
      <View style={styles.bubbleHead}>
        <Text variant="caption" color={message.isYours ? 'accent' : 'secondary'}>
          {message.isYours ? 'You' : (message.authorDisplayName ?? 'Someone')}
        </Text>
        <Text variant="caption" color="muted">
          {formatPostedAt(message.postedAtUtc)}
          {message.isEdited ? ' · edited' : ''}
        </Text>
      </View>

      <Text variant="bodySmall">{message.body}</Text>

      {message.isYours || canRemove ? (
        <View style={styles.bubbleActions}>
          {message.isYours ? (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Edit your message"
              onPress={onEdit}
              disabled={busy}
            >
              <Text variant="caption" color="accent">
                Edit
              </Text>
            </Pressable>
          ) : null}
          {canRemove ? (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Remove this message"
              onPress={onRemove}
              disabled={busy}
            >
              <Text variant="caption" color="muted">
                Remove
              </Text>
            </Pressable>
          ) : null}
        </View>
      ) : null}
    </View>
  );
}

function EditMessage({
  message,
  busy,
  onCancel,
  onSave,
}: {
  message: DiscussionMessage;
  busy: boolean;
  onCancel: () => void;
  onSave: (body: string) => void;
}) {
  const [body, setBody] = useState(message.body ?? '');

  return (
    <View style={[styles.bubble, styles.bubbleYours, styles.editing]}>
      <TextInput
        label="Your message"
        value={body}
        onChangeText={setBody}
        multiline
      />
      <Button
        label="Save"
        loading={busy}
        onPress={() => {
          const trimmed = body.trim();
          if (trimmed.length > 0) {
            onSave(trimmed);
          }
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onCancel} />
    </View>
  );
}

/**
 * Times inside a thread only need to answer "how long ago". The date is on the
 * booking above, so repeating it on every message adds nothing.
 */
function formatPostedAt(postedAtUtc: string): string {
  const posted = new Date(postedAtUtc);
  const minutes = Math.floor((Date.now() - posted.getTime()) / 60000);

  if (minutes < 1) {
    return 'just now';
  }
  if (minutes < 60) {
    return `${minutes}m ago`;
  }
  if (minutes < 24 * 60) {
    return `${Math.floor(minutes / 60)}h ago`;
  }

  return posted.toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
  });
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  thread: {
    gap: spacing.sm,
  },
  bubble: {
    borderRadius: radii.lg,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    gap: 4,
  },
  bubbleYours: {
    backgroundColor: colors.tealSoft,
  },
  bubbleTheirs: {
    backgroundColor: colors.surface.subtle,
  },
  tombstone: {
    backgroundColor: 'transparent',
    borderWidth: 1,
    borderColor: colors.border.default,
    borderStyle: 'dashed',
  },
  editing: {
    gap: spacing.sm,
  },
  bubbleHead: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: spacing.sm,
  },
  bubbleActions: {
    flexDirection: 'row',
    gap: spacing.lg,
    marginTop: 2,
    minHeight: 28,
    alignItems: 'center',
  },
  composer: {
    gap: spacing.sm,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.border.default,
  },
});
