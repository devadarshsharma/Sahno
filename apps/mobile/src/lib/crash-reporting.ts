import * as Sentry from '@sentry/react-native';

/**
 * Crash reporting (D-069, D-117), through Sentry. Off unless the build has a
 * DSN (`EXPO_PUBLIC_SENTRY_DSN` in the EAS environment), and never in
 * development, where the red screen already says what broke.
 *
 * It reports what the app could never show anybody: native crashes (an iOS
 * alert falling over inside UIKit), Android crashes and freezes, and
 * JavaScript errors — each with the screen it happened on and the last few
 * taps and requests before it.
 *
 * What it does not send: names, emails or phone numbers (the user is an
 * opaque id), request bodies, or query strings — a venue search or a cursor
 * never leaves the phone in a report.
 */
const dsn = process.env.EXPO_PUBLIC_SENTRY_DSN;

let started = false;

function withoutQuery(url: unknown): unknown {
  return typeof url === 'string' ? url.split('?')[0] : url;
}

export function startCrashReporting(): void {
  if (started || !dsn || __DEV__) {
    return;
  }
  started = true;

  Sentry.init({
    dsn,
    environment: process.env.EXPO_PUBLIC_APP_ENV ?? 'preview',
    sendDefaultPii: false,
    // Errors only. Performance tracing is a separate decision with its own cost.
    tracesSampleRate: 0,
    attachScreenshot: false,
    beforeBreadcrumb: (breadcrumb) => {
      if (breadcrumb.category === 'console') {
        return null;
      }
      if ((breadcrumb.category === 'fetch' || breadcrumb.category === 'xhr') && breadcrumb.data) {
        breadcrumb.data = { ...breadcrumb.data, url: withoutQuery(breadcrumb.data.url) };
      }
      return breadcrumb;
    },
    beforeSend: (event) => {
      if (event.request) {
        event.request = { ...event.request, url: withoutQuery(event.request.url) as string | undefined, query_string: undefined };
      }
      if (event.user) {
        event.user = { id: event.user.id };
      }
      return event;
    },
  });
}

/** Who the reports are about, as an opaque id only — or nobody, once signed out. */
export function setCrashReportingUser(userId: string | null): void {
  if (!started) {
    return;
  }
  Sentry.setUser(userId ? { id: userId } : null);
}

/** An error the app caught and showed (a crash screen), which would otherwise go unreported. */
export function reportError(error: unknown): void {
  if (!started) {
    return;
  }
  Sentry.captureException(error);
}

/** Wraps the root so taps become breadcrumbs. A no-op wrapper when reporting is off. */
export const wrapRoot = Sentry.wrap;
