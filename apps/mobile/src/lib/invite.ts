import { Share } from 'react-native';

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
    `3. Enter this code: ${token}`
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
