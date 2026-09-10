import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { listDiscussion } from '@/api/discussion';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * The thread for one engagement, oldest first. Access follows the engagement,
 * so anyone who can open the booking can read this — there is no separate
 * permission to check on the client.
 */
export function useDiscussion(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'discussion'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listDiscussion(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Runs a discussion change and refreshes just the thread. Nothing here moves
 * readiness or the engagement list, so there is no reason to invalidate them.
 */
export function useDiscussionMutation<TArgs>(
  engagementId: string,
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
        queryKey: ['org', active?.id, 'engagements', engagementId, 'discussion'],
      }),
  });
}
