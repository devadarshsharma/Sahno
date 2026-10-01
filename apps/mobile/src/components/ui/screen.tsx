import { StatusBar } from 'expo-status-bar';
import type { PropsWithChildren } from 'react';
import { RefreshControl, StyleSheet, View } from 'react-native';
import {
  KeyboardAvoidingView,
  KeyboardAwareScrollView,
} from 'react-native-keyboard-controller';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { Hero, type HeroProps } from '@/components/ui/hero';
import { colors, spacing } from '@/theme';

/** Space kept between the focused field and the top of the keyboard. */
const KEYBOARD_GAP = spacing.xl;

export type ScreenProps = PropsWithChildren<{
  /**
   * Wrap content in a ScrollView. Use for content that may overflow — and for
   * every screen with text fields: the scrolling container is what moves a
   * focused field above the keyboard, so a form must be able to scroll.
   */
  scroll?: boolean;
  /**
   * Pull-to-refresh handler. Anything showing data other people can change
   * needs one: without it the only way to see a new member or invitation is
   * to leave the screen and come back.
   */
  onRefresh?: () => void;
  /** Whether a refresh is in flight, for the spinner. */
  refreshing?: boolean;
  /**
   * The navy page header shared with Home. When set, the header owns the top
   * inset so the navy runs under the status bar, and the content starts
   * beneath it.
   */
  hero?: HeroProps;
}>;

export function Screen({
  scroll = false,
  onRefresh,
  refreshing = false,
  hero,
  children,
}: ScreenProps) {
  const insets = useSafeAreaInsets();

  // A pull gesture needs something scrollable to hang off, so asking for
  // refresh implies a ScrollView even on a screen that would otherwise fit.
  const scrollable = scroll || onRefresh !== undefined;

  const body = (
    <>
      {hero ? <Hero {...hero} /> : null}
      {children}
    </>
  );

  return (
    <SafeAreaView
      style={styles.safeArea}
      edges={hero ? ['left', 'right', 'bottom'] : undefined}
    >
      {hero ? <StatusBar style="light" /> : null}
      {/* Android draws the app edge to edge, so scrolled content would pass
          under the clock. This strip owns that space: the page scrolls
          beneath it and the status bar stays on navy. */}
      {hero ? (
        <View
          pointerEvents="none"
          style={[styles.statusStrip, { height: insets.top }]}
        />
      ) : null}
      {/* The keyboard never covers a field. Android draws edge to edge, and
          then it no longer shrinks the window for the keyboard — the keyboard
          is simply drawn over the bottom of the page. These containers do the
          work instead: the scrolling one moves the focused field up into
          view, the fixed one gives up the keyboard's height. Every screen
          built on Screen gets this, so a new form needs nothing extra. */}
      {scrollable ? (
        <KeyboardAwareScrollView
          bottomOffset={KEYBOARD_GAP}
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
                tintColor={hero ? colors.offWhite : colors.tealText}
                colors={[colors.tealText]}
                progressViewOffset={hero ? spacing.xxl : undefined}
              />
            ) : undefined
          }
        >
          {body}
        </KeyboardAwareScrollView>
      ) : (
        <KeyboardAvoidingView behavior="padding" style={styles.fill}>
          <View style={[styles.content, styles.fill]}>{body}</View>
        </KeyboardAvoidingView>
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
  statusStrip: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    backgroundColor: colors.navy,
    zIndex: 1,
  },
});
