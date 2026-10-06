import { useQuery } from '@tanstack/react-query';

import { getMyCalendar } from '@/api/my-calendar';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/** How far back the calendar reaches (D-090). */
const HISTORY_MONTHS = 12;

function historyStart(): string {
  const from = new Date();
  from.setMonth(from.getMonth() - HISTORY_MONTHS, 1);
  return `${from.getFullYear()}-${`${from.getMonth() + 1}`.padStart(2, '0')}-01`;
}

/**
 * The caller's own calendar across all their organisations (D-091): the
 * calendar view, and what clashes are checked against. Kept under the active
 * organisation's key, so a change there (pushed live) refreshes it too, and
 * switching organisations starts it afresh.
 */
export function useMyCalendar(enabled = true) {
  const session = useSession();
  const { active } = useActiveOrg();
  const fromDate = historyStart();

  return useQuery({
    queryKey: ['org', active?.id, 'my-calendar', fromDate],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getMyCalendar(accessToken, fromDate, signal);
    },
    enabled: enabled && session.status === 'authenticated',
  });
}
