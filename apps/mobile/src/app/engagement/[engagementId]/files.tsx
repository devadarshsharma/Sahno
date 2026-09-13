import { EngagementSectionScreen } from '@/components/engagement-screen';
import { RehearsalsCard, ResourcesCard } from '@/components/preparation-cards';

/** Rehearsals and the repertoire, notes, and links that go with them (D-047 §4-5). */
export default function Files() {
  return (
    <EngagementSectionScreen title="Rehearsals & files">
      {({ engagement, isOrganiser }) => (
        <>
          <RehearsalsCard engagementId={engagement.id} isOrganiser={isOrganiser} />
          <ResourcesCard engagementId={engagement.id} isOrganiser={isOrganiser} />
        </>
      )}
    </EngagementSectionScreen>
  );
}
