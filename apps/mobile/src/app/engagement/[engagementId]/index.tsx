import { Ionicons } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import type { ComponentProps } from 'react';
import { ActivityIndicator, Pressable, Share, StyleSheet, View } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { AddToCalendarCard } from '@/components/calendar-card';
import { formatClock, StatusChip } from '@/components/engagement-card';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useAvailability, useOwnAvailability } from '@/hooks/use-availability';
import { useEngagementCustomer, useFinancialAccess } from '@/hooks/use-commercial';
import { useDiscussion } from '@/hooks/use-discussion';
import { formatEngagementDate, useEngagements } from '@/hooks/use-engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import {
  useRehearsals,
  useResources,
  useResponsibilities,
} from '@/hooks/use-preparation';
import { useReadiness } from '@/hooks/use-readiness';
import { useSetList } from '@/hooks/use-repertoire';
import { colors, fontFamilies, radii, spacing } from '@/theme';

type IconName = ComponentProps<typeof Ionicons>['name'];

/**
 * The event workspace as an index (D-047). The header says what and when; a
 * short strip says what a performer needs on the day; then one row per
 * section with a live one-line summary, each opening its own page.
 *
 * It replaced a twelve-card scroll. The cards are all still here — one screen
 * each, under the rows — but nobody has to pass the money card to reach the
 * chat any more.
 */
export default function EngagementOverview() {
  const { engagementId } = useLocalSearchParams<{ engagementId: string }>();
  const router = useRouter();
  const { active } = useActiveOrg();
  const engagementsQuery = useEngagements();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';
  const hasFinance = useFinancialAccess();
  // Off for members (the hook gates it), so this never fetches for them.
  const customerName = useEngagementCustomer(engagementId).data?.customer?.name ?? null;

  const engagement = engagementsQuery.data?.find((row) => row.id === engagementId);

  // Summaries for the rows. Every one of these is cached and shared with the
  // section screen it summarises, so opening a section costs nothing extra.
  const lineup = useAvailability(engagementId);
  const own = useOwnAvailability(engagementId);
  const jobs = useResponsibilities(engagementId);
  const rehearsals = useRehearsals(engagementId);
  const resources = useResources(engagementId);
  const setList = useSetList(engagementId);
  const thread = useDiscussion(engagementId);
  const readiness = useReadiness(engagementId);

  if (engagementsQuery.isPending) {
    return (
      <Screen hero={{ title: 'Loading' }}>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (!engagement) {
    return (
      <Screen hero={{ title: 'Not found' }}>
        <View style={styles.centered}>
          <Text color="secondary" style={styles.centeredText}>
            This booking may have been discarded.
          </Text>
        </View>
      </Screen>
    );
  }

  const go = (section: string) =>
    router.push({
      pathname: `/engagement/[engagementId]/${section}` as never,
      params: { engagementId: engagement.id },
    });

  const when = engagement.startTime
    ? `${formatEngagementDate(engagement)} · ${formatClock(engagement.startTime)}`
    : formatEngagementDate(engagement);

  // Row summaries, each in the words a person would use.
  const peopleSummary = isOrganiser
    ? lineup.data
      ? lineup.data.summary.selected === 0
        ? 'Nobody asked yet'
        : `${lineup.data.summary.selected - lineup.data.summary.outstanding} of ${lineup.data.summary.selected} answered`
      : undefined
    : own.data
      ? own.data.response === null
        ? 'You have not answered'
        : `You said ${own.data.response.toLowerCase()}`
      : undefined;

  const jobsSummary = jobs.data
    ? jobs.data.length === 0
      ? 'Nothing written down yet'
      : `${jobs.data.length} job${jobs.data.length === 1 ? '' : 's'} · ${jobs.data.filter((job) => job.isDone).length} done`
    : undefined;

  const filesSummary =
    rehearsals.data && resources.data
      ? [
          !setList.data || setList.data.length === 0
            ? null
            : `${setList.data.length} on the set list`,
          rehearsals.data.length === 0
            ? null
            : `${rehearsals.data.length} rehearsal${rehearsals.data.length === 1 ? '' : 's'}`,
          resources.data.length === 0
            ? null
            : `${resources.data.length} attached`,
        ]
          .filter(Boolean)
          .join(' · ') || 'Nothing yet'
      : undefined;

  const chatSummary = thread.data
    ? thread.data.length === 0
      ? 'Nothing said yet'
      : `${thread.data.length} message${thread.data.length === 1 ? '' : 's'}`
    : undefined;

  const readinessOutstanding = readiness.data?.filter((entry) => entry.state === 'Outstanding').length;
  const readinessSummary =
    readinessOutstanding === undefined
      ? undefined
      : readinessOutstanding === 0
        ? 'Ready to go'
        : `${readinessOutstanding} to sort out`;

  const owed = engagement.financeOutstanding ?? 0;

  return (
    <Screen
      scroll
      onRefresh={() => engagementsQuery.refetch()}
      refreshing={engagementsQuery.isRefetching}
      hero={{
        title: engagement.title,
        subtitle: engagement.venue ? `${when} · ${engagement.venue}` : when,
        // Organisers tap the chip to move the booking on; for everyone else
        // it is just the fact.
        right: isOrganiser ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Status: . Change.`}
            onPress={() => go('status')}
            hitSlop={8}
            style={({ pressed }) => [styles.statusButton, pressed ? styles.rowPressed : null]}
          >
            <StatusChip status={engagement.status} />
            <Ionicons name="chevron-down" size={14} color={colors.offWhite} />
          </Pressable>
        ) : (
          <StatusChip status={engagement.status} />
        ),
      }}
    >
      {!engagement.isSharedWithMembers ? (
        <Card style={styles.notice}>
          <Text variant="bodySmall" color="secondary">
            Still private. Nobody else sees it until you ask for availability.
          </Text>
        </Card>
      ) : null}

      {/* The day-of strip: what a performer opens the booking to find out. */}
      <View style={styles.strip}>
        <Fact icon="mic-outline" label="Be there" value={engagement.callTime ? formatClock(engagement.callTime) : '—'} />
        <Fact icon="time-outline" label="Starts" value={engagement.startTime ? formatClock(engagement.startTime) : '—'} />
        <Fact icon="shirt-outline" label="Wear" value={engagement.dressNotes ?? '—'} />
      </View>

      <Card style={styles.menu}>
        <Row icon="information-circle-outline" title="Details" summary="Date, venue, times, dress" onPress={() => go('details')} />
        <Row icon="people-outline" title="People" summary={peopleSummary} onPress={() => go('people')} />
        <Row icon="clipboard-outline" title="Jobs" summary={jobsSummary} onPress={() => go('jobs')} />
        <Row icon="folder-open-outline" title="Set list & rehearsals" summary={filesSummary} onPress={() => go('files')} />
        <Row icon="chatbubble-outline" title="Chat" summary={chatSummary} onPress={() => go('chat')} />
        {isOrganiser ? (
          <Row
            icon="checkmark-circle-outline"
            title="Readiness"
            summary={readinessSummary}
            tone={readinessOutstanding ? 'attention' : 'default'}
            onPress={() => go('readiness')}
          />
        ) : null}
        {isOrganiser ? (
          <Row
            icon="cash-outline"
            title={hasFinance ? 'Customer & money' : 'Customer'}
            summary={
              hasFinance && owed > 0
                ? `${owed} still owed`
                : customerName
                  ? `For ${customerName}`
                  : hasFinance
                    ? 'Fees, deposit, performer payments'
                    : 'Who this is for'
            }
            tone={hasFinance && owed > 0 ? 'attention' : 'default'}
            onPress={() => go('money')}
          />
        ) : null}
        {isOrganiser ? (
          <Row
            icon="time-outline"
            title="History"
            summary="Every change, who made it, and when"
            onPress={() => go('admin')}
            last
          />
        ) : null}
      </Card>

      <AddToCalendarCard
        engagement={engagement}
        organisationName={active?.name ?? 'your organisation'}
      />

      <View style={styles.actions}>
        <Button
          label="Share"
          variant="secondary"
          style={styles.action}
          onPress={() => shareDayOf(engagement, active?.name ?? 'Sahno')}
        />
        {isOrganiser ? (
          <Button label="Edit details" style={styles.action} onPress={() => go('details')} />
        ) : null}
      </View>
    </Screen>
  );
}

/**
 * The participant-facing essentials as plain text, for the share sheet. Only
 * what a member is allowed to see (D-022) — nothing here that the day-of card
 * would not show them.
 */
function shareDayOf(engagement: Engagement, organisationName: string) {
  const lines = [
    `${engagement.title} — ${organisationName}`,
    formatEngagementDate(engagement),
    engagement.venue ? `Where: ${engagement.venue}` : null,
    engagement.callTime ? `Be there: ${formatClock(engagement.callTime)}` : null,
    engagement.startTime ? `Starts: ${formatClock(engagement.startTime)}` : null,
    engagement.dressNotes ? `Wear: ${engagement.dressNotes}` : null,
  ].filter((line): line is string => line !== null);

  return Share.share({ message: lines.join('\n') }).catch(() => undefined);
}

function Fact({ icon, label, value }: { icon: IconName; label: string; value: string }) {
  return (
    <View style={styles.fact}>
      <Ionicons name={icon} size={16} color={colors.text.muted} />
      <View style={styles.factText}>
        <Text style={styles.factValue} numberOfLines={1}>
          {value}
        </Text>
        <Text style={styles.factLabel}>{label}</Text>
      </View>
    </View>
  );
}

function Row({
  icon,
  title,
  summary,
  tone = 'default',
  onPress,
  last = false,
}: {
  icon: IconName;
  title: string;
  summary?: string;
  tone?: 'default' | 'attention';
  onPress: () => void;
  last?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={summary ? `${title}. ${summary}.` : title}
      onPress={onPress}
      style={({ pressed }) => [
        styles.row,
        last ? styles.rowLast : null,
        pressed ? styles.rowPressed : null,
      ]}
    >
      <View style={[styles.rowIcon, tone === 'attention' ? styles.rowIconAttention : null]}>
        <Ionicons
          name={icon}
          size={18}
          color={tone === 'attention' ? '#8A4A05' : colors.tealText}
        />
      </View>
      <View style={styles.rowText}>
        <Text style={styles.rowTitle}>{title}</Text>
        <Text variant="caption" color="secondary" numberOfLines={1}>
          {summary ?? ' '}
        </Text>
      </View>
      <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
    </Pressable>
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
  notice: {
    marginBottom: spacing.md,
  },
  strip: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.md,
  },
  fact: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.sm,
    borderRadius: radii.md,
    backgroundColor: colors.surface.raised,
    borderWidth: 1,
    borderColor: colors.border.default,
  },
  factText: {
    flex: 1,
  },
  factValue: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 13,
    lineHeight: 17,
    color: colors.text.primary,
  },
  factLabel: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 10,
    lineHeight: 13,
    color: colors.text.muted,
  },
  menu: {
    gap: 0,
    paddingVertical: 0,
    marginBottom: spacing.lg,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border.default,
  },
  rowLast: {
    borderBottomWidth: 0,
  },
  rowPressed: {
    opacity: 0.7,
  },
  statusButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  rowIcon: {
    width: 36,
    height: 36,
    borderRadius: radii.md,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  rowIconAttention: {
    backgroundColor: colors.orangeSoft,
  },
  rowText: {
    flex: 1,
    gap: 1,
  },
  rowTitle: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 15,
    lineHeight: 20,
    color: colors.text.primary,
  },
  actions: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginTop: spacing.sm,
  },
  action: {
    flex: 1,
  },
});
