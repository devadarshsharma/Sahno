import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { filterPieces, PieceRow } from '@/components/piece-picker';
import { Card, Screen, Text, TextInput } from '@/components/ui';
import { formatDuration, usePieces } from '@/hooks/use-repertoire';
import { colors, radii, spacing } from '@/theme';

/**
 * The repertoire (D-079): everything the group performs, with lyrics a tap
 * away. Every member's — the words belong to the people singing them.
 */
export default function Repertoire() {
  const router = useRouter();
  const piecesQuery = usePieces();
  const [search, setSearch] = useState('');

  const rows = useMemo(() => filterPieces(piecesQuery.data ?? [], search), [piecesQuery.data, search]);
  const total = piecesQuery.data?.length ?? 0;
  const minutes = (piecesQuery.data ?? []).reduce((sum, piece) => sum + (piece.durationMinutes ?? 0), 0);

  return (
    <Screen
      scroll
      hero={{
        title: 'Repertoire',
        subtitle:
          total === 0
            ? 'Everything you perform, with the lyrics to hand. Add the first piece.'
            : `${total} piece${total === 1 ? '' : 's'}${minutes > 0 ? ` · about ${formatDuration(minutes)} all told` : ''}`,
        right: (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Add a piece"
            onPress={() => router.push('/repertoire/new')}
            hitSlop={8}
            style={({ pressed }) => [styles.addButton, pressed ? styles.pressed : null]}
          >
            <Ionicons name="add" size={22} color={colors.text.inverse} />
          </Pressable>
        ),
      }}
    >
      {piecesQuery.isPending ? (
        <ActivityIndicator color={colors.tealText} style={styles.spinner} />
      ) : (
        <>
          {total > 4 ? (
            <TextInput
              label="Search"
              placeholder="Title or who it is by"
              value={search}
              onChangeText={setSearch}
              autoCorrect={false}
            />
          ) : null}

          <Card style={styles.list}>
            {rows.length === 0 ? (
              <Text color="muted" variant="bodySmall" style={styles.empty}>
                {search.trim() ? 'Nothing by that name.' : 'No pieces yet.'}
              </Text>
            ) : (
              rows.map((piece) => (
                <PieceRow key={piece.id} piece={piece} onPress={() => router.push(`/repertoire/${piece.id}`)} />
              ))
            )}
          </Card>
        </>
      )}
      <View style={styles.footer} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  addButton: {
    width: 36,
    height: 36,
    borderRadius: radii.full,
    backgroundColor: colors.navySoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  pressed: {
    opacity: 0.6,
  },
  spinner: {
    marginTop: spacing.xl,
  },
  list: {
    paddingVertical: 0,
    paddingHorizontal: spacing.lg,
    marginTop: spacing.md,
  },
  empty: {
    paddingVertical: spacing.lg,
    textAlign: 'center',
  },
  footer: {
    height: spacing.xl,
  },
});
