import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { askForDetails, getDetailRequests, type AskableDetail } from '@/api/detail-requests';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/** Which details are still to come on an event, and which the caller asked for (D-089). */
export function useDetailRequests(engagementId: string, enabled = true) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'detail-requests'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getDetailRequests(accessToken, active!.id, engagementId, signal);
    },
    enabled: enabled && active !== null && session.status === 'authenticated',
  });
}

export function useAskForDetails(engagementId: string) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async (items: AskableDetail[]) => {
      const accessToken = await session.getAccessToken();
      return askForDetails(accessToken, active!.id, engagementId, items);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['org', active?.id, 'engagements', engagementId, 'detail-requests'],
      }),
  });
}
