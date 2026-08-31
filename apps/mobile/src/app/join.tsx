import { useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import {
  acceptInvitation,
  previewInvitation,
  type InvitationPreview,
} from '@/api/organisations';
import { ApiError } from '@/api/client';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { useActiveOrganisation } from '@/stores/active-organisation';
import { spacing } from '@/theme';

/**
 * Join with an invite code (D-045, D-056): the organisation's identity is
 * shown before the person confirms, and joining always makes them a Member.
 */
export default function Join() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const { setActiveOrganisation } = useActiveOrganisation();

  const [code, setCode] = useState('');
  const [preview, setPreview] = useState<InvitationPreview | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handlePreview() {
    setBusy(true);
    setError(null);
    try {
      const accessToken = await session.getAccessToken();
      setPreview(await previewInvitation(accessToken, code));
    } catch (caught) {
      setPreview(null);
      setError(
        caught instanceof ApiError && caught.status === 404
          ? 'That invite code is not valid — it may have been revoked or expired. Check it and try again.'
          : 'We could not check that code. Check your connection and try again.',
      );
    } finally {
      setBusy(false);
    }
  }

  async function handleJoin() {
    setBusy(true);
    setError(null);
    try {
      const accessToken = await session.getAccessToken();
      const joined = await acceptInvitation(accessToken, code);
      setActiveOrganisation(joined.organisationId);
      await queryClient.refetchQueries({
        queryKey: ['organisations'],
        type: 'all',
      });
      router.dismissTo('/');
    } catch {
      setError('Joining did not work. Please try again.');
      setBusy(false);
    }
  }

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">Join an organisation</Text>
        <Text color="secondary">
          Paste the invite code your organiser shared with you.
        </Text>
      </View>

      <Card style={styles.card}>
        <TextInput
          label="Invite code"
          placeholder="e.g. kx7m2p9qanb3vwrt56hjde2c8f"
          value={code}
          onChangeText={(value) => {
            setCode(value);
            setPreview(null);
          }}
          autoCapitalize="none"
          autoCorrect={false}
          error={error ?? undefined}
        />

        {preview === null ? (
          <Button
            label="Check code"
            onPress={handlePreview}
            loading={busy}
            disabled={busy || code.trim().length === 0}
          />
        ) : (
          <>
            <View style={styles.previewBox}>
              <Text variant="subheading">{preview.organisationName}</Text>
              <Text color="secondary" variant="bodySmall">
                {preview.groupType
                  ? `${preview.groupType} organisation`
                  : 'Organisation'}{' '}
                — you will join as a Member.
              </Text>
            </View>
            <Button
              label={`Join ${preview.organisationName}`}
              onPress={handleJoin}
              loading={busy}
            />
          </>
        )}

        <Button
          label="Back"
          variant="ghost"
          onPress={() => router.back()}
          disabled={busy}
        />
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  card: {
    gap: spacing.md,
  },
  previewBox: {
    gap: spacing.xs,
    paddingVertical: spacing.sm,
  },
});
