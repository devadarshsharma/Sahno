import { Ionicons } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import type { Piece } from '@/api/repertoire';
import { PieceForm } from '@/components/piece-form';
import { Text, TextInput } from '@/components/ui';
import { formatDuration, usePieces } from '@/hooks/use-repertoire';
import { colors, radii, spacing } from '@/theme';

/**
 * Pick a piece from the repertoire for a set list, or add one on the spot.
 * The repertoire comes first and "add new" is the fallback, so the same
 * piece is the same piece on every booking rather than typed in fresh.
 */
export function PiecePicker({
  visible,
  onSelect,
  onClose,
}: {
  visible: boolean;
  onSelect: (piece: Piece) => void;
  onClose: () => void;
}) {
  const insets = useSafeAreaInsets();
  const piecesQuery = usePieces();
  const [search, setSearch] = useState('');
  const [adding, setAdding] = useState(false);

  const rows = useMemo(() => filterPieces(piecesQuery.data ?? [], search), [piecesQuery.data, search]);

  const close = () => {
    setSearch('');
    setAdding(false);
    onClose();
  };

  const choose = (piece: Piece) => {
    setSearch('');
    setAdding(false);
    onSelect(piece);
  };

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={close}>
      <View style={styles.sheet}>
        <View style={[styles.header, { paddingTop: insets.top + spacing.md }]}>
          <Text style={styles.title} accessibilityRole="header">
            {adding ? 'New piece' : 'Add to set list'}
          </Text>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Close"
            onPress={close}
            hitSlop={12}
            style={styles.close}
          >
            <Ionicons name="close" size={22} color={colors.text.inverse} />
          </Pressable>
        </View>

        {adding ? (
          <FlatList
            data={[]}
            renderItem={null}
            keyboardShouldPersistTaps="handled"
            contentContainerStyle={[styles.body, { paddingBottom: insets.bottom + spacing.xl }]}
            ListHeaderComponent={
              <PieceForm onSaved={choose} onCancel={() => setAdding(false)} />
            }
          />
        ) : (
          <>
            <View style={styles.search}>
              <TextInput
                label="Search the repertoire"
                placeholder="Title or who it is by"
                value={search}
                onChangeText={setSearch}
                autoFocus
                autoCorrect={false}
              />
            </View>

            {piecesQuery.isPending ? (
              <ActivityIndicator color={colors.tealText} style={styles.spinner} />
            ) : (
              <FlatList
                data={rows}
                keyExtractor={(piece) => piece.id}
                keyboardShouldPersistTaps="handled"
                contentContainerStyle={[styles.list, { paddingBottom: insets.bottom + spacing.xl }]}
                ListHeaderComponent={
                  <Pressable
                    accessibilityRole="button"
                    onPress={() => setAdding(true)}
                    style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
                  >
                    <View style={[styles.avatar, styles.avatarAdd]}>
                      <Ionicons name="add" size={20} color={colors.tealText} />
                    </View>
                    <View style={styles.rowText}>
                      <Text style={styles.rowTitle}>
                        {search.trim() ? `Add “${search.trim()}”` : 'Add a new piece'}
                      </Text>
                      <Text variant="caption" color="secondary">
                        Something not in the repertoire yet
                      </Text>
                    </View>
                  </Pressable>
                }
                ListEmptyComponent={
                  <Text color="muted" variant="bodySmall" style={styles.empty}>
                    {search.trim()
                      ? 'Nothing by that name yet.'
                      : 'The repertoire is empty. Add the first piece above.'}
                  </Text>
                }
                renderItem={({ item }) => <PieceRow piece={item} onPress={() => choose(item)} />}
              />
            )}
          </>
        )}
      </View>
    </Modal>
  );
}

export function filterPieces(pieces: Piece[], search: string): Piece[] {
  const needle = search.trim().toLowerCase();
  if (!needle) {
    return pieces;
  }
  return pieces.filter(
    (piece) =>
      piece.title.toLowerCase().includes(needle) ||
      (piece.attribution ?? '').toLowerCase().includes(needle),
  );
}

/** One repertoire row: title, who it is by, length, how often it has been performed. */
export function PieceRow({ piece, onPress }: { piece: Piece; onPress: () => void }) {
  const meta = [
    piece.attribution,
    formatDuration(piece.durationMinutes),
    piece.useCount > 0 ? `${piece.useCount} booking${piece.useCount === 1 ? '' : 's'}` : null,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${piece.title}. ${meta}`}
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
    >
      <View style={styles.avatar}>
        <Ionicons
          name={piece.hasLyrics ? 'musical-notes' : 'musical-notes-outline'}
          size={18}
          color={colors.tealText}
        />
      </View>
      <View style={styles.rowText}>
        <Text style={styles.rowTitle} numberOfLines={1}>
          {piece.title}
        </Text>
        <Text variant="caption" color="secondary" numberOfLines={1}>
          {meta || (piece.hasLyrics ? 'Lyrics ready' : 'No lyrics yet')}
        </Text>
      </View>
      <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  sheet: {
    flex: 1,
    backgroundColor: colors.surface.canvas,
  },
  header: {
    backgroundColor: colors.navy,
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  title: {
    flex: 1,
    color: colors.text.inverse,
    fontSize: 22,
    fontWeight: '700',
  },
  close: {
    width: 36,
    height: 36,
    borderRadius: radii.full,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.navySoft,
  },
  body: {
    padding: spacing.lg,
  },
  search: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
  },
  spinner: {
    marginTop: spacing.xl,
  },
  list: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border.default,
  },
  rowPressed: {
    opacity: 0.6,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: radii.full,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarAdd: {
    backgroundColor: colors.surface.subtle,
    borderWidth: 1,
    borderColor: colors.border.strong,
    borderStyle: 'dashed',
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  rowTitle: {
    fontWeight: '600',
  },
  empty: {
    paddingVertical: spacing.lg,
    textAlign: 'center',
  },
});
