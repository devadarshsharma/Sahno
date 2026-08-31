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
  switchTo: (organisationId: string) => void;
} {
  const organisationsQuery = useOrganisations();
  const queryClient = useQueryClient();
  const { activeOrganisationId, setActiveOrganisation } =
    useActiveOrganisation();

  const organisations = organisationsQuery.data ?? [];
  const active =
    organisations.find((org) => org.id === activeOrganisationId) ??
    organisations[0] ??
    null;

  return {
    organisations,
    active,
    isPending: organisationsQuery.isPending,
    isFetching: organisationsQuery.isFetching,
    switchTo: (organisationId: string) => {
      setActiveOrganisation(organisationId);
      // Switching strictly changes the data context (D-044): drop every
      // org-scoped cache so nothing leaks across organisations.
      queryClient.removeQueries({ queryKey: ['org'] });
    },
  };
}
