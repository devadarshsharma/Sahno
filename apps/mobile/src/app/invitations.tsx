import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { Alert, StyleSheet, View } from 'react-native';

import {
  createInvitation,
  listInvitations,
  revokeInvitation,
  type Invitation,
} from '@/api/organisations';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { shareInvite } from '@/lib/invite';
import { useSession } from '@/providers/auth-provider';
import { spacing } from '@/theme';

/**
 * Owner/Admin invite management (D-045, D-077). Codes are multi-use until
 * revoked, so every active code can be shared again as often as needed —
 * dismissing the share sheet loses nothing.
 */
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
      await shareInvite(active!.name, invitation.token);
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
  const hasCodes = activeInvitations.length > 0;

  function confirmRevoke(invitation: Invitation) {
    Alert.alert(
      'Revoke this code?',
      'Anyone still holding it will not be able to join. People who already joined stay members.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Revoke',
          style: 'destructive',
          onPress: () => revokeMutation.mutate(invitation.id),
        },
      ],
    );
  }

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">Invite members</Text>
        <Text color="secondary">
          Share a code for {active.name}. Anyone with an active code joins as a
          Member, and a code keeps working until you revoke it — so you can
          share the same one with as many people as you like.
        </Text>
      </View>

      <Button
        label={hasCodes ? 'Create another code' : 'Create an invite code'}
        variant={hasCodes ? 'secondary' : 'primary'}
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
        ) : !hasCodes ? (
          <Text color="muted" variant="bodySmall">
            No active invite codes yet.
          </Text>
        ) : (
          activeInvitations.map((invitation) => (
            <Card key={invitation.id} style={styles.inviteCard}>
              <Text variant="subheading" selectable style={styles.token}>
                {invitation.token}
              </Text>
              <Text variant="caption" color="muted">
                Created {new Date(invitation.createdAtUtc).toLocaleDateString()}
              </Text>
              <View style={styles.inviteActions}>
                <Button
                  label="Share"
                  onPress={() => shareInvite(active.name, invitation.token)}
                  style={styles.action}
                />
                <Button
                  label="Revoke"
                  variant="secondary"
                  onPress={() => confirmRevoke(invitation)}
                  disabled={revokeMutation.isPending}
                  style={styles.action}
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
    gap: spacing.xs,
  },
  token: {
    letterSpacing: 1,
  },
  inviteActions: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginTop: spacing.sm,
  },
  action: {
    flex: 1,
  },
});
