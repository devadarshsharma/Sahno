import { useState } from 'react';
import {
  TextInput as RNTextInput,
  StyleSheet,
  View,
  type TextInputProps as RNTextInputProps,
} from 'react-native';

import { Text } from '@/components/ui/text';
import { colors, radii, spacing, textVariants } from '@/theme';

export type TextInputProps = Omit<RNTextInputProps, 'editable'> & {
  label: string;
  /** Guidance shown under the field when there is no error. */
  helperText?: string;
  /** Error message; when set, the field renders in its error state. */
  error?: string;
  disabled?: boolean;
};

export function TextInput({
  label,
  helperText,
  error,
  disabled = false,
  style,
  onFocus,
  onBlur,
  ...rest
}: TextInputProps) {
  const [focused, setFocused] = useState(false);

  const borderColor = error
    ? colors.border.error
    : focused
      ? colors.border.focus
      : colors.border.default;

  return (
    <View>
      <Text variant="label" color={disabled ? 'muted' : 'secondary'}>
        {label}
      </Text>
      <RNTextInput
        {...rest}
        accessibilityLabel={rest.accessibilityLabel ?? label}
        accessibilityState={{ disabled }}
        editable={!disabled}
        onFocus={(event) => {
          setFocused(true);
          onFocus?.(event);
        }}
        onBlur={(event) => {
          setFocused(false);
          onBlur?.(event);
        }}
        placeholderTextColor={colors.text.muted}
        style={[
          styles.input,
          {
            borderColor,
            borderWidth: focused || error ? 2 : 1,
            backgroundColor: disabled
              ? colors.surface.subtle
              : colors.surface.raised,
            color: disabled ? colors.text.muted : colors.text.primary,
          },
          style,
        ]}
      />
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
  );
}

const styles = StyleSheet.create({
  input: {
    minHeight: 48,
    marginTop: spacing.xs,
    marginBottom: spacing.xs,
    borderRadius: radii.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    fontFamily: textVariants.body.fontFamily,
    fontSize: textVariants.body.fontSize,
  },
});
