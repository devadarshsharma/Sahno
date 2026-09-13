import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

export type PieceLink = {
  id: string;
  title: string;
  url: string;
};

/**
 * One piece in the organisation's repertoire (D-079). Every member sees it
 * and may edit it; `notes` is the organiser's and comes back null to anyone
 * else. The list arrives without `lyrics` (`hasLyrics` says whether there
 * are any); the detail carries them.
 */
export type Piece = {
  id: string;
  title: string;
  attribution: string | null;
  language: string | null;
  key: string | null;
  durationMinutes: number | null;
  hasLyrics: boolean;
  lyrics: string | null;
  notes: string | null;
  links: PieceLink[];
  useCount: number;
  updatedByUserId: string;
  updatedAtUtc: string;
};

export type PieceBooking = {
  engagementId: string;
  title: string;
  status: string;
  startDate: string | null;
};

export type PieceDetail = {
  piece: Piece;
  bookings: PieceBooking[];
};

export type SavePieceInput = {
  title: string;
  attribution: string | null;
  language: string | null;
  key: string | null;
  durationMinutes: number | null;
  lyrics: string | null;
};

/** One line of a booking's set list, with the piece resolved. */
export type SetListEntry = {
  id: string;
  pieceId: string;
  position: number;
  title: string;
  attribution: string | null;
  durationMinutes: number | null;
  hasLyrics: boolean;
  note: string | null;
};

function isPiece(value: unknown): value is Piece {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'title' in value &&
    'hasLyrics' in value
  );
}

function isPieceList(value: unknown): value is Piece[] {
  return Array.isArray(value) && value.every(isPiece);
}

function isPieceDetail(value: unknown): value is PieceDetail {
  return (
    typeof value === 'object' &&
    value !== null &&
    'piece' in value &&
    'bookings' in value &&
    isPiece((value as PieceDetail).piece)
  );
}

function isPieceLink(value: unknown): value is PieceLink {
  return typeof value === 'object' && value !== null && 'id' in value && 'url' in value;
}

function isSetListEntry(value: unknown): value is SetListEntry {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'pieceId' in value &&
    'position' in value
  );
}

function isSetList(value: unknown): value is SetListEntry[] {
  return Array.isArray(value) && value.every(isSetListEntry);
}

function repertoire(organisationId: string): string {
  return `/api/organisations/${organisationId}/repertoire`;
}

function setList(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}/setlist`;
}

// ---- Repertoire ---------------------------------------------------------

export function listPieces(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<Piece[]> {
  return getAuthorizedJson(repertoire(organisationId), accessToken, isPieceList, signal);
}

export function getPieceDetail(
  accessToken: string,
  organisationId: string,
  pieceId: string,
  signal?: AbortSignal,
): Promise<PieceDetail> {
  return getAuthorizedJson(
    `${repertoire(organisationId)}/${pieceId}`,
    accessToken,
    isPieceDetail,
    signal,
  );
}

export function createPiece(
  accessToken: string,
  organisationId: string,
  input: SavePieceInput,
): Promise<Piece> {
  return postAuthorizedJson(repertoire(organisationId), accessToken, input, isPiece);
}

export function updatePiece(
  accessToken: string,
  organisationId: string,
  pieceId: string,
  input: SavePieceInput,
): Promise<void> {
  return sendAuthorizedJson(`${repertoire(organisationId)}/${pieceId}`, accessToken, 'PUT', input);
}

export function updatePieceNotes(
  accessToken: string,
  organisationId: string,
  pieceId: string,
  notes: string | null,
): Promise<void> {
  return sendAuthorizedJson(
    `${repertoire(organisationId)}/${pieceId}/notes`,
    accessToken,
    'PUT',
    { notes },
  );
}

export function deletePiece(
  accessToken: string,
  organisationId: string,
  pieceId: string,
): Promise<void> {
  return sendAuthorized(`${repertoire(organisationId)}/${pieceId}`, accessToken, 'DELETE');
}

export function addPieceLink(
  accessToken: string,
  organisationId: string,
  pieceId: string,
  input: { title: string | null; url: string },
): Promise<PieceLink> {
  return postAuthorizedJson(
    `${repertoire(organisationId)}/${pieceId}/links`,
    accessToken,
    input,
    isPieceLink,
  );
}

export function removePieceLink(
  accessToken: string,
  organisationId: string,
  pieceId: string,
  linkId: string,
): Promise<void> {
  return sendAuthorized(
    `${repertoire(organisationId)}/${pieceId}/links/${linkId}`,
    accessToken,
    'DELETE',
  );
}

// ---- Set list -----------------------------------------------------------

export function listSetList(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<SetListEntry[]> {
  return getAuthorizedJson(setList(organisationId, engagementId), accessToken, isSetList, signal);
}

export function addSetListEntry(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: { pieceId: string; note: string | null },
): Promise<SetListEntry> {
  return postAuthorizedJson(
    setList(organisationId, engagementId),
    accessToken,
    input,
    isSetListEntry,
  );
}

export function updateSetListEntry(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  entryId: string,
  note: string | null,
): Promise<void> {
  return sendAuthorizedJson(
    `${setList(organisationId, engagementId)}/${entryId}`,
    accessToken,
    'PUT',
    { note },
  );
}

export function reorderSetList(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  entryIds: string[],
): Promise<void> {
  return sendAuthorizedJson(
    `${setList(organisationId, engagementId)}/reorder`,
    accessToken,
    'PUT',
    { entryIds },
  );
}

export function removeSetListEntry(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  entryId: string,
): Promise<void> {
  return sendAuthorized(
    `${setList(organisationId, engagementId)}/${entryId}`,
    accessToken,
    'DELETE',
  );
}
