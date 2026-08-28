import { View, type ViewProps } from 'react-native';

import { colors, radii, shadows, spacing, type ShadowKey } from '@/theme';

export type CardProps = ViewProps & {
  shadow?: ShadowKey;
  padded?: boolean;
};

export function Card({
  shadow = 'sm',
  padded = true,
  style,
  ...rest
}: CardProps) {
  return (
    <View
      {...rest}
      style={[
        {
          backgroundColor: colors.surface.raised,
          borderRadius: radii.lg,
          padding: padded ? spacing.lg : 0,
        },
        shadows[shadow],
        style,
      ]}
    />
  );
}
