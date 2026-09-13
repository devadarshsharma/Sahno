import { Ionicons } from '@expo/vector-icons';
import type { ComponentProps } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
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
import { useState } from 'react';

import type { Engagement } from '@/api/engagements';
import type { Member } from '@/api/members';
import type { ReadinessItem } from '@/api/readiness';
import { dismissSetupChecklist } from '@/api/organisations';
import { SahnoSymbol } from '@/components/brand';
import { EngagementCard } from '@/components/engagement-card';
import { Button, Card, Screen, Text } from '@/components/ui';
import { firstNameOf, useMe, useNeedsDisplayName } from '@/hooks/use-me';
import {
  needsAttention,
  useEngagements,
  type BookingsFilter,
} from '@/hooks/use-engagements';
import { useMembers } from '@/hooks/use-members';
import { useUnreadCount } from '@/hooks/use-notifications';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { useSeenPeople } from '@/stores/seen-people';
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
  const unreadQuery = useUnreadCount();

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
    unreadQuery.refetch();
  }

  const openEngagement = (engagementId: string) =>
    router.push({
      pathname: '/engagement/[engagementId]',
      params: { engagementId },
    });

  // A tile is a count; tapping it opens the list it counts.
  const openBookings = (filter: BookingsFilter) =>
    router.push({ pathname: '/(tabs)/events', params: { filter } });

  const engagements = engagementsQuery.data ?? [];
  // No data yet (first load, or a reload). Zeros and "Nothing on yet" would
  // be a lie for the second it takes; the tiles show a dash and the body a
  // spinner instead.
  const loadingBookings = engagementsQuery.isPending;

  // The pipeline as D-041 ranks it, kept as separate lists rather than one
  // "live" pile: provisionally on and actually booked mean different things to
  // a group deciding whether to take other work.
  const byStatus = (status: Engagement['status']) =>
    engagements.filter((engagement) => engagement.status === status);

  const confirmed = byStatus('Confirmed');
  const tentative = byStatus('Tentative');
  const enquiries = byStatus('Draft');
  // Unresolved work, which D-041 puts first. Four things qualify:
  //   - somebody has still to answer;
  //   - everyone has answered and the lineup is now the organiser's call,
  //     because Sahno never advances to Tentative on its own (D-026);
  //   - it is postponed, so it needs a new date or a decision to resume;
  //   - it is on, but critical detail is missing (Slice 7, D-048);
  //   - it is over, and somebody is still owed money (Slice 11, D-008).
  // A booking here also stays in its diary section below — that is where it
  // IS, and this is what to DO. The two lists are different shapes so the
  // same booking never appears as two cards.
  const waiting = engagements.filter(needsAttention);

  // With every section hiding itself when empty, a quiet organisation would
  // show a bare screen. One honest line is better than five "nothing yet"
  // cards or an empty page.
  const organiserHasNothing =
    !loadingBookings &&
    waiting.length === 0 &&
    confirmed.length === 0 &&
    tentative.length === 0 &&
    enquiries.length === 0;

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

  const memberHasNothing =
    !loadingBookings &&
    needsYourAnswer.length === 0 && yourEvents.length === 0;

  return (
    <View style={styles.screen}>
      <StatusBar style="light" />
      {/* The page scrolls under this strip rather than under the clock. */}
      <View
        pointerEvents="none"
        style={[styles.statusStrip, { height: insets.top }]}
      />
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
          <View style={styles.heroControls}>
            <Bell
              unread={unreadQuery.data ?? 0}
              onPress={() => router.push('/notifications')}
            />
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
          </View>

          <Text style={styles.greeting}>
            {greetingForNow()}
            {firstName ? `, ${firstName}` : ''}
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
            <StatTile
              value={loadingBookings ? '–' : String(waiting.length)}
              label="Needs attention"
              onPress={() => openBookings('attention')}
            />
            <StatTile
              value={loadingBookings ? '–' : String(confirmed.length)}
              label="Confirmed"
              onPress={() => openBookings('Confirmed')}
            />
            <StatTile
              value={loadingBookings ? '–' : String(tentative.length)}
              label="Tentative"
              onPress={() => openBookings('Tentative')}
            />
            <StatTile
              value={loadingBookings ? '–' : String(enquiries.length)}
              label="Enquiries"
              onPress={() => openBookings('Draft')}
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

          {loadingBookings ? (
            <ActivityIndicator color={colors.tealText} style={styles.bodySpinner} />
          ) : null}

          {isOrganiser ? (
            <>
              {waiting.length > 0 ? (
              <View style={styles.attention}>
                <View style={styles.attentionHeader}>
                  <View style={styles.attentionBell}>
                    <Ionicons name="notifications" size={18} color={colors.offWhite} />
                  </View>
                  <Text variant="subheading" style={styles.attentionHeading}>
                    Needs your attention
                  </Text>
                  <Pressable
                    accessibilityRole="button"
                    accessibilityLabel="View all bookings needing attention"
                    hitSlop={8}
                    onPress={() => openBookings('attention')}
                    style={styles.viewAll}
                  >
                    <Text variant="bodySmall" color="accent">
                      View all
                    </Text>
                    <Ionicons name="chevron-forward" size={14} color={colors.tealText} />
                  </Pressable>
                </View>

                <View style={styles.attentionList}>
                  {waiting.map((engagement) => (
                    <AttentionRow
                      key={engagement.id}
                      engagement={engagement}
                      onPress={() => openEngagement(engagement.id)}
                    />
                  ))}
                </View>
              </View>
              ) : null}

              {organiserHasNothing ? (
                <HomeSection title="Nothing on yet">
                  Start an enquiry from the Bookings tab — a title is all you
                  need, and everything else can come later.
                </HomeSection>
              ) : null}

              {/* D-041 order: exceptions first, then confirmed, tentative,
                  new enquiries, and recent activity last. */}
              <EngagementList
                title="Upcoming confirmed bookings"
                engagements={confirmed}
                isOrganiser={isOrganiser}
                onOpen={openEngagement}
              />

              <EngagementList
                title="Tentative bookings"
                engagements={tentative}
                isOrganiser={isOrganiser}
                onOpen={openEngagement}
              />

              <EngagementList
                title="New enquiries"
                engagements={enquiries}
                isOrganiser={isOrganiser}
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
              {memberHasNothing ? (
                <HomeSection title="Nothing on yet">
                  Your events appear here as soon as an organiser asks whether
                  you are available.
                </HomeSection>
              ) : null}

              {/* D-040 order: what must I do, then where do I need to be. */}
              <EngagementList
                title="Needs your response"
                engagements={needsYourAnswer}
                isOrganiser={isOrganiser}
                onOpen={openEngagement}
              />
              <EngagementList
                title="Next confirmed event"
                engagements={nextConfirmed}
                isOrganiser={isOrganiser}
                onOpen={openEngagement}
              />
              <EngagementList
                title="Tentative events"
                engagements={yourTentative}
                isOrganiser={isOrganiser}
                onOpen={openEngagement}
              />
              <EngagementList
                title="Later events"
                engagements={laterEvents}
                isOrganiser={isOrganiser}
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

type AttentionIcon = ComponentProps<typeof Ionicons>['name'];

/** What is missing, said as the thing to do, with the icon that kind of thing gets. */
const MISSING_ACTIONS: Record<ReadinessItem, { action: string; icon: AttentionIcon }> = {
  Lineup: { action: 'Lineup not settled', icon: 'people-outline' },
  Venue: { action: 'Venue not set', icon: 'location-outline' },
  CallTime: { action: 'Sound-check time not set', icon: 'mic-outline' },
  StartTime: { action: 'Start time not set', icon: 'time-outline' },
  Responsibilities: { action: 'Jobs not handed out', icon: 'clipboard-outline' },
  Dress: { action: 'Dress not decided', icon: 'shirt-outline' },
  Rehearsal: { action: 'No rehearsal booked', icon: 'musical-notes-outline' },
  Resources: { action: 'Nothing attached yet', icon: 'folder-open-outline' },
};

/**
 * What to DO about this booking, in the order things usually need doing —
 * with the icon for that kind of doing. Written as an action rather than a
 * description, because this list is a to-do list; the diary below already
 * says what each booking is.
 */
function attentionAction(engagement: Engagement): { action: string; icon: AttentionIcon } {
  const waiting = engagement.outstandingCount ?? 0;
  if (waiting > 0) {
    return {
      action: waiting === 1 ? "1 member hasn't responded" : `${waiting} members haven't responded`,
      icon: 'people-outline',
    };
  }

  if (engagement.status === 'CheckingAvailability') {
    return { action: 'Everyone answered — decide the lineup', icon: 'checkmark-circle-outline' };
  }

  if (engagement.status === 'Postponed') {
    return engagement.startDate === null
      ? { action: 'New date needed', icon: 'calendar-outline' }
      : { action: 'Resume when ready', icon: 'play-circle-outline' };
  }

  const owed = engagement.financeOutstanding ?? 0;
  if (engagement.status === 'Completed' && owed > 0) {
    return {
      action: owed === 1 ? '1 payment still owed' : `${owed} payments still owed`,
      icon: 'cash-outline',
    };
  }

  const missing = engagement.readinessMissing ?? [];
  if (missing.length > 0) {
    const first = MISSING_ACTIONS[missing[0]];
    return {
      action: missing.length === 1 ? first.action : `${first.action} · ${missing.length - 1} more`,
      icon: first.icon,
    };
  }

  return { action: 'Needs a look', icon: 'alert-circle-outline' };
}

/**
 * One line per booking in Needs attention: the thing to do, then which
 * booking. No card, no chips, no venue — that is all in the diary below, and
 * repeating it here would have every booking on the screen twice.
 */
function AttentionRow({
  engagement,
  onPress,
}: {
  engagement: Engagement;
  onPress: () => void;
}) {
  const { action, icon } = attentionAction(engagement);
  const where = `${engagement.title} (${shortDate(engagement)})`;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${action}. ${where}.`}
      onPress={onPress}
      style={({ pressed }) => [
        styles.attentionRow,
        pressed ? styles.attentionRowPressed : null,
      ]}
    >
      <View style={styles.attentionIconWell}>
        <Ionicons name={icon} size={20} color={colors.text.primary} />
      </View>
      <View style={styles.attentionText}>
        <Text numberOfLines={1} style={styles.attentionTitle}>
          {action}
        </Text>
        <Text variant="bodySmall" color="secondary" numberOfLines={1}>
          {where}
        </Text>
      </View>
      <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
    </Pressable>
  );
}

/** "22 Jun" — enough to tell two bookings apart on one line. */
function shortDate(engagement: Engagement): string {
  if (engagement.startDate === null) {
    return 'date TBC';
  }
  return new Date(`${engagement.startDate}T00:00:00`).toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
  });
}
function EngagementList({
  title,
  engagements,
  isOrganiser,
  onOpen,
}: {
  title: string;
  engagements: Engagement[];
  isOrganiser: boolean;
  onOpen: (engagementId: string) => void;
}) {
  // A section with nothing in it says nothing. Home is read at a glance, and
  // five "nothing yet" cards make the one that does matter harder to find.
  if (engagements.length === 0) {
    return null;
  }

  return (
    <View style={styles.section}>
      <Text variant="subheading">{title}</Text>
      <View style={styles.cards}>
        {engagements.map((engagement) => (
          <EngagementCard
            key={engagement.id}
            engagement={engagement}
            isOrganiser={isOrganiser}
            onPress={() => onOpen(engagement.id)}
          />
        ))}
      </View>
    </View>
  );
}
/**
 * The bell (D-042): in the top bar rather than a tab, with the unread count on
 * it. The number is what people look at first when they open the app, so it
 * sits where the eye lands.
 */
function Bell({ unread, onPress }: { unread: number; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={
        unread === 0
          ? 'Notifications'
          : `Notifications, ${unread} unread`
      }
      onPress={onPress}
      style={({ pressed }) => [
        styles.bell,
        pressed ? styles.orgPillPressed : null,
      ]}
    >
      <Ionicons name="notifications-outline" size={22} color={colors.offWhite} />
      {unread > 0 ? (
        <View style={styles.bellBadge}>
          <Text style={styles.bellBadgeText}>
            {unread > 9 ? '9+' : String(unread)}
          </Text>
        </View>
      ) : null}
    </Pressable>
  );
}

function StatTile({
  value,
  label,
  onPress,
}: {
  value: string;
  label: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${value} ${label}. Open the list.`}
      onPress={onPress}
      style={({ pressed }) => [styles.statTile, pressed ? styles.statTilePressed : null]}
    >
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </Pressable>
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
  statusStrip: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    backgroundColor: colors.navy,
    zIndex: 1,
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
  heroControls: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    flexShrink: 1,
  },
  bell: {
    width: 40,
    height: 40,
    borderRadius: radii.full,
    backgroundColor: 'rgba(250, 247, 242, 0.12)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  bellBadge: {
    position: 'absolute',
    top: -2,
    right: -2,
    minWidth: 18,
    height: 18,
    paddingHorizontal: 4,
    borderRadius: 9,
    backgroundColor: colors.orange,
    alignItems: 'center',
    justifyContent: 'center',
  },
  bellBadgeText: {
    fontFamily: fontFamilies.bold,
    fontSize: 11,
    lineHeight: 14,
    color: colors.offWhite,
  },
  orgPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    maxWidth: 150,
    flexShrink: 1,
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
  statTilePressed: {
    backgroundColor: colors.surface.subtle,
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
  bodySpinner: {
    marginVertical: spacing.xl,
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
    padding: spacing.md,
    gap: spacing.md,
  },
  attentionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  attentionBell: {
    width: 34,
    height: 34,
    borderRadius: 17,
    backgroundColor: colors.orange,
    alignItems: 'center',
    justifyContent: 'center',
  },
  attentionHeading: {
    flex: 1,
  },
  viewAll: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
  },
  attentionIconWell: {
    width: 36,
    height: 36,
    borderRadius: radii.md,
    backgroundColor: colors.surface.subtle,
    alignItems: 'center',
    justifyContent: 'center',
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
  attentionList: {
    backgroundColor: colors.surface.raised,
    borderRadius: radii.md,
    paddingHorizontal: spacing.md,
  },
  attentionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border.default,
  },
  attentionRowPressed: {
    opacity: 0.7,
  },
  attentionText: {
    flex: 1,
    gap: 2,
  },
  attentionTitle: {
    fontFamily: fontFamilies.uiMedium,
  },
  section: {
    gap: spacing.sm,
  },
  cards: {
    gap: spacing.md,
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
  const seenAtUtc = useSeenPeople((state) => state.seenAtUtc);

  // Anything since they last looked at People, within the outer window. Once
  // they have looked, the notice has done its job and goes.
  // Fixed at mount: a clock read during render would give a cutoff that moves
  // every re-render, which React treats as impure and which could drop a row
  // mid-scroll.
  const [now] = useState(() => Date.now());
  const cutoff = Math.max(
    now - RECENT_JOIN_WINDOW_MS,
    seenAtUtc ? Date.parse(seenAtUtc) : 0,
  );

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
          <Ionicons name="person-add-outline" size={18} color={colors.text.primary} />
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
