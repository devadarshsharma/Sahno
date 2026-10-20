import { Ionicons } from '@expo/vector-icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Pressable, StyleSheet, View } from 'react-native';

import { listDeletedOrganisations, restoreOrganisation } from '@/api/organisations';
import { showAlert, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { colors, radii, spacing } from '@/theme';

/** "12 November" — the day it goes for good. */
export function formatPurgeDay(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'long' });
}

/**
 * Organisations the person deleted and can still bring back (D-101). Shown
 * in the switcher, and on onboarding for someone who deleted their only one.
 * Renders nothing when there are none.
 */
export function RecentlyDeleted({ onRestored }: { onRestored?: () => void } = {}) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { switchTo } = useActiveOrg();

  const deleted = useQuery({
    queryKey: ['organisations', 'deleted'],
    queryFn: async ({ signal }) => listDeletedOrganisations(await session.getAccessToken(), signal),
    enabled: session.status === 'authenticated',
  });

  const restore = useMutation({
    mutationFn: async (organisationId: string) => {
      await restoreOrganisation(await session.getAccessToken(), organisationId);
      return organisationId;
    },
    onSuccess: async (organisationId) => {
      await queryClient.refetchQueries({ queryKey: ['organisations'], type: 'all' });
      switchTo(organisationId);
      onRestored?.();
    },
    onError: () => showAlert('Could not restore it', 'Check your connection and try again.'),
  });

  const rows = deleted.data ?? [];
  if (rows.length === 0) {
    return null;
  }

  return (
    <View style={styles.section}>
      <Text variant="subheading">Recently deleted</Text>
      {rows.map((organisation) => (
        <View key={organisation.id} style={styles.row}>
          <View style={styles.rowText}>
            <Text variant="body">{organisation.name}</Text>
            <Text variant="caption" color="muted">
              Gone for good on {formatPurgeDay(organisation.purgeAfterUtc)}
            </Text>
          </View>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Restore ${organisation.name}`}
            disabled={restore.isPending}
            onPress={() => restore.mutate(organisation.id)}
            hitSlop={8}
            style={({ pressed }) => [styles.restore, pressed ? styles.pressed : null]}
          >
            <Ionicons name="arrow-undo-outline" size={16} color={colors.text.accent} />
            <Text variant="label" color="accent">
              Restore
            </Text>
          </Pressable>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: spacing.sm,
    marginTop: spacing.xl,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    minHeight: 56,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderStyle: 'dashed',
    borderColor: colors.border.strong,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  restore: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    minHeight: 44,
  },
  pressed: {
    opacity: 0.6,
  },
});
