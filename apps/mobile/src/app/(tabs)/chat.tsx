import { useRouter } from 'expo-router';
import { Ionicons } from '@expo/vector-icons';
import { StyleSheet, View } from 'react-native';

import { Button, Screen, Text } from '@/components/ui';
import { colors, spacing } from '@/theme';

/**
 * Discussion lives inside each event rather than in an inbox (D-024, D-047
 * §6), so this tab is a signpost, not a screen of its own: it says where the
 * conversations are and takes you there. A cross-event inbox is a later
 * question, if a real group ever asks for one.
 */
export default function Chat() {
  const router = useRouter();

  return (
    <Screen
      hero={{
        title: 'Chat',
        subtitle: 'Conversations live with their events.',
      }}
    >
      <View style={styles.container}>
        <View style={styles.illustration}>
          <Ionicons name="chatbubbles-outline" size={40} color={colors.tealText} />
        </View>
        <Text variant="heading" style={styles.centered}>
          Open an event to talk about it
        </Text>
        <Text color="secondary" variant="bodySmall" style={styles.centered}>
          Every booking has its own discussion at the bottom of its page, so
          what was said about Saturday stays next to Saturday.
        </Text>
        <Button label="Go to events" onPress={() => router.push('/(tabs)/events')} />
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
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  centered: {
    textAlign: 'center',
  },
});
