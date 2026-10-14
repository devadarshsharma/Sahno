import { useQuery, useQueryClient } from '@tanstack/react-query';

import { listOrganisations, type Organisation } from '@/api/organisations';
import { useSession } from '@/providers/auth-provider';
import { useActiveOrganisation } from '@/stores/active-organisation';

export function useOrganisations() {
  const session = useSession();

  return useQuery({
    queryKey: ['organisations'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listOrganisations(accessToken, signal);
    },
    enabled: session.status === 'authenticated',
  });
}

/**
 * The active organisation (D-044): the explicitly selected one, defaulting to
 * the first membership. Null while loading or when the person has none.
 */
export function useActiveOrg(): {
  organisations: Organisation[];
  active: Organisation | null;
  isPending: boolean;
  isFetching: boolean;
  isError: boolean;
  refetch: () => void;
  switchTo: (organisationId: string) => void;
} {
  const organisationsQuery = useOrganisations();
  const queryClient = useQueryClient();
  const { activeOrganisationId, setActiveOrganisation } =
    useActiveOrganisation();

  const organisations = organisationsQuery.data ?? [];
  // What was picked this session, else the person's default (D-099), else
  // the first they joined.
  const active =
    organisations.find((org) => org.id === activeOrganisationId) ??
    organisations.find((org) => org.isDefault) ??
    organisations[0] ??
    null;

  return {
    organisations,
    active,
    isPending: organisationsQuery.isPending,
    isFetching: organisationsQuery.isFetching,
    isError: organisationsQuery.isError,
    refetch: organisationsQuery.refetch,
    switchTo: (organisationId: string) => {
      setActiveOrganisation(organisationId);
      // Switching strictly changes the data context (D-044). Every
      // org-scoped query is keyed by its organisation's id, so nothing can
      // leak across — and each organisation keeps its own cache. Switching
      // back shows what was there at once and refreshes it underneath,
      // instead of starting every screen from a spinner (D-103).
      void queryClient.invalidateQueries({ queryKey: ['org', organisationId] });
    },
  };
}
