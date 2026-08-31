import { useQuery } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { getHealth } from '@/api/health';
import { getMe } from '@/api/me';
import { SahnoLogo } from '@/components/brand';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { colors, spacing } from '@/theme';

export default function Index() {
  const router = useRouter();
  const session = useSession();

  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => getHealth(signal),
  });

  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getMe(accessToken, signal);
    },
    enabled: session.status === 'authenticated',
  });

  return (
    <Screen scroll>
      <View style={styles.logo}>
        <SahnoLogo size={64} tagline />
      </View>

      <Card style={styles.card}>
        <Text variant="subheading">Signed in</Text>
        {session.user?.name ? <Text>{session.user.name}</Text> : null}
        {session.user?.email ? (
          <Text color="secondary">{session.user.email}</Text>
        ) : null}

        {meQuery.isPending ? (
          <Text color="muted" variant="bodySmall">
            Loading your Sahno account…
          </Text>
        ) : null}
        {meQuery.isSuccess ? (
          <Text color="muted" variant="caption">
            Sahno account {meQuery.data.userId}
          </Text>
        ) : null}
        {meQuery.isError ? (
          <>
            <Text color="error" variant="bodySmall">
              Could not load your Sahno account.
            </Text>
            <Button
              label="Retry"
              variant="secondary"
              onPress={() => meQuery.refetch()}
            />
          </>
        ) : null}

        <Button label="Sign out" variant="secondary" onPress={session.signOut} />
      </Card>

      <Card style={styles.card}>
        {healthQuery.isPending ? (
          <>
            <ActivityIndicator color={colors.tealText} />
            <Text color="secondary" style={styles.centeredText}>
              Connecting to Sahno API…
            </Text>
          </>
        ) : null}

        {healthQuery.isError ? (
          <>
            <Text variant="heading" color="error" style={styles.centeredText}>
              Could not connect
            </Text>
            <Text color="secondary" style={styles.centeredText}>
              {healthQuery.error.message}
            </Text>
            <Button label="Try again" onPress={() => healthQuery.refetch()} />
          </>
        ) : null}

        {healthQuery.isSuccess ? (
          <>
            <Text variant="heading" color="accent" style={styles.centeredText}>
              API connected
            </Text>
            <Text color="secondary" style={styles.centeredText}>
              Status: {healthQuery.data.status}
            </Text>
          </>
        ) : null}
      </Card>

      {/* Temporary entry point for the visual-foundation preview. */}
      <Button
        label="Brand preview"
        variant="ghost"
        onPress={() => router.push('/brand-preview')}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  logo: {
    alignItems: 'center',
    marginTop: spacing.xl,
    marginBottom: spacing.xxl,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  centeredText: {
    textAlign: 'center',
  },
});
