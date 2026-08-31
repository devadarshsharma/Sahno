import { useRouter } from 'expo-router';
import { StyleSheet, View } from 'react-native';

import { SahnoLogo } from '@/components/brand';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { spacing } from '@/theme';

/**
 * First-time branching (D-056): no organisations yet — create one (becoming
 * its Owner) or join with an invite code.
 */
export default function Onboarding() {
  const router = useRouter();
  const session = useSession();

  return (
    <Screen>
      <View style={styles.container}>
        <View style={styles.logo}>
          <SahnoLogo size={56} />
        </View>

        <Card style={styles.card}>
          <Text variant="heading">Welcome{session.user?.name ? `, ${session.user.name.split(' ')[0]}` : ''}!</Text>
          <Text color="secondary">
            Sahno organises groups around private organisations. Create one for
            your group, or join one you have been invited to.
          </Text>
          <Button
            label="Create an organisation"
            onPress={() => router.push('/create-organisation')}
          />
          <Button
            label="I have an invite code"
            variant="secondary"
            onPress={() => router.push('/join')}
          />
        </Card>

        <Button
          label="Sign out"
          variant="ghost"
          onPress={session.signOut}
        />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    gap: spacing.xl,
  },
  logo: {
    alignItems: 'center',
  },
  card: {
    gap: spacing.md,
  },
});
