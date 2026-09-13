import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import { createPiece, updatePiece, type Piece } from '@/api/repertoire';
import { Button, Text, TextInput } from '@/components/ui';
import { useRepertoireMutation } from '@/hooks/use-repertoire';
import { spacing } from '@/theme';

/**
 * Add or edit a piece. Only a title is required — the lyrics are usually
 * the reason somebody opens this, but a placeholder with a title is better
 * than nothing on the set list while the words are still being typed up.
 */
export function PieceForm({
  piece,
  submitLabel = piece ? 'Save' : 'Add to repertoire',
  onSaved,
  onCancel,
}: {
  /** Absent means create. */
  piece?: Piece;
  submitLabel?: string;
  onSaved: (piece: Piece) => void;
  onCancel: () => void;
}) {
  const [title, setTitle] = useState(piece?.title ?? '');
  const [attribution, setAttribution] = useState(piece?.attribution ?? '');
  const [language, setLanguage] = useState(piece?.language ?? '');
  const [key, setKey] = useState(piece?.key ?? '');
  const [duration, setDuration] = useState(
    piece?.durationMinutes === null || piece?.durationMinutes === undefined
      ? ''
      : String(piece.durationMinutes),
  );
  const [lyrics, setLyrics] = useState(piece?.lyrics ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = useRepertoireMutation<void, Piece>(async (accessToken, organisationId) => {
    const minutes = duration.trim() ? Number.parseInt(duration.trim(), 10) : null;
    const input = {
      title: title.trim(),
      attribution: attribution.trim() || null,
      language: language.trim() || null,
      key: key.trim() || null,
      durationMinutes: minutes !== null && Number.isFinite(minutes) ? minutes : null,
      lyrics: lyrics.trim() || null,
    };
    if (piece) {
      await updatePiece(accessToken, organisationId, piece.id, input);
      return { ...piece, ...input, hasLyrics: input.lyrics !== null };
    }
    return createPiece(accessToken, organisationId, input);
  });

  const submit = () => {
    if (!title.trim()) {
      setError('A piece needs a title.');
      return;
    }
    const minutes = duration.trim() ? Number.parseInt(duration.trim(), 10) : null;
    if (minutes !== null && (!Number.isFinite(minutes) || minutes <= 0 || minutes > 600)) {
      setError('Duration is a number of minutes.');
      return;
    }
    setError(null);
    save.mutate(undefined, {
      onSuccess: onSaved,
      onError: () => setError('Could not save that. Check your connection and try again.'),
    });
  };

  return (
    <View style={styles.form}>
      <TextInput
        label="Title"
        placeholder="e.g. Tumhe Dillagi"
        value={title}
        onChangeText={setTitle}
        error={error === 'A piece needs a title.' ? error : undefined}
        autoFocus={!piece}
      />
      <TextInput
        label="By (optional)"
        placeholder="Composer, poet, or whose version"
        value={attribution}
        onChangeText={setAttribution}
      />
      <View style={styles.row}>
        <View style={styles.half}>
          <TextInput label="Language" placeholder="Urdu" value={language} onChangeText={setLanguage} />
        </View>
        <View style={styles.half}>
          <TextInput label="Key or raag" placeholder="Bhairavi" value={key} onChangeText={setKey} />
        </View>
      </View>
      <TextInput
        label="Duration (minutes)"
        placeholder="e.g. 18"
        value={duration}
        onChangeText={setDuration}
        keyboardType="number-pad"
        error={error === 'Duration is a number of minutes.' ? error : undefined}
      />
      <TextInput
        label="Lyrics"
        placeholder="Written however you sing it. One verse per paragraph works well."
        value={lyrics}
        onChangeText={setLyrics}
        multiline
        style={styles.lyrics}
        textAlignVertical="top"
      />

      {error && !error.startsWith('A piece') && !error.startsWith('Duration') ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}

      <Button label={submitLabel} loading={save.isPending} onPress={submit} />
      <Button label="Cancel" variant="ghost" onPress={onCancel} disabled={save.isPending} />
    </View>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.md,
  },
  row: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  half: {
    flex: 1,
  },
  lyrics: {
    minHeight: 200,
  },
});
