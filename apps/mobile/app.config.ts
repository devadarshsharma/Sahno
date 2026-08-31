import type { ConfigContext, ExpoConfig } from 'expo/config';

/**
 * Dynamic Expo config layered over app.json.
 *
 * The Auth0 config plugin needs the tenant domain at prebuild time to register
 * the native callback intent filter. It reads the SAME environment variable as
 * the runtime (EXPO_PUBLIC_AUTH0_DOMAIN), so build-time and runtime values
 * cannot silently diverge. When the variable is unset (e.g. CI lint/doctor
 * runs), a sentinel domain keeps config evaluation working; the app itself
 * refuses authentication attempts until real configuration is provided (see
 * src/config/environment.ts), and a development build made with the sentinel
 * must be rebuilt after setting the real domain.
 */
const auth0Domain =
  process.env.EXPO_PUBLIC_AUTH0_DOMAIN ?? 'unconfigured.invalid';

export default ({ config }: ConfigContext): ExpoConfig => ({
  ...config,
  name: config.name ?? 'sahno',
  slug: config.slug ?? 'sahno',
  ios: {
    ...config.ios,
    bundleIdentifier: 'app.sahno.mobile',
  },
  android: {
    ...config.android,
    package: 'app.sahno.mobile',
  },
  plugins: [
    ...(config.plugins ?? []),
    [
      'react-native-auth0',
      {
        domain: auth0Domain,
        customScheme: 'sahno',
      },
    ],
  ],
});
