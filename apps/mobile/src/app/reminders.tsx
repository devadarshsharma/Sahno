import { Ionicons } from '@expo/vector-icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Pressable, TextInput as NativeTextInput, StyleSheet, View } from 'react-native';

import { setReminders, type AvailabilityNudge, type ReminderTiming } from '@/api/organisations';
import { Button, Card, Screen, showAlert, Text } from '@/components/ui';
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

/** "before:360" → 360; anything else → null (D-124). */
function customMinutes(value: string): number | null {
  const match = /^before:(\d+)$/.exec(value);
  return match ? Number(match[1]) : null;
}

/** 360 → "6 hours before", 2880 → "2 days before". */
function describeMinutes(minutes: number): string {
  if (minutes % (24 * 60) === 0) {
    const days = minutes / (24 * 60);
    return `${days} ${days === 1 ? 'day' : 'days'} before`;
  }
  if (minutes % 60 === 0) {
    const hours = minutes / 60;
    return `${hours} ${hours === 1 ? 'hour' : 'hours'} before`;
  }
  return `${minutes} minutes before`;
}

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
        allowCustom
        onChange={(value) => save.mutate({ ...current, eventReminder: value })}
      />
      <Choice
        title="Rehearsals"
        note="Counted back from the rehearsal's start time."
        value={rehearsalReminder}
        options={OPTIONS}
        allowCustom
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
  allowCustom = false,
}: {
  title: string;
  note: string;
  value: T;
  options: Option<T>[];
  onChange: (value: T) => void;
  /** Offer "Custom…": any number of hours or days before (D-124). */
  allowCustom?: boolean;
}) {
  const custom = customMinutes(value);
  const [editing, setEditing] = useState(false);

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
        {allowCustom ? (
          <Pressable
            accessibilityRole="radio"
            accessibilityState={{ checked: custom !== null, expanded: editing }}
            accessibilityLabel={custom !== null ? `Custom, ${describeMinutes(custom)}` : 'Custom'}
            onPress={() => setEditing((open) => !open)}
            style={({ pressed }) => [styles.option, styles.optionDivider, pressed ? styles.optionPressed : null]}
          >
            <View style={styles.optionText}>
              <Text variant="bodySmall">{custom !== null ? `Custom: ${describeMinutes(custom)}` : 'Custom…'}</Text>
              <Text variant="caption" color="muted">
                Any number of hours or days before
              </Text>
            </View>
            <Ionicons
              name={custom !== null ? 'radio-button-on' : 'radio-button-off'}
              size={20}
              color={custom !== null ? colors.tealText : colors.text.muted}
            />
          </Pressable>
        ) : null}
      </View>
      {allowCustom && editing ? (
        <CustomLead
          initial={custom}
          onSet={(minutes) => {
            setEditing(false);
            onChange(`before:${minutes}` as T);
          }}
        />
      ) : null}
    </Card>
  );
}

/** Two weeks, the furthest ahead a reminder can go (D-124). */
const MAX_HOURS = 14 * 24;
const MAX_DAYS = 14;

/** The number and the unit, and Set. Hours from 1 to 336, days from 1 to 14. */
function CustomLead({ initial, onSet }: { initial: number | null; onSet: (minutes: number) => void }) {
  const startsInDays = initial !== null && initial % (24 * 60) === 0;
  const [unit, setUnit] = useState<'hours' | 'days'>(startsInDays ? 'days' : 'hours');
  const [text, setText] = useState(
    initial === null ? '' : String(startsInDays ? initial / (24 * 60) : Math.round(initial / 60)),
  );

  const amount = Number(text);
  const max = unit === 'hours' ? MAX_HOURS : MAX_DAYS;
  const valid = text !== '' && Number.isInteger(amount) && amount >= 1 && amount <= max;

  return (
    <View style={styles.custom}>
      <View style={styles.customRow}>
        <NativeTextInput
          value={text}
          onChangeText={(next) => setText(next.replace(/[^0-9]/g, '').slice(0, 3))}
          keyboardType="number-pad"
          placeholder={unit === 'hours' ? '6' : '2'}
          placeholderTextColor={colors.text.muted}
          accessibilityLabel={`Number of ${unit}`}
          style={styles.customInput}
          autoFocus
        />
        {(['hours', 'days'] as const).map((option) => (
          <Pressable
            key={option}
            accessibilityRole="button"
            accessibilityState={{ selected: unit === option }}
            onPress={() => setUnit(option)}
            style={[styles.unit, unit === option ? styles.unitOn : null]}
          >
            <Text variant="bodySmall" color={unit === option ? 'inverse' : 'secondary'}>
              {option === 'hours' ? 'Hours' : 'Days'}
            </Text>
          </Pressable>
        ))}
        <Text variant="bodySmall" color="secondary">
          before
        </Text>
      </View>
      <Text variant="caption" color={valid || text === '' ? 'muted' : 'error'}>
        {unit === 'hours' ? `1 to ${MAX_HOURS} hours.` : `1 to ${MAX_DAYS} days.`} Counted back from the start time.
      </Text>
      <Button
        label="Set"
        disabled={!valid}
        onPress={() => onSet(unit === 'hours' ? amount * 60 : amount * 24 * 60)}
      />
    </View>
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
  custom: {
    gap: spacing.sm,
    paddingTop: spacing.sm,
  },
  customRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  customInput: {
    width: 64,
    minHeight: 44,
    paddingHorizontal: spacing.md,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border.default,
    backgroundColor: colors.surface.raised,
    color: colors.text.primary,
    fontSize: 18,
    textAlign: 'center',
  },
  unit: {
    minHeight: 36,
    paddingHorizontal: spacing.md,
    justifyContent: 'center',
    borderRadius: radii.full,
    backgroundColor: colors.surface.subtle,
  },
  unitOn: {
    backgroundColor: colors.interactive.primary,
  },
  footnote: {
    marginTop: spacing.xs,
  },
});
