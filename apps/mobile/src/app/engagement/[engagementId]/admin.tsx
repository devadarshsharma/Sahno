import { useRouter } from 'expo-router';

import { DiscardCard, HistoryCard } from '@/components/admin-cards';
import { EngagementSectionScreen } from '@/components/engagement-screen';

/**
 * The record of every move, and the one irreversible action — last in the
 * overview because they are the least often needed and the most consequential.
 * Changing the status lives with the status chip; dates live with Details.
 */
export default function History() {
  const router = useRouter();

  return (
    <EngagementSectionScreen title="History">
      {({ engagement, isOrganiser }) =>
        isOrganiser ? (
          <>
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
