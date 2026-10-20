import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';
import { normalizeInviteCode } from '@/lib/invite';

export type Organisation = {
  id: string;
  name: string;
  groupType: string | null;
  timeZoneId: string;
  logoUrl: string | null;
  role: 'Owner' | 'Admin' | 'Member';
  /** The Owner, or an Admin the Owner has granted finance (D-016). */
  hasFinancialAccess: boolean;
  showSetupChecklist: boolean;
  memberCount: number;
  /** The organisation the app opens in (D-099). Absent from older APIs. */
  isDefault?: boolean;
  /** When event reminders go out (D-118). Absent from older APIs, which mean "morning". */
  eventReminder?: ReminderTiming;
  /** When rehearsal reminders go out (D-118). */
  rehearsalReminder?: ReminderTiming;
  /** How soon unanswered availability is nudged (D-120). Absent from older APIs. */
  availabilityNudge?: AvailabilityNudge;
};

/** Automatic nudges for unanswered availability (D-120). */
export type AvailabilityNudge = '2d' | '1d' | 'off';

/** When an organisation's reminders go out (D-118). */
export type ReminderTiming = 'morning' | '1h' | '2h' | '3h' | 'day-before' | 'off';

/** Organisers only. */
export function setReminders(
  accessToken: string,
  organisationId: string,
  timings: { eventReminder: ReminderTiming; rehearsalReminder: ReminderTiming; availabilityNudge?: AvailabilityNudge },
): Promise<void> {
  return sendAuthorizedJson(`/api/organisations/${organisationId}/reminders`, accessToken, 'PUT', timings);
}

export type Invitation = {
  id: string;
  token: string;
  type: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
  revokedAtUtc: string | null;
};

export type InvitationPreview = {
  organisationName: string;
  groupType: string | null;
  logoUrl: string | null;
};

export type AcceptedInvitation = {
  organisationId: string;
  organisationName: string;
  role: string;
};

function isOrganisation(value: unknown): value is Organisation {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    typeof value.id === 'string' &&
    'name' in value &&
    typeof value.name === 'string' &&
    'role' in value &&
    typeof value.role === 'string'
  );
}

function isOrganisationList(value: unknown): value is Organisation[] {
  return Array.isArray(value) && value.every(isOrganisation);
}

function isInvitation(value: unknown): value is Invitation {
  return (
    typeof value === 'object' &&
    value !== null &&
    'token' in value &&
    typeof value.token === 'string'
  );
}

function isInvitationList(value: unknown): value is Invitation[] {
  return Array.isArray(value) && value.every(isInvitation);
}

function isInvitationPreview(value: unknown): value is InvitationPreview {
  return (
    typeof value === 'object' &&
    value !== null &&
    'organisationName' in value &&
    typeof value.organisationName === 'string'
  );
}

function isAcceptedInvitation(value: unknown): value is AcceptedInvitation {
  return (
    typeof value === 'object' &&
    value !== null &&
    'organisationId' in value &&
    typeof value.organisationId === 'string'
  );
}

export function listOrganisations(
  accessToken: string,
  signal?: AbortSignal,
): Promise<Organisation[]> {
  return getAuthorizedJson('/api/organisations', accessToken, isOrganisationList, signal);
}

export function createOrganisation(
  accessToken: string,
  input: { name: string; groupType?: string | null; timeZoneId?: string | null },
): Promise<Organisation> {
  return postAuthorizedJson('/api/organisations', accessToken, input, isOrganisation);
}

/** Makes this the organisation the app opens in for the caller (D-099). */
export function setDefaultOrganisation(accessToken: string, organisationId: string): Promise<void> {
  return sendAuthorizedJson(`/api/organisations/${organisationId}/default`, accessToken, 'PUT', {});
}

/**
 * Deletes the organisation (D-101): hidden from everyone at once, restorable
 * by its Owner until `purgeAfterUtc`, then gone for good. The server checks
 * the typed name too.
 */
export function deleteOrganisation(accessToken: string, organisationId: string, name: string): Promise<void> {
  return sendAuthorizedJson(`/api/organisations/${organisationId}`, accessToken, 'DELETE', { name });
}

/** A deleted organisation its Owner can still restore (D-101). */
export type DeletedOrganisation = {
  id: string;
  name: string;
  deletedAtUtc: string;
  /** When it, and everything in it, goes for good. */
  purgeAfterUtc: string;
};

function isDeletedOrganisationList(value: unknown): value is DeletedOrganisation[] {
  return (
    Array.isArray(value) &&
    value.every(
      (row) =>
        typeof row === 'object' &&
        row !== null &&
        typeof (row as DeletedOrganisation).id === 'string' &&
        typeof (row as DeletedOrganisation).purgeAfterUtc === 'string',
    )
  );
}

export function listDeletedOrganisations(accessToken: string, signal?: AbortSignal): Promise<DeletedOrganisation[]> {
  return getAuthorizedJson('/api/organisations/deleted', accessToken, isDeletedOrganisationList, signal);
}

export function restoreOrganisation(accessToken: string, organisationId: string): Promise<void> {
  return sendAuthorized(`/api/organisations/${organisationId}/restore`, accessToken, 'POST');
}

export function dismissSetupChecklist(
  accessToken: string,
  organisationId: string,
): Promise<void> {
  return sendAuthorized(
    `/api/organisations/${organisationId}/setup-checklist/dismiss`,
    accessToken,
    'POST',
  );
}

export function listInvitations(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<Invitation[]> {
  return getAuthorizedJson(
    `/api/organisations/${organisationId}/invitations`,
    accessToken,
    isInvitationList,
    signal,
  );
}

export function createInvitation(
  accessToken: string,
  organisationId: string,
): Promise<Invitation> {
  return postAuthorizedJson(
    `/api/organisations/${organisationId}/invitations`,
    accessToken,
    { expiresAtUtc: null },
    isInvitation,
  );
}

export function revokeInvitation(
  accessToken: string,
  organisationId: string,
  invitationId: string,
): Promise<void> {
  return sendAuthorized(
    `/api/organisations/${organisationId}/invitations/${invitationId}`,
    accessToken,
    'DELETE',
  );
}

export function previewInvitation(
  accessToken: string,
  token: string,
  signal?: AbortSignal,
): Promise<InvitationPreview> {
  return getAuthorizedJson(
    `/api/invitations/${encodeURIComponent(normalizeInviteCode(token))}`,
    accessToken,
    isInvitationPreview,
    signal,
  );
}

export function acceptInvitation(
  accessToken: string,
  token: string,
): Promise<AcceptedInvitation> {
  return postAuthorizedJson(
    `/api/invitations/${encodeURIComponent(normalizeInviteCode(token))}/accept`,
    accessToken,
    undefined,
    isAcceptedInvitation,
  );
}
