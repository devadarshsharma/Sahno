import {
  HttpTransportType,
  HubConnectionBuilder,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, type PropsWithChildren } from 'react';
import { AppState } from 'react-native';

import { environment } from '@/config/environment';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

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
 * the phone's battery out of it.
 */
export function LiveUpdatesProvider({ children }: PropsWithChildren) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();
  const connectionRef = useRef<HubConnection | null>(null);

  const organisationId = active?.id ?? null;
  const authenticated = session.status === 'authenticated';
  const getAccessToken = session.getAccessToken;

  useEffect(() => {
    if (!authenticated || organisationId === null) {
      return;
    }

    let disposed = false;

    const invalidate = () =>
      queryClient.invalidateQueries({ queryKey: ['org', organisationId] });

    const connection = new HubConnectionBuilder()
      .withUrl(
        `${environment.apiUrl}/hubs/live?organisationId=${organisationId}`,
        {
          // React Native's WebSocket cannot carry headers, so the token goes
          // in the query string — the one place the API accepts it.
          accessTokenFactory: getAccessToken,
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        },
      )
      .withAutomaticReconnect()
      .configureLogging(__DEV__ ? LogLevel.Warning : LogLevel.None)
      .build();

    connection.on('changed', invalidate);
    connection.onreconnected(invalidate);
    connectionRef.current = connection;

    const start = async () => {
      if (disposed || connection.state !== 'Disconnected') {
        return;
      }
      try {
        await connection.start();
      } catch {
        // The API being away is not the app's problem to surface. Focus
        // refetching still works; the next foreground tries again.
      }
    };

    const stop = () => connection.stop().catch(() => undefined);

    if (AppState.currentState === 'active') {
      void start();
    }

    const subscription = AppState.addEventListener('change', (status) => {
      if (status === 'active') {
        void start();
      } else {
        void stop();
      }
    });

    return () => {
      disposed = true;
      subscription.remove();
      connection.off('changed', invalidate);
      void stop();
      connectionRef.current = null;
    };
  }, [authenticated, organisationId, getAccessToken, queryClient]);

  return <>{children}</>;
}
