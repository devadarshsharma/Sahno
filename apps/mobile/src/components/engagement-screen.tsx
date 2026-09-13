import { useLocalSearchParams } from 'expo-router';
import type { ReactNode } from 'react';
import { ActivityIndicator, StyleSheet, View } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { Screen, Text } from '@/components/ui';
import { useEngagements } from '@/hooks/use-engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, spacing } from '@/theme';

/** What every section screen of the workspace needs to know. */
export type EngagementContext = {
  engagement: Engagement;
  isOrganiser: boolean;
  organisationName: string;
};

/**
 * One section of the workspace: loads the booking, handles loading and
 * not-found, and renders a hero with the booking's name as the eyebrow and the
 * section as the title — so every section page looks like the overview and
 * says where it belongs.
 */
export function EngagementSectionScreen({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: (context: EngagementContext) => ReactNode;
}) {
  const { engagementId } = useLocalSearchParams<{ engagementId: string }>();
  const { active } = useActiveOrg();
  const engagementsQuery = useEngagements();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  const engagement = engagementsQuery.data?.find((row) => row.id === engagementId);

  if (engagementsQuery.isPending) {
    return (
      <Screen hero={{ title }}>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (!engagement) {
    return (
      <Screen hero={{ title }}>
        <View style={styles.centered}>
          <Text variant="heading">Not found</Text>
          <Text color="secondary" style={styles.centeredText}>
            This booking may have been discarded.
          </Text>
        </View>
      </Screen>
    );
  }

  return (
    <Screen
      scroll
      onRefresh={() => engagementsQuery.refetch()}
      refreshing={engagementsQuery.isRefetching}
      hero={{ eyebrow: engagement.title, title, subtitle }}
    >
      {children({
        engagement,
        isOrganiser,
        organisationName: active?.name ?? 'your organisation',
      })}
    </Screen>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingVertical: spacing.xxl,
  },
  centeredText: {
    textAlign: 'center',
  },
});
