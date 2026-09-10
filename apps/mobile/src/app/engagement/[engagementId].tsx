import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Alert, StyleSheet, View } from 'react-native';

import {
  discardEngagement,
  setEngagementDates,
  transitionEngagement,
  transitionNeedsReason,
  type Engagement,
  type EngagementStatus,
} from '@/api/engagements';
import { ApiError } from '@/api/client';
import { LineupCard, MyAvailabilityCard } from '@/components/availability-cards';
import { Button, Card, DateField, Screen, Text, TextInput } from '@/components/ui';
import {
  formatEngagementDate,
  STATUS_LABELS,
  TRANSITION_LABELS,
  useEngagementActivity,
  useEngagementMutation,
  useEngagements,
} from '@/hooks/use-engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import { colors, spacing } from '@/theme';

/**
 * One engagement and the moves it can actually make. The lifecycle is the
 * API's to decide, so the buttons come from the allowedTransitions it returns
 * rather than from a copy of the rules kept here.
 */
export default function EngagementDetail() {
  const { engagementId } = useLocalSearchParams<{ engagementId: string }>();
  const router = useRouter();
  const { active } = useActiveOrg();
  const engagementsQuery = useEngagements();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  const engagement = engagementsQuery.data?.find(
    (row) => row.id === engagementId,
  );

  if (engagementsQuery.isPending) {
    return (
      <Screen>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  if (!engagement) {
    return (
      <Screen>
        <View style={styles.centered}>
          <Text variant="heading">Not found</Text>
          <Text color="secondary" style={styles.centeredText}>
            This enquiry may have been discarded.
          </Text>
          <Button label="Back" variant="secondary" onPress={() => router.back()} />
        </View>
      </Screen>
    );
  }

  return (
    <Screen
      scroll
      onRefresh={() => engagementsQuery.refetch()}
      refreshing={engagementsQuery.isRefetching}
    >
      <View style={styles.header}>
        <Text variant="title">{engagement.title}</Text>
        <Text color="secondary">
          {STATUS_LABELS[engagement.status]} · {formatEngagementDate(engagement)}
        </Text>
        {engagement.venue ? (
          <Text color="muted" variant="bodySmall">
            {engagement.venue}
          </Text>
        ) : null}
      </View>

      {!engagement.isSharedWithMembers ? (
        <Card style={styles.card}>
          <Text variant="bodySmall" color="secondary">
            This is still private. Nobody else sees it until you request
            availability.
          </Text>
        </Card>
      ) : null}

      <MyAvailabilityCard engagementId={engagement.id} />

      {isOrganiser ? (
        <LineupCard
          engagementId={engagement.id}
          onSelectMembers={() =>
            router.push({
              pathname: '/select-members',
              params: { engagementId: engagement.id },
            })
          }
        />
      ) : null}

      {isOrganiser ? <DatesCard engagement={engagement} /> : null}
      {isOrganiser ? <TransitionsCard engagement={engagement} /> : null}
      {isOrganiser ? <HistoryCard engagementId={engagement.id} /> : null}

      {isOrganiser && engagement.canBeDiscarded ? (
        <DiscardCard engagement={engagement} onDiscarded={() => router.back()} />
      ) : null}

      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

/**
 * The date. Editable while it is a private Draft or being rescheduled after a
 * postponement; otherwise Members hold the old one and it moves only by
 * postponing (D-038), so the card explains that rather than hiding.
 */
function DatesCard({ engagement }: { engagement: Engagement }) {
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
function TransitionsCard({ engagement }: { engagement: Engagement }) {
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
            label="Back"
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
function HistoryCard({ engagementId }: { engagementId: string }) {
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

  const to = entry.toStatus ? STATUS_LABELS[entry.toStatus] : 'unknown';
  const from = entry.fromStatus ? STATUS_LABELS[entry.fromStatus] : null;
  return from ? `${from} → ${to}` : to;
}

function DiscardCard({
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
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.md,
  },
  centeredText: {
    textAlign: 'center',
  },
  header: {
    gap: spacing.xs,
    marginBottom: spacing.xl,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  historyEntry: {
    gap: 2,
  },
});
