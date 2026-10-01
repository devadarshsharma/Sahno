import { useQuery } from '@tanstack/react-query';

import { listUpcomingRehearsals } from '@/api/upcoming-rehearsals';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * Upcoming rehearsals across events, for Home and the calendar. Under the
 * organisation's key, so booking or moving one refreshes it live.
 */
export function useUpcomingRehearsals() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'rehearsals', 'upcoming'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listUpcomingRehearsals(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}
