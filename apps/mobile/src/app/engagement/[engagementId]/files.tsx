import { EngagementSectionScreen } from '@/components/engagement-screen';
import { RehearsalsCard, ResourcesCard } from '@/components/preparation-cards';
import { SetListCard } from '@/components/setlist-card';

/** The set list, rehearsals, and the notes and links that go with them (D-047 §4-5, D-079). */
export default function Files() {
  return (
    <EngagementSectionScreen title="Set list & rehearsals">
      {({ engagement, isOrganiser }) => (
        <>
          <SetListCard engagementId={engagement.id} isOrganiser={isOrganiser} />
          <RehearsalsCard engagementId={engagement.id} isOrganiser={isOrganiser} />
          <ResourcesCard engagementId={engagement.id} isOrganiser={isOrganiser} />
        </>
      )}
    </EngagementSectionScreen>
  );
}
