import { StyleSheet, View } from 'react-native';

import { SahnoSymbol } from '@/components/brand/sahno-symbol';
import { Text } from '@/components/ui/text';
import { colors, fontFamilies, spacing } from '@/theme';

export type SahnoLogoProps = {
  /** Symbol height in dp; the wordmark scales with it. */
  size?: number;
  /** Show the tagline under the wordmark. */
  tagline?: boolean;
  /** Use the reversed (off-white) wordmark for dark backgrounds. */
  onDark?: boolean;
};

/** Primary horizontal lockup: symbol + Bricolage Bold wordmark. */
export function SahnoLogo({
  size = 56,
  tagline = false,
  onDark = false,
}: SahnoLogoProps) {
  return (
    <View style={styles.row}>
      <SahnoSymbol size={size} />
      <View>
        <Text
          accessibilityRole="header"
          style={{
            fontFamily: fontFamilies.bold,
            fontSize: size * 0.62,
            lineHeight: size * 0.75,
            color: onDark ? colors.text.inverse : colors.navy,
          }}
        >
          Sahno
        </Text>
        {tagline ? (
          <Text
            style={{
              fontFamily: fontFamilies.medium,
              fontSize: size * 0.24,
              lineHeight: size * 0.32,
              color: onDark ? colors.tealSoft : colors.text.accent,
            }}
          >
            Make it happen, together.
          </Text>
        ) : null}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
});
