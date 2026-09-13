import { Ionicons } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Linking, Pressable, StyleSheet, View } from 'react-native';

import type { CustomerBooking } from '@/api/customers';
import type { EngagementStatus } from '@/api/engagements';
import { CustomerForm } from '@/components/customer-form';
import { bookingsLabel } from '@/components/customer-picker';
import { StatusChip } from '@/components/engagement-card';
import { Button, Card, formatIsoDate, Screen, Text } from '@/components/ui';
import { useCustomerDetail } from '@/hooks/use-customers';
import { colors, radii, spacing } from '@/theme';

/**
 * One customer: who they are, how to reach them, and every booking they have
 * had — "the UOC has hired us twice" as a list rather than a memory. A new
 * booking can start from here, already in their name.
 */
export default function CustomerDetail() {
  const { customerId } = useLocalSearchParams<{ customerId: string }>();
  const router = useRouter();
  const detailQuery = useCustomerDetail(customerId ?? null);
  const [editing, setEditing] = useState(false);

  if (detailQuery.isPending) {
    return (
      <Screen hero={{ title: 'Customer' }}>
        <ActivityIndicator color={colors.tealText} style={styles.spinner} />
      </Screen>
    );
  }

  if (!detailQuery.isSuccess) {
    return (
      <Screen hero={{ title: 'Customer' }}>
        <Card style={styles.card}>
          <Text variant="subheading">Customer not found</Text>
          <Text color="secondary" variant="bodySmall">
            They may have been removed, or belong to another organisation.
          </Text>
          <Button label="Back to customers" variant="secondary" onPress={() => router.replace('/customers')} />
        </Card>
      </Screen>
    );
  }

  const { customer, bookings } = detailQuery.data;

  return (
    <Screen
      scroll
      hero={{
        eyebrow: 'CUSTOMER',
        title: customer.name,
        subtitle: bookingsLabel(customer.bookingCount),
      }}
    >
      <Card style={styles.card}>
        {editing ? (
          <CustomerForm
            customer={customer}
            onSaved={() => setEditing(false)}
            onCancel={() => setEditing(false)}
          />
        ) : (
          <>
            <Text variant="subheading">Contact</Text>
            {customer.contactName || customer.phone || customer.email ? (
              <View style={styles.rows}>
                {customer.contactName ? (
                  <Detail icon="person-outline" value={customer.contactName} />
                ) : null}
                {customer.phone ? (
                  <Detail
                    icon="call-outline"
                    value={customer.phone}
                    onPress={() => Linking.openURL(`tel:${customer.phone}`)}
                  />
                ) : null}
                {customer.email ? (
                  <Detail
                    icon="mail-outline"
                    value={customer.email}
                    onPress={() => Linking.openURL(`mailto:${customer.email}`)}
                  />
                ) : null}
              </View>
            ) : (
              <Text color="muted" variant="bodySmall">
                No contact details yet.
              </Text>
            )}

            {customer.notes ? (
              <View style={styles.notes}>
                <Text variant="label" color="secondary">
                  Notes
                </Text>
                <Text variant="bodySmall">{customer.notes}</Text>
              </View>
            ) : null}

            <Button label="Edit" variant="secondary" onPress={() => setEditing(true)} />
          </>
        )}
      </Card>

      <Card style={styles.card}>
        <Text variant="subheading">Bookings</Text>
        {bookings.length === 0 ? (
          <Text color="muted" variant="bodySmall">
            Nothing yet. Start one below and it will show here.
          </Text>
        ) : (
          <View style={styles.bookings}>
            {bookings.map((booking) => (
              <BookingRow
                key={booking.engagementId}
                booking={booking}
                onPress={() => router.push(`/engagement/${booking.engagementId}`)}
              />
            ))}
          </View>
        )}
        <Button
          label="New booking for this customer"
          onPress={() =>
            router.push({
              pathname: '/create-engagement',
              params: { customerId: customer.id, customerName: customer.name },
            })
          }
        />
      </Card>
      <View style={styles.footer} />
    </Screen>
  );
}

function BookingRow({ booking, onPress }: { booking: CustomerBooking; onPress: () => void }) {
  const when = booking.startDate ? formatIsoDate(booking.startDate) : 'Date TBC';
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${booking.title}, ${when}.`}
      onPress={onPress}
      style={({ pressed }) => [styles.booking, pressed ? styles.pressed : null]}
    >
      <View style={styles.bookingText}>
        <Text style={styles.bookingTitle} numberOfLines={1}>
          {booking.title}
        </Text>
        <Text variant="caption" color="secondary" numberOfLines={1}>
          {[when, booking.venue].filter(Boolean).join(' · ')}
        </Text>
      </View>
      <StatusChip status={booking.status as EngagementStatus} />
    </Pressable>
  );
}

function Detail({
  icon,
  value,
  onPress,
}: {
  icon: React.ComponentProps<typeof Ionicons>['name'];
  value: string;
  onPress?: () => void;
}) {
  return (
    <View style={styles.detail}>
      <Ionicons name={icon} size={16} color={colors.text.muted} />
      <Text
        variant="bodySmall"
        color={onPress ? 'accent' : 'primary'}
        onPress={onPress}
        suppressHighlighting
        style={styles.detailValue}
      >
        {value}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  spinner: {
    marginTop: spacing.xl,
  },
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  rows: {
    gap: spacing.sm,
  },
  detail: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  detailValue: {
    flex: 1,
  },
  notes: {
    gap: 2,
  },
  bookings: {
    borderRadius: radii.md,
    overflow: 'hidden',
  },
  booking: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border.default,
  },
  bookingText: {
    flex: 1,
    gap: 2,
  },
  bookingTitle: {
    fontWeight: '600',
  },
  pressed: {
    opacity: 0.6,
  },
  footer: {
    height: spacing.xl,
  },
});
