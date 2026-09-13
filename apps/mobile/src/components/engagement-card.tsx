import { Pressable, StyleSheet, View } from 'react-native';

import type { Engagement, EngagementStatus } from '@/api/engagements';
import { Text } from '@/components/ui';
import { formatEngagementDate } from '@/hooks/use-engagements';
import { colors, fontFamilies, radii, shadows, spacing } from '@/theme';

/**
 * One booking as a card: what it is, when, where, and three numbers that say
 * how ready it is. Built from what the list already carries, so it costs no
 * extra request per row.
 *
 * The same card serves organisers and members, but the chips differ because
 * the questions differ. An organiser wants to know how many have answered and
 * how much is still to sort out; a member wants to know what they said and
 * when to turn up.
 */
export function EngagementCard({
  engagement,
  isOrganiser,
  onPress,
  reason,
}: {
  engagement: Engagement;
  isOrganiser: boolean;
  onPress: () => void;
  /**
   * When the card sits in Needs attention, the reason it is there. Shown in
   * place of the venue line, because it is the more urgent of the two.
   */
  reason?: string;
}) {
  const when = formatWhen(engagement);

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${engagement.title}. ${when}.${reason ? ` ${reason}.` : ''}`}
      onPress={onPress}
      style={({ pressed }) => [styles.card, pressed ? styles.cardPressed : null]}
    >
      <View style={styles.head}>
        <Text style={styles.title} numberOfLines={2}>
          {engagement.title}
        </Text>
        <StatusChip status={engagement.status} />
      </View>

      <View style={styles.lines}>
        <Line icon="📅" text={when} />
        {reason ? (
          <Line icon="🔔" text={reason} tone="attention" />
        ) : engagement.venue ? (
          <Line icon="📍" text={engagement.venue} />
        ) : null}
      </View>

      <View style={styles.stats}>
        {isOrganiser ? (
          <OrganiserStats engagement={engagement} />
        ) : (
          <MemberStats engagement={engagement} />
        )}
      </View>
    </Pressable>
  );
}

function OrganiserStats({ engagement }: { engagement: Engagement }) {
  const selected = engagement.selectedCount ?? 0;
  const outstanding = engagement.outstandingCount ?? 0;
  const answered = selected - outstanding;
  const toSort = engagement.readinessOutstanding ?? 0;
  const owed = engagement.financeOutstanding ?? 0;

  return (
    <>
      <Stat
        icon="👥"
        value={selected === 0 ? 'Nobody' : `${answered}/${selected}`}
        label={selected === 0 ? 'asked yet' : 'answered'}
        tone={selected > 0 && outstanding > 0 ? 'attention' : 'default'}
      />
      <Stat
        icon="🎚️"
        value={engagement.callTime ? formatClock(engagement.callTime) : '—'}
        label="Sound-check"
      />
      {engagement.status === 'Completed' && owed > 0 ? (
        <Stat icon="💸" value={String(owed)} label="still owed" tone="attention" />
      ) : (
        <Stat
          icon="☑️"
          value={toSort === 0 ? 'Ready' : String(toSort)}
          label={toSort === 0 ? 'to go' : 'to sort out'}
          tone={toSort > 0 ? 'attention' : 'default'}
        />
      )}
    </>
  );
}

function MemberStats({ engagement }: { engagement: Engagement }) {
  return (
    <>
      <Stat
        icon="🙋"
        value={
          engagement.yourResponse === null
            ? 'Not yet'
            : engagement.yourResponse === 'Available'
              ? 'Available'
              : engagement.yourResponse === 'Maybe'
                ? 'Maybe'
                : 'Not available'
        }
        label="your answer"
        tone={engagement.yourResponse === null ? 'attention' : 'default'}
      />
      <Stat
        icon="🎚️"
        value={engagement.callTime ? formatClock(engagement.callTime) : '—'}
        label="Be there"
      />
      <Stat
        icon="🎤"
        value={engagement.startTime ? formatClock(engagement.startTime) : '—'}
        label="Starts"
      />
    </>
  );
}

function Stat({
  icon,
  value,
  label,
  tone = 'default',
}: {
  icon: string;
  value: string;
  label: string;
  tone?: 'default' | 'attention';
}) {
  return (
    <View style={[styles.stat, tone === 'attention' ? styles.statAttention : null]}>
      <Text style={styles.statIcon}>{icon}</Text>
      <View style={styles.statText}>
        <Text style={styles.statValue} numberOfLines={1}>
          {value}
        </Text>
        <Text style={styles.statLabel} numberOfLines={1}>
          {label}
        </Text>
      </View>
    </View>
  );
}

function Line({
  icon,
  text,
  tone = 'default',
}: {
  icon: string;
  text: string;
  tone?: 'default' | 'attention';
}) {
  return (
    <View style={styles.line}>
      <Text style={styles.lineIcon}>{icon}</Text>
      <Text
        variant="bodySmall"
        color={tone === 'attention' ? 'primary' : 'secondary'}
        style={tone === 'attention' ? styles.lineAttention : null}
        numberOfLines={1}
      >
        {text}
      </Text>
    </View>
  );
}

export function StatusChip({ status }: { status: EngagementStatus }) {
  return (
    <View style={[styles.chip, CHIP_STYLE[status]]}>
      <Text variant="caption" style={[styles.chipText, CHIP_TEXT[status]]}>
        {CHIP_LABEL[status]}
      </Text>
    </View>
  );
}

const CHIP_LABEL: Record<EngagementStatus, string> = {
  Draft: 'Enquiry',
  CheckingAvailability: 'Awaiting',
  Tentative: 'Tentative',
  Confirmed: 'Confirmed',
  Postponed: 'Postponed',
  Completed: 'Done',
  Cancelled: 'Cancelled',
};

/** Date and time on one line: "Sat, 30 Aug 2026 · 7:00 PM". */
function formatWhen(engagement: Engagement): string {
  const date = formatEngagementDate(engagement);
  return engagement.startTime ? `${date} · ${formatClock(engagement.startTime)}` : date;
}

/** "19:30:00" → "7:30 PM". */
export function formatClock(time: string): string {
  const [hours, minutes] = time.split(':').map(Number);
  const suffix = hours >= 12 ? 'PM' : 'AM';
  const twelve = hours % 12 === 0 ? 12 : hours % 12;
  return `${twelve}:${String(minutes).padStart(2, '0')} ${suffix}`;
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface.raised,
    borderRadius: radii.lg,
    padding: spacing.lg,
    gap: spacing.md,
    ...shadows.md,
  },
  cardPressed: {
    opacity: 0.85,
  },
  head: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  title: {
    flex: 1,
    fontFamily: fontFamilies.bold,
    fontSize: 18,
    lineHeight: 24,
    color: colors.text.primary,
  },
  lines: {
    gap: spacing.xs,
  },
  line: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  lineIcon: {
    fontSize: 13,
    lineHeight: 18,
    width: 18,
    textAlign: 'center',
  },
  lineAttention: {
    fontFamily: fontFamilies.uiMedium,
  },
  stats: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  stat: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.sm,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.default,
    backgroundColor: colors.surface.canvas,
  },
  statAttention: {
    borderColor: colors.orangeSoft,
    backgroundColor: colors.orangeSoft,
  },
  statIcon: {
    fontSize: 14,
    lineHeight: 18,
  },
  statText: {
    flex: 1,
  },
  statValue: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 12,
    lineHeight: 16,
    color: colors.text.primary,
  },
  statLabel: {
    fontFamily: fontFamilies.uiRegular,
    fontSize: 10,
    lineHeight: 13,
    color: colors.text.muted,
  },
  chip: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 3,
    borderRadius: radii.full,
    backgroundColor: colors.surface.subtle,
    flexShrink: 0,
  },
  chipText: {
    fontFamily: fontFamilies.uiMedium,
    color: colors.text.secondary,
  },
  chipConfirmed: { backgroundColor: colors.tealSoft },
  chipConfirmedText: { color: colors.tealText },
  chipWarm: { backgroundColor: colors.orangeSoft },
  chipWarmText: { color: '#8A4A05' },
  chipQuiet: { backgroundColor: colors.surface.subtle },
  chipQuietText: { color: colors.text.muted },
});

const CHIP_STYLE: Record<EngagementStatus, object> = {
  Draft: styles.chipQuiet,
  CheckingAvailability: styles.chipWarm,
  Tentative: styles.chipWarm,
  Confirmed: styles.chipConfirmed,
  Postponed: styles.chipWarm,
  Completed: styles.chipQuiet,
  Cancelled: styles.chipQuiet,
};

const CHIP_TEXT: Record<EngagementStatus, object> = {
  Draft: styles.chipQuietText,
  CheckingAvailability: styles.chipWarmText,
  Tentative: styles.chipWarmText,
  Confirmed: styles.chipConfirmedText,
  Postponed: styles.chipWarmText,
  Completed: styles.chipQuietText,
  Cancelled: styles.chipQuietText,
};
