import { EngagementSectionScreen } from '@/components/engagement-screen';
import { ReadinessCard } from '@/components/workspace-cards';

/** The organiser's checklist (D-048). Members never see it. */
export default function Readiness() {
  return (
    <EngagementSectionScreen title="Readiness">
      {({ engagement, isOrganiser }) =>
        isOrganiser ? <ReadinessCard engagementId={engagement.id} /> : null
      }
    </EngagementSectionScreen>
  );
}
