import { Ionicons } from '@expo/vector-icons';
import { useEffect } from 'react';
import { StyleSheet, View } from 'react-native';
import Animated, { FadeInDown, FadeOutDown } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { create } from 'zustand';

import { Text } from './text';
import { colors, fontFamilies, radii, spacing } from '@/theme';

type Toast = { id: number; message: string; tone: 'done' | 'info' };

const useToasts = create<{ toast: Toast | null; set: (toast: Toast | null) => void }>((set) => ({
  toast: null,
  set: (toast) => set({ toast }),
}));

let nextId = 1;

/** How long a toast stays: long enough to read, short enough to forget. */
const VISIBLE_MS = 2600;

/**
 * A short confirmation at the bottom of the screen — "Reminder sent to
 * Adarsh" — that goes by itself (D-123). For something that happened, not a
 * question: it never blocks a tap, and a newer one replaces it.
 */
export function showToast(message: string, tone: Toast['tone'] = 'done'): void {
  useToasts.getState().set({ id: nextId++, message, tone });
}

/** Mounted once, at the root, beside the dialog host. */
export function ToastHost() {
  const toast = useToasts((state) => state.toast);
  const set = useToasts((state) => state.set);
  const insets = useSafeAreaInsets();

  useEffect(() => {
    if (!toast) {
      return;
    }
    const timer = setTimeout(() => {
      // Only clear the one this timer was for; a newer toast keeps its time.
      if (useToasts.getState().toast?.id === toast.id) {
        set(null);
      }
    }, VISIBLE_MS);
    return () => clearTimeout(timer);
  }, [toast, set]);

  if (!toast) {
    return null;
  }

  return (
    <View pointerEvents="none" style={[styles.layer, { bottom: insets.bottom + 72 }]}>
      <Animated.View
        key={toast.id}
        entering={FadeInDown.duration(180)}
        exiting={FadeOutDown.duration(180)}
        style={styles.toast}
        accessibilityLiveRegion="polite"
        accessibilityRole="alert"
      >
        <Ionicons
          name={toast.tone === 'done' ? 'checkmark-circle' : 'information-circle'}
          size={18}
          color={colors.offWhite}
        />
        <Text style={styles.text} numberOfLines={2}>
          {toast.message}
        </Text>
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  layer: {
    position: 'absolute',
    left: 0,
    right: 0,
    alignItems: 'center',
    paddingHorizontal: spacing.xl,
    zIndex: 900,
    elevation: 900,
  },
  toast: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    maxWidth: 420,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderRadius: radii.full,
    backgroundColor: colors.navy,
  },
  text: {
    flexShrink: 1,
    fontFamily: fontFamilies.uiMedium,
    fontSize: 14,
    color: colors.offWhite,
  },
});
