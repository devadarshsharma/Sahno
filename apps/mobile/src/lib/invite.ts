import { Share } from 'react-native';

/** Invite codes are 8 characters (Invitation.TokenLength on the API). */
const CODE_LENGTH = 8;

/**
 * How a code is shown and shared: "K7MP9QAB" as "K7MP-9QAB", which is easier
 * to read aloud and copy by eye. The dash is only for people — the API
 * ignores it, along with spaces and letter case.
 */
export function formatInviteCode(token: string): string {
  return token.length === CODE_LENGTH
    ? `${token.slice(0, 4)}-${token.slice(4)}`
    : token;
}

/**
 * A typed code as the API expects it in a URL: spaces and dashes dropped, and
 * an 8-character code upper-cased. A longer code (issued before codes were
 * shortened) keeps its case, because it must match exactly.
 */
export function normalizeInviteCode(typed: string): string {
  const compact = typed.replace(/[\s-]/g, '');
  return compact.length === CODE_LENGTH ? compact.toUpperCase() : compact;
}

/**
 * The message sent with an invite code. Kept in one place because it names
 * the exact option people must tap in onboarding — when that wording drifts,
 * the instructions send them looking for a button that is not there.
 */
export function inviteMessage(
  organisationName: string,
  token: string,
): string {
  return (
    `You're invited to join ${organisationName} on Sahno!\n\n` +
    `1. Install the Sahno app and sign in\n` +
    `2. Choose "Join an organisation"\n` +
    `3. Enter this code: ${formatInviteCode(token)}`
  );
}

/**
 * Opens the share sheet for an invite code. Dismissing it is not a failure —
 * the code stays active and can be shared again from the invite list.
 */
export function shareInvite(
  organisationName: string,
  token: string,
): Promise<unknown> {
  return Share.share({ message: inviteMessage(organisationName, token) });
}
