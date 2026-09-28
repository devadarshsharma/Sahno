import {
  HttpTransportType,
  HubConnectionBuilder,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, type PropsWithChildren } from 'react';
import { AppState } from 'react-native';

import { isNotificationPayload } from '@/api/notifications';
import { environment } from '@/config/environment';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { useNotificationsContext } from '@/providers/notifications-provider';

/**
 * Keeps one hub connection open for the active organisation while the app is
 * in the foreground, and refetches everything under it whenever the server
 * says "changed" (TECHNICAL_ARCHITECTURE: accepted realtime architecture).
 *
 * The signal carries nothing, so the client does nothing clever with it: it
 * invalidates the organisation's query tree and lets React Query refetch what
 * is actually on screen through the REST endpoints it already trusts. A
 * reconnect does the same, because anything could have happened while the
 * socket was down. Dropping the connection in the background is what keeps
 * the phone's battery out of it — and what hands the job to push (D-080):
 * while this socket is up the banner is Sahno's own, and when it is down the
 * phone's tray takes over, so nothing is ever shown twice.
 */
export function LiveUpdatesProvider({ children }: PropsWithChildren) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();
  const { announce } = useNotificationsContext();
  const connectionRef = useRef<HubConnection | null>(null);

  const organisationId = active?.id ?? null;
  const authenticated = session.status === 'authenticated';
  const getAccessToken = session.getAccessToken;

  useEffect(() => {
    if (!authenticated || organisationId === null) {
      return;
    }

    let disposed = false;
    // Whether the socket should be up: only while the app is in the
    // foreground. Every retry path checks it, so backgrounding always wins.
    let wanted = AppState.currentState === 'active';
    let retryTimer: ReturnType<typeof setTimeout> | null = null;
    let failedStarts = 0;
    let connectedBefore = false;

    const invalidate = () =>
      queryClient.invalidateQueries({ queryKey: ['org', organisationId] });

    const connection = new HubConnectionBuilder()
      .withUrl(
        `${environment.liveHubUrl}?organisationId=${organisationId}`,
        {
          // React Native's WebSocket cannot carry headers, so the token goes
          // in the query string — the one place the API accepts it. It is
          // asked for again on every reconnect, so a refreshed token is used.
          accessTokenFactory: getAccessToken,
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        },
      )
      // Never give up while the app is open: a phone walks in and out of
      // signal, and a hosted API may be waking up or redeploying. The loop
      // ends when the app is backgrounded (stop) or this effect is disposed.
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: ({ previousRetryCount }) =>
          retryDelay(previousRetryCount),
      })
      // Silent even in development. A dropped socket is reconnected without
      // help, and a red box for every API restart teaches people to ignore
      // red boxes.
      .configureLogging(LogLevel.None)
      .build();

    // The one event with a payload: this person's own new notification, so
    // the banner goes up the moment it commits rather than after a refetch.
    const notify = (payload: unknown) => {
      if (isNotificationPayload(payload)) {
        announce(payload);
      }
    };

    const clearRetry = () => {
      if (retryTimer !== null) {
        clearTimeout(retryTimer);
        retryTimer = null;
      }
    };

    // A start that failed, or a connection that closed for good, is tried
    // again on the same backoff as a reconnect — as long as it is wanted.
    const scheduleStart = () => {
      if (disposed || !wanted || retryTimer !== null) {
        return;
      }
      const delay = retryDelay(failedStarts);
      failedStarts += 1;
      retryTimer = setTimeout(() => {
        retryTimer = null;
        void start();
      }, delay);
    };

    const start = async () => {
      clearRetry();
      // Only ever one connection: a start while one is connecting, connected
      // or reconnecting is a no-op, and one mid-stop is picked up by onclose.
      if (disposed || !wanted || connection.state !== 'Disconnected') {
        return;
      }
      try {
        await connection.start();
        failedStarts = 0;
        // Anything could have changed while there was no socket.
        if (connectedBefore) {
          void invalidate();
        }
        connectedBefore = true;
      } catch {
        // The API being away is not the app's problem to surface. Focus
        // refetching still works, and this tries again shortly.
        scheduleStart();
      }
    };

    const stop = () => connection.stop().catch(() => undefined);

    connection.on('changed', invalidate);
    connection.on('notification', notify);
    connection.onreconnected(() => void invalidate());
    connection.onclose(scheduleStart);
    connectionRef.current = connection;

    if (wanted) {
      void start();
    }

    const subscription = AppState.addEventListener('change', (status) => {
      if (status === 'active') {
        wanted = true;
        failedStarts = 0;
        void start();
      } else {
        wanted = false;
        clearRetry();
        void stop();
      }
    });

    return () => {
      disposed = true;
      wanted = false;
      clearRetry();
      subscription.remove();
      connection.off('changed', invalidate);
      connection.off('notification', notify);
      void stop();
      connectionRef.current = null;
    };
  }, [authenticated, organisationId, getAccessToken, queryClient, announce]);

  return <>{children}</>;
}

const RETRY_DELAYS_MS = [0, 2_000, 5_000, 10_000, 30_000];

/** Quick at first, then every 30 seconds for as long as it takes. */
function retryDelay(attempt: number): number {
  return RETRY_DELAYS_MS[Math.min(attempt, RETRY_DELAYS_MS.length - 1)];
}
