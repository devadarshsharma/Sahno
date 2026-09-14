import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

/**
 * When this person last saw the people who had joined.
 *
 * "Recently joined" on Home is a notice, not a standing list: it exists to
 * tell an organiser something happened while they were not looking, and once
 * they have looked it has done its job. Two things count as looking: opening
 * People, and having had the notice on screen for a session. Either way it
 * goes and does not come back for those joins.
 *
 * Kept on the device, so a cold start does not show the same notice again.
 * It used to be in memory (better twice than never, with no other
 * notification path); now that joins are notified (Slice 10) the notice can
 * afford to be once.
 */
type SeenPeopleState = {
  /** Joins at or before this moment have been seen. */
  seenAtUtc: string | null;
  /** True once the persisted value has been read; render nothing before it. */
  hydrated: boolean;
  /** Opening People: everything to now is seen. */
  markPeopleSeen: () => void;
  /** The notice was on screen: everything up to the newest join shown is seen. */
  markJoinsSeen: (upToUtc: string) => void;
  setHydrated: () => void;
};

export const useSeenPeople = create<SeenPeopleState>()(
  persist(
    (set, get) => ({
      seenAtUtc: null,
      hydrated: false,
      setHydrated: () => set({ hydrated: true }),
      markPeopleSeen: () => set({ seenAtUtc: new Date().toISOString() }),
      markJoinsSeen: (upToUtc) => {
        const current = get().seenAtUtc;
        if (!current || Date.parse(upToUtc) > Date.parse(current)) {
          set({ seenAtUtc: upToUtc });
        }
      },
    }),
    {
      name: 'sahno.seen-people',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({ seenAtUtc: state.seenAtUtc }),
      onRehydrateStorage: () => (state) => {
        state?.setHydrated();
      },
    },
  ),
);
