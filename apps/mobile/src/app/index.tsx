import { useQuery } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { getHealth } from '@/api/health';
import { SahnoLogo } from '@/components/brand';
import { Button, Card, Screen, Text } from '@/components/ui';
import { colors, spacing } from '@/theme';

export default function Index() {
  const router = useRouter();
  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => getHealth(signal),
  });

  return (
    <Screen>
      <View style={styles.center}>
        <View style={styles.logo}>
          <SahnoLogo size={64} tagline />
        </View>

        <Card style={styles.statusCard}>
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
      </View>

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
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'stretch',
    gap: spacing.xxl,
  },
  logo: {
    alignItems: 'center',
  },
  statusCard: {
    gap: spacing.md,
  },
  centeredText: {
    textAlign: 'center',
  },
});
