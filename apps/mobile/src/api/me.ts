import { getAuthorizedJson, patchAuthorizedJson } from '@/api/client';

export type MeResponse = {
  userId: string;
  email: string | null;
  /**
   * The name to show. Null when the account has no usable one yet — the
   * passwordless email connection supplies the email address in place of a
   * name, which the API reports as absent rather than presenting it.
   */
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

/** Sets the person's own display name (D-046). */
export function updateMe(
  accessToken: string,
  displayName: string,
  signal?: AbortSignal,
): Promise<MeResponse> {
  return patchAuthorizedJson(
    '/api/me',
    accessToken,
    { displayName },
    isMeResponse,
    signal,
  );
}
