import { Ionicons } from '@expo/vector-icons';
import { useState } from 'react';
import { Alert, StyleSheet, View } from 'react-native';

import type { Engagement } from '@/api/engagements';
import { Button, Card, Text } from '@/components/ui';
import {
  addToPersonalCalendar,
  canAddToPersonalCalendar,
  personalCalendarTitle,
  removeFromPersonalCalendar,
  type CalendarOrganisation,
} from '@/lib/personal-calendar';
import { useCalendarLinks } from '@/stores/calendar-links';
import { colors, spacing } from '@/theme';

/**
 * Puts a booking in the person's own calendar, and says whether it is there
 * (D-105). Offered only once the event is actually on — a draft or an open
 * availability request is not a commitment, and putting one there would have
 * someone holding a date the group has not taken. Once added, Sahno keeps the
 * entry up to date itself.
 */
export function AddToCalendarCard({
  engagement,
  organisation,
}: {
  engagement: Engagement;
  organisation: CalendarOrganisation;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const linked = useCalendarLinks((state) => engagement.id in state.links);

  if (!canAddToPersonalCalendar(engagement) && !linked) {
    return null;
  }

  async function add() {
    setBusy(true);
    setError(null);
    const result = await addToPersonalCalendar(engagement, organisation);
    setBusy(false);

    if (result.ok) {
      Alert.alert(
        'Added to your calendar',
        result.tentative
          ? `Saved as "${personalCalendarTitle(engagement)}". If it is confirmed, moves, or is called off, Sahno updates the entry the next time you open the app.`
          : 'If it moves or is called off, Sahno updates the entry the next time you open the app.',
      );
      return;
    }

    setError(
      result.reason === 'permission'
        ? 'Sahno needs access to your calendar to add this and keep it up to date. You can allow it in Settings.'
        : result.reason === 'no-calendar'
          ? 'No calendar on this phone can be written to.'
          : 'Could not add it to your calendar.',
    );
  }

  async function remove() {
    setBusy(true);
    await removeFromPersonalCalendar(engagement.id);
    setBusy(false);
  }

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Your calendar</Text>
      {linked ? (
        <>
          <View style={styles.added}>
            <Ionicons name="checkmark-circle" size={18} color={colors.tealText} />
            <Text variant="bodySmall" style={styles.addedText}>
              In your calendar. Sahno keeps it up to date if this booking changes.
            </Text>
          </View>
          <Button label="Remove from my calendar" variant="ghost" loading={busy} onPress={remove} />
        </>
      ) : (
        <>
          <Text color="secondary" variant="bodySmall">
            {engagement.status === 'Tentative'
              ? 'Not confirmed yet, so it goes in marked tentative.'
              : 'Add this to the calendar on your phone. It shows wherever that calendar syncs.'}
          </Text>
          <Button label="Add to my calendar" variant="secondary" loading={busy} onPress={add} />
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

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  added: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  addedText: {
    flex: 1,
  },
});
