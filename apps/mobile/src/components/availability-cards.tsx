import { useState } from 'react';
import { Alert, Pressable, StyleSheet, View } from 'react-native';

import {
  ANSWER_LABELS,
  remindParticipant,
  removeParticipant,
  respondToAvailability,
  type AvailabilityAnswer,
  type Participant,
} from '@/api/availability';
import { Button, Card, Text } from '@/components/ui';
import {
  useAvailability,
  useAvailabilityMutation,
  useOwnAvailability,
} from '@/hooks/use-availability';
import { colors, radii, spacing } from '@/theme';

const ANSWERS: AvailabilityAnswer[] = ['Available', 'Maybe', 'Unavailable'];

/**
 * The member's own answer, and the only availability they can see. Nobody
 * else's answer appears here by design: knowing what the rest of the lineup
 * said would colour the answer people give (D-021).
 */
export function MyAvailabilityCard({ engagementId }: { engagementId: string }) {
  const ownQuery = useOwnAvailability(engagementId);
  const [error, setError] = useState<string | null>(null);

  const respond = useAvailabilityMutation<AvailabilityAnswer>(
    (accessToken, organisationId, answer) =>
      respondToAvailability(accessToken, organisationId, engagementId, answer),
  );

  if (!ownQuery.isSuccess || !ownQuery.data.isSelected) {
    return null;
  }

  const current = ownQuery.data.response;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">
        {current ? 'Your answer' : 'Are you available?'}
      </Text>
      <Text color="secondary" variant="bodySmall">
        {current
          ? 'You can change this while the organiser is still deciding.'
          : 'Only the organisers see your answer.'}
      </Text>

      <View style={styles.answers}>
        {ANSWERS.map((answer) => {
          const selected = current === answer;
          return (
            <Pressable
              key={answer}
              accessibilityRole="button"
              accessibilityState={{ selected }}
              accessibilityLabel={ANSWER_LABELS[answer]}
              disabled={respond.isPending}
              onPress={() => {
                setError(null);
                respond.mutate(answer, {
                  onError: () => setError('Could not save your answer.'),
                });
              }}
              style={[styles.answer, selected ? styles.answerSelected : null]}
            >
              <Text
                variant="label"
                color={selected ? 'inverse' : 'secondary'}
                style={styles.answerText}
              >
                {ANSWER_LABELS[answer]}
              </Text>
            </Pressable>
          );
        })}
      </View>

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

/**
 * The organiser's view of the lineup: who answered what, who has not, and the
 * totals. Outstanding answers stay visible after confirmation, because an
 * unresolved lineup is still a problem once the booking is agreed (D-029).
 */
export function LineupCard({
  engagementId,
  onSelectMembers,
}: {
  engagementId: string;
  onSelectMembers: () => void;
}) {
  const availabilityQuery = useAvailability(engagementId);
  const [error, setError] = useState<string | null>(null);

  const remind = useAvailabilityMutation<string>(
    (accessToken, organisationId, userId) =>
      remindParticipant(accessToken, organisationId, engagementId, userId),
  );
  const remove = useAvailabilityMutation<string>(
    (accessToken, organisationId, userId) =>
      removeParticipant(accessToken, organisationId, engagementId, userId),
  );

  if (!availabilityQuery.isSuccess) {
    return null;
  }

  const { summary, participants } = availabilityQuery.data;
  const lineup = participants.filter((participant) => participant.isActive);
  const dropped = participants.filter((participant) => !participant.isActive);

  function confirmRemove(participant: Participant) {
    const name = participant.displayName ?? 'this member';
    Alert.alert(
      `Take ${name} off this event?`,
      'They lose access to it. Their answer stays on your record.',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Remove',
          style: 'destructive',
          onPress: () =>
            remove.mutate(participant.userId, {
              onError: () => setError('Could not remove them.'),
            }),
        },
      ],
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Who is available</Text>

      {lineup.length === 0 ? (
        <Text color="secondary" variant="bodySmall">
          Nobody has been asked yet. Choose who you need and Sahno will ask them.
        </Text>
      ) : (
        <>
          <Text color="secondary" variant="bodySmall">
            {summary.available} available · {summary.maybe} maybe ·{' '}
            {summary.unavailable} not available
            {summary.outstanding > 0
              ? ` · ${summary.outstanding} yet to answer`
              : ''}
          </Text>

          <View style={styles.rows}>
            {lineup.map((participant) => (
              <View key={participant.userId} style={styles.row}>
                <View style={styles.rowText}>
                  <Text numberOfLines={1}>
                    {participant.displayName ?? 'Member'}
                  </Text>
                  <Text
                    variant="caption"
                    color={participant.response ? 'secondary' : 'muted'}
                  >
                    {participant.response
                      ? ANSWER_LABELS[participant.response]
                      : participant.remindedAtUtc
                        ? 'No answer yet · reminded'
                        : 'No answer yet'}
                  </Text>
                </View>

                {participant.response === null ? (
                  <Pressable
                    accessibilityRole="button"
                    accessibilityLabel={`Remind ${participant.displayName ?? 'member'}`}
                    disabled={remind.isPending}
                    onPress={() =>
                      remind.mutate(participant.userId, {
                        onError: () => setError('Could not send that reminder.'),
                      })
                    }
                  >
                    <Text variant="caption" color="accent">
                      Remind
                    </Text>
                  </Pressable>
                ) : null}

                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={`Remove ${participant.displayName ?? 'member'}`}
                  disabled={remove.isPending}
                  onPress={() => confirmRemove(participant)}
                >
                  <Text variant="caption" color="muted">
                    Remove
                  </Text>
                </Pressable>
              </View>
            ))}
          </View>
        </>
      )}

      <Button
        label={lineup.length === 0 ? 'Ask members' : 'Add more members'}
        variant="secondary"
        onPress={onSelectMembers}
      />

      {dropped.length > 0 ? (
        <View style={styles.dropped}>
          <Text variant="caption" color="muted">
            No longer on this event: {dropped
              .map(
                (participant) =>
                  `${participant.displayName ?? 'Member'}${
                    participant.response
                      ? ` (${ANSWER_LABELS[participant.response].toLowerCase()})`
                      : ''
                  }`,
              )
              .join(', ')}
          </Text>
        </View>
      ) : null}

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
  answers: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  answer: {
    flex: 1,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.sm,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.strong,
    backgroundColor: colors.surface.raised,
  },
  answerSelected: {
    backgroundColor: colors.interactive.primary,
    borderColor: colors.interactive.primary,
  },
  answerText: {
    textAlign: 'center',
  },
  rows: {
    gap: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  dropped: {
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.border.default,
  },
});
