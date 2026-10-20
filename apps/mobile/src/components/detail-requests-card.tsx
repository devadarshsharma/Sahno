import { Ionicons } from '@expo/vector-icons';
import type { ComponentProps } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { DETAIL_LABELS, type AskableDetail } from '@/api/detail-requests';
import { Button, Card, showAlert, Text } from '@/components/ui';
import { useAskForDetails, useDetailRequests } from '@/hooks/use-detail-requests';
import { colors, fontFamilies, radii, spacing } from '@/theme';

const ICONS: Record<AskableDetail, ComponentProps<typeof Ionicons>['name']> = {
  Venue: 'location-outline',
  CallTime: 'mic-outline',
  StartTime: 'time-outline',
  Dress: 'shirt-outline',
};

/**
 * What a performer is still waiting to hear (D-089): each detail of the day
 * that is not set yet, with a button to ask the organisers for it. Once
 * asked it says so, and asking again does not ping anybody twice. Goes away
 * when there is nothing missing.
 */
export function DetailRequestsCard({ engagementId }: { engagementId: string }) {
  const query = useDetailRequests(engagementId);
  const ask = useAskForDetails(engagementId);

  if (!query.isSuccess || query.data.missing.length === 0) {
    return null;
  }

  const { missing, asked } = query.data;
  const notAsked = missing.filter((item) => !asked.includes(item));

  const send = (items: AskableDetail[]) =>
    ask.mutate(items, {
      onError: () => showAlert('Could not ask', 'Check your connection and try again.'),
    });

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Still to come</Text>
      <Text variant="bodySmall" color="secondary">
        Not set yet. Ask and the organisers get a notification.
      </Text>

      <View style={styles.list}>
        {missing.map((item) => {
          const done = asked.includes(item);
          return (
            <View key={item} style={styles.row}>
              <Ionicons name={ICONS[item]} size={18} color={colors.text.muted} />
              <Text style={styles.label}>{DETAIL_LABELS[item]}</Text>
              {done ? (
                <View style={styles.asked} accessibilityLabel={`${DETAIL_LABELS[item]}: asked`}>
                  <Ionicons name="checkmark" size={14} color={colors.tealText} />
                  <Text style={styles.askedText}>Asked</Text>
                </View>
              ) : (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={`Ask the organisers for the ${DETAIL_LABELS[item].toLowerCase()}`}
                  disabled={ask.isPending}
                  onPress={() => send([item])}
                  hitSlop={6}
                  style={({ pressed }) => [styles.askButton, pressed ? styles.pressed : null]}
                >
                  <Text style={styles.askText}>Ask</Text>
                </Pressable>
              )}
            </View>
          );
        })}
      </View>

      {notAsked.length > 1 ? (
        <Button
          label="Ask for all of these"
          variant="secondary"
          loading={ask.isPending}
          onPress={() => send(notAsked)}
        />
      ) : null}
    </Card>
  );
}

/**
 * The organiser's side (D-089): who on the event is waiting on which detail,
 * at the top of the screen where it is filled in. Filling it in answers them.
 */
export function DetailAskersCard({ engagementId }: { engagementId: string }) {
  const query = useDetailRequests(engagementId);
  const askers = query.data?.askers ?? [];

  if (askers.length === 0) {
    return null;
  }

  return (
    <Card style={[styles.card, styles.waiting]}>
      <Text variant="subheading">People are asking</Text>
      {askers.map(({ item, names }) => (
        <View key={item} style={styles.row}>
          <Ionicons name={ICONS[item]} size={18} color={colors.orange} />
          <Text style={styles.label}>
            <Text style={styles.strong}>{DETAIL_LABELS[item]}</Text>
            {` — ${names.join(', ')}`}
          </Text>
        </View>
      ))}
      <Text variant="bodySmall" color="secondary">
        Fill these in below and they will hear about it.
      </Text>
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.sm,
  },
  waiting: {
    borderWidth: 1,
    borderColor: colors.orange,
  },
  strong: {
    fontFamily: fontFamilies.uiMedium,
  },
  list: {
    gap: 2,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    minHeight: 40,
  },
  label: {
    flex: 1,
    fontSize: 15,
    color: colors.text.primary,
  },
  askButton: {
    paddingHorizontal: spacing.md,
    paddingVertical: 6,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.tealText,
  },
  askText: {
    fontFamily: fontFamilies.uiMedium,
    fontSize: 13,
    color: colors.tealText,
  },
  asked: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: spacing.sm,
  },
  askedText: {
    fontSize: 13,
    color: colors.text.muted,
  },
  pressed: {
    opacity: 0.7,
  },
});
