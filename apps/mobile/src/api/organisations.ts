import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
} from '@/api/client';

export type Organisation = {
  id: string;
  name: string;
  groupType: string | null;
  timeZoneId: string;
  logoUrl: string | null;
  role: 'Owner' | 'Admin' | 'Member';
  showSetupChecklist: boolean;
};

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
    `/api/invitations/${token.trim()}`,
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
    `/api/invitations/${token.trim()}/accept`,
    accessToken,
    undefined,
    isAcceptedInvitation,
  );
}
