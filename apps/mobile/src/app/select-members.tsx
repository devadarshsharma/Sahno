import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { requestAvailability } from '@/api/availability';
import { ApiError } from '@/api/client';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useAvailability, useAvailabilityMutation } from '@/hooks/use-availability';
import { useEngagements } from '@/hooks/use-engagements';
import { sortedForDisplay, useMembers } from '@/hooks/use-members';
import { colors, radii, spacing } from '@/theme';

/**
 * Chooses who to ask about an event. People already on the lineup are shown as
 * such rather than hidden, so it is clear who is being added to whom — and
 * re-selecting someone who was taken off restores the answer they gave rather
 * than asking again.
 */
export default function SelectMembers() {
  const { engagementId } = useLocalSearchParams<{ engagementId: string }>();
  const router = useRouter();
  const membersQuery = useMembers();
  const availabilityQuery = useAvailability(engagementId);
  const engagement = useEngagements().data?.find((row) => row.id === engagementId);
  // Availability is a question about a date: with none, there is nothing to
  // ask, and the API refuses (D-126). Say so before anyone picks a name.
  const needsDate = engagement !== undefined && engagement.startDate === null;
  const closed = engagement?.status === 'Completed' || engagement?.status === 'Cancelled';
  const [chosen, setChosen] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  const ask = useAvailabilityMutation<string[]>(
    (accessToken, organisationId, userIds) =>
      requestAvailability(accessToken, organisationId, engagementId, userIds),
  );

  if (membersQuery.isPending || availabilityQuery.isPending) {
    return (
      <Screen>
        <View style={styles.centered}>
          <ActivityIndicator color={colors.tealText} />
        </View>
      </Screen>
    );
  }

  const onLineup = new Set(
    (availabilityQuery.data?.participants ?? [])
      .filter((participant) => participant.isActive)
      .map((participant) => participant.userId),
  );

  const candidates = sortedForDisplay(membersQuery.data ?? []).filter(
    (member) => !member.isYou || member.role === 'Member',
  );

  function toggle(userId: string) {
    setChosen((current) =>
      current.includes(userId)
        ? current.filter((id) => id !== userId)
        : [...current, userId],
    );
  }

  return (
    <Screen
      scroll
      hero={{
        title: 'Who do you need?',
        subtitle:
          'Everyone you pick is asked whether they are available. They see only their own answer.',
      }}
    >

      {needsDate ? (
        <Card style={[styles.card, styles.notice]}>
          <Text variant="subheading">Set a date first</Text>
          <Text color="secondary" variant="bodySmall">
            People are asked whether they can make a date, so this needs one. Set the proposed date, then come
            back to ask.
          </Text>
          <Button
            label="Set the date"
            variant="secondary"
            onPress={() =>
              router.replace({ pathname: '/engagement/[engagementId]/details', params: { engagementId } })
            }
          />
        </Card>
      ) : null}

      <Card style={styles.card}>
        {candidates.map((member) => {
          const already = onLineup.has(member.userId);
          const selected = chosen.includes(member.userId);
          return (
            <Pressable
              key={member.membershipId}
              accessibilityRole="checkbox"
              accessibilityState={{ checked: selected || already }}
              accessibilityLabel={member.displayName ?? 'Member'}
              disabled={already}
              onPress={() => toggle(member.userId)}
              style={[styles.row, selected ? styles.rowSelected : null]}
            >
              <View style={styles.rowText}>
                <Text color={already ? 'muted' : 'primary'} numberOfLines={1}>
                  {member.displayName ?? 'Member'}
                </Text>
                {member.function ? (
                  <Text variant="caption" color="muted">
                    {member.function}
                  </Text>
                ) : null}
              </View>
              <Text variant="caption" color={selected ? 'accent' : 'muted'}>
                {already ? 'Already asked' : selected ? 'Selected' : 'Tap to add'}
              </Text>
            </Pressable>
          );
        })}
      </Card>

      {error ? (
        <Text color="error" variant="bodySmall" style={styles.error}>
          {error}
        </Text>
      ) : null}

      <Button
        label={
          chosen.length === 0
            ? 'Choose someone to ask'
            : `Ask ${chosen.length} ${chosen.length === 1 ? 'person' : 'people'}`
        }
        disabled={chosen.length === 0 || needsDate || closed}
        loading={ask.isPending}
        onPress={() =>
          ask.mutate(chosen, {
            onSuccess: () => router.back(),
            // The API says no only for a reason: say which (D-126).
            onError: (failure) =>
              setError(
                failure instanceof ApiError && failure.status === 400
                  ? needsDate
                    ? 'Set a date first — there is nothing to ask about yet.'
                    : closed
                      ? 'This event is closed, so nobody new can be asked.'
                      : 'Somebody picked is no longer in the group. Refresh and try again.'
                  : 'Could not send those requests. Check your connection and try again.',
              ),
          })
        }
      />
      <Button
        label="Cancel"
        variant="ghost"
        onPress={() => router.back()}
        disabled={ask.isPending}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  notice: {
    gap: spacing.sm,
    borderWidth: 2,
    borderColor: colors.orange,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  card: {
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.default,
  },
  rowSelected: {
    borderColor: colors.border.focus,
    backgroundColor: colors.tealSoft,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  error: {
    marginBottom: spacing.sm,
  },
});
