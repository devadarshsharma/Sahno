import Constants from 'expo-constants';
import * as Device from 'expo-device';
import * as Notifications from 'expo-notifications';
import { Platform } from 'react-native';

import { isNotificationPayload, type NotificationPayload } from '@/api/notifications';

/** The Android channel every Sahno push lands in. Matches app.json and the API. */
export const DEFAULT_CHANNEL_ID = 'default';

/**
 * How a push that arrives while the app is OPEN is treated: the OS shows
 * nothing, because Sahno puts its own banner up (from the live connection
 * usually, from this push if that did not arrive). In the background the OS
 * shows it natively and this is never consulted.
 */
export function installForegroundHandler(): void {
  Notifications.setNotificationHandler({
    handleNotification: async () => ({
      shouldShowBanner: false,
      shouldShowList: false,
      shouldPlaySound: false,
      shouldSetBadge: false,
    }),
  });
}

/**
 * Android needs a channel before the first notification or it is shown
 * silently on an unnamed one. Importance MAX is what puts a heads-up banner
 * on screen when the phone is unlocked.
 */
export async function ensureAndroidChannel(): Promise<void> {
  if (Platform.OS !== 'android') {
    return;
  }
  await Notifications.setNotificationChannelAsync(DEFAULT_CHANNEL_ID, {
    name: 'Sahno',
    description: 'Bookings, jobs, messages and announcements from your group.',
    importance: Notifications.AndroidImportance.MAX,
    vibrationPattern: [0, 250, 250, 250],
    lightColor: '#F97B0A',
    lockscreenVisibility: Notifications.AndroidNotificationVisibility.PRIVATE,
  });
}

export type PushRegistration =
  | { status: 'registered'; token: string; platform: 'ios' | 'android'; deviceName: string | null }
  | { status: 'denied' }
  | { status: 'unavailable'; reason: string };

/**
 * Asks for permission (once — a settled answer is not asked again) and gets
 * this phone's Expo push token. Every failure is a status, not a throw:
 * push is a convenience and the app must be exactly as usable without it.
 */
export async function registerForPush(): Promise<PushRegistration> {
  if (!Device.isDevice) {
    return { status: 'unavailable', reason: 'Push needs a physical device.' };
  }
  if (Platform.OS !== 'ios' && Platform.OS !== 'android') {
    return { status: 'unavailable', reason: `Push is not supported on ${Platform.OS}.` };
  }

  await ensureAndroidChannel();

  let { status } = await Notifications.getPermissionsAsync();
  if (status !== 'granted') {
    ({ status } = await Notifications.requestPermissionsAsync({
      ios: { allowAlert: true, allowBadge: true, allowSound: true },
    }));
  }
  if (status !== 'granted') {
    return { status: 'denied' };
  }

  const projectId =
    Constants.expoConfig?.extra?.eas?.projectId ?? Constants.easConfig?.projectId;
  if (!projectId) {
    return {
      status: 'unavailable',
      reason: 'No EAS project id in app config (run `eas init`).',
    };
  }

  try {
    const { data: token } = await Notifications.getExpoPushTokenAsync({ projectId });
    return {
      status: 'registered',
      token,
      platform: Platform.OS,
      deviceName: Device.deviceName ?? Device.modelName ?? null,
    };
  } catch (error) {
    // On Android this is usually "Default FirebaseApp is not initialized":
    // no google-services.json on the machine that built the app.
    return {
      status: 'unavailable',
      reason: error instanceof Error ? error.message : String(error),
    };
  }
}

/** The structured payload out of a push, or null if it is not one of ours. */
export function payloadOf(
  notification: Notifications.Notification | null | undefined,
): NotificationPayload | null {
  const data = notification?.request.content.data;
  return isNotificationPayload(data) ? data : null;
}
