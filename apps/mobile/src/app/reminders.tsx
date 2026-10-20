import { Ionicons } from '@expo/vector-icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Pressable, StyleSheet, View } from 'react-native';

import { setReminders, type AvailabilityNudge, type ReminderTiming } from '@/api/organisations';
import { Card, Screen, showAlert, Text } from '@/components/ui';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { colors, radii, spacing } from '@/theme';

type Option<T extends string> = { value: T; label: string; detail?: string };

const NUDGE_OPTIONS: Option<AvailabilityNudge>[] = [
  { value: '2d', label: '2 days after asking', detail: 'Then a last call the day before, at 10:00' },
  { value: '1d', label: '1 day after asking', detail: 'Then a last call the day before, at 10:00' },
  { value: 'off', label: 'Off', detail: 'Organisers chase with Remind themselves' },
];

const OPTIONS: Option<ReminderTiming>[] = [
  { value: 'morning', label: 'Morning of', detail: '8:00, or two hours before if it starts earlier' },
  { value: '1h', label: '1 hour before' },
  { value: '2h', label: '2 hours before' },
  { value: '3h', label: '3 hours before' },
  { value: 'day-before', label: 'The day before', detail: 'at 6:00 pm' },
  { value: 'off', label: 'Off', detail: 'No reminder' },
];

/**
 * When this organisation's reminders go out (D-118), one choice for events
 * and one for rehearsals, and how soon unanswered availability is nudged (D-120). Organisers only; it applies to everybody reminded,
 * members and organisers alike. "Hours before" counts back from the arrival
 * (call time, else start) — and something with no time set is reminded in
 * the morning instead.
 */
export default function Reminders() {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  const save = useMutation({
    mutationFn: async (next: {
      eventReminder: ReminderTiming;
      rehearsalReminder: ReminderTiming;
      availabilityNudge: AvailabilityNudge;
    }) =>
      setReminders(await session.getAccessToken(), active!.id, next),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['organisations'] }),
    onError: () => showAlert('Could not save that', 'Check your connection and try again.'),
  });

  if (!active || (active.role !== 'Owner' && active.role !== 'Admin')) {
    return (
      <Screen hero={{ title: 'Reminders' }}>
        <Text color="secondary">Only organisers can change when reminders go out.</Text>
      </Screen>
    );
  }

  // What is on screen follows the save at once; the server is the record.
  const eventReminder = save.isPending ? save.variables.eventReminder : (active.eventReminder ?? 'morning');
  const rehearsalReminder = save.isPending ? save.variables.rehearsalReminder : (active.rehearsalReminder ?? 'morning');
  const availabilityNudge = save.isPending ? save.variables.availabilityNudge : (active.availabilityNudge ?? '2d');
  const current = { eventReminder, rehearsalReminder, availabilityNudge };

  return (
    <Screen
      scroll
      hero={{
        title: 'Reminders',
        subtitle: `When ${active.name} reminds everybody about what is coming up.`,
      }}
    >
      <Choice
        title="Events"
        note="Confirmed events. Counted back from the arrival time, or the start time if there is none."
        value={eventReminder}
        options={OPTIONS}
        onChange={(value) => save.mutate({ ...current, eventReminder: value })}
      />
      <Choice
        title="Rehearsals"
        note="Counted back from the rehearsal's start time."
        value={rehearsalReminder}
        options={OPTIONS}
        onChange={(value) => save.mutate({ ...current, rehearsalReminder: value })}
      />
      <Choice
        title="Unanswered availability"
        note="Anyone asked who has not answered is nudged, until they do. Stops once they answer, or once the event has passed."
        value={availabilityNudge}
        options={NUDGE_OPTIONS}
        onChange={(value) => save.mutate({ ...current, availabilityNudge: value })}
      />
      <Text variant="caption" color="muted" style={styles.footnote}>
        Goes to everyone expected and to the organisers. Something with no time set is reminded at 8:00 on the day.
        Nobody is reminded twice about the same thing.
      </Text>
    </Screen>
  );
}

function Choice<T extends string>({
  title,
  note,
  value,
  options,
  onChange,
}: {
  title: string;
  note: string;
  value: T;
  options: Option<T>[];
  onChange: (value: T) => void;
}) {
  return (
    <Card style={styles.card}>
      <Text variant="subheading">{title}</Text>
      <Text variant="bodySmall" color="secondary">
        {note}
      </Text>
      <View style={styles.options} accessibilityRole="radiogroup">
        {options.map((option, index) => {
          const selected = option.value === value;
          return (
            <Pressable
              key={option.value}
              accessibilityRole="radio"
              accessibilityState={{ checked: selected }}
              accessibilityLabel={[option.label, option.detail].filter(Boolean).join(', ')}
              onPress={() => (selected ? undefined : onChange(option.value))}
              style={({ pressed }) => [
                styles.option,
                index > 0 ? styles.optionDivider : null,
                pressed ? styles.optionPressed : null,
              ]}
            >
              <View style={styles.optionText}>
                <Text variant="bodySmall">{option.label}</Text>
                {option.detail ? (
                  <Text variant="caption" color="muted">
                    {option.detail}
                  </Text>
                ) : null}
              </View>
              <Ionicons
                name={selected ? 'radio-button-on' : 'radio-button-off'}
                size={20}
                color={selected ? colors.tealText : colors.text.muted}
              />
            </Pressable>
          );
        })}
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  options: {
    marginTop: spacing.xs,
    borderRadius: radii.md,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border.default,
    overflow: 'hidden',
  },
  option: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 52,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
  },
  optionDivider: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border.default,
  },
  optionPressed: {
    backgroundColor: colors.surface.subtle,
  },
  optionText: {
    flex: 1,
    gap: 2,
  },
  footnote: {
    marginTop: spacing.xs,
  },
});
