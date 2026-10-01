import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { listDiscussion, markDiscussionRead } from '@/api/discussion';
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

/**
 * While a chat is on screen, what is in it has been read. Marks it so on
 * opening, and again whenever a newer message arrives while it is open, then
 * refreshes the event list (the unread bubbles) and the bell (the same rows).
 */
export function useMarkDiscussionRead(engagementId: string) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();
  const thread = useDiscussion(engagementId);

  const organisationId = active?.id ?? null;
  const latestMessageId = thread.data?.at(-1)?.id ?? null;
  const getAccessToken = session.getAccessToken;

  useEffect(() => {
    if (organisationId === null || !thread.isSuccess) {
      return;
    }

    let cancelled = false;
    void (async () => {
      try {
        const accessToken = await getAccessToken();
        await markDiscussionRead(accessToken, organisationId, engagementId);
        if (cancelled) {
          return;
        }
        // The list (not the thread under it), the Chat inbox, and the bell.
        await Promise.all([
          queryClient.invalidateQueries({
            queryKey: ['org', organisationId, 'engagements'],
            exact: true,
          }),
          queryClient.invalidateQueries({
            queryKey: ['org', organisationId, 'chats'],
          }),
          queryClient.invalidateQueries({
            queryKey: ['org', organisationId, 'notifications'],
          }),
        ]);
      } catch {
        // A bubble left on is the worst outcome; the next open clears it.
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [organisationId, engagementId, latestMessageId, thread.isSuccess, getAccessToken, queryClient]);
}
