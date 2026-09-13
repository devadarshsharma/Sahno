import { Ionicons } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Alert, Linking, Pressable, StyleSheet, View } from 'react-native';

import type { EngagementStatus } from '@/api/engagements';
import {
  addPieceLink,
  deletePiece,
  removePieceLink,
  updatePieceNotes,
  type PieceBooking,
} from '@/api/repertoire';
import { StatusChip } from '@/components/engagement-card';
import { PieceForm } from '@/components/piece-form';
import { Button, Card, formatIsoDate, Screen, Text, TextInput } from '@/components/ui';
import { useIsOrganiser } from '@/hooks/use-customers';
import { formatDuration, usePieceDetail, useRepertoireMutation } from '@/hooks/use-repertoire';
import { colors, radii, spacing } from '@/theme';

/**
 * One piece: what it is, the lyrics (opened full-screen), links, the
 * organiser's notes, and every booking it has been on. Any member edits the
 * piece; deleting it is an organiser's call.
 */
export default function PieceDetail() {
  const { pieceId } = useLocalSearchParams<{ pieceId: string }>();
  const router = useRouter();
  const isOrganiser = useIsOrganiser();
  const detailQuery = usePieceDetail(pieceId ?? null);
  const [editing, setEditing] = useState(false);
  const [editingNotes, setEditingNotes] = useState(false);
  const [addingLink, setAddingLink] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const notes = useRepertoireMutation<string | null>((accessToken, organisationId, text) =>
    updatePieceNotes(accessToken, organisationId, pieceId!, text),
  );
  const addLink = useRepertoireMutation<{ title: string | null; url: string }>(
    (accessToken, organisationId, input) => addPieceLink(accessToken, organisationId, pieceId!, input),
  );
  const removeLink = useRepertoireMutation<string>((accessToken, organisationId, linkId) =>
    removePieceLink(accessToken, organisationId, pieceId!, linkId),
  );
  const remove = useRepertoireMutation<void>((accessToken, organisationId) =>
    deletePiece(accessToken, organisationId, pieceId!),
  );

  if (detailQuery.isPending) {
    return (
      <Screen hero={{ title: 'Piece' }}>
        <ActivityIndicator color={colors.tealText} style={styles.spinner} />
      </Screen>
    );
  }

  if (!detailQuery.isSuccess) {
    return (
      <Screen hero={{ title: 'Piece' }}>
        <Card style={styles.card}>
          <Text variant="subheading">Piece not found</Text>
          <Text color="secondary" variant="bodySmall">
            It may have been removed from the repertoire.
          </Text>
          <Button label="Back to repertoire" variant="secondary" onPress={() => router.replace('/repertoire')} />
        </Card>
      </Screen>
    );
  }

  const { piece, bookings } = detailQuery.data;
  const meta = [piece.language, piece.key, formatDuration(piece.durationMinutes)].filter(Boolean).join(' · ');
  const fail = () => setError('Could not save that.');

  const confirmDelete = () =>
    Alert.alert(
      'Remove from repertoire?',
      `“${piece.title}” comes off every set list it is on. This cannot be undone.`,
      [
        { text: 'Keep it', style: 'cancel' },
        {
          text: 'Remove',
          style: 'destructive',
          onPress: () => remove.mutate(undefined, { onSuccess: () => router.replace('/repertoire'), onError: fail }),
        },
      ],
    );

  return (
    <Screen
      scroll
      hero={{
        eyebrow: piece.attribution ? piece.attribution.toUpperCase() : 'REPERTOIRE',
        title: piece.title,
        subtitle: meta || undefined,
      }}
    >
      {editing ? (
        <Card style={styles.card}>
          <PieceForm piece={piece} onSaved={() => setEditing(false)} onCancel={() => setEditing(false)} />
        </Card>
      ) : (
        <>
          <Card style={styles.card}>
            <Text variant="subheading">Lyrics</Text>
            {piece.lyrics ? (
              <>
                <Text variant="bodySmall" color="secondary" numberOfLines={4} style={styles.preview}>
                  {piece.lyrics}
                </Text>
                <Button label="Open lyrics" onPress={() => router.push(`/repertoire/${piece.id}/lyrics`)} />
              </>
            ) : (
              <Text color="muted" variant="bodySmall">
                Not typed up yet. Edit the piece to add them.
              </Text>
            )}
            <Button label="Edit piece" variant="secondary" onPress={() => setEditing(true)} />
          </Card>

          <Card style={styles.card}>
            <Text variant="subheading">Links</Text>
            {piece.links.length === 0 && !addingLink ? (
              <Text color="muted" variant="bodySmall">
                A recording, a reference, a folder — anything that helps.
              </Text>
            ) : null}
            {piece.links.map((link) => (
              <View key={link.id} style={styles.link}>
                <Pressable
                  accessibilityRole="link"
                  onPress={() => Linking.openURL(link.url)}
                  style={({ pressed }) => [styles.linkMain, pressed ? styles.pressed : null]}
                >
                  <Ionicons name="link-outline" size={16} color={colors.tealText} />
                  <Text color="accent" style={styles.linkTitle} numberOfLines={1}>
                    {link.title}
                  </Text>
                </Pressable>
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={`Remove link ${link.title}`}
                  hitSlop={8}
                  onPress={() => removeLink.mutate(link.id, { onError: fail })}
                >
                  <Ionicons name="close" size={18} color={colors.text.muted} />
                </Pressable>
              </View>
            ))}
            {addingLink ? (
              <LinkForm
                saving={addLink.isPending}
                onSave={(input) => addLink.mutate(input, { onSuccess: () => setAddingLink(false), onError: fail })}
                onCancel={() => setAddingLink(false)}
              />
            ) : (
              <Button label="Add a link" variant="secondary" onPress={() => setAddingLink(true)} />
            )}
          </Card>

          {isOrganiser ? (
            <Card style={styles.card}>
              <Text variant="subheading">Organiser notes</Text>
              <Text color="secondary" variant="bodySmall">
                Yours and the other organisers&apos;. Members do not see this.
              </Text>
              {editingNotes ? (
                <NotesForm
                  initial={piece.notes ?? ''}
                  saving={notes.isPending}
                  onSave={(text) => notes.mutate(text, { onSuccess: () => setEditingNotes(false), onError: fail })}
                  onCancel={() => setEditingNotes(false)}
                />
              ) : (
                <>
                  {piece.notes ? (
                    <Text variant="bodySmall">{piece.notes}</Text>
                  ) : (
                    <Text color="muted" variant="bodySmall">
                      e.g. drop the third verse on a short set.
                    </Text>
                  )}
                  <Button label={piece.notes ? 'Edit notes' : 'Add notes'} variant="secondary" onPress={() => setEditingNotes(true)} />
                </>
              )}
            </Card>
          ) : null}

          <Card style={styles.card}>
            <Text variant="subheading">Performed at</Text>
            {bookings.length === 0 ? (
              <Text color="muted" variant="bodySmall">
                Not on a set list yet.
              </Text>
            ) : (
              <View>
                {bookings.map((booking) => (
                  <BookingRow key={booking.engagementId} booking={booking} onPress={() => router.push(`/engagement/${booking.engagementId}`)} />
                ))}
              </View>
            )}
          </Card>

          {isOrganiser ? (
            <Button label="Remove from repertoire" variant="ghost" onPress={confirmDelete} loading={remove.isPending} />
          ) : null}
        </>
      )}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
      <View style={styles.footer} />
    </Screen>
  );
}

function BookingRow({ booking, onPress }: { booking: PieceBooking; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => [styles.booking, pressed ? styles.pressed : null]}
    >
      <View style={styles.bookingText}>
        <Text style={styles.bookingTitle} numberOfLines={1}>
          {booking.title}
        </Text>
        <Text variant="caption" color="secondary">
          {booking.startDate ? formatIsoDate(booking.startDate) : 'Date TBC'}
        </Text>
      </View>
      <StatusChip status={booking.status as EngagementStatus} />
    </Pressable>
  );
}

function LinkForm({
  saving,
  onSave,
  onCancel,
}: {
  saving: boolean;
  onSave: (input: { title: string | null; url: string }) => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState('');
  const [url, setUrl] = useState('');
  const [error, setError] = useState<string | null>(null);

  return (
    <View style={styles.form}>
      <TextInput
        label="Web address"
        placeholder="https://"
        value={url}
        onChangeText={setUrl}
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="url"
        error={error ?? undefined}
        autoFocus
      />
      <TextInput label="Label (optional)" placeholder="e.g. Nusrat's version" value={title} onChangeText={setTitle} />
      <Button
        label="Add link"
        loading={saving}
        onPress={() => {
          if (!/^https?:\/\//i.test(url.trim())) {
            setError('Starts with http:// or https://');
            return;
          }
          setError(null);
          onSave({ title: title.trim() || null, url: url.trim() });
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onCancel} />
    </View>
  );
}

function NotesForm({
  initial,
  saving,
  onSave,
  onCancel,
}: {
  initial: string;
  saving: boolean;
  onSave: (notes: string | null) => void;
  onCancel: () => void;
}) {
  const [text, setText] = useState(initial);
  return (
    <View style={styles.form}>
      <TextInput label="Notes" value={text} onChangeText={setText} multiline autoFocus />
      <Button label="Save" loading={saving} onPress={() => onSave(text.trim() || null)} />
      <Button label="Cancel" variant="ghost" onPress={onCancel} />
    </View>
  );
}

const styles = StyleSheet.create({
  spinner: {
    marginTop: spacing.xl,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  preview: {
    padding: spacing.md,
    borderRadius: radii.md,
    backgroundColor: colors.surface.subtle,
  },
  link: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  linkMain: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  linkTitle: {
    flex: 1,
  },
  form: {
    gap: spacing.md,
  },
  booking: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border.default,
  },
  bookingText: {
    flex: 1,
    gap: 2,
  },
  bookingTitle: {
    fontWeight: '600',
  },
  pressed: {
    opacity: 0.6,
  },
  footer: {
    height: spacing.xl,
  },
});
