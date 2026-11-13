import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import { listAvatarVersions } from '@/api/avatars';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/**
 * Photo versions for the active organisation's members (D-129): userId →
 * version, only for those who have one. One small request, shared by every
 * avatar on screen.
 */
export function useAvatarVersions(): Map<string, number> {
  const session = useSession();
  const { active } = useActiveOrg();

  const query = useQuery({
    queryKey: ['org', active?.id, 'avatars'],
    queryFn: async ({ signal }) => listAvatarVersions(await session.getAccessToken(), active!.id, signal),
    enabled: active !== null && session.status === 'authenticated',
    staleTime: 5 * 60 * 1000,
  });

  return new Map((query.data ?? []).map((row) => [row.userId, row.version]));
}

/**
 * A bearer token for image requests, which cannot ask for one themselves.
 * Fetched once per mount; a photo that loads after it expires simply falls
 * back to the initials.
 */
export function useImageToken(): string | null {
  const session = useSession();
  const getAccessToken = session.getAccessToken;
  const authenticated = session.status === 'authenticated';
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    if (!authenticated) {
      return;
    }
    let cancelled = false;
    void getAccessToken()
      .then((value) => {
        if (!cancelled) {
          setToken(value);
        }
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [authenticated, getAccessToken]);

  return token;
}
