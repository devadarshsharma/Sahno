import { useEffect, type ReactNode } from 'react';
import { StyleSheet, type StyleProp, type ViewStyle } from 'react-native';
import Animated, {
  interpolateColor,
  useAnimatedStyle,
  useSharedValue,
  withDelay,
  withRepeat,
  withSequence,
  withTiming,
} from 'react-native-reanimated';

import { radii } from '@/theme';

/** Three soft pulses, about two seconds in all, once the screen has settled. */
const SETTLE_MS = 400;
const UP_MS = 320;
const DOWN_MS = 420;
const PULSES = 3;

/**
 * Lights up the thing to do, briefly, when a screen is opened from Needs your
 * attention (D-130): the item said "Jobs not handed out", so Jobs glows for a
 * moment and nobody has to work out where on the page that is. Draws nothing
 * extra when not active.
 */
export function Glow({
  active,
  inset = 6,
  style,
  children,
}: {
  active: boolean;
  /** How far the glow reaches beyond what it wraps. */
  inset?: number;
  style?: StyleProp<ViewStyle>;
  children: ReactNode;
}) {
  const glow = useSharedValue(0);

  useEffect(() => {
    if (!active) {
      return;
    }
    glow.value = withDelay(
      SETTLE_MS,
      withRepeat(withSequence(withTiming(1, { duration: UP_MS }), withTiming(0, { duration: DOWN_MS })), PULSES),
    );
  }, [active, glow]);

  const animated = useAnimatedStyle(() => ({
    backgroundColor: interpolateColor(glow.value, [0, 1], ['rgba(249,123,10,0)', 'rgba(249,123,10,0.16)']),
    borderColor: interpolateColor(glow.value, [0, 1], ['rgba(249,123,10,0)', 'rgba(249,123,10,0.7)']),
  }));

  if (!active) {
    return <>{children}</>;
  }

  return (
    <Animated.View style={[styles.glow, { margin: -inset, padding: inset - 1.5 }, animated, style]}>
      {children}
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  glow: {
    borderWidth: 1.5,
    borderRadius: radii.lg,
  },
});
