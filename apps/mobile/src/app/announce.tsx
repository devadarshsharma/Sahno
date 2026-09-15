import { useRouter } from 'expo-router';
import { useState } from 'react';
import { StyleSheet } from 'react-native';

import { sendAnnouncement } from '@/api/notifications';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import { useIsOrganiser } from '@/hooks/use-customers';
import { useActiveOrg } from '@/hooks/use-organisations';
import { useSession } from '@/providers/auth-provider';
import { spacing } from '@/theme';

/**
 * An organiser writes to everybody (D-080): every member's bell, and a push
 * to every phone they are signed in on. For the things that are not about
 * one booking — a rehearsal moved, a reminder about subs, a thank-you.
 */
export default function Announce() {
  const router = useRouter();
  const session = useSession();
  const { active } = useActiveOrg();
  const isOrganiser = useIsOrganiser();
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const send = async () => {
    if (!title.trim()) {
      setError('Give it a title — that is what shows on the phone.');
      return;
    }
    setError(null);
    setSending(true);
    try {
      const accessToken = await session.getAccessToken();
      await sendAnnouncement(accessToken, active!.id, {
        title: title.trim(),
        body: body.trim() || null,
      });
      router.back();
    } catch {
      setError('Could not send that. Check your connection and try again.');
      setSending(false);
    }
  };

  return (
    <Screen
      scroll
      hero={{
        title: 'Announcement',
        subtitle: isOrganiser
          ? `Everyone in ${active?.name ?? 'the organisation'} gets this on their phone.`
          : 'Organisers only.',
      }}
    >
      {isOrganiser ? (
        <Card style={styles.card}>
          <TextInput
            label="Title"
            placeholder="e.g. Rehearsal moved to Thursday"
            value={title}
            onChangeText={setTitle}
            error={error && !title.trim() ? error : undefined}
            autoFocus
          />
          <TextInput
            label="Message (optional)"
            placeholder="Same place, 7pm. Bring the new lyrics."
            value={body}
            onChangeText={setBody}
            multiline
          />
          {error && title.trim() ? (
            <Text color="error" variant="bodySmall">
              {error}
            </Text>
          ) : null}
          <Button label="Send to everyone" loading={sending} onPress={send} />
          <Button label="Cancel" variant="ghost" onPress={() => router.back()} disabled={sending} />
        </Card>
      ) : null}
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
  },
});
