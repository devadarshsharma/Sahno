import { getAuthorizedJson } from '@/api/client';

export type MeResponse = {
  userId: string;
  email: string | null;
  displayName: string | null;
  createdAtUtc: string;
};

function isMeResponse(value: unknown): value is MeResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    'userId' in value &&
    typeof value.userId === 'string' &&
    'createdAtUtc' in value &&
    typeof value.createdAtUtc === 'string'
  );
}

export function getMe(
  accessToken: string,
  signal?: AbortSignal,
): Promise<MeResponse> {
  return getAuthorizedJson('/api/me', accessToken, isMeResponse, signal);
}
