import {
  BricolageGrotesque_400Regular,
  BricolageGrotesque_500Medium,
  BricolageGrotesque_600SemiBold,
  BricolageGrotesque_700Bold,
  useFonts,
} from '@expo-google-fonts/bricolage-grotesque';
import { Stack } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useEffect } from 'react';
import { StyleSheet, View } from 'react-native';

import { SahnoSymbol } from '@/components/brand';
import { AuthProvider, useSession } from '@/providers/auth-provider';
import { QueryProvider } from '@/providers/query-provider';
import { colors } from '@/theme';

SplashScreen.preventAutoHideAsync();

export default function RootLayout() {
  const [fontsLoaded, fontError] = useFonts({
    BricolageGrotesque_400Regular,
    BricolageGrotesque_500Medium,
    BricolageGrotesque_600SemiBold,
    BricolageGrotesque_700Bold,
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

  // Session restoration from secure storage: continue the OS splash visual
  // (navy + centred mark) rather than flashing the sign-in screen at an
  // already-authenticated person.
  if (status === 'loading') {
    return (
      <View style={styles.loading}>
        <SahnoSymbol size={155} />
      </View>
    );
  }

  const isAuthenticated = status === 'authenticated';

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={isAuthenticated}>
        <Stack.Screen name="index" />
        <Stack.Screen name="brand-preview" />
      </Stack.Protected>
      <Stack.Protected guard={!isAuthenticated}>
        <Stack.Screen name="sign-in" />
      </Stack.Protected>
    </Stack>
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
