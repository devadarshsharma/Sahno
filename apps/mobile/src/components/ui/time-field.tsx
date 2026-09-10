import { DateTimePicker } from '@expo/ui/community/datetime-picker';
import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { Text } from '@/components/ui/text';
import { colors, radii, spacing, textVariants } from '@/theme';

export type TimeFieldProps = {
  label: string;
  /** "19:30:00", or null when not set. */
  value: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  helperText?: string;
  clearable?: boolean;
};

/**
 * A tappable time field backed by the platform picker, matching DateField.
 * Times are stored as a wall clock — the hour written on the booking sheet —
 * rather than an instant, because a call time is "be there at five", not a
 * moment on a timeline that shifts with a device's zone.
 */
export function TimeField({
  label,
  value,
  onChange,
  placeholder = 'Not set',
  helperText,
  clearable = true,
}: TimeFieldProps) {
  const [picking, setPicking] = useState(false);

  return (
    <View>
      <Text variant="label" color="secondary">
        {label}
      </Text>

      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${label}. ${value ? formatTime(value) : placeholder}.`}
        onPress={() => setPicking(true)}
        style={({ pressed }) => [
          styles.field,
          pressed ? styles.fieldPressed : null,
        ]}
      >
        <Text style={value ? undefined : styles.placeholder}>
          {value ? formatTime(value) : placeholder}
        </Text>
      </Pressable>

      {picking ? (
        <DateTimePicker
          mode="time"
          value={value ? parseTime(value) : defaultTime()}
          accentColor={colors.tealText}
          onValueChange={(_event, date) => {
            setPicking(false);
            onChange(toTimeString(date));
          }}
          onDismiss={() => setPicking(false)}
        />
      ) : null}

      <View style={styles.footer}>
        <View style={styles.footerText}>
          {helperText ? (
            <Text variant="bodySmall" color="muted">
              {helperText}
            </Text>
          ) : null}
        </View>
        {clearable && value ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Clear ${label}`}
            onPress={() => onChange(null)}
          >
            <Text variant="bodySmall" color="accent">
              Clear
            </Text>
          </Pressable>
        ) : null}
      </View>
    </View>
  );
}

function defaultTime(): Date {
  const date = new Date();
  date.setHours(19, 0, 0, 0);
  return date;
}

export function toTimeString(date: Date): string {
  const hours = `${date.getHours()}`.padStart(2, '0');
  const minutes = `${date.getMinutes()}`.padStart(2, '0');
  return `${hours}:${minutes}:00`;
}

export function parseTime(value: string): Date {
  const [hours, minutes] = value.split(':').map(Number);
  const date = new Date();
  date.setHours(hours, minutes, 0, 0);
  return date;
}

export function formatTime(value: string): string {
  return parseTime(value).toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  });
}

const styles = StyleSheet.create({
  field: {
    minHeight: 48,
    justifyContent: 'center',
    marginTop: spacing.xs,
    marginBottom: spacing.xs,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.default,
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
