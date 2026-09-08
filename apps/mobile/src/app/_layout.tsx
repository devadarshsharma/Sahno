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
import { StyleSheet, View } from 'react-native';

import { SahnoSymbol } from '@/components/brand';
import { AuthProvider, useSession } from '@/providers/auth-provider';
import { QueryProvider } from '@/providers/query-provider';
import { useActiveOrganisation } from '@/stores/active-organisation';
import { colors } from '@/theme';

SplashScreen.preventAutoHideAsync();

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
        <RootNavigator />
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
        <Stack.Screen name="create-organisation" />
        <Stack.Screen name="join" />
        <Stack.Screen name="invitations" />
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
