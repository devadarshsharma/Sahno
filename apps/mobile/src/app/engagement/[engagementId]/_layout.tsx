import { Stack } from 'expo-router';

/**
 * The event workspace as a small stack: an overview that indexes the sections
 * (D-047), and one screen per section beneath it. Splitting it was not for
 * tidiness — it was a twelve-card scroll, and every slice added to it.
 */
export default function EngagementLayout() {
  return <Stack screenOptions={{ headerShown: false }} />;
}
