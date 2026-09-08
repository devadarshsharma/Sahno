import { StyleSheet, View } from 'react-native';

import { Screen, Text } from '@/components/ui';
import { colors, spacing } from '@/theme';

/** Placeholder until the membership-directory slice lands (D-018, Slice 3). */
export default function People() {
  return (
    <Screen>
      <View style={styles.container}>
        <View style={styles.illustration}>
          <Text style={styles.emoji}>👥</Text>
        </View>
        <Text variant="heading" style={styles.centered}>
          People
        </Text>
        <Text color="secondary" variant="bodySmall" style={styles.centered}>
          The member directory arrives here in an upcoming update.
        </Text>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.xl,
  },
  illustration: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: colors.surface.subtle,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emoji: {
    fontSize: 40,
    lineHeight: 50,
  },
  centered: {
    textAlign: 'center',
  },
});
