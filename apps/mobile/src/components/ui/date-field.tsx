import { DateTimePicker } from '@expo/ui/community/datetime-picker';
import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { Text } from '@/components/ui/text';
import { colors, radii, spacing, textVariants } from '@/theme';

export type DateFieldProps = {
  label: string;
  /** ISO calendar date, "YYYY-MM-DD", or null when not set. */
  value: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  helperText?: string;
  error?: string;
  /** Offers a "Clear" action. Only for dates that are allowed to be unknown. */
  clearable?: boolean;
};

/**
 * A tappable date field backed by the platform picker. Typing dates by hand
 * invites the wrong month and the wrong format; a picker cannot produce
 * either.
 *
 * The value is a plain calendar date rather than an instant: an engagement
 * happens on a day, and turning it into a timestamp would let a time zone move
 * it to the day before.
 */
export function DateField({
  label,
  value,
  onChange,
  placeholder = 'Choose a date',
  helperText,
  error,
  clearable = false,
}: DateFieldProps) {
  const [picking, setPicking] = useState(false);

  return (
    <View>
      <Text variant="label" color="secondary">
        {label}
      </Text>

      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${label}. ${value ? formatIsoDate(value) : placeholder}.`}
        onPress={() => setPicking(true)}
        style={({ pressed }) => [
          styles.field,
          { borderColor: error ? colors.border.error : colors.border.default },
          pressed ? styles.fieldPressed : null,
        ]}
      >
        <Text style={value ? undefined : styles.placeholder}>
          {value ? formatIsoDate(value) : placeholder}
        </Text>
      </Pressable>

      {picking ? (
        <DateTimePicker
          mode="date"
          value={value ? parseIsoDate(value) : new Date()}
          accentColor={colors.tealText}
          onValueChange={(_event, date) => {
            setPicking(false);
            onChange(toIsoDate(date));
          }}
          onDismiss={() => setPicking(false)}
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

/** Local calendar date, so a time zone cannot shift it to the day before. */
export function toIsoDate(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function parseIsoDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

export function formatIsoDate(value: string): string {
  return parseIsoDate(value).toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
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
