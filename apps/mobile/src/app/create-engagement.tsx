import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { StyleSheet } from 'react-native';
import { z } from 'zod';

import { createEngagement } from '@/api/engagements';
import { Button, Card, DateField, Screen, Text, TextInput } from '@/components/ui';
import { useEngagementMutation } from '@/hooks/use-engagements';
import { spacing } from '@/theme';

/**
 * Starts an enquiry. Only a title is required (D-025): the date, venue, and
 * time are what an organiser often does not know when the phone call comes in,
 * so asking for them here would just invite made-up answers.
 */
const schema = z.object({
  title: z
    .string()
    .trim()
    .min(1, 'Give the enquiry a name you will recognise.')
    .max(200, 'Keep the title under 200 characters.'),
  startDate: z.string().nullable(),
  venue: z.string().trim().max(200, 'Keep the venue under 200 characters.'),
});

type FormValues = z.infer<typeof schema>;

export default function CreateEngagement() {
  const router = useRouter();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const create = useEngagementMutation<{
    title: string;
    startDate: string | null;
    venue: string | null;
  }>((accessToken, organisationId, args) =>
    createEngagement(accessToken, organisationId, args),
  );

  const { control, handleSubmit, formState } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { title: '', startDate: null, venue: '' },
  });

  const onSubmit = handleSubmit((values) => {
    setSubmitError(null);
    create.mutate(
      {
        title: values.title,
        startDate: values.startDate,
        venue: values.venue ? values.venue : null,
      },
      {
        onSuccess: () => router.back(),
        onError: () =>
          setSubmitError(
            'We could not save the enquiry. Check your connection and try again.',
          ),
      },
    );
  });

  return (
    <Screen
      scroll
      hero={{
        title: 'New enquiry',
        subtitle:
          'A name is all you need. It stays private to you and your admins until you request availability.',
      }}
    >

      <Card style={styles.card}>
        <Controller
          control={control}
          name="title"
          render={({ field }) => (
            <TextInput
              label="What is it?"
              placeholder="e.g. Sharma wedding reception"
              value={field.value}
              onChangeText={field.onChange}
              error={formState.errors.title?.message}
              autoFocus
            />
          )}
        />

        <Controller
          control={control}
          name="startDate"
          render={({ field }) => (
            <DateField
              label="Proposed date"
              value={field.value}
              onChange={field.onChange}
              placeholder="Not set yet"
              helperText="You will need one before you can ask members for availability."
              clearable
            />
          )}
        />

        <Controller
          control={control}
          name="venue"
          render={({ field }) => (
            <TextInput
              label="Venue (optional)"
              placeholder="Still to be confirmed"
              value={field.value}
              onChangeText={field.onChange}
              error={formState.errors.venue?.message}
            />
          )}
        />

        {submitError ? (
          <Text color="error" variant="bodySmall">
            {submitError}
          </Text>
        ) : null}

        <Button
          label="Save enquiry"
          onPress={onSubmit}
          loading={create.isPending}
        />
        <Button
          label="Cancel"
          variant="ghost"
          onPress={() => router.back()}
          disabled={create.isPending}
        />
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
  },
});
