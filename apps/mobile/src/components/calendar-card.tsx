import { useState } from 'react';
import { Alert, StyleSheet } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { Button, Card, Text } from '@/components/ui';
import {
  addToPersonalCalendar,
  canAddToPersonalCalendar,
  personalCalendarTitle,
} from '@/lib/personal-calendar';
import { spacing } from '@/theme';

/**
 * Puts a booking in the person's own calendar. Offered only once the event is
 * actually on — a draft or an open availability request is not a commitment,
 * and putting one here would have someone holding a date the group has not
 * taken.
 */
export function AddToCalendarCard({
  engagement,
  organisationName,
}: {
  engagement: Engagement;
  organisationName: string;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!canAddToPersonalCalendar(engagement)) {
    return null;
  }

  async function add() {
    setBusy(true);
    setError(null);
    const result = await addToPersonalCalendar(engagement, organisationName);
    setBusy(false);

    if (result.ok) {
      Alert.alert(
        'Added to your calendar',
        result.tentative
          ? `Saved as "${personalCalendarTitle(engagement)}" so it reads as tentative at a glance. Sahno stays the place it changes.`
          : 'Sahno stays the place it changes — update it here if the booking moves.',
      );
      return;
    }

    if (result.reason === 'already-added') {
      Alert.alert(
        'Already in your calendar',
        'This booking is there once already. Sahno stays the place it changes.',
      );
      return;
    }

    setError(
      result.reason === 'permission'
        ? 'Sahno needs permission to use your calendar. You can grant it in Settings.'
        : result.reason === 'no-calendar'
          ? 'No calendar on this device can be written to.'
          : 'Could not add it to your calendar.',
    );
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Your calendar</Text>
      <Text color="secondary" variant="bodySmall">
        {engagement.status === 'Tentative'
          ? 'Not confirmed yet, so it goes in marked tentative.'
          : 'Add this to the calendar on your phone.'}
      </Text>
      <Button
        label="Add to my calendar"
        variant="secondary"
        loading={busy}
        onPress={add}
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
});
