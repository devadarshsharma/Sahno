import { useQueryClient } from '@tanstack/react-query';
import * as Notifications from 'expo-notifications';
import { router } from 'expo-router';
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type PropsWithChildren,
} from 'react';
import { AppState } from 'react-native';

import { registerPushDevice, type NotificationPayload } from '@/api/notifications';
import { NotificationBanner } from '@/components/notification-banner';
import { useActiveOrg } from '@/hooks/use-organisations';
import { markShown, wasShown } from '@/notifications/dedupe';
import { installForegroundHandler, payloadOf, registerForPush } from '@/notifications/push';
import { useSession } from '@/providers/auth-provider';
import { usePushToken } from '@/stores/push-token';

type NotificationsContextValue = {
  /**
   * Shows the in-app banner for a notification that just arrived (from the
   * live connection, or from a push while the app is open). Duplicates by
   * notification id are dropped.
   */
  announce: (payload: NotificationPayload) => void;
  /** Opens what a notification is about, switching organisation if it must. */
  open: (payload: Pick<NotificationPayload, 'organisationId' | 'route'>) => void;
};

const NotificationsContext = createContext<NotificationsContextValue | null>(null);

// Installed once, at module load, so it is in place before the first push —
// including the one that may be launching the app.
installForegroundHandler();

/**
 * Everything about notifications reaching this phone (D-080):
 *
 * - permission and the Expo push token, registered with the API after
 *   sign-in and re-registered if it changes; a denied permission is simply
 *   no push, and nothing else about the app changes;
 * - the in-app banner while the app is open, fed by the live connection
 *   (see LiveUpdatesProvider) and by pushes that arrive in the foreground;
 * - taps — on the banner, in the tray, on the lock screen, or the one that
 *   cold-started the app — all going to the route the API named, once the
 *   session and organisation list are ready to take them there.
 */
export function NotificationsProvider({ children }: PropsWithChildren) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { organisations, active, isPending, switchTo } = useActiveOrg();
  const setToken = usePushToken((state) => state.setToken);

  const [banner, setBanner] = useState<NotificationPayload | null>(null);
  const pendingRoute = useRef<Pick<NotificationPayload, 'organisationId' | 'route'> | null>(null);
  const handledResponse = useRef<string | null>(null);

  const authenticated = session.status === 'authenticated';
  const getAccessToken = session.getAccessToken;
  const ready = authenticated && !isPending && organisations.length > 0;
  const activeId = active?.id ?? null;

  // ---- Token registration ---------------------------------------------

  useEffect(() => {
    if (!authenticated) {
      return;
    }

    let cancelled = false;

    const register = async () => {
      const registration = await registerForPush();
      if (cancelled) {
        return;
      }
      if (registration.status !== 'registered') {
        setToken(null);
        if (registration.status === 'unavailable' && __DEV__) {
          console.info(`[push] not registered: ${registration.reason}`);
        }
        return;
      }
      try {
        const accessToken = await getAccessToken();
        await registerPushDevice(accessToken, {
          token: registration.token,
          platform: registration.platform,
          deviceName: registration.deviceName,
        });
        if (!cancelled) {
          setToken(registration.token);
        }
      } catch {
        // The API being away is not the app's problem to surface; the next
        // sign-in or foreground registers again.
      }
    };

    void register();

    // Expo rotates tokens rarely, but when it does the old one goes dead and
    // Expo tells the app the new one here.
    const rotation = Notifications.addPushTokenListener(() => {
      void register();
    });

    return () => {
      cancelled = true;
      rotation.remove();
    };
  }, [authenticated, getAccessToken, setToken]);

  // ---- Opening what a notification is about ----------------------------

  const go = useCallback(
    (target: Pick<NotificationPayload, 'organisationId' | 'route'>) => {
      if (target.organisationId !== activeId) {
        if (!organisations.some((org) => org.id === target.organisationId)) {
          // No longer a member of that organisation. Home is the honest place.
          router.navigate('/(tabs)');
          return;
        }
        switchTo(target.organisationId);
      }
      router.navigate(target.route as never);
    },
    [activeId, organisations, switchTo],
  );

  const open = useCallback(
    (target: Pick<NotificationPayload, 'organisationId' | 'route'>) => {
      if (!ready) {
        // Cold start from a tap: the session or the organisation list is
        // still loading. Hold the route and take it once they are here.
        pendingRoute.current = target;
        return;
      }
      go(target);
    },
    [ready, go],
  );

  useEffect(() => {
    if (ready && pendingRoute.current) {
      const target = pendingRoute.current;
      pendingRoute.current = null;
      go(target);
    }
  }, [ready, go]);

  // ---- The banner -------------------------------------------------------

  const refreshBell = useCallback(
    (organisationId: string) =>
      queryClient.invalidateQueries({ queryKey: ['org', organisationId, 'notifications'] }),
    [queryClient],
  );

  const announce = useCallback(
    (payload: NotificationPayload) => {
      void refreshBell(payload.organisationId);
      if (wasShown(payload.notificationId)) {
        return;
      }
      markShown(payload.notificationId);
      if (AppState.currentState === 'active') {
        setBanner(payload);
      }
    },
    [refreshBell],
  );

  const dismissBanner = useCallback(() => setBanner(null), []);

  // ---- Push listeners -------------------------------------------------------

  useEffect(() => {
    // A push that lands while the app is open. The OS shows nothing (see the
    // handler); Sahno shows its banner — unless the live connection already
    // did for this very notification.
    const received = Notifications.addNotificationReceivedListener((notification) => {
      const payload = payloadOf(notification);
      if (payload) {
        announce(payload);
      }
    });

    // A tap, wherever it happened: tray, lock screen, or the banner the OS
    // showed while the app was in the background.
    const responded = Notifications.addNotificationResponseReceivedListener((response) => {
      const payload = payloadOf(response.notification);
      if (!payload) {
        return;
      }
      handledResponse.current = response.notification.request.identifier;
      markShown(payload.notificationId);
      void refreshBell(payload.organisationId);
      open(payload);
    });

    return () => {
      received.remove();
      responded.remove();
    };
  }, [announce, open, refreshBell]);

  // The tap that launched the app from cold. Asked once the session is
  // ready, and remembered so a re-render does not open it twice.
  useEffect(() => {
    if (!ready) {
      return;
    }
    let cancelled = false;
    void Notifications.getLastNotificationResponseAsync().then((response) => {
      if (cancelled || !response) {
        return;
      }
      const identifier = response.notification.request.identifier;
      if (handledResponse.current === identifier) {
        return;
      }
      const payload = payloadOf(response.notification);
      if (!payload) {
        return;
      }
      handledResponse.current = identifier;
      open(payload);
    });
    return () => {
      cancelled = true;
    };
  }, [ready, open]);

  const value = useMemo<NotificationsContextValue>(() => ({ announce, open }), [announce, open]);

  return (
    <NotificationsContext.Provider value={value}>
      {children}
      {banner ? (
        <NotificationBanner
          payload={banner}
          onOpen={() => {
            setBanner(null);
            open(banner);
          }}
          onDismiss={dismissBanner}
        />
      ) : null}
    </NotificationsContext.Provider>
  );
}

export function useNotificationsContext(): NotificationsContextValue {
  const value = useContext(NotificationsContext);
  if (!value) {
    throw new Error('useNotificationsContext must be used inside NotificationsProvider');
  }
  return value;
}
