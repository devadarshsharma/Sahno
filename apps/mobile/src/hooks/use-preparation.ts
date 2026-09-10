import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { listRehearsals, listResources } from '@/api/preparation';
import { listResponsibilities } from '@/api/responsibilities';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * The job list. Everyone on the event gets it, members included — knowing that
 * somebody else is bringing the harmonium is what stops two people bringing
 * one and nobody bringing the tabla.
 */
export function useResponsibilities(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: [
      'org',
      active?.id,
      'engagements',
      engagementId,
      'responsibilities',
    ],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listResponsibilities(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

export function useRehearsals(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'rehearsals'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listRehearsals(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Notes and links. A member's copy of this list has already had the
 * Admins-only rows removed by the API, so nothing here needs hiding.
 */
export function useResources(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'resources'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listResources(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Runs a preparation change and refreshes the whole engagement tree. Any of
 * these can move a readiness item, and readiness drives what appears on the
 * organiser's Home, so nothing narrower would keep the screens honest.
 */
export function usePreparationMutation<TArgs>(
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
