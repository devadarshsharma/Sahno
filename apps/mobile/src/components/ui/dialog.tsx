import { useEffect } from 'react';
import { BackHandler, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { create } from 'zustand';

import { Text } from './text';
import { colors, fontFamilies, radii, shadows, spacing } from '@/theme';

export type DialogButton = {
  text: string;
  style?: 'default' | 'cancel' | 'destructive';
  onPress?: () => void;
};

type DialogOptions = {
  /** Tapping outside, or Android's back, dismisses it as Cancel would. Default true. */
  cancelable?: boolean;
  onDismiss?: () => void;
};

type Dialog = {
  id: number;
  title: string;
  message?: string;
  buttons: DialogButton[];
  options: DialogOptions;
};

type DialogState = {
  queue: Dialog[];
  push: (dialog: Dialog) => void;
  shift: () => void;
};

const useDialogs = create<DialogState>((set) => ({
  queue: [],
  push: (dialog) => set((state) => ({ queue: [...state.queue, dialog] })),
  shift: () => set((state) => ({ queue: state.queue.slice(1) })),
}));

let nextId = 1;

/**
 * Sahno's own alert, drawn by the app over everything else (D-116). Called
 * exactly like React Native's `Alert.alert`, which it replaces: on iOS 26 the
 * native alert crashed the app inside UIKit while being drawn, where no error
 * screen can catch it. This one is plain views, so it cannot take that path.
 *
 * Callable from anywhere — a component, a mutation callback, the crash
 * handler — with no hook. One shows at a time; the rest wait their turn.
 */
export function showAlert(
  title: string,
  message?: string,
  buttons?: DialogButton[],
  options: DialogOptions = {},
): void {
  useDialogs.getState().push({
    id: nextId++,
    title,
    message,
    buttons: buttons && buttons.length > 0 ? buttons : [{ text: 'OK' }],
    options,
  });
}

/** Renders the dialog at the front of the queue. Mounted once, at the root. */
export function DialogHost() {
  const dialog = useDialogs((state) => state.queue[0]);
  const shift = useDialogs((state) => state.shift);

  const cancelable = dialog?.options.cancelable ?? true;

  const close = (button?: DialogButton) => {
    shift();
    if (button) {
      button.onPress?.();
    } else {
      dialog?.buttons.find((candidate) => candidate.style === 'cancel')?.onPress?.();
      dialog?.options.onDismiss?.();
    }
  };

  // Android's back button closes it like Cancel, as the system dialog did.
  useEffect(() => {
    if (!dialog) {
      return;
    }
    const subscription = BackHandler.addEventListener('hardwareBackPress', () => {
      if (cancelable) {
        close();
      }
      return true;
    });
    return () => subscription.remove();
  });

  if (!dialog) {
    return null;
  }

  // Cancel first and the rest in order, as iOS lays out an alert's buttons;
  // three or more stack, one per row.
  const stacked = dialog.buttons.length > 2;
  const ordered = [
    ...dialog.buttons.filter((button) => button.style === 'cancel'),
    ...dialog.buttons.filter((button) => button.style !== 'cancel'),
  ];

  return (
    <View style={styles.overlay} accessibilityViewIsModal>
      <Pressable
        style={styles.backdrop}
        accessibilityLabel="Close"
        accessibilityRole="button"
        onPress={cancelable ? () => close() : undefined}
      />
      <View style={styles.card} accessibilityRole="alert">
        <ScrollView style={styles.textScroll} contentContainerStyle={styles.text}>
          <Text style={styles.title}>{dialog.title}</Text>
          {dialog.message ? (
            <Text variant="bodySmall" color="secondary" style={styles.message} selectable>
              {dialog.message}
            </Text>
          ) : null}
        </ScrollView>
        <View style={[styles.buttons, stacked ? styles.buttonsStacked : null]}>
          {(stacked ? dialog.buttons : ordered).map((button, index) => (
            <Pressable
              key={`${dialog.id}-${index}`}
              accessibilityRole="button"
              onPress={() => close(button)}
              style={({ pressed }) => [
                styles.button,
                stacked ? styles.buttonStacked : styles.buttonSide,
                index > 0 ? (stacked ? styles.dividerTop : styles.dividerLeft) : null,
                pressed ? styles.pressed : null,
              ]}
            >
              <Text
                style={[
                  styles.buttonText,
                  button.style === 'cancel' ? styles.cancelText : null,
                  button.style === 'destructive' ? styles.destructiveText : null,
                ]}
              >
                {button.text}
              </Text>
            </Pressable>
          ))}
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    zIndex: 1000,
    elevation: 1000,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.xl,
  },
  backdrop: {
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    backgroundColor: 'rgba(11, 27, 42, 0.45)',
  },
  card: {
    width: '100%',
    maxWidth: 340,
    maxHeight: '80%',
    borderRadius: radii.lg,
    backgroundColor: colors.surface.raised,
    overflow: 'hidden',
    ...shadows.md,
  },
  textScroll: {
    flexGrow: 0,
  },
  text: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    paddingBottom: spacing.md,
    gap: spacing.xs,
  },
  title: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 17,
    lineHeight: 22,
    color: colors.text.primary,
    textAlign: 'center',
  },
  message: {
    textAlign: 'center',
  },
  buttons: {
    flexDirection: 'row',
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border.default,
  },
  buttonsStacked: {
    flexDirection: 'column',
  },
  button: {
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.md,
  },
  buttonSide: {
    flex: 1,
  },
  buttonStacked: {
    alignSelf: 'stretch',
  },
  dividerLeft: {
    borderLeftWidth: StyleSheet.hairlineWidth,
    borderLeftColor: colors.border.default,
  },
  dividerTop: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border.default,
  },
  pressed: {
    backgroundColor: colors.surface.subtle,
  },
  buttonText: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 16,
    color: colors.tealText,
    textAlign: 'center',
  },
  cancelText: {
    color: colors.text.secondary,
  },
  destructiveText: {
    color: colors.text.error,
  },
});
