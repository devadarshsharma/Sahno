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
