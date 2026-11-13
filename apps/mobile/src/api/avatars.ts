import { ApiError, getAuthorizedJson, sendAuthorized } from '@/api/client';
import { environment } from '@/config/environment';

/** Who in an organisation has a photo, and its version (D-129). */
export type AvatarVersion = { userId: string; version: number };

function isAvatarVersionList(value: unknown): value is AvatarVersion[] {
  return (
    Array.isArray(value) &&
    value.every((row) => typeof row === 'object' && row !== null && typeof (row as AvatarVersion).userId === 'string')
  );
}

export function listAvatarVersions(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<AvatarVersion[]> {
  return getAuthorizedJson(`/api/organisations/${organisationId}/avatars`, accessToken, isAvatarVersionList, signal);
}

/** Where somebody's photo is; the version makes a changed photo a new address. */
export function avatarUrl(userId: string, version: number): string {
  return `${environment.apiUrl}/api/users/${userId}/avatar?v=${version}`;
}

/** Sends your own photo: a small JPEG file on this phone (already cropped and shrunk). */
export async function uploadAvatar(accessToken: string, fileUri: string): Promise<AvatarVersion> {
  const form = new FormData();
  // React Native's FormData takes a file as { uri, name, type }.
  form.append('photo', { uri: fileUri, name: 'photo.jpg', type: 'image/jpeg' } as unknown as Blob);

  const response = await fetch(`${environment.apiUrl}/api/me/avatar`, {
    method: 'PUT',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: form,
  });
  if (!response.ok) {
    throw new ApiError('/api/me/avatar', response.status);
  }
  return (await response.json()) as AvatarVersion;
}

export function removeAvatar(accessToken: string): Promise<void> {
  return sendAuthorized('/api/me/avatar', accessToken, 'DELETE');
}
