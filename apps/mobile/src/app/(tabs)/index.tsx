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

import type { Engagement } from '@/api/engagements';
import type { Member } from '@/api/members';
import { dismissSetupChecklist, listInvitations } from '@/api/organisations';
import { SahnoSymbol } from '@/components/brand';
import { Button, Card, Screen, Text } from '@/components/ui';
import { firstNameOf, useMe, useNeedsDisplayName } from '@/hooks/use-me';
import {
  formatEngagementDate,
  useEngagements,
} from '@/hooks/use-engagements';
import { useMembers } from '@/hooks/use-members';
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
  const membersQuery = useMembers();
  const engagementsQuery = useEngagements();

  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

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
    membersQuery.refetch();
    engagementsQuery.refetch();
  }

  const openEngagement = (engagementId: string) =>
    router.push({
      pathname: '/engagement/[engagementId]',
      params: { engagementId },
    });

  const engagements = engagementsQuery.data ?? [];

  // The pipeline as D-041 ranks it, kept as separate lists rather than one
  // "live" pile: provisionally on and actually booked mean different things to
  // a group deciding whether to take other work.
  const byStatus = (status: Engagement['status']) =>
    engagements.filter((engagement) => engagement.status === status);

  const confirmed = byStatus('Confirmed');
  const tentative = byStatus('Tentative');
  const enquiries = byStatus('Draft');
  // not people, so the tile and the list beneath it say the same thing.
  // Unresolved work, which D-041 puts first. Three things qualify, and a
  // booking in any of them is invisible everywhere else on Home:
  //   - somebody has still to answer;
  //   - everyone has answered and the lineup is now the organiser's call,
  //     because Sahno never advances to Tentative on its own (D-026);
  //   - it is postponed, so it needs a new date or a decision to resume.
  const waiting = engagements.filter((engagement) => {
    if (engagement.status === 'Cancelled' || engagement.status === 'Completed') {
      return false;
    }
    if ((engagement.outstandingCount ?? 0) > 0) {
      return true;
    }
    if (
      engagement.status === 'CheckingAvailability' &&
      (engagement.selectedCount ?? 0) > 0
    ) {
      return true;
    }
    return engagement.status === 'Postponed';
  });

  // A member's own list: the ones nobody has heard back from them about.
  const needsYourAnswer = engagements.filter(
    (engagement) =>
      engagement.yourResponse === null &&
      engagement.status === 'CheckingAvailability',
  );

  // What a member has already replied to and is still on.
  const yourEvents = engagements.filter(
    (engagement) =>
      engagement.yourResponse !== null &&
      ['CheckingAvailability', 'Tentative', 'Confirmed'].includes(
        engagement.status,
      ),
  );

  // A member's own diary, in D-040's order. "Next confirmed" is deliberately
  // one event: the question it answers is where they need to be next, and a
  // list of six does not answer it.
  const yourConfirmed = yourEvents.filter(
    (engagement) => engagement.status === 'Confirmed',
  );
  const nextConfirmed = yourConfirmed.slice(0, 1);
  const laterEvents = yourConfirmed.slice(1);
  const yourTentative = yourEvents.filter(
    (engagement) => engagement.status === 'Tentative',
  );

  return (
    <View style={styles.screen}>
      <StatusBar style="light" />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={isFetching || membersQuery.isRefetching}
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

        {/* Stat tiles overlap the hero (organisers only). Four counts of the
            same thing — bookings — in the order D-041 ranks them, so the row
            reads as one scale rather than four unrelated numbers. */}
        {isOrganiser ? (
          <View style={styles.statsRow}>
            <StatTile value={String(waiting.length)} label="Needs attention" />
            <StatTile value={String(confirmed.length)} label="Confirmed" />
            <StatTile value={String(tentative.length)} label="Tentative" />
            <StatTile value={String(enquiries.length)} label="Enquiries" />
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

                {waiting.length === 0 ? (
                  <Text color="secondary" variant="bodySmall">
                    Nothing needs your attention right now. Unanswered requests,
                    missing details, and follow-ups will appear here.
                  </Text>
                ) : (
                  waiting.map((engagement) => (
                    <Pressable
                      key={engagement.id}
                      accessibilityRole="button"
                      accessibilityLabel={`${engagement.title}. ${attentionReason(engagement)}.`}
                      onPress={() =>
                        router.push({
                          pathname: '/engagement/[engagementId]',
                          params: { engagementId: engagement.id },
                        })
                      }
                      style={styles.attentionRow}
                    >
                      <View style={styles.attentionText}>
                        <Text numberOfLines={1}>{engagement.title}</Text>
                        <Text variant="caption" color="secondary">
                          {attentionReason(engagement)}
                          {' · '}
                          {formatEngagementDate(engagement)}
                        </Text>
                      </View>
                      <Text color="muted">›</Text>
                    </Pressable>
                  ))
                )}
              </View>

              {/* D-041 order: exceptions first, then confirmed, tentative,
                  new enquiries, and recent activity last. */}
              <EngagementList
                title="Upcoming confirmed bookings"
                engagements={confirmed}
                empty="Nothing confirmed yet."
                onOpen={openEngagement}
              />

              <EngagementList
                title="Tentative bookings"
                engagements={tentative}
                empty="Nothing tentative right now."
                onOpen={openEngagement}
              />

              <EngagementList
                title="New enquiries"
                engagements={enquiries}
                empty="No open enquiries. Start one from the Bookings tab — a title is all you need."
                onOpen={openEngagement}
              />

              <RecentJoins
                members={membersQuery.data ?? []}
                onSeeAll={() => router.push('/(tabs)/people')}
              />

              <Button
                label="Invite members"
                variant="secondary"
                onPress={() => router.push('/invitations')}
              />
            </>
          ) : (
            <>
              {/* D-040 order: what must I do, then where do I need to be. */}
              <EngagementList
                title="Needs your response"
                engagements={needsYourAnswer}
                empty="Nothing needs your response right now."
                onOpen={openEngagement}
              />
              <EngagementList
                title="Next confirmed event"
                engagements={nextConfirmed}
                empty="Nothing confirmed yet."
                onOpen={openEngagement}
              />
              <EngagementList
                title="Tentative events"
                engagements={yourTentative}
                empty="Nothing tentative right now."
                onOpen={openEngagement}
              />
              <EngagementList
                title="Later events"
                engagements={laterEvents}
                empty="Nothing else in the diary yet."
                onOpen={openEngagement}
              />
            </>
          )}
        </View>
      </ScrollView>
    </View>
  );
}


/**
 * A short list of engagements on Home, with the date and — for organisers —
 * how many people are still to answer, since that is usually the reason to
 * open one.
 */

/**
 * Why this booking is on the attention list. Naming the reason matters more
 * than flagging it: "still to answer" and "ready to decide" need opposite
 * actions from the organiser.
 */
function attentionReason(engagement: Engagement): string {
  const waiting = engagement.outstandingCount ?? 0;
  if (waiting > 0) {
    return waiting === 1
      ? '1 member still to answer'
      : `${waiting} members still to answer`;
  }

  if (engagement.status === 'CheckingAvailability') {
    const answered = engagement.selectedCount ?? 0;
    return answered === 1
      ? 'Answered · ready to decide'
      : `All ${answered} answered · ready to decide`;
  }

  if (engagement.status === 'Postponed') {
    return engagement.startDate === null
      ? 'Postponed · needs a new date'
      : 'Postponed · resume when ready';
  }

  return 'Needs a look';
}
function EngagementList({
  title,
  engagements,
  empty,
  onOpen,
}: {
  title: string;
  engagements: Engagement[];
  empty: string;
  onOpen: (engagementId: string) => void;
}) {
  if (engagements.length === 0) {
    return <HomeSection title={title}>{empty}</HomeSection>;
  }

  return (
    <View style={styles.section}>
      <Text variant="subheading">{title}</Text>
      <Card style={styles.sectionCard}>
        {engagements.map((engagement) => (
          <Pressable
            key={engagement.id}
            accessibilityRole="button"
            accessibilityLabel={`${engagement.title}. ${formatEngagementDate(engagement)}.`}
            onPress={() => onOpen(engagement.id)}
            style={styles.attentionRow}
          >
            <View style={styles.attentionText}>
              <Text numberOfLines={1}>{engagement.title}</Text>
              <Text variant="caption" color="secondary">
                {formatEngagementDate(engagement)}
                {(engagement.outstandingCount ?? 0) > 0
                  ? ` · ${engagement.outstandingCount} to answer`
                  : ''}
              </Text>
            </View>
            <Text color="muted">›</Text>
          </Pressable>
        ))}
      </Card>
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
  sectionCard: {
    gap: 0,
  },
  attentionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: spacing.sm,
  },
  attentionText: {
    flex: 1,
    gap: 2,
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

/**
 * Who has joined lately (Slice 10's "recent activity", D-042). Until push
 * exists, an organiser has no way of learning that an invite was accepted, so
 * the fact is surfaced where they already look rather than announced.
 */
function RecentJoins({
  members,
  onSeeAll,
}: {
  members: Member[];
  onSeeAll: () => void;
}) {
  const cutoff = Date.now() - RECENT_JOIN_WINDOW_MS;

  const recent = members
    .filter((member) => !member.isYou)
    .filter((member) => Date.parse(member.joinedAtUtc) >= cutoff)
    .sort((a, b) => Date.parse(b.joinedAtUtc) - Date.parse(a.joinedAtUtc))
    .slice(0, 3);

  if (recent.length === 0) {
    return null;
  }

  return (
    <View style={styles.attention}>
      <View style={styles.attentionHeader}>
        <View style={styles.attentionIcon}>
          <Text style={styles.attentionEmoji}>👋</Text>
        </View>
        <Text variant="subheading">Recently joined</Text>
      </View>

      {recent.map((member) => (
        <Text key={member.membershipId} variant="bodySmall">
          {member.displayName ?? 'Someone'}
          <Text color="muted" variant="bodySmall">
            {' · '}
            {joinedLabel(member.joinedAtUtc)}
          </Text>
        </Text>
      ))}

      <Button label="See everyone" variant="ghost" onPress={onSeeAll} />
    </View>
  );
}

/** How long a join stays worth pointing out on Home. */
const RECENT_JOIN_WINDOW_MS = 14 * 24 * 60 * 60 * 1000;

function joinedLabel(joinedAtUtc: string): string {
  const days = Math.floor(
    (Date.now() - Date.parse(joinedAtUtc)) / (24 * 60 * 60 * 1000),
  );

  if (days <= 0) {
    return 'joined today';
  }
  if (days === 1) {
    return 'joined yesterday';
  }
  return `joined ${days} days ago`;
}
