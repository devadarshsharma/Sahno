import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

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

/** Every unread notification — what the bell is for (D-111). */
export function useUnreadNotifications() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'notifications', 'list', 'unread'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      const rows = await listNotifications(accessToken, active!.id, signal, { state: 'unread' });
      // An older API answers with both kinds; keep only what is unread.
      return rows.filter((row) => !row.isRead);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/** Page size of the read list; matches the API's (D-111). */
const READ_PAGE_SIZE = 20;

/**
 * Notifications already read, twenty at a time, newest first — fetched only
 * once someone asks to see them (D-111).
 */
export function useReadNotifications(enabled: boolean) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useInfiniteQuery({
    queryKey: ['org', active?.id, 'notifications', 'list', 'read'],
    initialPageParam: undefined as string | undefined,
    queryFn: async ({ signal, pageParam }) => {
      const accessToken = await session.getAccessToken();
      const rows = await listNotifications(accessToken, active!.id, signal, {
        state: 'read',
        before: pageParam,
      });
      return rows.filter((row) => row.isRead);
    },
    getNextPageParam: (last) =>
      last.length < READ_PAGE_SIZE ? undefined : last[last.length - 1].createdAtUtc,
    enabled: enabled && active !== null && session.status === 'authenticated',
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
