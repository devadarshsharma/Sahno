import {
  BricolageGrotesque_400Regular,
  BricolageGrotesque_500Medium,
  BricolageGrotesque_600SemiBold,
  BricolageGrotesque_700Bold,
  useFonts,
} from '@expo-google-fonts/bricolage-grotesque';
import {
  InstrumentSans_400Regular,
  InstrumentSans_500Medium,
  InstrumentSans_600SemiBold,
} from '@expo-google-fonts/instrument-sans';
import { useQueryClient } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { Appearance, StyleSheet, View } from 'react-native';

import { SahnoSymbol } from '@/components/brand';
import { AuthProvider, useSession } from '@/providers/auth-provider';
import { LiveUpdatesProvider } from '@/providers/live-updates-provider';
import { QueryProvider } from '@/providers/query-provider';
import { useActiveOrganisation } from '@/stores/active-organisation';
import { colors } from '@/theme';

SplashScreen.preventAutoHideAsync();

// Sahno has one palette and it is a light one, so the app says so rather than
// following the device. Without this, anything the platform themes for itself —
// date pickers, dialogs, keyboards — turns dark against light Sahno surfaces.
// app.json and the Android theme declare the same thing for a fresh build; this
// is what makes it true in an already-installed one.
Appearance.setColorScheme('light');

export default function RootLayout() {
  const [fontsLoaded, fontError] = useFonts({
    BricolageGrotesque_400Regular,
    BricolageGrotesque_500Medium,
    BricolageGrotesque_600SemiBold,
    BricolageGrotesque_700Bold,
    InstrumentSans_400Regular,
    InstrumentSans_500Medium,
    InstrumentSans_600SemiBold,
  });

  useEffect(() => {
    if (fontsLoaded || fontError) {
      SplashScreen.hideAsync();
    }
  }, [fontsLoaded, fontError]);

  // Keep the splash screen up until the brand font is ready so branded UI
  // never renders with a fallback typeface.
  if (!fontsLoaded && !fontError) {
    return null;
  }

  return (
    <AuthProvider>
      <QueryProvider>
        <LiveUpdatesProvider>
          <RootNavigator />
        </LiveUpdatesProvider>
      </QueryProvider>
    </AuthProvider>
  );
}

function RootNavigator() {
  const { status } = useSession();
  const queryClient = useQueryClient();
  const setActiveOrganisation = useActiveOrganisation(
    (state) => state.setActiveOrganisation,
  );

  // Signing out must clear every trace of the previous account: cached
  // queries (organisations, profile) and the active-organisation context.
  // Without this, the next sign-in briefly shows the previous account's data.
  useEffect(() => {
    if (status === 'unauthenticated') {
      queryClient.clear();
      setActiveOrganisation(null);
    }
  }, [status, queryClient, setActiveOrganisation]);

  // Session restoration from secure storage: continue the OS splash visual
  // (navy + centred mark) rather than flashing the sign-in screen at an
  // already-authenticated person.
  if (status === 'loading') {
    return (
      <View style={styles.loading}>
        <StatusBar style="light" />
        <SahnoSymbol size={155} />
      </View>
    );
  }

  const isAuthenticated = status === 'authenticated';

  return (
    <>
      {/* Dark status-bar icons for the light app surfaces; the navy sign-in
          screen overrides this with its own light-icon StatusBar while
          mounted. */}
      <StatusBar style="dark" />
      <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={isAuthenticated}>
        <Stack.Screen name="(tabs)" />
        <Stack.Screen name="onboarding" />
        <Stack.Screen name="set-name" />
        <Stack.Screen name="member/[membershipId]" />
        <Stack.Screen name="create-engagement" />
        <Stack.Screen name="customers/index" />
        <Stack.Screen name="customers/new" />
        <Stack.Screen name="customers/[customerId]" />
        <Stack.Screen name="repertoire/index" />
        <Stack.Screen name="repertoire/new" />
        <Stack.Screen name="repertoire/[pieceId]/index" />
        <Stack.Screen name="repertoire/[pieceId]/lyrics" options={{ presentation: 'fullScreenModal' }} />
        <Stack.Screen name="engagement/[engagementId]" />
        <Stack.Screen name="select-members" />
        <Stack.Screen name="create-organisation" />
        <Stack.Screen name="join" />
        <Stack.Screen name="invitations" />
        <Stack.Screen name="notifications" />
        <Stack.Screen name="switch-organisation" />
        <Stack.Screen name="brand-preview" />
      </Stack.Protected>
      <Stack.Protected guard={!isAuthenticated}>
        <Stack.Screen name="sign-in" />
      </Stack.Protected>
      </Stack>
    </>
  );
}

const styles = StyleSheet.create({
  loading: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.navy,
  },
});
