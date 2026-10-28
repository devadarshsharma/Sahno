import { Ionicons } from '@expo/vector-icons';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import {
  ANSWER_LABELS,
  remindParticipant,
  removeParticipant,
  respondToAvailability,
  type AvailabilityAnswer,
  type Participant,
} from '@/api/availability';
import { ApiError } from '@/api/client';
import { YourClashNotice } from '@/components/clash-notice';
import { Button, Card, showAlert, showToast, Text, TextAction } from '@/components/ui';
import {
  useAvailability,
  useAvailabilityMutation,
  useGoing,
  useOwnAvailability,
} from '@/hooks/use-availability';
import { useEngagements } from '@/hooks/use-engagements';
import { colors, fontFamilies, radii, spacing } from '@/theme';

const ANSWERS: AvailabilityAnswer[] = ['Available', 'Maybe', 'Unavailable'];

/**
 * The member's own answer, and the only availability they can see. Nobody
 * else's answer appears here by design: knowing what the rest of the lineup
 * said would colour the answer people give (D-021).
 */
export function MyAvailabilityCard({ engagementId }: { engagementId: string }) {
  const ownQuery = useOwnAvailability(engagementId);
  const engagement = useEngagements().data?.find((row) => row.id === engagementId);

  // Never silently nothing (D-119): a member looking for where to answer
  // must see it is coming, or that it could not load, not an empty page.
  if (ownQuery.isPending) {
    return (
      <Card style={styles.card}>
        <Text variant="subheading">Your availability</Text>
        <ActivityIndicator color={colors.tealText} style={styles.loading} />
      </Card>
    );
  }

  if (ownQuery.isError) {
    return (
      <Card style={styles.card}>
        <Text variant="subheading">Your availability</Text>
        <Text color="secondary" variant="bodySmall">
          Could not load your answer.
        </Text>
        <Button label="Try again" variant="secondary" onPress={() => void ownQuery.refetch()} />
      </Card>
    );
  }

  if (!ownQuery.data.isSelected) {
    return null;
  }

  const current = ownQuery.data.response;
  const alreadyConfirmed = engagement?.status === 'Confirmed';

  return (
    <Card style={[styles.card, current ? null : styles.cardToAnswer]}>
      <Text variant="subheading">
        {current ? 'Your answer' : 'Are you available?'}
      </Text>
      <Text color="secondary" variant="bodySmall">
        {current
          ? 'You can change this while the organiser is still deciding.'
          : alreadyConfirmed
            ? 'Already confirmed — the organisers still need your answer. Only they see it.'
            : 'Only the organisers see your answer.'}
      </Text>

      {engagement ? <YourClashNotice day={engagement} /> : null}

      <AvailabilityAnswerButtons engagementId={engagementId} current={current} />
    </Card>
  );
}

/**
 * Available / Maybe / Not available, saved on tap. Used on the event page and
 * straight on the member's Home card, so answering never needs a detour.
 *
 * The press stays "pending" until the refetched lists arrive (the mutation
 * waits for its invalidation), so the chosen answer never flickers back.
 */
export function AvailabilityAnswerButtons({
  engagementId,
  current,
}: {
  engagementId: string;
  current: AvailabilityAnswer | null;
}) {
  const [error, setError] = useState<string | null>(null);

  const respond = useAvailabilityMutation<AvailabilityAnswer>(
    (accessToken, organisationId, answer) =>
      respondToAvailability(accessToken, organisationId, engagementId, answer),
  );

  const shown = respond.isPending ? respond.variables : current;

  return (
    <View style={styles.answerGroup}>
      <View style={styles.answers}>
        {ANSWERS.map((answer) => {
          const selected = shown === answer;
          return (
            <Pressable
              key={answer}
              accessibilityRole="button"
              accessibilityState={{ selected, busy: respond.isPending }}
              accessibilityLabel={ANSWER_LABELS[answer]}
              disabled={respond.isPending}
              onPress={() => {
                setError(null);
                respond.mutate(answer, {
                  // Said out loud: on Home the event moves down as this lands (D-127).
                  onSuccess: () => showToast(`Answer sent: ${ANSWER_LABELS[answer]}`),
                  onError: () => setError('Could not save your answer. Try again.'),
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
    </View>
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
  closed = false,
}: {
  engagementId: string;
  onSelectMembers: () => void;
  /** Done or cancelled: nobody new is asked about it (D-097). */
  closed?: boolean;
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
    showAlert(
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
                        ? `No answer yet · reminded ${remindedAgo(participant.remindedAtUtc)}`
                        : 'No answer yet'}
                  </Text>
                </View>

                {participant.response === null ? (
                  <RemindButton
                    name={participant.displayName ?? 'Member'}
                    remindedAtUtc={participant.remindedAtUtc}
                    busy={remind.isPending && remind.variables === participant.userId}
                    onPress={() => {
                      const name = participant.displayName ?? 'them';
                      remind.mutate(participant.userId, {
                        onSuccess: () => showToast(`Reminder sent to ${name}`),
                        onError: (failure) =>
                          failure instanceof ApiError && failure.status === 429
                            ? showToast(`${name} was reminded less than an hour ago`, 'info')
                            : setError('Could not send that reminder.'),
                      });
                    }}
                  />
                ) : null}

                <TextAction
                  label="Remove"
                  accessibilityLabel={`Remove ${participant.displayName ?? 'member'}`}
                  busy={remove.isPending && remove.variables === participant.userId}
                  onPress={() => confirmRemove(participant)}
                />
              </View>
            ))}
          </View>
        </>
      )}

      {closed ? (
        <Text variant="caption" color="muted">
          This event is closed, so nobody new can be asked about it.
        </Text>
      ) : (
        <Button
          label={lineup.length === 0 ? 'Ask members' : 'Add more members'}
          variant="secondary"
          onPress={onSelectMembers}
        />
      )}

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

/**
 * Who is going (D-021, D-108). Once the event is confirmed, everyone on it
 * sees the people who said yes: names and what they do, no answers. Before
 * that it renders nothing — while availability is being collected, a member
 * sees only their own.
 */
export function GoingCard({ engagementId }: { engagementId: string }) {
  const going = useGoing(engagementId);

  if (!going.data?.shown) {
    return null;
  }

  const people = going.data.people;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Who’s going</Text>
      <Text color="secondary" variant="bodySmall">
        {people.length === 0
          ? 'Nobody has said yes yet.'
          : people.length === 1
            ? '1 person.'
            : `${people.length} people.`}
      </Text>
      {people.map((person) => {
        const name = person.isYou ? 'You' : (person.displayName ?? 'A member');
        return (
          <View
            key={person.userId}
            style={styles.goingRow}
            accessible
            accessibilityLabel={[name, person.function].filter(Boolean).join(', ')}
          >
            <View style={styles.goingInitial}>
              <Text variant="label" style={styles.goingInitialText}>
                {(person.displayName ?? '?').trim().charAt(0).toUpperCase()}
              </Text>
            </View>
            <View style={styles.goingText}>
              <Text variant="bodySmall">{name}</Text>
              {person.function ? (
                <Text variant="caption" color="muted">
                  {person.function}
                </Text>
              ) : null}
            </View>
          </View>
        );
      })}
    </Card>
  );
}

/** "just now", "25 min ago", "3 h ago", or the day. */
function remindedAgo(iso: string): string {
  const minutes = Math.floor((Date.now() - new Date(iso).getTime()) / 60_000);
  if (minutes < 1) {
    return 'just now';
  }
  if (minutes < 60) {
    return `${minutes} min ago`;
  }
  if (minutes < 24 * 60) {
    return `${Math.floor(minutes / 60)} h ago`;
  }
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
}

/** The API holds a second manual reminder back for this long (D-123). */
const REMIND_COOLDOWN_MS = 60 * 60 * 1000;

/**
 * Remind as a button, not a word (D-123): it shows it is sending, the toast
 * says it went, and within the hour after it reads "Reminded" and rests, so
 * an impatient second tap is not a second push and email.
 */
function RemindButton({
  name,
  remindedAtUtc,
  busy,
  onPress,
}: {
  name: string;
  remindedAtUtc: string | null;
  busy: boolean;
  onPress: () => void;
}) {
  // The time the row was shown: a reminder sent since reads as recent too.
  const [shownAt] = useState(() => Date.now());
  const recent = remindedAtUtc !== null && shownAt - new Date(remindedAtUtc).getTime() < REMIND_COOLDOWN_MS;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={recent ? `${name} was reminded recently` : `Remind ${name}`}
      accessibilityState={{ disabled: recent || busy, busy }}
      disabled={recent || busy}
      onPress={onPress}
      hitSlop={6}
      style={({ pressed }) => [
        styles.remind,
        recent ? styles.remindDone : null,
        pressed ? styles.remindPressed : null,
      ]}
    >
      {busy ? (
        <ActivityIndicator size="small" color={colors.tealText} />
      ) : (
        <Ionicons
          name={recent ? 'checkmark' : 'notifications-outline'}
          size={14}
          color={recent ? colors.text.muted : colors.tealText}
        />
      )}
      <Text variant="caption" style={recent ? styles.remindTextDone : styles.remindText}>
        {recent ? 'Reminded' : 'Remind'}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  goingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 44,
  },
  goingInitial: {
    width: 32,
    height: 32,
    borderRadius: radii.full,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  goingInitialText: {
    color: colors.tealText,
  },
  goingText: {
    flex: 1,
    gap: 2,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  remind: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    minHeight: 32,
    paddingHorizontal: spacing.md,
    borderRadius: radii.full,
    borderWidth: 1,
    borderColor: colors.tealText,
  },
  remindDone: {
    borderColor: colors.border.default,
  },
  remindPressed: {
    backgroundColor: colors.tealSoft,
  },
  remindText: {
    fontFamily: fontFamilies.uiMedium,
    color: colors.tealText,
  },
  remindTextDone: {
    color: colors.text.muted,
  },
  // Still to answer: it stands out on the page, as on Home.
  cardToAnswer: {
    borderWidth: 2,
    borderColor: colors.orange,
  },
  loading: {
    alignSelf: 'flex-start',
  },
  answerGroup: {
    gap: spacing.sm,
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
