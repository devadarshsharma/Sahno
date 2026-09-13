import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { getPieceDetail, listPieces, listSetList } from '@/api/repertoire';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';

/** The whole repertoire, without lyrics. Every member. */
export function usePieces() {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'repertoire'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listPieces(accessToken, active!.id, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * One piece with lyrics and history. Cached generously: on stage, the lyrics
 * that were there a minute ago should still be there when the signal drops.
 */
export function usePieceDetail(pieceId: string | null) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'repertoire', pieceId],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return getPieceDetail(accessToken, active!.id, pieceId!, signal);
    },
    enabled: active !== null && pieceId !== null && session.status === 'authenticated',
    staleTime: 5 * 60 * 1000,
    gcTime: 24 * 60 * 60 * 1000,
  });
}

/** A booking's set list. Everyone on the lineup, plus organisers. */
export function useSetList(engagementId: string) {
  const session = useSession();
  const { active } = useActiveOrg();

  return useQuery({
    queryKey: ['org', active?.id, 'engagements', engagementId, 'setlist'],
    queryFn: async ({ signal }) => {
      const accessToken = await session.getAccessToken();
      return listSetList(accessToken, active!.id, engagementId, signal);
    },
    enabled: active !== null && session.status === 'authenticated',
  });
}

/**
 * Runs a repertoire or set list change and refreshes both trees: a piece
 * edit shows on every set list it is on, and a set list change moves the
 * piece's use count and the booking's readiness.
 */
export function useRepertoireMutation<TArgs, TResult = unknown>(
  action: (
    accessToken: string,
    organisationId: string,
    args: TArgs,
  ) => Promise<TResult>,
) {
  const session = useSession();
  const queryClient = useQueryClient();
  const { active } = useActiveOrg();

  return useMutation({
    mutationFn: async (args: TArgs) => {
      const accessToken = await session.getAccessToken();
      return action(accessToken, active!.id, args);
    },
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['org', active?.id, 'repertoire'] }),
        queryClient.invalidateQueries({ queryKey: ['org', active?.id, 'engagements'] }),
      ]),
  });
}

export function formatDuration(minutes: number | null): string | null {
  if (minutes === null) {
    return null;
  }
  if (minutes < 60) {
    return `${minutes} min`;
  }
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`;
}
