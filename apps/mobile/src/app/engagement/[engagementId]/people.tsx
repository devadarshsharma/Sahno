import { useRouter } from 'expo-router';

import { LineupCard, MyAvailabilityCard } from '@/components/availability-cards';
import { EngagementSectionScreen } from '@/components/engagement-screen';

/** The lineup for organisers; a member's own answer for everyone else. */
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
              onSelectMembers={() =>
                router.push({
                  pathname: '/select-members',
                  params: { engagementId: engagement.id },
                })
              }
            />
          ) : null}
        </>
      )}
    </EngagementSectionScreen>
  );
}
