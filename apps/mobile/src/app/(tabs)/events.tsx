import { StyleSheet, View } from 'react-native';

import { Screen, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, spacing } from '@/theme';

/** Placeholder until the engagement slices land (Bookings/Events per D-039). */
export default function Events() {
  const { active } = useActiveOrg();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return (
    <Screen>
      <View style={styles.container}>
        <View style={styles.illustration}>
          <Text style={styles.emoji}>📅</Text>
        </View>
        <Text variant="heading" style={styles.centered}>
          {isOrganiser ? 'Bookings' : 'Events'}
        </Text>
        <Text color="secondary" variant="bodySmall" style={styles.centered}>
          {isOrganiser
            ? 'Enquiries, availability, and your booking pipeline arrive here in an upcoming update.'
            : 'Your events, RSVPs, and schedules arrive here in an upcoming update.'}
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
