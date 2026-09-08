import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { StyleSheet, View } from 'react-native';
import { z } from 'zod';

import { updateMe } from '@/api/me';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import { useMe } from '@/hooks/use-me';
import { useSession } from '@/providers/auth-provider';
import { spacing } from '@/theme';

const schema = z.object({
  displayName: z
    .string()
    .trim()
    .min(1, 'Please enter a name.')
    .max(200, 'Keep the name under 200 characters.'),
});

type FormValues = z.infer<typeof schema>;

/**
 * Asks for the display name D-046 requires. Google and Apple supply a real
 * name, but the passwordless email connection sends the email address in its
 * place — so people who signed in with a code arrive here with nothing to be
 * called. Only the name is asked for: the rest of the profile is optional
 * (D-046) and travel-grade identity is collected just-in-time (D-076).
 * Also reached from More to change an existing name.
 */
export default function SetName() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const meQuery = useMe();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const currentName = meQuery.data?.displayName ?? '';

  const { control, handleSubmit, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: currentName },
  });

  const onSubmit = handleSubmit(async (values) => {
    setSubmitError(null);
    try {
      const accessToken = await session.getAccessToken();
      await updateMe(accessToken, values.displayName);
      // Refetch rather than invalidate: the screens that send people here
      // read this query, and navigating against a stale cache would bounce
      // them straight back.
      await queryClient.refetchQueries({ queryKey: ['me'], type: 'all' });
      if (router.canGoBack()) {
        router.back();
        return;
      }
      router.replace('/(tabs)');
    } catch {
      setSubmitError(
        'We could not save your name. Check your connection and try again.',
      );
    }
  });

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text style={styles.emoji}>✋</Text>
        <Text variant="title">
          {currentName ? 'Change your name' : 'What should we call you?'}
        </Text>
        <Text color="secondary">
          This is the name the rest of your group sees. You can change it later.
        </Text>
      </View>

      <Card style={styles.card}>
        <Controller
          control={control}
          name="displayName"
          render={({ field }) => (
            <TextInput
              label="Your name"
              placeholder="e.g. Adarsh Sharma"
              value={field.value}
              onChangeText={field.onChange}
              error={formState.errors.displayName?.message}
              autoCapitalize="words"
              autoComplete="name"
              autoFocus
              returnKeyType="done"
              onSubmitEditing={onSubmit}
            />
          )}
        />

        {submitError ? (
          <Text color="error" variant="bodySmall">
            {submitError}
          </Text>
        ) : null}

        <Button
          label={currentName ? 'Save' : 'Continue'}
          onPress={onSubmit}
          loading={formState.isSubmitting}
        />
      </Card>

      {currentName ? (
        <Button
          label="Cancel"
          variant="ghost"
          onPress={() => router.back()}
          disabled={formState.isSubmitting}
        />
      ) : (
        <Button
          label="Sign out"
          variant="ghost"
          onPress={session.signOut}
          disabled={formState.isSubmitting}
        />
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: {
    gap: spacing.sm,
    marginBottom: spacing.xl,
  },
  emoji: {
    fontSize: 44,
    lineHeight: 56,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
});
