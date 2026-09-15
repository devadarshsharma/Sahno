import { useRouter } from 'expo-router';
import { StyleSheet } from 'react-native';

import { Button, Card, Screen, Text } from '@/components/ui';
import { useMe } from '@/hooks/use-me';
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
  const meQuery = useMe();

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return (
    <Screen scroll hero={{ title: 'More', subtitle: 'Your account and this organisation.' }}>

      <Card style={styles.card}>
        <Text variant="subheading">Account</Text>
        {/* The Sahno record, not the identity-provider claim: for email
            sign-ins the provider's name is just the address again. */}
        {meQuery.data?.displayName ? (
          <Text>{meQuery.data.displayName}</Text>
        ) : null}
        {meQuery.data?.email ? (
          <Text color="secondary">{meQuery.data.email}</Text>
        ) : null}
        <Button
          label="Change your name"
          variant="secondary"
          onPress={() => router.push('/set-name')}
        />
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
      </Card>

      <Card style={styles.card}>
        <Text variant="subheading">Repertoire</Text>
        <Text color="secondary" variant="bodySmall">
          Everything you perform, with the lyrics to hand. Anyone can add to it.
        </Text>
        <Button
          label="Repertoire"
          variant="secondary"
          onPress={() => router.push('/repertoire')}
        />
      </Card>

      {isOrganiser ? (
        <Card style={styles.card}>
          <Text variant="subheading">Announcement</Text>
          <Text color="secondary" variant="bodySmall">
            Write to everybody at once — it reaches every phone.
          </Text>
          <Button
            label="Send an announcement"
            variant="secondary"
            onPress={() => router.push('/announce')}
          />
        </Card>
      ) : null}

      {isOrganiser ? (
        <Card style={styles.card}>
          <Text variant="subheading">Customers</Text>
          <Text color="secondary" variant="bodySmall">
            Everyone who has booked you, and how often. Members never see this.
          </Text>
          <Button
            label="Customer directory"
            variant="secondary"
            onPress={() => router.push('/customers')}
          />
        </Card>
      ) : null}

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
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
});
