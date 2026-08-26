import { environment } from '@/config/environment';

export type HealthResponse = {
  status: string;
};

function isHealthResponse(value: unknown): value is HealthResponse {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    typeof value.status === 'string'
  );
}

export async function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  const response = await fetch(`${environment.apiUrl}/api/health`, {
    signal,
  });

  if (!response.ok) {
    throw new Error(`Health request failed with status ${response.status}.`);
  }

  const data: unknown = await response.json();

  if (!isHealthResponse(data)) {
    throw new Error('Health response has an unexpected format.');
  }

  return data;
}