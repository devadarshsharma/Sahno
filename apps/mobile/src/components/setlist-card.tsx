import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import {
  addSetListEntry,
  removeSetListEntry,
  reorderSetList,
  updateSetListEntry,
  type SetListEntry,
} from '@/api/repertoire';
import { PiecePicker } from '@/components/piece-picker';
import { Button, Card, Text, TextInput } from '@/components/ui';
import { formatDuration, useRepertoireMutation, useSetList } from '@/hooks/use-repertoire';
import { colors, radii, spacing } from '@/theme';

/**
 * The booking's set list (D-079): pieces from the repertoire in running
 * order. Everyone on the lineup reads it and taps through to the lyrics;
 * organisers arrange it. Order is changed with up/down rather than drag,
 * which is exact and works one-handed in a green room.
 */
export function SetListCard({
  engagementId,
  isOrganiser,
}: {
  engagementId: string;
  isOrganiser: boolean;
}) {
  const router = useRouter();
  const setListQuery = useSetList(engagementId);
  const [picking, setPicking] = useState(false);
  const [editingNote, setEditingNote] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const add = useRepertoireMutation<{ pieceId: string }>((accessToken, organisationId, args) =>
    addSetListEntry(accessToken, organisationId, engagementId, { pieceId: args.pieceId, note: null }),
  );
  const reorder = useRepertoireMutation<string[]>((accessToken, organisationId, entryIds) =>
    reorderSetList(accessToken, organisationId, engagementId, entryIds),
  );
  const remove = useRepertoireMutation<string>((accessToken, organisationId, entryId) =>
    removeSetListEntry(accessToken, organisationId, engagementId, entryId),
  );
  const note = useRepertoireMutation<{ entryId: string; note: string | null }>(
    (accessToken, organisationId, args) =>
      updateSetListEntry(accessToken, organisationId, engagementId, args.entryId, args.note),
  );

  if (!setListQuery.isSuccess) {
    return null;
  }

  const entries = setListQuery.data;
  const busy = add.isPending || reorder.isPending || remove.isPending || note.isPending;

  if (entries.length === 0 && !isOrganiser) {
    return null;
  }

  const totalMinutes = entries.reduce((sum, entry) => sum + (entry.durationMinutes ?? 0), 0);
  const fail = () => setError('Could not save that.');

  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= entries.length) {
      return;
    }
    const ids = entries.map((entry) => entry.id);
    [ids[index], ids[target]] = [ids[target]!, ids[index]!];
    setError(null);
    reorder.mutate(ids, { onError: fail });
  };

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Set list</Text>
      <Text color="secondary" variant="bodySmall">
        {entries.length === 0
          ? 'Nothing chosen yet. Pick from the repertoire.'
          : `${entries.length} piece${entries.length === 1 ? '' : 's'}${totalMinutes > 0 ? ` · about ${formatDuration(totalMinutes)}` : ''}. Tap one for the lyrics.`}
      </Text>

      {entries.length > 0 ? (
        <View style={styles.list}>
          {entries.map((entry, index) => (
            <View key={entry.id} style={[styles.entry, index === entries.length - 1 ? styles.entryLast : null]}>
              <Pressable
                accessibilityRole="button"
                accessibilityLabel={`${index + 1}. ${entry.title}. ${entry.hasLyrics ? 'Open lyrics.' : 'No lyrics yet.'}`}
                onPress={() => router.push(`/repertoire/${entry.pieceId}/lyrics`)}
                style={({ pressed }) => [styles.entryMain, pressed ? styles.pressed : null]}
              >
                <Text style={styles.position}>{index + 1}</Text>
                <View style={styles.entryText}>
                  <Text style={styles.entryTitle} numberOfLines={1}>
                    {entry.title}
                  </Text>
                  <Text variant="caption" color="secondary" numberOfLines={2}>
                    {[
                      entry.attribution,
                      formatDuration(entry.durationMinutes),
                      entry.hasLyrics ? null : 'no lyrics yet',
                    ]
                      .filter(Boolean)
                      .join(' · ')}
                    {entry.note ? `${entry.attribution || entry.durationMinutes ? ' · ' : ''}${entry.note}` : ''}
                  </Text>
                </View>
                {entry.hasLyrics ? (
                  <Ionicons name="document-text-outline" size={18} color={colors.tealText} />
                ) : null}
              </Pressable>

              {isOrganiser ? (
                <View style={styles.tools}>
                  <Tool icon="chevron-up" label="Move up" disabled={busy || index === 0} onPress={() => move(index, -1)} />
                  <Tool
                    icon="chevron-down"
                    label="Move down"
                    disabled={busy || index === entries.length - 1}
                    onPress={() => move(index, 1)}
                  />
                  <Tool
                    icon="create-outline"
                    label="Note for this booking"
                    disabled={busy}
                    onPress={() => setEditingNote(editingNote === entry.id ? null : entry.id)}
                  />
                  <Tool
                    icon="close"
                    label="Remove from set list"
                    disabled={busy}
                    onPress={() => remove.mutate(entry.id, { onError: fail })}
                  />
                </View>
              ) : null}

              {editingNote === entry.id ? (
                <NoteForm
                  entry={entry}
                  saving={note.isPending}
                  onSave={(text) =>
                    note.mutate(
                      { entryId: entry.id, note: text },
                      { onSuccess: () => setEditingNote(null), onError: fail },
                    )
                  }
                  onCancel={() => setEditingNote(null)}
                />
              ) : null}
            </View>
          ))}
        </View>
      ) : null}

      {isOrganiser ? (
        <Button label="Add a piece" variant="secondary" onPress={() => setPicking(true)} loading={add.isPending} />
      ) : null}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}

      <PiecePicker
        visible={picking}
        onSelect={(piece) => {
          setPicking(false);
          setError(null);
          add.mutate({ pieceId: piece.id }, { onError: fail });
        }}
        onClose={() => setPicking(false)}
      />
    </Card>
  );
}

function Tool({
  icon,
  label,
  disabled,
  onPress,
}: {
  icon: React.ComponentProps<typeof Ionicons>['name'];
  label: string;
  disabled: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      disabled={disabled}
      onPress={onPress}
      hitSlop={6}
      style={({ pressed }) => [styles.tool, disabled ? styles.toolDisabled : null, pressed ? styles.pressed : null]}
    >
      <Ionicons name={icon} size={18} color={disabled ? colors.text.muted : colors.text.primary} />
    </Pressable>
  );
}

function NoteForm({
  entry,
  saving,
  onSave,
  onCancel,
}: {
  entry: SetListEntry;
  saving: boolean;
  onSave: (note: string | null) => void;
  onCancel: () => void;
}) {
  const [text, setText] = useState(entry.note ?? '');

  return (
    <View style={styles.noteForm}>
      <TextInput
        label="Note for this booking"
        placeholder="e.g. Open with this — bride's request"
        value={text}
        onChangeText={setText}
        autoFocus
      />
      <Button label="Save" loading={saving} onPress={() => onSave(text.trim() || null)} />
      <Button label="Cancel" variant="ghost" onPress={onCancel} />
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  list: {
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.default,
    overflow: 'hidden',
  },
  entry: {
    borderBottomWidth: 1,
    borderBottomColor: colors.border.default,
  },
  entryLast: {
    borderBottomWidth: 0,
  },
  entryMain: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
  },
  position: {
    width: 22,
    textAlign: 'center',
    fontWeight: '700',
    color: colors.tealText,
  },
  entryText: {
    flex: 1,
    gap: 2,
  },
  entryTitle: {
    fontWeight: '600',
  },
  tools: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: spacing.xs,
    paddingHorizontal: spacing.sm,
    paddingBottom: spacing.sm,
    marginTop: -spacing.xs,
  },
  tool: {
    width: 36,
    height: 36,
    borderRadius: radii.full,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface.subtle,
  },
  toolDisabled: {
    opacity: 0.4,
  },
  pressed: {
    opacity: 0.6,
  },
  noteForm: {
    gap: spacing.sm,
    padding: spacing.md,
    paddingTop: 0,
  },
});
