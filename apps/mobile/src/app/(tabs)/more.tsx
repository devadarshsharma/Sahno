import { useQuery } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { StyleSheet, View } from 'react-native';

import { getMe } from '@/api/me';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { spacing } from '@/theme';

/**
 * The More surface (D-042): account details, organisation actions, and
 * sign-out live here rather than on Home.
 */
export default function More() {
  const router = useRouter();
  const session = useSession();
  const { active, organisations } = useActiveOrg();

  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getMe(accessToken, signal);
    },
    enabled: session.status === 'authenticated',
  });

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">More</Text>
      </View>

      <Card style={styles.card}>
        <Text variant="subheading">Account</Text>
        {session.user?.name ? <Text>{session.user.name}</Text> : null}
        {session.user?.email ? (
          <Text color="secondary">{session.user.email}</Text>
        ) : null}
        {meQuery.isSuccess ? (
          <Text color="muted" variant="caption">
            Sahno account {meQuery.data.userId}
          </Text>
        ) : null}
      </Card>

      <Card style={styles.card}>
        <Text variant="subheading">Organisations</Text>
        <Text color="secondary" variant="bodySmall">
          {active
            ? `Active: ${active.name} (${active.role})`
            : 'No active organisation.'}
          {organisations.length > 1
            ? ` · ${organisations.length} organisations`
            : ''}
        </Text>
        <Button
          label="Switch organisation"
          variant="secondary"
          onPress={() => router.push('/switch-organisation')}
        />
        {isOrganiser ? (
          <Button
            label="Invite members"
            variant="secondary"
            onPress={() => router.push('/invitations')}
          />
        ) : null}
      </Card>

      <Card style={styles.card}>
        <Text variant="subheading">Development</Text>
        <Text color="muted" variant="caption">
          Temporary tools — removed before release.
        </Text>
        <Button
          label="Brand preview"
          variant="ghost"
          onPress={() => router.push('/brand-preview')}
        />
      </Card>

      <Button label="Sign out" variant="secondary" onPress={session.signOut} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    marginBottom: spacing.xl,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
});
