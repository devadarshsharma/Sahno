import { Ionicons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { CustomerRow } from '@/components/customer-picker';
import { Card, Screen, Text, TextInput } from '@/components/ui';
import { useCustomers, useIsOrganiser } from '@/hooks/use-customers';
import { colors, radii, spacing } from '@/theme';

/**
 * The customer directory (D-022): every family, venue and company that has
 * booked the group, with how often. Organisers only — a member who lands
 * here by deep link sees nothing, because the query never runs for them.
 */
export default function Customers() {
  const router = useRouter();
  const isOrganiser = useIsOrganiser();
  const customersQuery = useCustomers();
  const [search, setSearch] = useState('');

  const rows = useMemo(() => {
    const all = customersQuery.data ?? [];
    const needle = search.trim().toLowerCase();
    if (!needle) {
      return all;
    }
    return all.filter(
      (customer) =>
        customer.name.toLowerCase().includes(needle) ||
        (customer.contactName ?? '').toLowerCase().includes(needle),
    );
  }, [customersQuery.data, search]);

  const total = customersQuery.data?.length ?? 0;
  const bookings = (customersQuery.data ?? []).reduce(
    (sum, customer) => sum + customer.bookingCount,
    0,
  );

  return (
    <Screen
      scroll
      hero={{
        title: 'Customers',
        subtitle: isOrganiser
          ? total === 0
            ? 'The people who book you. Add one and they are a tap away next time.'
            : `${total} customer${total === 1 ? '' : 's'} · ${bookings} booking${bookings === 1 ? '' : 's'} between them`
          : 'Organisers only.',
        right: isOrganiser ? (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Add a customer"
            onPress={() => router.push('/customers/new')}
            hitSlop={8}
            style={({ pressed }) => [styles.addButton, pressed ? styles.pressed : null]}
          >
            <Ionicons name="add" size={22} color={colors.text.inverse} />
          </Pressable>
        ) : undefined,
      }}
    >
      {!isOrganiser ? null : customersQuery.isPending ? (
        <ActivityIndicator color={colors.tealText} style={styles.spinner} />
      ) : (
        <>
          {total > 4 ? (
            <TextInput
              label="Search"
              placeholder="Name or contact"
              value={search}
              onChangeText={setSearch}
              autoCorrect={false}
            />
          ) : null}

          <Card style={styles.list}>
            {rows.length === 0 ? (
              <Text color="muted" variant="bodySmall" style={styles.empty}>
                {search.trim() ? 'Nobody by that name.' : 'No customers yet.'}
              </Text>
            ) : (
              rows.map((customer) => (
                <CustomerRow
                  key={customer.id}
                  customer={customer}
                  onPress={() => router.push(`/customers/${customer.id}`)}
                />
              ))
            )}
          </Card>
        </>
      )}
      <View style={styles.footer} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  addButton: {
    width: 36,
    height: 36,
    borderRadius: radii.full,
    backgroundColor: colors.navySoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  pressed: {
    opacity: 0.6,
  },
  spinner: {
    marginTop: spacing.xl,
  },
  list: {
    paddingVertical: 0,
    paddingHorizontal: spacing.lg,
    marginTop: spacing.md,
  },
  empty: {
    paddingVertical: spacing.lg,
    textAlign: 'center',
  },
  footer: {
    height: spacing.xl,
  },
});
