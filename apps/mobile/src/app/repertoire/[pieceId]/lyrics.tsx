import { Ionicons } from '@expo/vector-icons';
import { useKeepAwake } from 'expo-keep-awake';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Text } from '@/components/ui';
import { usePieceDetail } from '@/hooks/use-repertoire';
import { colors, radii, spacing } from '@/theme';

const SIZES = [18, 22, 26, 32] as const;

/**
 * The lyrics, for the stage: big, high-contrast, the screen stays awake, and
 * nothing else on the page. Text size is a tap away because the distance
 * from a music stand to a face is not the distance from a hand to a face.
 */
export default function Lyrics() {
  useKeepAwake();
  const { pieceId } = useLocalSearchParams<{ pieceId: string }>();
  const router = useRouter();
  const insets = useSafeAreaInsets();
  const detailQuery = usePieceDetail(pieceId ?? null);
  const [sizeIndex, setSizeIndex] = useState(1);
  const [dark, setDark] = useState(true);

  const piece = detailQuery.data?.piece;
  const fontSize = SIZES[sizeIndex]!;
  const ink = dark ? colors.offWhite : colors.navy;
  const paper = dark ? colors.navy : colors.offWhite;

  return (
    <View style={[styles.page, { backgroundColor: paper, paddingTop: insets.top }]}>
      <View style={styles.bar}>
        <Pressable accessibilityRole="button" accessibilityLabel="Close" onPress={() => router.back()} hitSlop={12} style={styles.barButton}>
          <Ionicons name="chevron-down" size={24} color={ink} />
        </Pressable>
        <View style={styles.barTitle}>
          <Text style={[styles.title, { color: ink }]} numberOfLines={1}>
            {piece?.title ?? ''}
          </Text>
          {piece?.attribution ? (
            <Text style={[styles.attribution, { color: ink }]} numberOfLines={1}>
              {piece.attribution}
            </Text>
          ) : null}
        </View>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Smaller text"
          disabled={sizeIndex === 0}
          onPress={() => setSizeIndex((index) => Math.max(0, index - 1))}
          hitSlop={8}
          style={[styles.barButton, sizeIndex === 0 ? styles.disabled : null]}
        >
          <Text style={[styles.sizeGlyph, { color: ink, fontSize: 14 }]}>A</Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Larger text"
          disabled={sizeIndex === SIZES.length - 1}
          onPress={() => setSizeIndex((index) => Math.min(SIZES.length - 1, index + 1))}
          hitSlop={8}
          style={[styles.barButton, sizeIndex === SIZES.length - 1 ? styles.disabled : null]}
        >
          <Text style={[styles.sizeGlyph, { color: ink, fontSize: 20 }]}>A</Text>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={dark ? 'Light background' : 'Dark background'}
          onPress={() => setDark((value) => !value)}
          hitSlop={8}
          style={styles.barButton}
        >
          <Ionicons name={dark ? 'sunny-outline' : 'moon-outline'} size={20} color={ink} />
        </Pressable>
      </View>

      {detailQuery.isPending ? (
        <ActivityIndicator color={colors.tealText} style={styles.spinner} />
      ) : (
        <ScrollView
          contentContainerStyle={[styles.body, { paddingBottom: insets.bottom + spacing.xxl }]}
          showsVerticalScrollIndicator={false}
        >
          {piece?.lyrics ? (
            <Text style={[styles.lyrics, { color: ink, fontSize, lineHeight: fontSize * 1.55 }]} selectable>
              {piece.lyrics}
            </Text>
          ) : (
            <Text style={[styles.empty, { color: ink }]}>
              {detailQuery.isSuccess ? 'No lyrics typed up for this piece yet.' : 'Could not load this piece.'}
            </Text>
          )}
        </ScrollView>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  page: {
    flex: 1,
  },
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.sm,
  },
  barButton: {
    width: 40,
    height: 40,
    borderRadius: radii.full,
    alignItems: 'center',
    justifyContent: 'center',
  },
  disabled: {
    opacity: 0.35,
  },
  barTitle: {
    flex: 1,
    paddingHorizontal: spacing.xs,
  },
  title: {
    fontWeight: '700',
    fontSize: 16,
  },
  attribution: {
    fontSize: 12,
    opacity: 0.7,
  },
  sizeGlyph: {
    fontWeight: '700',
  },
  spinner: {
    marginTop: spacing.xl,
  },
  body: {
    paddingHorizontal: spacing.xl,
    paddingTop: spacing.lg,
  },
  lyrics: {
    fontWeight: '500',
  },
  empty: {
    opacity: 0.7,
    textAlign: 'center',
    marginTop: spacing.xl,
  },
});
