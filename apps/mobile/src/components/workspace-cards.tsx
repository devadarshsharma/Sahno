import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { updateEngagement } from '@/api/engagements';
import {
  READINESS_LABELS,
  setReadiness,
  type ReadinessEntry,
  type ReadinessItem,
} from '@/api/readiness';
import {
  Card,
  Text,
  TextInput,
  TimeField,
  formatTime,
} from '@/components/ui';
import {
  formatEngagementDate,
  useEngagementMutation,
} from '@/hooks/use-engagements';
import { useReadiness, useReadinessMutation } from '@/hooks/use-readiness';
import { colors, spacing } from '@/theme';

/**
 * What a participant needs on the day, in the order they need it (Slice 7):
 * where, when to arrive, when it starts, what to wear. Arrival comes before
 * the start time because turning up on time is the thing they can get wrong.
 */
export function DayOfCard({ engagement }: { engagement: Engagement }) {
  const rows: { label: string; value: string | null }[] = [
    { label: 'Where', value: engagement.venue },
    {
      label: 'Be there',
      value: engagement.callTime ? formatTime(engagement.callTime) : null,
    },
    {
      label: 'Starts',
      value: engagement.startTime ? formatTime(engagement.startTime) : null,
    },
    { label: 'Wear', value: engagement.dressNotes },
  ];

  const known = rows.filter((row) => row.value !== null);

  return (
    <Card style={styles.card}>
      <Text variant="subheading">On the day</Text>
      <Text color="secondary" variant="bodySmall">
        {formatEngagementDate(engagement)}
      </Text>

      {known.length === 0 ? (
        <Text color="muted" variant="bodySmall">
          The details are still being worked out. They will appear here.
        </Text>
      ) : (
        <View style={styles.rows}>
          {rows.map((row) => (
            <View key={row.label} style={styles.row}>
              <Text variant="caption" color="muted" style={styles.rowLabel}>
                {row.label}
              </Text>
              <Text
                variant="bodySmall"
                color={row.value ? 'primary' : 'muted'}
                style={styles.rowValue}
              >
                {row.value ?? 'To be confirmed'}
              </Text>
            </View>
          ))}
        </View>
      )}
    </Card>
  );
}

/** The organiser's editable version of the same details. */
export function DetailsCard({ engagement }: { engagement: Engagement }) {
  const [dress, setDress] = useState(engagement.dressNotes ?? '');
  const [venue, setVenue] = useState(engagement.venue ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = useEngagementMutation<{
    callTime?: string | null;
    startTime?: string | null;
    dressNotes?: string | null;
    venue?: string | null;
  }>((accessToken, organisationId, update) =>
    updateEngagement(accessToken, organisationId, engagement.id, {
      // Every field is sent together: the API replaces what it is given, so
      // omitting one would clear it.
      startTime: engagement.startTime,
      callTime: engagement.callTime,
      dressNotes: engagement.dressNotes,
      venue: engagement.venue,
      ...update,
    }),
  );

  function run(update: Record<string, string | null>) {
    setError(null);
    save.mutate(update, { onError: () => setError('Could not save that.') });
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Details</Text>

      <TimeField
        label="Call or sound-check time"
        value={engagement.callTime}
        onChange={(value) => run({ callTime: value })}
        helperText="When people need to be there."
      />

      <TimeField
        label="Start time"
        value={engagement.startTime}
        onChange={(value) => run({ startTime: value })}
        helperText="When it begins."
      />

      <TextInput
        label="Venue"
        placeholder="Still to be confirmed"
        value={venue}
        onChangeText={setVenue}
        onBlur={() => run({ venue: venue.trim() === '' ? null : venue })}
      />

      <TextInput
        label="What to wear"
        placeholder="e.g. Black kurta, white shalwar"
        value={dress}
        onChangeText={setDress}
        onBlur={() => run({ dressNotes: dress.trim() === '' ? null : dress })}
        multiline
      />

      {save.isPending ? (
        <Text color="muted" variant="caption">
          Saving…
        </Text>
      ) : null}
      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

/**
 * The readiness checklist (D-048). A list of what is left, not a score — and
 * every item can be set aside when it does not apply to this event, which is
 * what keeps the remaining ones meaningful.
 */
export function ReadinessCard({ engagementId }: { engagementId: string }) {
  const readinessQuery = useReadiness(engagementId);
  const [error, setError] = useState<string | null>(null);

  const set = useReadinessMutation<{ item: ReadinessItem; notRequired: boolean }>(
    (accessToken, organisationId, args) =>
      setReadiness(
        accessToken,
        organisationId,
        engagementId,
        args.item,
        args.notRequired,
      ),
  );

  if (!readinessQuery.isSuccess) {
    return null;
  }

  const entries = readinessQuery.data;

  // The same count Home shows, arrived at the same way.
  const outstanding = entries.filter(
    (entry) => entry.state === 'Outstanding',
  );

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Readiness</Text>
      <Text color="secondary" variant="bodySmall">
        {outstanding.length === 0
          ? 'Nothing left to sort out.'
          : `${outstanding.length} still to sort out.`}
      </Text>

      <View style={styles.rows}>
        {entries.map((entry) => (
          <ReadinessRow
            key={entry.item}
            entry={entry}
            busy={set.isPending}
            onToggle={() =>
              set.mutate(
                {
                  item: entry.item,
                  notRequired: entry.state !== 'NotRequired',
                },
                { onError: () => setError('Could not change that.') },
              )
            }
          />
        ))}
      </View>

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function ReadinessRow({
  entry,
  busy,
  onToggle,
}: {
  entry: ReadinessEntry;
  busy: boolean;
  onToggle: () => void;
}) {
  return (
    <View style={styles.readinessRow}>
      <View style={[styles.tick, tickStyle(entry.state)]}>
        <Text variant="caption" style={styles.tickMark}>
          {entry.state === 'Done' ? '✓' : entry.state === 'NotRequired' ? '–' : ''}
        </Text>
      </View>

      <View style={styles.rowValue}>
        <Text
          variant="bodySmall"
          color={entry.state === 'Outstanding' ? 'primary' : 'muted'}
        >
          {READINESS_LABELS[entry.item]}
        </Text>
      </View>

      <Text
        variant="caption"
        color="accent"
        onPress={busy ? undefined : onToggle}
        suppressHighlighting
      >
        {entry.state === 'NotRequired' ? 'Put back' : 'Not required'}
      </Text>
    </View>
  );
}

function tickStyle(state: ReadinessEntry['state']) {
  if (state === 'Done') {
    return styles.tickDone;
  }
  return state === 'NotRequired' ? styles.tickWaived : styles.tickOutstanding;
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  rows: {
    gap: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
  },
  rowLabel: {
    width: 68,
    paddingTop: 2,
  },
  rowValue: {
    flex: 1,
    gap: 2,
  },
  readinessRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  tick: {
    width: 20,
    height: 20,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
  },
  tickMark: {
    color: colors.text.inverse,
    lineHeight: 14,
  },
  tickDone: {
    backgroundColor: colors.tealText,
    borderColor: colors.tealText,
  },
  tickWaived: {
    backgroundColor: colors.border.strong,
    borderColor: colors.border.strong,
  },
  tickOutstanding: {
    backgroundColor: 'transparent',
    borderColor: colors.border.strong,
  },
});
