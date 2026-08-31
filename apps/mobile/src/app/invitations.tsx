import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { Share, StyleSheet, View } from 'react-native';

import {
  createInvitation,
  listInvitations,
  revokeInvitation,
} from '@/api/organisations';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { spacing } from '@/theme';

/** Owner/Admin invite management: create a shareable link, list, revoke (D-045, D-077). */
export default function Invitations() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  const invitationsQuery = useQuery({
    queryKey: ['org', active?.id, 'invitations'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listInvitations(accessToken, active!.id, signal);
    },
    enabled: active !== null,
  });

  const createMutation = useMutation({
    mutationFn: async () => {
      const accessToken = await session.getAccessToken();
      return createInvitation(accessToken, active!.id);
    },
    onSuccess: async (invitation) => {
      await queryClient.invalidateQueries({
        queryKey: ['org', active!.id, 'invitations'],
      });
      await Share.share({
        message:
          `You're invited to join ${active!.name} on Sahno!\n\n` +
          `1. Install the Sahno app and sign in\n` +
          `2. Choose "I have an invite code"\n` +
          `3. Enter this code: ${invitation.token}`,
      });
    },
  });

  const revokeMutation = useMutation({
    mutationFn: async (invitationId: string) => {
      const accessToken = await session.getAccessToken();
      await revokeInvitation(accessToken, active!.id, invitationId);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['org', active!.id, 'invitations'],
      }),
  });

  if (!active) {
    return null;
  }

  const activeInvitations = (invitationsQuery.data ?? []).filter(
    (invitation) => invitation.revokedAtUtc === null,
  );

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">Invite members</Text>
        <Text color="secondary">
          Share a link code for {active.name}. Anyone with an active code joins
          as a Member — codes work until you revoke them.
        </Text>
      </View>

      <Button
        label="Create and share an invite code"
        onPress={() => createMutation.mutate()}
        loading={createMutation.isPending}
      />
      {createMutation.isError ? (
        <Text color="error" variant="bodySmall" style={styles.errorText}>
          Could not create the invitation. Please try again.
        </Text>
      ) : null}

      <View style={styles.list}>
        <Text variant="subheading">Active codes</Text>
        {invitationsQuery.isPending ? (
          <Text color="muted" variant="bodySmall">
            Loading…
          </Text>
        ) : activeInvitations.length === 0 ? (
          <Text color="muted" variant="bodySmall">
            No active invite codes yet.
          </Text>
        ) : (
          activeInvitations.map((invitation) => (
            <Card key={invitation.id} style={styles.inviteCard}>
              <View style={styles.inviteRow}>
                <View style={styles.inviteText}>
                  <Text variant="label">{invitation.token}</Text>
                  <Text variant="caption" color="muted">
                    Created{' '}
                    {new Date(invitation.createdAtUtc).toLocaleDateString()}
                  </Text>
                </View>
                <Button
                  label="Revoke"
                  variant="secondary"
                  onPress={() => revokeMutation.mutate(invitation.id)}
                  disabled={revokeMutation.isPending}
                />
              </View>
            </Card>
          ))
        )}
      </View>

      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  errorText: {
    marginTop: spacing.sm,
  },
  list: {
    gap: spacing.md,
    marginTop: spacing.xl,
    marginBottom: spacing.lg,
  },
  inviteCard: {
    paddingVertical: spacing.md,
  },
  inviteRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  inviteText: {
    flex: 1,
    gap: 2,
  },
});
