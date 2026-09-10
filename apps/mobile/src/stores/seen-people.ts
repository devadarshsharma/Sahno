import { create } from 'zustand';

/**
 * When this person last looked at the People tab.
 *
 * "Recently joined" on Home is a notice, not a standing list: it exists to
 * tell an organiser something happened while they were not looking, and once
 * they have looked it has done its job. Opening People is that acknowledgement
 * — no separate dismiss to find, and no notice sitting there for a fortnight
 * after it was read.
 *
 * In-memory like the active organisation, so a cold start shows a recent join
 * once more. That errs towards telling someone twice rather than never, which
 * is the right way round for something with no other notification path until
 * Slice 10.
 */
type SeenPeopleState = {
  seenAtUtc: string | null;
  markPeopleSeen: () => void;
};

export const useSeenPeople = create<SeenPeopleState>((set) => ({
  seenAtUtc: null,
  markPeopleSeen: () => set({ seenAtUtc: new Date().toISOString() }),
}));
