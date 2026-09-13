import { EngagementSectionScreen } from '@/components/engagement-screen';
import { ResponsibilitiesCard } from '@/components/preparation-cards';

/** Who is doing or bringing what (D-047 §3). */
export default function Jobs() {
  return (
    <EngagementSectionScreen title="Jobs">
      {({ engagement, isOrganiser }) => (
        <ResponsibilitiesCard engagementId={engagement.id} isOrganiser={isOrganiser} />
      )}
    </EngagementSectionScreen>
  );
}
