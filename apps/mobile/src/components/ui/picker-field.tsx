import { DateTimePicker } from '@expo/ui/community/datetime-picker';
import { useState } from 'react';
import { Platform, Pressable, StyleSheet, View } from 'react-native';

import { Text } from './text';
import { TextAction } from './text-action';
import { colors, radii, spacing, textVariants } from '@/theme';

export type PickerFieldProps = {
  label: string;
  mode: 'date' | 'time';
  /** The stored value ("2026-10-24", "19:30:00"), or null when not set. */
  value: string | null;
  onChange: (value: string | null) => void;
  /** Stored value → the Date the picker shows, and back. */
  toDate: (value: string) => Date;
  fromDate: (date: Date) => string;
  format: (value: string) => string;
  /** What an empty field opens on. */
  suggested: () => Date;
  placeholder: string;
  helperText?: string;
  error?: string;
  clearable?: boolean;
};

/**
 * A date or time field that behaves the way each platform's own apps do
 * (D-107).
 *
 * **Android**: tapping opens the system dialog; OK sets the value — the
 * suggested one too — and Cancel leaves it.
 *
 * **iOS**: as in Calendar and Settings, tapping expands the system wheel (or
 * calendar) under the field; the field follows it live; tapping the field
 * again, or Done, closes it. An empty field takes the suggested value the
 * moment it opens, so keeping 7 pm needs no scrolling.
 *
 * Either way `onChange` fires **once**, when the choice is made — never per
 * tick of a wheel. Several fields save straight to the server, and a save per
 * tick would tell members about every hour the wheel passed through.
 */
export function PickerField({
  label,
  mode,
  value,
  onChange,
  toDate,
  fromDate,
  format,
  suggested,
  placeholder,
  helperText,
  error,
  clearable = false,
}: PickerFieldProps) {
  const [open, setOpen] = useState(false);
  // iOS only: what the wheel shows while open, committed on close.
  const [draft, setDraft] = useState<string | null>(null);
  const ios = Platform.OS === 'ios';

  const openPicker = () => {
    if (ios) {
      setDraft(value ?? fromDate(suggested()));
    }
    setOpen(true);
  };

  const closePicker = () => {
    setOpen(false);
    if (ios && draft !== null && draft !== value) {
      onChange(draft);
    }
    setDraft(null);
  };

  const shown = ios && open ? draft : value;

  return (
    <View>
      <Text variant="label" color="secondary">
        {label}
      </Text>

      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${label}. ${shown ? format(shown) : placeholder}.`}
        accessibilityState={{ expanded: ios ? open : undefined }}
        onPress={ios && open ? closePicker : openPicker}
        style={({ pressed }) => [
          styles.field,
          { borderColor: error ? colors.border.error : open ? colors.border.focus : colors.border.default },
          pressed ? styles.fieldPressed : null,
        ]}
      >
        <Text style={shown ? (ios && open ? styles.editing : undefined) : styles.placeholder}>
          {shown ? format(shown) : placeholder}
        </Text>
      </Pressable>

      {open && ios && draft !== null ? (
        <View style={styles.inline}>
          <DateTimePicker
            mode={mode}
            value={toDate(draft)}
            display={mode === 'time' ? 'spinner' : 'inline'}
            accentColor={colors.tealText}
            onValueChange={(_event, date) => setDraft(fromDate(date))}
          />
          <View style={styles.done}>
            <TextAction label="Done" color="accent" onPress={closePicker} />
          </View>
        </View>
      ) : null}

      {open && !ios ? (
        <DateTimePicker
          mode={mode}
          value={value ? toDate(value) : suggested()}
          accentColor={colors.tealText}
          onValueChange={(_event, date) => {
            setOpen(false);
            onChange(fromDate(date));
          }}
          onDismiss={() => setOpen(false)}
        />
      ) : null}

      <View style={styles.footer}>
        <View style={styles.footerText}>
          {error ? (
            <Text variant="bodySmall" color="error" accessibilityLiveRegion="polite">
              {error}
            </Text>
          ) : helperText ? (
            <Text variant="bodySmall" color="muted">
              {helperText}
            </Text>
          ) : null}
        </View>
        {clearable && value && !open ? (
          <TextAction label="Clear" color="accent" accessibilityLabel={`Clear ${label}`} onPress={() => onChange(null)} />
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  field: {
    minHeight: 48,
    justifyContent: 'center',
    marginTop: spacing.xs,
    marginBottom: spacing.xs,
    borderRadius: radii.md,
    borderWidth: 1,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    backgroundColor: colors.surface.raised,
  },
  fieldPressed: {
    backgroundColor: colors.surface.subtle,
  },
  placeholder: {
    color: colors.text.muted,
    fontFamily: textVariants.body.fontFamily,
  },
  // The value being chosen reads as live, the way iOS tints it.
  editing: {
    color: colors.tealText,
  },
  inline: {
    marginBottom: spacing.xs,
  },
  done: {
    alignItems: 'flex-end',
  },
  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  footerText: {
    flex: 1,
  },
});
