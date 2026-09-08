import { getAuthorizedJson, sendAuthorized, sendAuthorizedJson } from '@/api/client';

export type MemberRole = 'Owner' | 'Admin' | 'Member';

/**
 * One directory row. `email` and `phoneNumber` are null when the caller is not
 * entitled to see them (D-018) — that is "not shared", not "not set", so the
 * UI must never present it as an empty value.
 */
export type Member = {
  membershipId: string;
  userId: string;
  displayName: string | null;
  function: string | null;
  email: string | null;
  phoneNumber: string | null;
  role: MemberRole;
  canManageFinances: boolean;
  sharesContactDetails: boolean;
  internalNotes: string | null;
  isYou: boolean;
  joinedAtUtc: string;
};

function isMember(value: unknown): value is Member {
  return (
    typeof value === 'object' &&
    value !== null &&
    'membershipId' in value &&
    typeof value.membershipId === 'string' &&
    'role' in value &&
    typeof value.role === 'string'
  );
}

function isMemberList(value: unknown): value is Member[] {
  return Array.isArray(value) && value.every(isMember);
}

export function listMembers(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<Member[]> {
  return getAuthorizedJson(
    `/api/organisations/${organisationId}/members`,
    accessToken,
    isMemberList,
    signal,
  );
}

/** Appoints or removes an Admin. The Owner's alone (D-014). */
export function changeMemberRole(
  accessToken: string,
  organisationId: string,
  membershipId: string,
  role: 'Admin' | 'Member',
): Promise<void> {
  return sendAuthorizedJson(
    `/api/organisations/${organisationId}/members/${membershipId}`,
    accessToken,
    'PATCH',
    { role },
  );
}

/** Grants or revokes an Admin's financial permission (D-016). */
export function setMemberFinancialAccess(
  accessToken: string,
  organisationId: string,
  membershipId: string,
  canManageFinances: boolean,
): Promise<void> {
  return sendAuthorizedJson(
    `/api/organisations/${organisationId}/members/${membershipId}`,
    accessToken,
    'PATCH',
    { canManageFinances },
  );
}

/** Organiser-only notes about a member (D-046). */
export function setMemberInternalNotes(
  accessToken: string,
  organisationId: string,
  membershipId: string,
  internalNotes: string,
): Promise<void> {
  return sendAuthorizedJson(
    `/api/organisations/${organisationId}/members/${membershipId}`,
    accessToken,
    'PATCH',
    { internalNotes },
  );
}

export function removeMember(
  accessToken: string,
  organisationId: string,
  membershipId: string,
): Promise<void> {
  return sendAuthorized(
    `/api/organisations/${organisationId}/members/${membershipId}`,
    accessToken,
    'DELETE',
  );
}

/** Hands ownership to another member (D-014). */
export function transferOwnership(
  accessToken: string,
  organisationId: string,
  membershipId: string,
): Promise<void> {
  return sendAuthorizedJson(
    `/api/organisations/${organisationId}/members/transfer-ownership`,
    accessToken,
    'POST',
    { membershipId },
  );
}

/**
 * Your own function and contact-sharing choice in this organisation. Sharing
 * is yours to decide, so it has its own route rather than sitting with the
 * management actions (D-018).
 */
export function updateOwnMembership(
  accessToken: string,
  organisationId: string,
  update: { function?: string | null; sharesContactDetails?: boolean },
): Promise<void> {
  return sendAuthorizedJson(
    `/api/organisations/${organisationId}/members/me`,
    accessToken,
    'PATCH',
    update,
  );
}
