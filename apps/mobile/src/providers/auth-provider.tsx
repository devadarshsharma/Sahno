import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  type PropsWithChildren,
} from 'react';
import { Auth0Provider, useAuth0 } from 'react-native-auth0';

import { environment } from '@/config/environment';

export type SessionStatus = 'loading' | 'authenticated' | 'unauthenticated';

export type SessionUser = {
  name?: string;
  email?: string;
};

export type Session = {
  status: SessionStatus;
  user: SessionUser | null;
  /** Human-readable message for the most recent failed auth action, if any. */
  error: string | null;
  continueWithGoogle: () => Promise<void>;
  continueWithApple: () => Promise<void>;
  sendEmailCode: (email: string) => Promise<void>;
  continueWithEmailCode: (email: string, code: string) => Promise<void>;
  signOut: () => Promise<void>;
  /** Access token for calling the Sahno API. Refreshes automatically. */
  getAccessToken: () => Promise<string>;
};

const SessionContext = createContext<Session | null>(null);

export function useSession(): Session {
  const session = useContext(SessionContext);
  if (!session) {
    throw new Error('useSession must be used inside AuthProvider.');
  }
  return session;
}

const NOT_CONFIGURED_MESSAGE =
  'Authentication is not configured for this development build. Set the ' +
  'EXPO_PUBLIC_AUTH0_* variables in apps/mobile/.env and rebuild — see ' +
  'docs/LOCAL_DEVELOPMENT.md.';

const AUTH_SCOPE = 'openid profile email offline_access';

/**
 * Auth0-backed session boundary. Tokens live in the SDK's platform-secure
 * credentials manager (Keychain/Android Keystore) — never AsyncStorage — and
 * the session is restored from there when the app opens.
 */
export function AuthProvider({ children }: PropsWithChildren) {
  if (!environment.auth0) {
    return (
      <UnconfiguredSessionProvider>{children}</UnconfiguredSessionProvider>
    );
  }

  return (
    <Auth0Provider
      domain={environment.auth0.domain}
      clientId={environment.auth0.clientId}
    >
      <Auth0SessionBridge audience={environment.auth0.audience}>
        {children}
      </Auth0SessionBridge>
    </Auth0Provider>
  );
}

/** Keeps the app bootable without Auth0 config; actions fail with guidance. */
function UnconfiguredSessionProvider({ children }: PropsWithChildren) {
  const fail = useCallback(async () => {
    throw new Error(NOT_CONFIGURED_MESSAGE);
  }, []);

  const session = useMemo<Session>(
    () => ({
      status: 'unauthenticated',
      user: null,
      error: NOT_CONFIGURED_MESSAGE,
      continueWithGoogle: fail,
      continueWithApple: fail,
      sendEmailCode: fail,
      continueWithEmailCode: fail,
      signOut: async () => {},
      getAccessToken: fail as () => Promise<string>,
    }),
    [fail],
  );

  return (
    <SessionContext.Provider value={session}>
      {children}
    </SessionContext.Provider>
  );
}

function Auth0SessionBridge({
  audience,
  children,
}: PropsWithChildren<{ audience: string }>) {
  const {
    authorize,
    sendEmailCode: auth0SendEmailCode,
    authorizeWithEmail,
    clearSession,
    clearCredentials,
    getCredentials,
    resumeSession,
    user,
    error,
    isLoading,
  } = useAuth0();

  // Android may kill the app while the browser completes login; this drains a
  // login recovered after process death. Safe no-op elsewhere.
  useEffect(() => {
    resumeSession().catch(() => {
      // A failed recovery leaves the user signed out; nothing to surface.
    });
  }, [resumeSession]);

  const continueWithGoogle = useCallback(async () => {
    await authorize(
      {
        connection: 'google-oauth2',
        audience,
        scope: AUTH_SCOPE,
      },
      // Must match the scheme the config plugin registers in the native
      // manifest and the callback URLs in the Auth0 dashboard; without it the
      // SDK defaults to "<applicationId>.auth0".
      { customScheme: 'sahno' },
    );
  }, [authorize, audience]);

  const continueWithApple = useCallback(async () => {
    await authorize(
      {
        connection: 'apple',
        audience,
        scope: AUTH_SCOPE,
      },
      { customScheme: 'sahno' },
    );
  }, [authorize, audience]);

  const sendEmailCode = useCallback(
    async (email: string) => {
      await auth0SendEmailCode({ email, send: 'code' });
    },
    [auth0SendEmailCode],
  );

  const continueWithEmailCode = useCallback(
    async (email: string, code: string) => {
      await authorizeWithEmail({
        email,
        code,
        audience,
        scope: AUTH_SCOPE,
      });
    },
    [authorizeWithEmail, audience],
  );

  const signOut = useCallback(async () => {
    try {
      // Clears the Auth0 browser session and stored credentials.
      await clearSession({}, { customScheme: 'sahno' });
    } catch {
      // The browser step can fail or be dismissed; local credentials must
      // still be cleared so the app returns to a signed-out state.
      await clearCredentials();
    }
  }, [clearSession, clearCredentials]);

  const getAccessToken = useCallback(async () => {
    const credentials = await getCredentials();
    return credentials.accessToken;
  }, [getCredentials]);

  const session = useMemo<Session>(
    () => ({
      status: isLoading ? 'loading' : user ? 'authenticated' : 'unauthenticated',
      user: user ? { name: user.name, email: user.email } : null,
      error: error ? toRecoverableMessage(error) : null,
      continueWithGoogle,
      continueWithApple,
      sendEmailCode,
      continueWithEmailCode,
      signOut,
      getAccessToken,
    }),
    [
      isLoading,
      user,
      error,
      continueWithGoogle,
      continueWithApple,
      sendEmailCode,
      continueWithEmailCode,
      signOut,
      getAccessToken,
    ],
  );

  return (
    <SessionContext.Provider value={session}>
      {children}
    </SessionContext.Provider>
  );
}

/**
 * Generic, recoverable copy. Never surfaces raw provider errors that could
 * reveal whether an account exists or leak token details.
 */
function toRecoverableMessage(error: { message?: string }): string {
  const message = error.message ?? '';
  if (/cancel|dismiss/i.test(message)) {
    return 'Sign-in was cancelled. You can try again whenever you are ready.';
  }
  if (/network|timeout|connect/i.test(message)) {
    return 'We could not reach the sign-in service. Check your connection and try again.';
  }
  if (/code|otp|verification/i.test(message)) {
    return 'That code did not work. Check the latest email and try again.';
  }
  return 'Something went wrong while signing you in. Please try again.';
}
