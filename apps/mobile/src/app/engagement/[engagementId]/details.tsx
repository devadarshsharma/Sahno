import { DatesCard } from '@/components/admin-cards';
import { EngagementSectionScreen } from '@/components/engagement-screen';
import { DayOfCard, DetailsCard } from '@/components/workspace-cards';

/** Where, when, what to wear — read by everyone, edited by organisers. */
export default function Details() {
  return (
    <EngagementSectionScreen title="Details">
      {({ engagement, isOrganiser }) => (
        <>
          <DayOfCard engagement={engagement} />
          {isOrganiser ? <DatesCard engagement={engagement} /> : null}
          {isOrganiser ? <DetailsCard engagement={engagement} /> : null}
        </>
      )}
    </EngagementSectionScreen>
  );
}
