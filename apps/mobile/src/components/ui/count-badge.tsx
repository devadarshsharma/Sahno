import { StyleSheet, View } from 'react-native';

import { Text } from '@/components/ui/text';
import { colors, fontFamilies } from '@/theme';

/**
 * The unread bubble: a count on a small orange pill, in the bell's colour so
 * "something new" looks the same everywhere. Nothing at all at zero, since an
 * empty bubble would read as something to look at. Above 99 it says "99+";
 * the exact number stops mattering long before that.
 */
export function CountBadge({ count }: { count: number }) {
  if (count <= 0) {
    return null;
  }

  const label = count > 99 ? '99+' : String(count);

  return (
    <View
      style={styles.badge}
      accessibilityLabel={`${label} unread`}
      accessibilityRole="text"
    >
      <Text style={styles.text}>{label}</Text>
    </View>
  );
}

/** One place for the colour, so the tab bar badge matches. */
export const BADGE_COLOR = colors.orange;

const styles = StyleSheet.create({
  badge: {
    minWidth: 20,
    height: 20,
    paddingHorizontal: 6,
    borderRadius: 10,
    backgroundColor: BADGE_COLOR,
    alignItems: 'center',
    justifyContent: 'center',
  },
  text: {
    color: colors.offWhite,
    fontFamily: fontFamilies.bold,
    fontSize: 12,
    lineHeight: 14,
  },
});
