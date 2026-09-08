import {
  focusManager,
  QueryClient,
  QueryClientProvider,
} from '@tanstack/react-query';
import { PropsWithChildren, useEffect, useState } from 'react';
import { AppState, type AppStateStatus } from 'react-native';

export function QueryProvider({ children }: PropsWithChildren) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            retry: 2,
            staleTime: 30_000,
          },
        },
      }),
  );

  // React Query's focus tracking is written for browsers, so on native it
  // never fires and stale data sits there until something invalidates it.
  // Wiring it to AppState means coming back to the app refreshes what other
  // people may have changed while it was in the background.
  useEffect(() => {
    const subscription = AppState.addEventListener(
      'change',
      (status: AppStateStatus) => {
        focusManager.setFocused(status === 'active');
      },
    );

    return () => subscription.remove();
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
}
