import { create } from 'zustand';

/**
 * The active-organisation context (D-044, D-064). Deliberately in-memory for
 * this slice: on a cold start the app defaults to the person's first
 * organisation. Switching strictly changes the data context — callers must
 * invalidate org-scoped queries when this changes.
 */
type ActiveOrganisationState = {
  activeOrganisationId: string | null;
  setActiveOrganisation: (organisationId: string | null) => void;
};

export const useActiveOrganisation = create<ActiveOrganisationState>((set) => ({
  activeOrganisationId: null,
  setActiveOrganisation: (organisationId) =>
    set({ activeOrganisationId: organisationId }),
}));
