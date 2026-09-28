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
  
 // google-services.json is gitignored (it belongs to the machine, not the
// repo), so it never reaches an EAS worker through the project archive. On
// EAS it arrives as a file-type environment variable holding an absolute
// path; locally the variable is unset and the sibling file is used.
const googleServicesFile =
  process.env.GOOGLE_SERVICES_JSON ?? './google-services.json';

// Android release builds refuse plain http:// unless told otherwise. Allow it
// only when this build talks to an http:// API (a laptop on the LAN); a build
// for the hosted test or production API is HTTPS-only.
const usesCleartextTraffic =
  process.env.EXPO_PUBLIC_API_URL?.startsWith('http://') ?? false;

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
    googleServicesFile,
  },
  plugins: [
    ...(config.plugins ?? []),
    ['expo-build-properties', { android: { usesCleartextTraffic } }],
    [
      'react-native-auth0',
      {
        domain: auth0Domain,
        customScheme: 'sahno',
      },
    ],
  ],
});
