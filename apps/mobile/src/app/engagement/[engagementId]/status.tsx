import { StyleSheet, View } from 'react-native';

import type { EngagementStatus } from '@/api/engagements';
import { TransitionsCard } from '@/components/admin-cards';
import { StatusChip } from '@/components/engagement-card';
import { EngagementSectionScreen } from '@/components/engagement-screen';
import { Card, Text } from '@/components/ui';
import { STATUS_WORDS } from '@/hooks/use-engagements';
import { spacing } from '@/theme';

/**
 * Where a booking is in its life, and where it can go next. Reached from the
 * status chip on the overview — you tap the thing you want to change.
 */
export default function Status() {
  return (
    <EngagementSectionScreen title="Status">
      {({ engagement, isOrganiser }) => (
        <>
          <Card style={styles.card}>
            <View style={styles.now}>
              <StatusChip status={engagement.status} />
              <Text variant="subheading">{STATUS_WORDS[engagement.status]}</Text>
            </View>
            <Text color="secondary" variant="bodySmall">
              {MEANING[engagement.status]}
            </Text>
          </Card>
          {isOrganiser ? <TransitionsCard engagement={engagement} /> : null}
        </>
      )}
    </EngagementSectionScreen>
  );
}

/** One line on what each state means for the people on it (D-039). */
const MEANING: Record<EngagementStatus, string> = {
  Draft: 'Private to the organisers. Nobody else sees it until you ask for availability.',
  CheckingAvailability: 'Members have been asked whether they can make it. Answers are coming in.',
  Tentative: 'Provisionally on. The lineup is settled but the customer has not confirmed.',
  Confirmed: 'Booked. Everyone on the lineup is expected there.',
  Postponed: 'On hold until a new date is agreed. Nothing is expected of anyone yet.',
  Completed: 'Done. Kept for the record and for anything still owed.',
  Cancelled: 'Not going ahead. Kept for the record.',
};

const styles = StyleSheet.create({
  card: {
    gap: spacing.sm,
    marginBottom: spacing.lg,
  },
  now: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
});
