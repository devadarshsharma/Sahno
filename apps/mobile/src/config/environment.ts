const apiUrl = process.env.EXPO_PUBLIC_API_URL;

if (!apiUrl) {
  throw new Error(
    'EXPO_PUBLIC_API_URL is required. Copy .env.example to .env and provide the API address.',
  );
}

const auth0Domain = process.env.EXPO_PUBLIC_AUTH0_DOMAIN;
const auth0ClientId = process.env.EXPO_PUBLIC_AUTH0_CLIENT_ID;
const auth0Audience = process.env.EXPO_PUBLIC_AUTH0_AUDIENCE;

/**
 * Auth0 client configuration. These are public, client-safe values (never
 * secrets). When absent the app still boots — health checks and the design
 * system keep working — but authentication actions fail with a clear
 * development-configuration message instead of crashing.
 */
const auth0 =
  auth0Domain && auth0ClientId && auth0Audience
    ? {
        domain: auth0Domain,
        clientId: auth0ClientId,
        audience: auth0Audience,
      }
    : null;

const apiBaseUrl = apiUrl.replace(/\/+$/, '');

/**
 * Every address the app calls is derived from EXPO_PUBLIC_API_URL, which is
 * chosen per build: .env for local development, the EAS environment for
 * preview (test) and production builds. Moving the API to another host is a
 * change of that one value — see docs/TEST_DEPLOYMENT.md.
 */
export const environment = {
  apiUrl: apiBaseUrl,
  /** The SignalR hub (LiveHub.Path on the API). */
  liveHubUrl: `${apiBaseUrl}/hubs/live`,
  auth0,
} as const;
