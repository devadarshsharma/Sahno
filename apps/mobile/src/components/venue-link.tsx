import { Ionicons } from '@expo/vector-icons';
import { Pressable, StyleSheet, type StyleProp, type ViewStyle } from 'react-native';

import { Text } from '@/components/ui';
import { openInMaps } from '@/lib/maps';
import { colors, spacing } from '@/theme';

/**
 * A venue you can tap to open in maps — wherever a venue is shown, getting
 * there is the next thing somebody wants to do with it.
 */
export function VenueLink({
  venue,
  size = 'body',
  showIcon = true,
  style,
}: {
  venue: string;
  size?: 'body' | 'small' | 'caption';
  showIcon?: boolean;
  style?: StyleProp<ViewStyle>;
}) {
  const textVariant = size === 'body' ? 'body' : size === 'small' ? 'bodySmall' : 'caption';

  return (
    <Pressable
      accessibilityRole="link"
      accessibilityLabel={`${venue}. Open in maps`}
      onPress={() => void openInMaps(venue)}
      hitSlop={6}
      style={({ pressed }) => [styles.link, pressed ? styles.pressed : null, style]}
    >
      {showIcon ? (
        <Ionicons name="location-outline" size={size === 'caption' ? 13 : 15} color={colors.tealText} />
      ) : null}
      <Text variant={textVariant} style={styles.text} numberOfLines={2}>
        {venue}
      </Text>
      <Ionicons name="navigate-outline" size={size === 'caption' ? 12 : 14} color={colors.tealText} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  link: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    alignSelf: 'flex-start',
    flexShrink: 1,
  },
  text: {
    color: colors.tealText,
    textDecorationLine: 'underline',
    flexShrink: 1,
  },
  pressed: {
    opacity: 0.6,
  },
});
