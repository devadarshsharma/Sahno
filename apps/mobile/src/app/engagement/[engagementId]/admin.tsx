import { useRouter } from 'expo-router';

import {
  DatesCard,
  DiscardCard,
  HistoryCard,
  TransitionsCard,
} from '@/components/admin-cards';
import { EngagementSectionScreen } from '@/components/engagement-screen';

/**
 * The lifecycle controls and the record of every move — last in the overview
 * because they are the least often needed and the most consequential.
 */
export default function Admin() {
  const router = useRouter();

  return (
    <EngagementSectionScreen title="Status & history">
      {({ engagement, isOrganiser }) =>
        isOrganiser ? (
          <>
            <TransitionsCard engagement={engagement} />
            <DatesCard engagement={engagement} />
            <HistoryCard engagementId={engagement.id} />
            {engagement.canBeDiscarded ? (
              <DiscardCard
                engagement={engagement}
                onDiscarded={() => router.replace('/(tabs)/events')}
              />
            ) : null}
          </>
        ) : null
      }
    </EngagementSectionScreen>
  );
}
