import { useRouter } from 'expo-router';

import { GoingCard, LineupCard, MyAvailabilityCard } from '@/components/availability-cards';
import { EngagementSectionScreen } from '@/components/engagement-screen';

/**
 * The lineup for organisers; for everyone else, their own answer and — once
 * the event is confirmed — who is going (D-108).
 */
export default function People() {
  const router = useRouter();

  return (
    <EngagementSectionScreen title="People">
      {({ engagement, isOrganiser }) => (
        <>
          <MyAvailabilityCard engagementId={engagement.id} />
          {isOrganiser ? (
            <LineupCard
              engagementId={engagement.id}
              closed={engagement.status === 'Completed' || engagement.status === 'Cancelled'}
              onSelectMembers={() =>
                router.push({
                  pathname: '/select-members',
                  params: { engagementId: engagement.id },
                })
              }
            />
          ) : (
            <GoingCard engagementId={engagement.id} />
          )}
        </>
      )}
    </EngagementSectionScreen>
  );
}
