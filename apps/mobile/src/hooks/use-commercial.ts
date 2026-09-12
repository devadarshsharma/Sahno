import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { getCustomer, getFinance, listPayments } from '@/api/commercial';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * Whether the caller may see money here. The Owner, or an Admin the Owner
 * has granted it (D-016). Cards check this before rendering rather than
 * rendering and being refused.
 */
export function useFinancialAccess(): boolean {
  const { active } = useActiveOrg();
  return active?.hasFinancialAccess === true;
}

/** The customer. Organisers only; the query stays off for everyone else. */
export function useCustomer(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();
  const isOrganiser = active?.role === 'Owner' || active?.role === 'Admin';

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'customer'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getCustomer(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && isOrganiser && session.status === 'authenticated',
  });
}

export function useFinance(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();
  const allowed = useFinancialAccess();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'finance'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getFinance(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && allowed && session.status === 'authenticated',
  });
}

export function usePayments(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();
  const allowed = useFinancialAccess();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'payments'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listPayments(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && allowed && session.status === 'authenticated',
  });
}

/**
 * Runs a commercial change and refreshes the engagement tree. Money moves the
 * outstanding count on the list, which is what Home's attention row reads.
 */
export function useCommercialMutation<TArgs>(
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
