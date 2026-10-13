import { Ionicons } from '@expo/vector-icons';
import { useQueryClient } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import { deleteOrganisation } from '@/api/organisations';
import { formatPurgeDay } from '@/components/recently-deleted';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { useActiveOrganisation } from '@/stores/active-organisation';
import { colors, spacing } from '@/theme';

/** How long the Owner can restore it (Organisation.RestoreWindow, D-101). */
const RESTORE_DAYS = 30;

/**
 * Deleting an organisation (D-101). The Owner's alone, and everybody loses it
 * at once, so it asks for the name typed out rather than a tap on an alert
 * that a thumb can hit by mistake.
 */
export default function DeleteOrganisation() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const { organisationId } = useLocalSearchParams<{ organisationId: string }>();
  const { organisations, active } = useActiveOrg();
  const { setActiveOrganisation } = useActiveOrganisation();
  const [typed, setTyped] = useState('');
  // The server keeps it this long (D-101); shown so the Owner knows the way back.
  const [purgeDay] = useState(() =>
    formatPurgeDay(new Date(Date.now() + RESTORE_DAYS * 24 * 60 * 60 * 1000).toISOString()),
  );
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const organisation = organisations.find((org) => org.id === organisationId);

  if (!organisation || organisation.role !== 'Owner') {
    return (
      <Screen hero={{ title: 'Delete organisation' }}>
        <Text color="secondary">Only the Owner can delete an organisation.</Text>
      </Screen>
    );
  }

  const matches = typed.trim().toLowerCase() === organisation.name.trim().toLowerCase();

  const onDelete = async () => {
    setError(null);
    setDeleting(true);
    try {
      const accessToken = await session.getAccessToken();
      await deleteOrganisation(accessToken, organisation.id, typed);
      if (active?.id === organisation.id) {
        // Falls back to the default, else the first one left; with none,
        // Home sends them to onboarding.
        setActiveOrganisation(null);
        queryClient.removeQueries({ queryKey: ['org'] });
      }
      await queryClient.refetchQueries({ queryKey: ['organisations'], type: 'all' });
      router.dismissAll();
      router.replace('/');
    } catch {
      setError('Could not delete it. Check your connection and try again.');
      setDeleting(false);
    }
  };

  return (
    <Screen
      scroll
      hero={{
        title: `Delete ${organisation.name}?`,
        subtitle: `It disappears for everyone now. You can restore it until ${purgeDay}; after that it is gone for good.`,
      }}
    >
      <Card style={styles.warning}>
        <View style={styles.warningHeading}>
          <Ionicons name="warning-outline" size={20} color={colors.text.error} />
          <Text variant="subheading" color="error">
            On {purgeDay}, everything in it goes for good
          </Text>
        </View>
        <Text color="secondary">
          Every booking and its chat, rehearsals, jobs, set lists and payments; customers and
          repertoire; invitations; and every member’s place in it. Members are told it was deleted.
        </Text>
        {organisation.memberCount > 1 ? (
          <Text color="secondary">
            {organisation.memberCount - 1} other {organisation.memberCount === 2 ? 'person' : 'people'} will lose it.
          </Text>
        ) : null}
        <Text color="secondary">
          Changed your mind? Restore it from Your organisations before then.
        </Text>
        <Text color="secondary">
          To hand it on instead, transfer ownership from a member’s page in Members.
        </Text>
      </Card>

      <TextInput
        label={`Type ${organisation.name} to confirm`}
        value={typed}
        onChangeText={setTyped}
        autoCapitalize="none"
        autoCorrect={false}
        error={error ?? undefined}
        disabled={deleting}
      />

      <Button
        label="Delete organisation"
        onPress={onDelete}
        disabled={!matches || deleting}
        loading={deleting}
        style={[styles.delete, matches ? styles.deleteArmed : null]}
      />
      <Button label="Keep it" variant="ghost" onPress={() => router.back()} disabled={deleting} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  warning: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  warningHeading: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  delete: {
    marginTop: spacing.lg,
    marginBottom: spacing.sm,
  },
  deleteArmed: {
    backgroundColor: colors.text.error,
  },
});
