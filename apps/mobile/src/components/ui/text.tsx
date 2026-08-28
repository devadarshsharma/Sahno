import { Text as RNText, type TextProps as RNTextProps } from 'react-native';

import { colors, textVariants, type TextVariant } from '@/theme';

const textColors = {
  primary: colors.text.primary,
  secondary: colors.text.secondary,
  muted: colors.text.muted,
  inverse: colors.text.inverse,
  accent: colors.text.accent,
  error: colors.text.error,
} as const;

export type TextColor = keyof typeof textColors;

export type TextProps = RNTextProps & {
  variant?: TextVariant;
  color?: TextColor;
};

export function Text({
  variant = 'body',
  color = 'primary',
  style,
  ...rest
}: TextProps) {
  return (
    <RNText
      {...rest}
      style={[textVariants[variant], { color: textColors[color] }, style]}
    />
  );
}
