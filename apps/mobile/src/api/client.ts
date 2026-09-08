import { environment } from '@/config/environment';

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PATCH' | 'DELETE';
  body?: unknown;
  signal?: AbortSignal;
};

export class ApiError extends Error {
  readonly status: number;

  constructor(path: string, status: number) {
    super(`Request to ${path} failed with status ${status}.`);
    this.status = status;
  }
}

/**
 * Calls a Sahno API endpoint with a bearer access token. Throws ApiError on
 * non-2xx responses; 204 responses resolve to undefined.
 */
async function authorizedRequest(
  path: string,
  accessToken: string,
  options: RequestOptions = {},
): Promise<unknown> {
  const response = await fetch(`${environment.apiUrl}${path}`, {
    method: options.method ?? 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(options.body !== undefined
        ? { 'Content-Type': 'application/json' }
        : {}),
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    signal: options.signal,
  });

  if (!response.ok) {
    throw new ApiError(path, response.status);
  }

  if (response.status === 204) {
    return undefined;
  }

  return response.json();
}

/** GET with payload validation via a type guard. */
export async function getAuthorizedJson<T>(
  path: string,
  accessToken: string,
  isValid: (value: unknown) => value is T,
  signal?: AbortSignal,
): Promise<T> {
  const data = await authorizedRequest(path, accessToken, { signal });

  if (!isValid(data)) {
    throw new Error(`Response from ${path} has an unexpected format.`);
  }

  return data;
}

/** POST with payload validation via a type guard. */
export async function postAuthorizedJson<T>(
  path: string,
  accessToken: string,
  body: unknown,
  isValid: (value: unknown) => value is T,
  signal?: AbortSignal,
): Promise<T> {
  const data = await authorizedRequest(path, accessToken, {
    method: 'POST',
    body,
    signal,
  });

  if (!isValid(data)) {
    throw new Error(`Response from ${path} has an unexpected format.`);
  }

  return data;
}

/** POST/PATCH/DELETE where no response body is expected. */
export async function sendAuthorized(
  path: string,
  accessToken: string,
  method: 'POST' | 'PATCH' | 'DELETE',
  signal?: AbortSignal,
): Promise<void> {
  await authorizedRequest(path, accessToken, { method, signal });
}

/** PATCH with payload validation via a type guard. */
export async function patchAuthorizedJson<T>(
  path: string,
  accessToken: string,
  body: unknown,
  isValid: (value: unknown) => value is T,
  signal?: AbortSignal,
): Promise<T> {
  const data = await authorizedRequest(path, accessToken, {
    method: 'PATCH',
    body,
    signal,
  });

  if (!isValid(data)) {
    throw new Error(`Response from ${path} has an unexpected format.`);
  }

  return data;
}

/** POST/PATCH with a body where no response body is expected. */
export async function sendAuthorizedJson(
  path: string,
  accessToken: string,
  method: 'POST' | 'PATCH',
  body: unknown,
  signal?: AbortSignal,
): Promise<void> {
  await authorizedRequest(path, accessToken, { method, body, signal });
}
