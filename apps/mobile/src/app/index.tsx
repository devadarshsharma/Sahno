import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { getHealth } from '@/api/health';
import { getMe } from '@/api/me';
import { dismissSetupChecklist } from '@/api/organisations';
import { SahnoSymbol } from '@/components/brand';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { colors, spacing } from '@/theme';

export default function Index() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const { organisations, active, isPending, isFetching } = useActiveOrg();

  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getMe(accessToken, signal);
    },
    enabled: session.status === 'authenticated',
  });

  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => getHealth(signal),
  });

  const dismissChecklist = useMutation({
    mutationFn: async () => {
      const accessToken = await session.getAccessToken();
      await dismissSetupChecklist(accessToken, active!.id);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['organisations'] }),
  });

  // Never decide "no organisations" from a stale or in-flight list — that
  // caused a bounce back to onboarding right after creating one.
  if (isPending || (organisations.length === 0 && isFetching)) {
    return (
      <Screen>
        <View style={styles.loading}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  // First-time branching (D-056): no organisations yet.
  if (organisations.length === 0) {
    return <Redirect href="/onboarding" />;
  }

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return (
    <Screen scroll>
      {/* Active-organisation header (D-044). */}
      <View style={styles.orgHeader}>
        <SahnoSymbol size={36} />
        <View style={styles.orgHeaderText}>
          <Text variant="heading" numberOfLines={1}>
            {active?.name}
          </Text>
          <Text variant="caption" color="muted">
            {active?.role}
            {organisations.length > 1
              ? ` · ${organisations.length} organisations`
              : ''}
          </Text>
        </View>
        <Button
          label="Switch"
          variant="ghost"
          onPress={() => router.push('/switch-organisation')}
        />
      </View>

      {active?.showSetupChecklist ? (
        <Card style={styles.card}>
          <Text variant="subheading">Get {active.name} going</Text>
          <View style={styles.checklist}>
            <ChecklistItem
              label="Invite your Members"
              actionLabel="Invite"
              onPress={() => router.push('/invitations')}
            />
            <ChecklistItem label="Create your first Enquiry" comingSoon />
            <ChecklistItem label="Add an organisation logo" comingSoon />
            <ChecklistItem label="Review organisation settings" comingSoon />
          </View>
          <Button
            label="Dismiss checklist"
            variant="ghost"
            onPress={() => dismissChecklist.mutate()}
            loading={dismissChecklist.isPending}
          />
        </Card>
      ) : null}

      {isOrganiser && !active?.showSetupChecklist ? (
        <Button
          label="Invite members"
          variant="secondary"
          onPress={() => router.push('/invitations')}
        />
      ) : null}

      <Card style={styles.card}>
        <Text variant="subheading">Signed in</Text>
        {session.user?.name ? <Text>{session.user.name}</Text> : null}
        {session.user?.email ? (
          <Text color="secondary">{session.user.email}</Text>
        ) : null}
        {meQuery.isSuccess ? (
          <Text color="muted" variant="caption">
            Sahno account {meQuery.data.userId}
          </Text>
        ) : null}
        <Button label="Sign out" variant="secondary" onPress={session.signOut} />
      </Card>

      <Card style={styles.card}>
        {healthQuery.isPending ? (
          <ActivityIndicator color={colors.tealText} />
        ) : healthQuery.isError ? (
          <>
            <Text color="error" style={styles.centeredText}>
              API not reachable
            </Text>
            <Button label="Try again" onPress={() => healthQuery.refetch()} />
          </>
        ) : (
          <Text color="accent" variant="bodySmall" style={styles.centeredText}>
            API connected — {healthQuery.data?.status}
          </Text>
        )}
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

function ChecklistItem({
  label,
  actionLabel,
  onPress,
  comingSoon = false,
}: {
  label: string;
  actionLabel?: string;
  onPress?: () => void;
  comingSoon?: boolean;
}) {
  return (
    <View style={styles.checklistItem}>
      <Text
        variant="body"
        color={comingSoon ? 'muted' : 'primary'}
        style={styles.checklistLabel}
      >
        {label}
      </Text>
      {comingSoon ? (
        <Text variant="caption" color="muted">
          coming soon
        </Text>
      ) : (
        <Button
          label={actionLabel ?? 'Open'}
          variant="ghost"
          onPress={onPress ?? (() => {})}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  loading: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  orgHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    marginBottom: spacing.xl,
  },
  orgHeaderText: {
    flex: 1,
    gap: 2,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  checklist: {
    gap: spacing.xs,
  },
  checklistItem: {
    flexDirection: 'row',
    alignItems: 'center',
    minHeight: 44,
    gap: spacing.md,
  },
  checklistLabel: {
    flex: 1,
  },
  centeredText: {
    textAlign: 'center',
  },
});
