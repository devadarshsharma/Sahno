import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { getCustomerDetail, listCustomers } from '@/api/customers';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/** Whether the caller may see the customer directory at all (D-022). */
export function useIsOrganiser(): boolean {
  const { active } = useActiveOrg();
  return active?.role === 'Owner' || active?.role === 'Admin';
}

/** The directory. Organisers only; the query stays off for everyone else. */
export function useCustomers() {
  const session = useSession();
  const { active } = useActiveOrg();
  const isOrganiser = useIsOrganiser();

  return useQuery({
    queryKey: ['org', active?.id, 'customers'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listCustomers(accessToken, active!.id, signal);
    },
    enabled: active !== null && isOrganiser && session.status === 'authenticated',
  });
}

/** One customer with every booking they have had. */
export function useCustomerDetail(customerId: string | null) {
  const session = useSession();
  const { active } = useActiveOrg();
  const isOrganiser = useIsOrganiser();

  return useQuery({
    queryKey: ['org', active?.id, 'customers', customerId],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getCustomerDetail(accessToken, active!.id, customerId!, signal);
    },
    enabled:
      active !== null &&
      isOrganiser &&
      customerId !== null &&
      session.status === 'authenticated',
  });
}

/**
 * Runs a directory change and refreshes it. Engagements are refreshed too:
 * a renamed customer shows on every booking's customer section.
 */
export function useCustomerMutation<TArgs, TResult = unknown>(
  action: (
    accessToken: string,
    organisationId: string,
    args: TArgs,
  ) => Promise<TResult>,
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
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['org', active?.id, 'customers'] }),
        queryClient.invalidateQueries({ queryKey: ['org', active?.id, 'engagements'] }),
      ]),
  });
}
