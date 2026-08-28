import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  View,
  type PressableProps,
  type StyleProp,
  type ViewStyle,
} from 'react-native';

import { Text } from '@/components/ui/text';
import { colors, radii, spacing, textVariants } from '@/theme';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost';

export type ButtonProps = Omit<PressableProps, 'children' | 'style'> & {
  label: string;
  variant?: ButtonVariant;
  loading?: boolean;
  style?: StyleProp<ViewStyle>;
};

const variantColors = {
  primary: {
    background: colors.interactive.primary,
    backgroundPressed: colors.interactive.primaryPressed,
    text: colors.text.inverse,
    border: 'transparent',
  },
  secondary: {
    background: 'transparent',
    backgroundPressed: colors.tealSoft,
    text: colors.text.primary,
    border: colors.border.strong,
  },
  ghost: {
    background: 'transparent',
    backgroundPressed: colors.surface.subtle,
    text: colors.text.accent,
    border: 'transparent',
  },
} as const;

export function Button({
  label,
  variant = 'primary',
  loading = false,
  disabled,
  style,
  accessibilityLabel,
  ...rest
}: ButtonProps) {
  const palette = variantColors[variant];
  const isDisabled = Boolean(disabled) || loading;

  return (
    <Pressable
      {...rest}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? label}
      accessibilityState={{ disabled: isDisabled, busy: loading }}
      disabled={isDisabled}
      style={({ pressed }) => [
        styles.base,
        {
          backgroundColor:
            pressed && !isDisabled
              ? palette.backgroundPressed
              : palette.background,
          borderColor: palette.border,
        },
        disabled && !loading && variant === 'primary'
          ? { backgroundColor: colors.interactive.disabledBackground }
          : null,
        style,
      ]}
    >
      {({ pressed }) => (
        <View style={styles.content}>
          {loading ? (
            <ActivityIndicator
              size="small"
              color={variant === 'primary' ? colors.text.inverse : colors.text.accent}
            />
          ) : null}
          <Text
            variant="button"
            style={[
              {
                color:
                  disabled && !loading
                    ? colors.interactive.disabledText
                    : palette.text,
              },
              pressed && !isDisabled && variant === 'ghost'
                ? { color: colors.text.primary }
                : null,
            ]}
          >
            {label}
          </Text>
        </View>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    minHeight: 48,
    borderRadius: radii.md,
    borderWidth: StyleSheet.hairlineWidth * 2,
    paddingHorizontal: spacing.xl,
    paddingVertical: spacing.md,
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    minHeight: textVariants.button.lineHeight,
  },
});
