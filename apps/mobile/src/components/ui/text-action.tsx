import { ActivityIndicator, Alert, Pressable, StyleSheet } from 'react-native';

import { Text, type TextColor } from './text';
import { colors } from '@/theme';

/**
 * A small text action on a row — Remove, Not required, Edit notes. Small to
 * look at, but not to hit: the touch area is a full 44 points tall and
 * reaches past the word, it dims when pressed, and it shows a spinner while
 * its request is out, so a slow connection never reads as a tap that did
 * nothing.
 *
 * `confirm` asks first. Anything that deletes should: a stray thumb on a row
 * should not cost someone a rehearsal.
 */
export function TextAction({
  label,
  onPress,
  busy = false,
  color = 'muted',
  confirm,
  accessibilityLabel,
}: {
  label: string;
  onPress: () => void;
  busy?: boolean;
  color?: TextColor;
  confirm?: { title: string; message?: string; action?: string };
  accessibilityLabel?: string;
}) {
  const press = () => {
    if (!confirm) {
      onPress();
      return;
    }
    Alert.alert(confirm.title, confirm.message, [
      { text: 'Cancel', style: 'cancel' },
      { text: confirm.action ?? label, style: 'destructive', onPress },
    ]);
  };

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? label}
      accessibilityState={{ busy, disabled: busy }}
      disabled={busy}
      onPress={press}
      hitSlop={{ top: 8, bottom: 8, left: 16, right: 16 }}
      style={({ pressed }) => [styles.action, pressed ? styles.pressed : null]}
    >
      {busy ? (
        <ActivityIndicator size="small" color={colors.text.muted} />
      ) : (
        <Text variant="caption" color={color}>
          {label}
        </Text>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  action: {
    minHeight: 32,
    minWidth: 44,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'flex-start',
  },
  pressed: {
    opacity: 0.4,
  },
});
