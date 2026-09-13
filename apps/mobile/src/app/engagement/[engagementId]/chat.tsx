import { DiscussionCard } from '@/components/discussion-card';
import { EngagementSectionScreen } from '@/components/engagement-screen';

/** The event's own conversation (D-047 §6, D-024). */
export default function Chat() {
  return (
    <EngagementSectionScreen title="Chat">
      {({ engagement, isOrganiser }) => (
        <DiscussionCard engagementId={engagement.id} isOrganiser={isOrganiser} />
      )}
    </EngagementSectionScreen>
  );
}
