import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

/** One Sahno event Sahno put in this phone's calendar (D-105). */
export type CalendarLink = {
  /** The phone calendar's id for the entry. */
  eventId: string;
  organisationId: string;
  /** What the entry was last written from; a change means it needs updating. */
  signature: string;
  /** The event's last day as last written, to tell "too old to be listed" from "gone". */
  lastDay: string;
};

/**
 * Which events Sahno added to this phone's calendar, so it can keep them
 * right when a booking moves, and take them out when it is cancelled
 * (D-105). Per phone, not per account: each phone has its own calendar, and
 * an id from one means nothing on another.
 */
type CalendarLinksState = {
  links: Record<string, CalendarLink>;
  hydrated: boolean;
  setLink: (engagementId: string, link: CalendarLink) => void;
  removeLink: (engagementId: string) => void;
  setHydrated: () => void;
};

export const useCalendarLinks = create<CalendarLinksState>()(
  persist(
    (set) => ({
      links: {},
      hydrated: false,
      setHydrated: () => set({ hydrated: true }),
      setLink: (engagementId, link) =>
        set((state) => ({ links: { ...state.links, [engagementId]: link } })),
      removeLink: (engagementId) =>
        set((state) => {
          const { [engagementId]: _removed, ...rest } = state.links;
          return { links: rest };
        }),
    }),
    {
      name: 'sahno.calendar-links',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({ links: state.links }),
      onRehydrateStorage: () => (state) => {
        state?.setHydrated();
      },
    },
  ),
);
