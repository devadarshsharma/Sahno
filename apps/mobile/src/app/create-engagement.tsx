import { zodResolver } from '@hookform/resolvers/zod';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Pressable, StyleSheet, View } from 'react-native';
import { z } from 'zod';
import { Ionicons } from '@expo/vector-icons';

import { createEngagement } from '@/api/engagements';
import { CustomerPicker } from '@/components/customer-picker';
import { Button, Card, DateField, Screen, Text, TextInput } from '@/components/ui';
import { useEngagementMutation } from '@/hooks/use-engagements';
import { useIsOrganiser } from '@/hooks/use-customers';
import { colors, radii, spacing } from '@/theme';

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
  // Arriving from a customer's page: the booking starts in their name.
  const params = useLocalSearchParams<{ customerId?: string; customerName?: string }>();
  const isOrganiser = useIsOrganiser();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [customer, setCustomer] = useState<{ id: string; name: string } | null>(
    params.customerId && params.customerName
      ? { id: params.customerId, name: params.customerName }
      : null,
  );
  const [picking, setPicking] = useState(false);

  const create = useEngagementMutation<{
    title: string;
    startDate: string | null;
    venue: string | null;
    customerId: string | null;
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
        customerId: customer?.id ?? null,
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

        {isOrganiser ? (
          <View style={styles.field}>
            <Text variant="label" color="secondary">
              Customer (optional)
            </Text>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel={customer ? `Customer: ${customer.name}. Change.` : 'Choose a customer'}
              onPress={() => setPicking(true)}
              style={({ pressed }) => [styles.customerField, pressed ? styles.pressed : null]}
            >
              <Ionicons
                name={customer ? 'person-circle-outline' : 'person-add-outline'}
                size={18}
                color={customer ? colors.tealText : colors.text.muted}
              />
              <Text style={[styles.customerName, customer ? null : styles.placeholder]} numberOfLines={1}>
                {customer ? customer.name : 'Who is this for?'}
              </Text>
              {customer ? (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel="Remove customer"
                  onPress={() => setCustomer(null)}
                  hitSlop={8}
                >
                  <Ionicons name="close-circle" size={18} color={colors.text.muted} />
                </Pressable>
              ) : (
                <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
              )}
            </Pressable>
            <Text variant="caption" color="muted">
              Pick a returning customer or add a new one. Members never see this.
            </Text>
          </View>
        ) : null}

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

      <CustomerPicker
        visible={picking}
        onSelect={(chosen) => {
          setCustomer({ id: chosen.id, name: chosen.name });
          setPicking(false);
        }}
        onClose={() => setPicking(false)}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
  },
  field: {
    gap: spacing.xs,
  },
  customerField: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    minHeight: 48,
    paddingHorizontal: spacing.md,
    borderWidth: 1,
    borderColor: colors.border.default,
    borderRadius: radii.md,
    backgroundColor: colors.surface.raised,
  },
  customerName: {
    flex: 1,
  },
  placeholder: {
    color: colors.text.muted,
  },
  pressed: {
    opacity: 0.7,
  },
});
