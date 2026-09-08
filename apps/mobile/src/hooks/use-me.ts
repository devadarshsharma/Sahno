import { useQuery } from '@tanstack/react-query';

import { getMe, type MeResponse } from '@/api/me';
import { useSession } from '@/providers/auth-provider';

/** The signed-in person's own Sahno account record. */
export function useMe() {
  const session = useSession();

  return useQuery({
    queryKey: ['me'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getMe(accessToken, signal);
    },
    enabled: session.status === 'authenticated',
  });
}

/**
 * Whether the person still owes us a display name (D-046 requires one). False
 * while the account is loading, so no screen redirects on incomplete data.
 */
export function useNeedsDisplayName(): boolean {
  const { data, isSuccess } = useMe();
  return isSuccess && data.displayName === null;
}

/** The first name to greet someone by, when one is known. */
export function firstNameOf(me: MeResponse | undefined): string | undefined {
  return me?.displayName?.trim().split(/\s+/)[0];
}
