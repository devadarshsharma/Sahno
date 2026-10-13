import { getAuthorizedJson } from '@/api/client';

/** A real place offered while typing a venue (D-102). `text` fills the field. */
export type PlaceSuggestion = {
  name: string;
  address: string | null;
  text: string;
};

function isPlaceSuggestionList(value: unknown): value is PlaceSuggestion[] {
  return (
    Array.isArray(value) &&
    value.every(
      (row) =>
        typeof row === 'object' &&
        row !== null &&
        typeof (row as PlaceSuggestion).name === 'string' &&
        typeof (row as PlaceSuggestion).text === 'string',
    )
  );
}

/**
 * Venue suggestions, through the Sahno API so the Google key never ships in
 * the app. Empty when suggestions are switched off on the server.
 */
export function suggestPlaces(
  accessToken: string,
  input: string,
  sessionToken: string,
  regionCode: string | null,
  signal?: AbortSignal,
): Promise<PlaceSuggestion[]> {
  const query = new URLSearchParams({ input, sessionToken });
  if (regionCode) {
    query.set('regionCode', regionCode);
  }
  return getAuthorizedJson(`/api/places/autocomplete?${query}`, accessToken, isPlaceSuggestionList, signal);
}
