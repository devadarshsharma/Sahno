import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import {
  getAvailability,
  getOwnAvailability,
} from '@/api/availability';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/** The organiser's view of a lineup. Members are refused this, so it is theirs alone. */
export function useAvailability(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'availability'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getAvailability(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && isOrganiser && session.status === 'authenticated',
  });
}

/** The caller's own answer — the only availability a member can read. */
export function useOwnAvailability(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'availability', 'me'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getOwnAvailability(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Runs an availability change and refreshes what it touches: the lineup, the
 * caller's own answer, and the engagement list, since the first request also
 * moves a Draft into Checking availability.
 */
export function useAvailabilityMutation<TArgs>(
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
