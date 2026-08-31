import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Pressable, StyleSheet, View } from 'react-native';
import { z } from 'zod';

import { createOrganisation } from '@/api/organisations';
import { Button, Card, Screen, Text, TextInput } from '@/components/ui';
import { useSession } from '@/providers/auth-provider';
import { useActiveOrganisation } from '@/stores/active-organisation';
import { colors, radii, spacing } from '@/theme';

const GROUP_TYPES = [
  'music',
  'choir',
  'dance',
  'theatre',
  'community',
  'other',
] as const;

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'An organisation name is required.')
    .max(120, 'Keep the name under 120 characters.'),
  groupType: z.string().nullable(),
  timeZoneId: z.string().trim().min(1, 'A time zone is required.'),
});

type FormValues = z.infer<typeof schema>;

/** Organisation creation (D-057): name required, everything else optional. */
export default function CreateOrganisation() {
  const router = useRouter();
  const session = useSession();
  const queryClient = useQueryClient();
  const { setActiveOrganisation } = useActiveOrganisation();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const detectedTimeZone =
    Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

  const { control, handleSubmit, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      groupType: null,
      timeZoneId: detectedTimeZone,
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    setSubmitError(null);
    try {
      const accessToken = await session.getAccessToken();
      const organisation = await createOrganisation(accessToken, {
        name: values.name,
        groupType: values.groupType,
        timeZoneId: values.timeZoneId,
      });
      setActiveOrganisation(organisation.id);
      // Refetch (not just invalidate) so home sees the new organisation
      // immediately — the list query is inactive while this screen is open,
      // and navigating against the stale empty cache bounced back here.
      await queryClient.refetchQueries({
        queryKey: ['organisations'],
        type: 'all',
      });
      router.dismissTo('/');
    } catch {
      setSubmitError(
        'We could not create the organisation. Check your connection and try again.',
      );
    }
  });

  return (
    <Screen scroll>
      <View style={styles.header}>
        <Text variant="title">Create your organisation</Text>
        <Text color="secondary">
          You will be its Owner. Invitations and settings come after — this
          takes ten seconds.
        </Text>
      </View>

      <Card style={styles.card}>
        <Controller
          control={control}
          name="name"
          render={({ field }) => (
            <TextInput
              label="Organisation name"
              placeholder="e.g. Australian Qawwal Party"
              value={field.value}
              onChangeText={field.onChange}
              error={formState.errors.name?.message}
            />
          )}
        />

        <Text variant="label" color="secondary">
          Group type (optional)
        </Text>
        <Controller
          control={control}
          name="groupType"
          render={({ field }) => (
            <View style={styles.chips}>
              {GROUP_TYPES.map((type) => {
                const selected = field.value === type;
                return (
                  <Pressable
                    key={type}
                    accessibilityRole="button"
                    accessibilityState={{ selected }}
                    onPress={() => field.onChange(selected ? null : type)}
                    style={[styles.chip, selected ? styles.chipSelected : null]}
                  >
                    <Text
                      variant="label"
                      color={selected ? 'inverse' : 'secondary'}
                    >
                      {type}
                    </Text>
                  </Pressable>
                );
              })}
            </View>
          )}
        />

        <Controller
          control={control}
          name="timeZoneId"
          render={({ field }) => (
            <TextInput
              label="Time zone"
              value={field.value}
              onChangeText={field.onChange}
              helperText="Detected automatically — edit if it looks wrong."
              error={formState.errors.timeZoneId?.message}
              autoCapitalize="none"
            />
          )}
        />

        {submitError ? (
          <Text color="error" variant="bodySmall">
            {submitError}
          </Text>
        ) : null}

        <Button
          label="Create organisation"
          onPress={onSubmit}
          loading={formState.isSubmitting}
        />
        <Button
          label="Back"
          variant="ghost"
          onPress={() => router.back()}
          disabled={formState.isSubmitting}
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
  chips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.sm,
  },
  chip: {
    minHeight: 40,
    justifyContent: 'center',
    paddingHorizontal: spacing.lg,
    borderRadius: radii.full,
    borderWidth: 1,
    borderColor: colors.border.strong,
    backgroundColor: colors.surface.raised,
  },
  chipSelected: {
    backgroundColor: colors.interactive.primary,
    borderColor: colors.interactive.primary,
  },
});
