import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { listMembers, type Member } from '@/api/members';
import { useSession } from '@/providers/auth-provider';
import { useActiveOrg } from '@/hooks/use-organisations';

/** The member directory of the active organisation (D-018). */
export function useMembers() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'members'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listMembers(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Runs a membership change and refreshes the directory. Also refreshes the
 * organisation list, because a change can alter the caller's own role — and
 * with it what the rest of the app lets them do.
 */
export function useMemberMutation<TArgs>(
  action: (accessToken: string, organisationId: string, args: TArgs) => Promise<void>,
) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async (args: TArgs) => {
      const accessToken = await session.getAccessToken();
      await action(accessToken, active!.id, args);
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['org', active?.id, 'members'],
        }),
        queryClient.invalidateQueries({ queryKey: ['organisations'] }),
      ]);
    },
  });
}

/** Sorts the directory the way people look for each other: organisers first. */
export function sortedForDisplay(members: Member[]): Member[] {
  const rank: Record<Member['role'], number> = { Owner: 0, Admin: 1, Member: 2 };
  return [...members].sort((a, b) => {
    if (rank[a.role] !== rank[b.role]) {
      return rank[a.role] - rank[b.role];
    }
    return (a.displayName ?? '').localeCompare(b.displayName ?? '');
  });
}
