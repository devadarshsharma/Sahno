import { Stack, useRouter, type ErrorBoundaryProps } from 'expo-router';

import { CrashScreen } from '@/components/crash-screen';

/**
 * The event workspace as a small stack: an overview that indexes the sections
 * (D-047), and one screen per section beneath it. Splitting it was not for
 * tidiness — it was a twelve-card scroll, and every slice added to it.
 */
export default function EngagementLayout() {
  return <Stack screenOptions={{ headerShown: false }} />;
}

/**
 * A section of one event that fails to draw shows this, and the rest of the
 * app carries on: Back leaves the event, Try again redraws it (D-110).
 */
export function ErrorBoundary({ error, retry }: ErrorBoundaryProps) {
  const router = useRouter();
  return (
    <CrashScreen
      error={error}
      onRetry={() => void retry()}
      onBack={router.canGoBack() ? () => router.back() : undefined}
    />
  );
}
