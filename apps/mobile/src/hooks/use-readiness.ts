import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { getReadiness } from '@/api/readiness';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/** The readiness checklist. Organisers only — members are refused it. */
export function useReadiness(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'readiness'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getReadiness(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && isOrganiser && session.status === 'authenticated',
  });
}

/**
 * Changes a checklist item and refreshes the engagement tree, since readiness
 * also drives what appears on the organiser's Home.
 */
export function useReadinessMutation<TArgs>(
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
