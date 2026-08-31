import { environment } from '@/config/environment';

/**
 * Fetches a Sahno API endpoint with a bearer access token and validates the
 * JSON payload with the provided type guard.
 */
export async function getAuthorizedJson<T>(
  path: string,
  accessToken: string,
  isValid: (value: unknown) => value is T,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(`${environment.apiUrl}${path}`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    signal,
  });

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}.`);
  }

  const data: unknown = await response.json();

  if (!isValid(data)) {
    throw new Error(`Response from ${path} has an unexpected format.`);
  }

  return data;
}
