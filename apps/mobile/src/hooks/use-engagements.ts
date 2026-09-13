import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import {
  listEngagementActivity,
  listEngagements,
  type Engagement,
  type EngagementStatus,
} from '@/api/engagements';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * The engagements this person may see. The API scopes it: organisers get the
 * whole pipeline, a member gets only the ones they are on the lineup for
 * (D-020), so the same query serves both.
 */
export function useEngagements() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listEngagements(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

export function useEngagementActivity(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'activity'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listEngagementActivity(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Runs a change and refreshes the pipeline and the engagement's own history,
 * since a transition writes to both.
 */
export function useEngagementMutation<TArgs>(
  action: (
    accessToken: string,
    organisationId: string,
    args: TArgs,
  ) => Promise<unknown>,
) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async (args: TArgs) => {
      const accessToken = await session.getAccessToken();
      return action(accessToken, active!.id, args);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['org', active?.id, 'engagements'],
      }),
  });
}

/**
 * The pipeline in the order an organiser works it: what is live and needs
 * attention first, what is settled next, and what is over last.
 */
export const PIPELINE_ORDER: EngagementStatus[] = [
  'Draft',
  'CheckingAvailability',
  'Tentative',
  'Confirmed',
  'Postponed',
  'Completed',
  'Cancelled',
];

/** Owner/Admin wording for each state (D-039). */
/** One engagement's state, as a word on its own page. */
export const STATUS_WORDS: Record<EngagementStatus, string> = {
  Draft: 'Enquiry',
  CheckingAvailability: 'Checking availability',
  Tentative: 'Tentative',
  Confirmed: 'Confirmed',
  Postponed: 'Postponed',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

/** The list-section headings — plural where the section holds several. */
export const STATUS_LABELS: Record<EngagementStatus, string> = {
  Draft: 'Enquiries',
  CheckingAvailability: 'Checking availability',
  Tentative: 'Tentative',
  Confirmed: 'Confirmed bookings',
  Postponed: 'Postponed',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

/** The label for a single transition button. */
export const TRANSITION_LABELS: Record<EngagementStatus, string> = {
  Draft: 'Back to draft',
  CheckingAvailability: 'Request availability',
  Tentative: 'Mark tentative',
  Confirmed: 'Confirm booking',
  Postponed: 'Postpone',
  Completed: 'Mark completed',
  Cancelled: 'Cancel',
};

export function groupByStatus(
  engagements: Engagement[],
): { status: EngagementStatus; items: Engagement[] }[] {
  return PIPELINE_ORDER.map((status) => ({
    status,
    items: engagements.filter((engagement) => engagement.status === status),
  })).filter((group) => group.items.length > 0);
}

/** "Sat 20 May 2027", or a plain note when the date is still unknown. */
export function formatEngagementDate(engagement: Engagement): string {
  if (engagement.startDate === null) {
    return 'Date TBC';
  }

  const start = new Date(`${engagement.startDate}T00:00:00`);
  const formatted = start.toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });

  return engagement.endDate === null
    ? formatted
    : `${formatted} – ${new Date(`${engagement.endDate}T00:00:00`).toLocaleDateString(
        undefined,
        { day: 'numeric', month: 'short', year: 'numeric' },
      )}`;
}

/**
 * Member wording for the same states (D-039). A member is not working a
 * pipeline, so the labels describe what the event means for them — most
 * importantly, which ones are waiting on their answer.
 */
export const MEMBER_STATUS_LABELS: Record<EngagementStatus, string> = {
  Draft: 'Not yet shared',
  CheckingAvailability: 'Needs your response',
  Tentative: 'Tentative',
  Confirmed: 'Confirmed',
  Postponed: 'Postponed',
  Completed: 'Past events',
  Cancelled: 'Cancelled',
};

/**
 * Whether a booking is unresolved work for an organiser (D-041). Four things
 * qualify: somebody has still to answer; everyone has answered and the lineup
 * is now the organiser's call, because Sahno never advances to Tentative on
 * its own (D-026); it is postponed; or it is on but critical detail is missing
 * (D-048). A finished event is off the list unless money is still owed on it —
 * and only whoever holds financial access gets that count at all (D-016).
 *
 * Shared by Home's attention list and the Bookings filter, so the tile, the
 * list, and the page it opens can never disagree.
 */
export function needsAttention(engagement: Engagement): boolean {
  if (engagement.status === 'Completed') {
    return (engagement.financeOutstanding ?? 0) > 0;
  }
  if (engagement.status === 'Cancelled') {
    return false;
  }
  if ((engagement.outstandingCount ?? 0) > 0) {
    return true;
  }
  if (
    engagement.status === 'CheckingAvailability' &&
    (engagement.selectedCount ?? 0) > 0
  ) {
    return true;
  }
  if (engagement.status === 'Postponed') {
    return true;
  }
  // Only once it is actually on. A Draft enquiry with no venue yet is not a
  // loose end — nothing has been agreed for it to be loose about.
  return (
    (engagement.status === 'Confirmed' || engagement.status === 'Tentative') &&
    (engagement.readinessOutstanding ?? 0) > 0
  );
}

/**
 * What the Bookings list can be narrowed to. A status, or the attention set —
 * the same four things Home's tile counts, so the number and the page agree.
 */
export type BookingsFilter = 'attention' | EngagementStatus;

export const FILTER_LABELS: Record<BookingsFilter, string> = {
  attention: 'Needs attention',
  Draft: 'Enquiries',
  CheckingAvailability: 'Checking availability',
  Tentative: 'Tentative',
  Confirmed: 'Confirmed',
  Postponed: 'Postponed',
  Completed: 'Completed',
  Cancelled: 'Cancelled',
};

export function matchesFilter(engagement: Engagement, filter: BookingsFilter): boolean {
  return filter === 'attention' ? needsAttention(engagement) : engagement.status === filter;
}
