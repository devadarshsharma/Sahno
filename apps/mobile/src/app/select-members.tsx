import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { requestAvailability } from '@/api/availability';
import { Button, Card, Screen, Text } from '@/components/ui';
import { useAvailability, useAvailabilityMutation } from '@/hooks/use-availability';
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
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">Who do you need?</Text>
        <Text color="secondary">
          Everyone you pick is asked whether they are available. They see only
          their own answer.
        </Text>
      </View>

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
        disabled={chosen.length === 0}
        loading={ask.isPending}
        onPress={() =>
          ask.mutate(chosen, {
            onSuccess: () => router.back(),
            onError: () => setError('Could not send those requests.'),
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
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  header: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
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
