import type { ReactNode } from 'react';
import { StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { Text } from '@/components/ui/text';
import { colors, fontFamilies, radii, spacing } from '@/theme';

export type HeroProps = {
  title: string;
  subtitle?: string;
  /** Something for the top-right — the bell, a count, an action. */
  right?: ReactNode;
  /** A line above the title in the small caps style, e.g. the booking's status. */
  eyebrow?: string;
};

/**
 * The navy header every page shares with Home. The same block, the same
 * corners, the same type — so moving between pages feels like turning a page
 * rather than opening a different app. Home keeps its own richer version with
 * the brand and the greeting; everything else gets a title and, optionally, a
 * line under it. There is no back control: the system gesture and button do
 * that, and one way back is easier to learn than two.
 *
 * It sits inside the scroll view rather than above it, so it moves with the
 * content the way Home's does, and the status-bar inset is part of the block
 * so the navy runs to the top edge.
 */
export function Hero({ title, subtitle, right, eyebrow }: HeroProps) {
  const insets = useSafeAreaInsets();

  return (
    <View style={[styles.hero, { paddingTop: insets.top + spacing.md }]}>
      {right ? <View style={styles.controls}>{right}</View> : null}

      {eyebrow ? <Text style={styles.eyebrow}>{eyebrow}</Text> : null}
      <Text style={styles.title} accessibilityRole="header">
        {title}
      </Text>
      {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  hero: {
    backgroundColor: colors.navy,
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xl,
    borderBottomLeftRadius: radii.xl,
    borderBottomRightRadius: radii.xl,
    // Pull the block out to the screen edges past Screen's horizontal padding,
    // and lift it over Screen's top padding, so the navy runs edge to edge.
    marginHorizontal: -spacing.lg,
    marginTop: -spacing.xl,
    marginBottom: spacing.lg,
  },
  controls: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    marginBottom: spacing.lg,
    minHeight: 40,
  },
  eyebrow: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 12,
    lineHeight: 16,
    letterSpacing: 0.6,
    textTransform: 'uppercase',
    color: colors.tealSoft,
    marginBottom: spacing.xs,
  },
  title: {
    fontFamily: fontFamilies.bold,
    fontSize: 26,
    lineHeight: 32,
    color: colors.offWhite,
  },
  subtitle: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 14,
    lineHeight: 20,
    color: 'rgba(250, 247, 242, 0.72)',
    marginTop: spacing.xs,
  },
});
