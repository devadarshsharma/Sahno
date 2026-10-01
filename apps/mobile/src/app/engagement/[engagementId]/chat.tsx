import { DiscussionCard } from '@/components/discussion-card';
import { EngagementSectionScreen } from '@/components/engagement-screen';
import { useMarkDiscussionRead } from '@/hooks/use-discussion';

/** The event's own conversation (D-047 §6, D-024). */
export default function Chat() {
  return (
    <EngagementSectionScreen title="Chat">
      {({ engagement, isOrganiser }) => (
        <Thread engagementId={engagement.id} isOrganiser={isOrganiser} />
      )}
    </EngagementSectionScreen>
  );
}

/** The thread, marked read for as long as it is on screen. */
function Thread({ engagementId, isOrganiser }: { engagementId: string; isOrganiser: boolean }) {
  useMarkDiscussionRead(engagementId);

  return <DiscussionCard engagementId={engagementId} isOrganiser={isOrganiser} />;
}
