import { useState } from 'react';
import { Alert, StyleSheet, View } from 'react-native';

import {
  discardEngagement,
  setEngagementDates,
  transitionEngagement,
  transitionNeedsReason,
  type Engagement,
  type EngagementStatus,
} from '@/api/engagements';
import { ApiError } from '@/api/client';
import { Button, Card, DateField, Text, TextInput } from '@/components/ui';
import {
  formatEngagementDate,
  STATUS_WORDS,
  TRANSITION_LABELS,
  useEngagementActivity,
  useEngagementMutation,
} from '@/hooks/use-engagements';
import { spacing } from '@/theme';

/**
 * The date. Editable while it is a private Draft or being rescheduled after a
 * postponement; otherwise Members hold the old one and it moves only by
 * postponing (D-038), so the card explains that rather than hiding.
 */
export function DatesCard({ engagement }: { engagement: Engagement }) {
  const [startDate, setStartDate] = useState(engagement.startDate);
  const [error, setError] = useState<string | null>(null);

  const save = useEngagementMutation<string | null>(
    (accessToken, organisationId, value) =>
      setEngagementDates(accessToken, organisationId, engagement.id, {
        startDate: value,
        endDate: null,
      }),
  );

  if (!engagement.canChangeDateDirectly) {
    return (
      <Card style={styles.card}>
        <Text variant="subheading">Date</Text>
        <Text color="secondary" variant="bodySmall">
          {formatEngagementDate(engagement)}
        </Text>
        <Text color="muted" variant="caption">
          Members have been told this date, so changing it means postponing —
          that way nobody is left holding a date that quietly moved, and their
          availability is asked for again.
        </Text>
      </Card>
    );
  }
  return (
    <Card style={styles.card}>
      <Text variant="subheading">Date</Text>
      <DateField
        label="Proposed date"
        value={startDate}
        onChange={(next) => {
          setStartDate(next);
          setError(null);
          save.mutate(next, {
            onError: () => setError('Could not save that date.'),
          });
        }}
        placeholder="Not set yet"
        helperText={
          engagement.status === 'Postponed'
            ? 'Setting a replacement date asks members for their availability again.'
            : 'Members are not told until you request availability.'
        }
        clearable
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
 * The moves available from here. Cancelling, postponing, reopening, and
 * reversing a completion each ask for a reason first, because the API will
 * refuse them without one — and because the reason is what makes the history
 * readable later.
 */
export function TransitionsCard({ engagement }: { engagement: Engagement }) {
  const [pending, setPending] = useState<EngagementStatus | null>(null);
  const [reason, setReason] = useState('');
  const [error, setError] = useState<string | null>(null);

  const move = useEngagementMutation<{
    status: EngagementStatus;
    reason: string | null;
    acknowledgeOutstanding?: boolean;
  }>((accessToken, organisationId, args) =>
    transitionEngagement(
      accessToken,
      organisationId,
      engagement.id,
      args.status,
      args.reason,
      args.acknowledgeOutstanding,
    ),
  );

  if (engagement.allowedTransitions.length === 0) {
    return null;
  }

  /**
   * Confirming with answers outstanding is refused with 409 until the
   * organiser says they know. The warning names what is unresolved rather than
   * asking a bare "are you sure" (D-029).
   */
  function warnThenConfirm(status: EngagementStatus) {
    Alert.alert(
      'Some members have not answered',
      'You can confirm the booking anyway — it is a fact about the customer, '
        + 'not the lineup. The outstanding answers stay visible until they are in.',
      [
        { text: 'Wait for answers', style: 'cancel' },
        {
          text: 'Confirm anyway',
          onPress: () =>
            move.mutate(
              { status, reason: null, acknowledgeOutstanding: true },
              { onError: () => setError('Could not make that change.') },
            ),
        },
      ],
    );
  }

  function start(status: EngagementStatus) {
    setError(null);
    if (transitionNeedsReason(engagement.status, status)) {
      setPending(status);
      setReason('');
      return;
    }
    move.mutate(
      { status, reason: null },
      {
        onError: (failure) => {
          if (failure instanceof ApiError && failure.status === 409) {
            warnThenConfirm(status);
            return;
          }
          setError('Could not make that change.');
        },
      },
    );
  }

  function confirm() {
    if (pending === null) {
      return;
    }
    if (reason.trim() === '') {
      setError('A short reason is required.');
      return;
    }
    move.mutate(
      { status: pending, reason: reason.trim() },
      {
        onSuccess: () => setPending(null),
        onError: () => setError('Could not make that change.'),
      },
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">What happens next</Text>

      {pending === null ? (
        engagement.allowedTransitions.map((status) => (
          <Button
            key={status}
            label={TRANSITION_LABELS[status]}
            variant={status === 'Cancelled' ? 'ghost' : 'secondary'}
            loading={move.isPending}
            onPress={() => start(status)}
          />
        ))
      ) : (
        <>
          <Text color="secondary" variant="bodySmall">
            {TRANSITION_LABELS[pending]} — everyone involved will see why.
          </Text>
          <TextInput
            label="Reason"
            placeholder="e.g. The customer moved the date"
            value={reason}
            onChangeText={setReason}
            autoFocus
          />
          <Button
            label={TRANSITION_LABELS[pending]}
            loading={move.isPending}
            onPress={confirm}
          />
          <Button
            label="Cancel"
            variant="ghost"
            onPress={() => {
              setPending(null);
              setError(null);
            }}
            disabled={move.isPending}
          />
        </>
      )}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

/** The engagement's history, newest first. */

/**
 * Puts a booking in the person's own calendar. Offered only once the event is
 * actually on — a draft or an open availability request is not a commitment,
 * and putting one here would have someone holding a date the group has not
 * taken.
 */
export function HistoryCard({ engagementId }: { engagementId: string }) {
  const activityQuery = useEngagementActivity(engagementId);

  if (!activityQuery.isSuccess || activityQuery.data.length === 0) {
    return null;
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">History</Text>
      {activityQuery.data.map((entry) => (
        <View key={entry.id} style={styles.historyEntry}>
          <Text variant="bodySmall">{describeActivity(entry)}</Text>
          <Text color="muted" variant="caption">
            {new Date(entry.occurredAtUtc).toLocaleString()}
          </Text>
          {entry.reason ? (
            <Text color="secondary" variant="caption">
              “{entry.reason}”
            </Text>
          ) : null}
        </View>
      ))}
    </Card>
  );
}

function describeActivity(entry: {
  type: string;
  fromStatus: EngagementStatus | null;
  toStatus: EngagementStatus | null;
  fromStartDate: string | null;
  toStartDate: string | null;
}): string {
  if (entry.type === 'Created') {
    return 'Enquiry created';
  }

  if (entry.type === 'DateChanged') {
    return `Date moved from ${entry.fromStartDate ?? 'TBC'} to ${
      entry.toStartDate ?? 'TBC'
    }`;
  }

  const to = entry.toStatus ? STATUS_WORDS[entry.toStatus] : 'unknown';
  const from = entry.fromStatus ? STATUS_WORDS[entry.fromStatus] : null;
  return from ? `${from} → ${to}` : to;
}

export function DiscardCard({
  engagement,
  onDiscarded,
}: {
  engagement: Engagement;
  onDiscarded: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const discard = useEngagementMutation<void>((accessToken, organisationId) =>
    discardEngagement(accessToken, organisationId, engagement.id),
  );

  // Only a private Draft can be discarded; once Members have seen it, the
  // action is Cancel and the record is kept (D-034).
  function confirmDiscard() {
    Alert.alert(
      `Discard ${engagement.title}?`,
      'Nobody else has seen this, so it will be deleted rather than kept in history.',
      [
        { text: 'Keep it', style: 'cancel' },
        {
          text: 'Discard',
          style: 'destructive',
          onPress: () =>
            discard.mutate(undefined, {
              onSuccess: onDiscarded,
              onError: () => setError('Could not discard it.'),
            }),
        },
      ],
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Discard</Text>
      <Text color="secondary" variant="bodySmall">
        Nobody has been told about this enquiry, so it can simply go.
      </Text>
      <Button
        label="Discard enquiry"
        variant="ghost"
        loading={discard.isPending}
        onPress={confirmDiscard}
      />
      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}


const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  historyEntry: {
    gap: 2,
  },
});
