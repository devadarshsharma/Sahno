import { create } from 'zustand';

/**
 * The Expo push token this phone registered, if any. In memory: it is
 * re-derived on every launch, and the only reader outside the notifications
 * provider is sign-out, which needs it to tell the API this phone is no
 * longer this person's.
 */
type PushTokenState = {
  token: string | null;
  setToken: (token: string | null) => void;
};

export const usePushToken = create<PushTokenState>((set) => ({
  token: null,
  setToken: (token) => set({ token }),
}));
