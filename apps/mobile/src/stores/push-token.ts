import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import { forgetPushDevice } from '@/api/notifications';
import { existingPushToken } from '@/notifications/push';

/**
 * The Expo push token this phone registered, if any. Kept across launches
 * (D-133): a phone whose sign-in ran out never went through Sign out, and
 * still needs to tell the API to stop pushing to it — which it can only do
 * if it remembers what it registered.
 */
type PushTokenState = {
  token: string | null;
  setToken: (token: string | null) => void;
};

export const usePushToken = create<PushTokenState>()(
  persist(
    (set) => ({
      token: null,
      setToken: (token) => set({ token }),
    }),
    {
      name: 'sahno.push-token',
      storage: createJSONStorage(() => AsyncStorage),
    },
  ),
);

/**
 * This phone is nobody's any more: the API stops pushing to it. Needs no
 * sign-in, so it works after one has run out (D-133). The token is only
 * dropped once the API has it; until then the next call tries again.
 */
export async function forgetThisPhone(): Promise<void> {
  if (!usePushToken.persist.hasHydrated()) {
    await usePushToken.persist.rehydrate();
  }
  // Remembered since D-133; before that, only the phone itself knows it.
  const token = usePushToken.getState().token ?? (await existingPushToken());
  if (!token) {
    return;
  }
  try {
    await forgetPushDevice(token);
    usePushToken.getState().setToken(null);
  } catch {
    // Offline or the API away: kept, and tried again next time.
  }
}
