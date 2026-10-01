import { useQuery } from '@tanstack/react-query';

import { listChats } from '@/api/chats';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * The Chat tab's inbox. Under the organisation's key, so the live "changed"
 * signal refreshes it like everything else — a new message moves its
 * conversation to the top without a pull.
 */
export function useChatInbox() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'chats'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listChats(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/** Unread across the inbox, for the tab badge: it counts what the tab shows. */
export function useUnreadChatTotal(): number {
  const inbox = useChatInbox();
  return (inbox.data ?? []).reduce((total, entry) => total + entry.unreadMessages, 0);
}
