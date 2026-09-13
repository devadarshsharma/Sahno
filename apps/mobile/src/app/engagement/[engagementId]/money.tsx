import { CustomerCard, FinanceCard } from '@/components/commercial-cards';
import { EngagementSectionScreen } from '@/components/engagement-screen';

/**
 * Customer and money (D-047 §7). Customer for every organiser; the money card
 * mounts only with financial access (D-016). Members are never routed here.
 */
export default function Money() {
  return (
    <EngagementSectionScreen title="Customer & money">
      {({ engagement, isOrganiser }) =>
        isOrganiser ? (
          <>
            <CustomerCard engagementId={engagement.id} />
            <FinanceCard engagementId={engagement.id} />
          </>
        ) : null
      }
    </EngagementSectionScreen>
  );
}
