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
import { colors, spacing } from '@/theme';

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
  const [joinedName, setJoinedName] = useState<string | null>(null);
  const [showCodeHelp, setShowCodeHelp] = useState(false);

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
      setJoinedName(joined.organisationName);
      setBusy(false);
    } catch {
      setError('Joining did not work. Please try again.');
      setBusy(false);
    }
  }

  if (joinedName) {
    return (
      <Screen>
        <View style={styles.success}>
          <Text style={styles.successEmoji}>🎉</Text>
          <Text variant="title" style={styles.centeredText}>
            You&apos;re in!
          </Text>
          <Text color="secondary" style={styles.centeredText}>
            Welcome to {joinedName}. Your events, tasks, and updates will show
            up on Home.
          </Text>
          <Button label="Go to Home" onPress={() => router.dismissTo('/(tabs)')} />
        </View>
      </Screen>
    );
  }

  return (
    <Screen scroll>
      <View style={styles.header}>
        <View style={styles.headerIllustration}>
          <Text style={styles.headerEmoji}>🔒</Text>
        </View>
        <Text variant="title" style={styles.centeredText}>
          Join an organisation
        </Text>
        <Text color="secondary" variant="bodySmall" style={styles.centeredText}>
          Ask your organiser for the invite code to join their organisation.
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
          label="How do I get an invite code?"
          variant="ghost"
          onPress={() => setShowCodeHelp((value) => !value)}
          disabled={busy}
        />
        {showCodeHelp ? (
          <Text variant="bodySmall" color="secondary">
            Ask your organiser — they can create and share an invite code from
            Sahno under Invite members. Codes are private to your group.
          </Text>
        ) : null}
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    alignItems: 'center',
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  headerIllustration: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.sm,
  },
  headerEmoji: {
    fontSize: 40,
    lineHeight: 50,
  },
  card: {
    gap: spacing.md,
  },
  previewBox: {
    gap: spacing.xs,
    paddingVertical: spacing.sm,
  },
  success: {
    flex: 1,
    justifyContent: 'center',
    gap: spacing.md,
  },
  successEmoji: {
    fontSize: 56,
    lineHeight: 68,
    textAlign: 'center',
  },
  centeredText: {
    textAlign: 'center',
  },
});
