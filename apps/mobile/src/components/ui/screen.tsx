import type { PropsWithChildren } from 'react';
import { RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { colors, spacing } from '@/theme';

export type ScreenProps = PropsWithChildren<{
  /** Wrap content in a ScrollView. Use for content that may overflow. */
  scroll?: boolean;
  /**
   * Pull-to-refresh handler. Anything showing data other people can change
   * needs one: without it the only way to see a new member or invitation is
   * to leave the screen and come back.
   */
  onRefresh?: () => void;
  /** Whether a refresh is in flight, for the spinner. */
  refreshing?: boolean;
}>;

export function Screen({
  scroll = false,
  onRefresh,
  refreshing = false,
  children,
}: ScreenProps) {
  // A pull gesture needs something scrollable to hang off, so asking for
  // refresh implies a ScrollView even on a screen that would otherwise fit.
  const scrollable = scroll || onRefresh !== undefined;

  return (
    <SafeAreaView style={styles.safeArea}>
      {scrollable ? (
        <ScrollView
          contentContainerStyle={[
            styles.content,
            scroll ? null : styles.fillContent,
          ]}
          keyboardShouldPersistTaps="handled"
          refreshControl={
            onRefresh ? (
              <RefreshControl
                refreshing={refreshing}
                onRefresh={onRefresh}
                tintColor={colors.tealText}
                colors={[colors.tealText]}
              />
            ) : undefined
          }
        >
          {children}
        </ScrollView>
      ) : (
        <View style={[styles.content, styles.fill]}>{children}</View>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.surface.canvas,
  },
  content: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.xl,
  },
  // Keeps a short screen pullable: the content must fill the viewport for the
  // gesture to have anywhere to start.
  fillContent: {
    flexGrow: 1,
  },
  fill: {
    flex: 1,
  },
});
