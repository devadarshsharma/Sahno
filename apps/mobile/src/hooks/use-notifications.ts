import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import {
  getUnreadCount,
  listNotifications,
  markAllNotificationsRead,
  markNotificationRead,
} from '@/api/notifications';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * The badge count. Refetched on every focus (the query client already wires
 * focusManager to AppState), which is as close to live as the pilot needs
 * until push or SignalR arrives.
 */
export function useUnreadCount() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'notifications', 'unread'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getUnreadCount(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
    // Cheap, and the number people look at first when they open the app.
    staleTime: 15_000,
  });
}

export function useNotifications() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'notifications', 'list'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listNotifications(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

export function useMarkRead() {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async (notificationId: string) => {
      const accessToken = await session.getAccessToken();
      await markNotificationRead(accessToken, active!.id, notificationId);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['org', active?.id, 'notifications'],
      }),
  });
}

export function useMarkAllRead() {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async () => {
      const accessToken = await session.getAccessToken();
      await markAllNotificationsRead(accessToken, active!.id);
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ['org', active?.id, 'notifications'],
      }),
  });
}
