import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Redirect, useRouter } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { dismissSetupChecklist, listInvitations } from '@/api/organisations';
import { SahnoSymbol } from '@/components/brand';
import { Button, Card, Screen, Text } from '@/components/ui';
import { firstNameOf, useMe, useNeedsDisplayName } from '@/hooks/use-me';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { colors, fontFamilies, radii, shadows, spacing } from '@/theme';

function greetingForNow(): string {
  const hour = new Date().getHours();
  if (hour < 12) {
    return 'Good morning';
  }
  if (hour < 18) {
    return 'Good afternoon';
  }
  return 'Good evening';
}

export default function Index() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const insets = useSafeAreaInsets();
  const { organisations, active, isPending, isFetching, isError, refetch } =
    useActiveOrg();

  const meQuery = useMe();
  const needsDisplayName = useNeedsDisplayName();

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  const invitationsQuery = useQuery({
    queryKey: ['org', active?.id, 'invitations'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listInvitations(accessToken, active!.id, signal);
    },
    enabled: active !== null && isOrganiser,
  });

  const dismissChecklist = useMutation({
    mutationFn: async () => {
      const accessToken = await session.getAccessToken();
      await dismissSetupChecklist(accessToken, active!.id);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ['organisations'] }),
  });

  if (isPending || (organisations.length === 0 && isFetching)) {
    return (
      <Screen>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (isError) {
    return (
      <Screen>
        <View style={styles.centered}>
          <Text variant="heading" color="error">
            Could not load your organisations
          </Text>
          <Text color="secondary" style={styles.centeredText}>
            Check your connection and try again.
          </Text>
          <Button label="Try again" onPress={() => refetch()} />
        </View>
      </Screen>
    );
  }

  // Existing accounts predating the name step, and anyone whose only sign-in
  // method supplied no usable name (D-046).
  if (needsDisplayName) {
    return <Redirect href="/set-name" />;
  }

  if (organisations.length === 0) {
    return <Redirect href="/onboarding" />;
  }

  const firstName = firstNameOf(meQuery.data);
  function refreshAll() {
    refetch();
    if (invitationsQuery.isSuccess || invitationsQuery.isError) {
      invitationsQuery.refetch();
    }
  }

  const activeInvites = (invitationsQuery.data ?? []).filter(
    (invitation) => invitation.revokedAtUtc === null,
  ).length;

  return (
    <View style={styles.screen}>
      <StatusBar style="light" />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={isFetching || invitationsQuery.isRefetching}
            onRefresh={refreshAll}
            tintColor={colors.offWhite}
            colors={[colors.tealText]}
          />
        }
      >
        {/* Navy hero header. */}
        <View style={[styles.hero, { paddingTop: insets.top + spacing.md }]}>
          <View style={styles.heroTop}>
            <View style={styles.heroBrand}>
              <SahnoSymbol size={34} />
              <View>
                <Text style={styles.heroWordmark}>Sahno</Text>
                <Text style={styles.heroTagline}>
                  Make it happen, together.
                </Text>
              </View>
            </View>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel={`Active organisation ${active?.name}. Switch organisation.`}
              onPress={() => router.push('/switch-organisation')}
              style={({ pressed }) => [
                styles.orgPill,
                pressed ? styles.orgPillPressed : null,
              ]}
            >
              <Text
                variant="label"
                numberOfLines={1}
                style={styles.orgPillText}
              >
                {active?.name}
              </Text>
              <Text style={styles.orgPillChevron}>▾</Text>
            </Pressable>
          </View>

          <Text style={styles.greeting}>
            {greetingForNow()}
            {firstName ? `, ${firstName}` : ''} 👋
          </Text>
          <Text style={styles.greetingSub}>
            {isOrganiser
              ? "Here's what's happening with your organisation."
              : 'Here are your upcoming events.'}
          </Text>
        </View>

        {/* Stat tiles overlap the hero (organisers only). */}
        {isOrganiser ? (
          <View style={styles.statsRow}>
            <StatTile value="0" label="Upcoming events" />
            <StatTile value={String(active?.memberCount ?? 1)} label="Members" />
            <StatTile value="0" label="Awaiting responses" />
            <StatTile
              value={invitationsQuery.isSuccess ? String(activeInvites) : '–'}
              label="Active invites"
            />
          </View>
        ) : null}

        <View style={[styles.body, isOrganiser ? null : styles.bodyMember]}>
          {isOrganiser && active?.showSetupChecklist ? (
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

          {isOrganiser ? (
            <>
              <View style={styles.attention}>
                <View style={styles.attentionHeader}>
                  <View style={styles.attentionIcon}>
                    <Text style={styles.attentionEmoji}>🔔</Text>
                  </View>
                  <Text variant="subheading">Needs your attention</Text>
                </View>
                <Text color="secondary" variant="bodySmall">
                  Nothing needs your attention right now. Unanswered requests,
                  missing details, and follow-ups will appear here.
                </Text>
              </View>

              <HomeSection title="Upcoming bookings">
                No bookings yet — enquiries and bookings arrive in an upcoming
                update.
              </HomeSection>

              <Button
                label="Create event — coming soon"
                disabled
                onPress={() => {}}
              />
              <Button
                label="Invite members"
                variant="secondary"
                onPress={() => router.push('/invitations')}
              />
            </>
          ) : (
            <>
              <HomeSection title="Needs your response">
                Nothing needs your response right now.
              </HomeSection>
              <HomeSection title="Upcoming events">
                No upcoming events yet — you will see them here as soon as your
                organiser adds you to one.
              </HomeSection>
            </>
          )}
        </View>
      </ScrollView>
    </View>
  );
}

function StatTile({ value, label }: { value: string; label: string }) {
  return (
    <View style={styles.statTile}>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

function HomeSection({
  title,
  children,
}: {
  title: string;
  children: string;
}) {
  return (
    <View style={styles.section}>
      <Text variant="subheading">{title}</Text>
      <Card style={styles.emptyCard}>
        <Text color="muted" variant="bodySmall">
          {children}
        </Text>
      </Card>
    </View>
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
  screen: {
    flex: 1,
    backgroundColor: colors.surface.canvas,
  },
  scrollContent: {
    paddingBottom: spacing.xxl,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.xl,
  },
  centeredText: {
    textAlign: 'center',
  },
  hero: {
    backgroundColor: colors.navy,
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.xxl + spacing.lg,
    borderBottomLeftRadius: radii.xl,
    borderBottomRightRadius: radii.xl,
  },
  heroTop: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.md,
    marginBottom: spacing.xl,
  },
  heroBrand: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  heroWordmark: {
    fontFamily: fontFamilies.bold,
    fontSize: 20,
    lineHeight: 24,
    color: colors.offWhite,
  },
  heroTagline: {
    fontFamily: fontFamilies.medium,
    fontSize: 10,
    lineHeight: 13,
    color: colors.tealSoft,
  },
  orgPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    maxWidth: 170,
    minHeight: 40,
    paddingHorizontal: spacing.md,
    borderRadius: radii.full,
    backgroundColor: 'rgba(250, 247, 242, 0.12)',
  },
  orgPillPressed: {
    backgroundColor: 'rgba(250, 247, 242, 0.22)',
  },
  orgPillText: {
    color: colors.offWhite,
    flexShrink: 1,
  },
  orgPillChevron: {
    color: colors.tealSoft,
    fontSize: 12,
  },
  greeting: {
    fontFamily: fontFamilies.bold,
    fontSize: 26,
    lineHeight: 32,
    color: colors.offWhite,
  },
  greetingSub: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 14,
    lineHeight: 20,
    color: 'rgba(250, 247, 242, 0.72)',
    marginTop: spacing.xs,
  },
  statsRow: {
    flexDirection: 'row',
    gap: spacing.sm,
    paddingHorizontal: spacing.lg,
    marginTop: -(spacing.xxl + spacing.xs),
    marginBottom: spacing.lg,
  },
  statTile: {
    flex: 1,
    backgroundColor: colors.surface.raised,
    borderRadius: radii.lg,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
    alignItems: 'center',
    gap: 2,
    ...shadows.md,
  },
  statValue: {
    fontFamily: fontFamilies.bold,
    fontSize: 20,
    lineHeight: 26,
    color: colors.text.primary,
  },
  statLabel: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 10,
    lineHeight: 13,
    color: colors.text.muted,
    textAlign: 'center',
  },
  body: {
    paddingHorizontal: spacing.lg,
    gap: spacing.lg,
  },
  bodyMember: {
    marginTop: spacing.lg,
  },
  card: {
    gap: spacing.md,
  },
  attention: {
    backgroundColor: colors.orangeSoft,
    borderRadius: radii.lg,
    padding: spacing.lg,
    gap: spacing.sm,
  },
  attentionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  attentionIcon: {
    width: 34,
    height: 34,
    borderRadius: 17,
    backgroundColor: colors.surface.raised,
    alignItems: 'center',
    justifyContent: 'center',
  },
  attentionEmoji: {
    fontSize: 16,
    lineHeight: 20,
  },
  section: {
    gap: spacing.sm,
  },
  emptyCard: {
    paddingVertical: spacing.lg,
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
});
