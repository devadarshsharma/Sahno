import { Ionicons } from '@expo/vector-icons';
import { useMemo, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import type { Customer } from '@/api/customers';
import { CustomerForm } from '@/components/customer-form';
import { Text, TextInput } from '@/components/ui';
import { useCustomers } from '@/hooks/use-customers';
import { colors, radii, spacing } from '@/theme';

/**
 * Pick a customer from the directory, or add one on the spot. The point of
 * the directory is that the Patels are chosen the second time, not retyped,
 * so the list comes first and "add new" is the fallback — and it searches
 * as you type so a hundred past customers is no slower than ten.
 */
export function CustomerPicker({
  visible,
  onSelect,
  onClose,
}: {
  visible: boolean;
  onSelect: (customer: Customer) => void;
  onClose: () => void;
}) {
  const insets = useSafeAreaInsets();
  const customersQuery = useCustomers();
  const [search, setSearch] = useState('');
  const [adding, setAdding] = useState(false);

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

  const close = () => {
    setSearch('');
    setAdding(false);
    onClose();
  };

  const choose = (customer: Customer) => {
    setSearch('');
    setAdding(false);
    onSelect(customer);
  };

  return (
    <Modal visible={visible} animationType="slide" onRequestClose={close}>
      <View style={styles.sheet}>
        <View style={[styles.header, { paddingTop: insets.top + spacing.md }]}>
          <Text style={styles.title} accessibilityRole="header">
            {adding ? 'New customer' : 'Choose a customer'}
          </Text>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Close"
            onPress={close}
            hitSlop={12}
            style={styles.close}
          >
            <Ionicons name="close" size={22} color={colors.text.inverse} />
          </Pressable>
        </View>

        {adding ? (
          <View style={styles.body}>
            <CustomerForm onSaved={choose} onCancel={() => setAdding(false)} />
          </View>
        ) : (
          <>
            <View style={styles.search}>
              <TextInput
                label="Search"
                placeholder="Name or contact"
                value={search}
                onChangeText={setSearch}
                autoFocus
                autoCorrect={false}
              />
            </View>

            {customersQuery.isPending ? (
              <ActivityIndicator color={colors.tealText} style={styles.spinner} />
            ) : (
              <FlatList
                data={rows}
                keyExtractor={(customer) => customer.id}
                keyboardShouldPersistTaps="handled"
                contentContainerStyle={[styles.list, { paddingBottom: insets.bottom + spacing.xl }]}
                ListHeaderComponent={
                  <Pressable
                    accessibilityRole="button"
                    onPress={() => setAdding(true)}
                    style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
                  >
                    <View style={[styles.avatar, styles.avatarAdd]}>
                      <Ionicons name="add" size={20} color={colors.tealText} />
                    </View>
                    <View style={styles.rowText}>
                      <Text style={styles.rowTitle}>
                        {search.trim() ? `Add “${search.trim()}”` : 'Add a new customer'}
                      </Text>
                      <Text variant="caption" color="secondary">
                        Somebody who has not booked before
                      </Text>
                    </View>
                  </Pressable>
                }
                ListEmptyComponent={
                  <Text color="muted" variant="bodySmall" style={styles.empty}>
                    {search.trim()
                      ? 'Nobody by that name yet.'
                      : 'No customers yet. Add the first one above.'}
                  </Text>
                }
                renderItem={({ item }) => (
                  <CustomerRow customer={item} onPress={() => choose(item)} />
                )}
              />
            )}
          </>
        )}
      </View>
    </Modal>
  );
}

/** One directory row: name, contact, and how often they have booked. */
export function CustomerRow({
  customer,
  onPress,
}: {
  customer: Customer;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={`${customer.name}. ${bookingsLabel(customer.bookingCount)}.`}
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed ? styles.rowPressed : null]}
    >
      <View style={styles.avatar}>
        <Text style={styles.avatarText}>{initials(customer.name)}</Text>
      </View>
      <View style={styles.rowText}>
        <Text style={styles.rowTitle} numberOfLines={1}>
          {customer.name}
        </Text>
        <Text variant="caption" color="secondary" numberOfLines={1}>
          {[customer.contactName, bookingsLabel(customer.bookingCount)]
            .filter(Boolean)
            .join(' · ')}
        </Text>
      </View>
      <Ionicons name="chevron-forward" size={18} color={colors.text.muted} />
    </Pressable>
  );
}

export function bookingsLabel(count: number): string {
  if (count === 0) {
    return 'No bookings yet';
  }
  return `${count} booking${count === 1 ? '' : 's'}`;
}

function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const letters = words.slice(0, 2).map((word) => word[0]?.toUpperCase() ?? '');
  return letters.join('') || '?';
}

const styles = StyleSheet.create({
  sheet: {
    flex: 1,
    backgroundColor: colors.surface.canvas,
  },
  header: {
    backgroundColor: colors.navy,
    paddingHorizontal: spacing.lg,
    paddingBottom: spacing.lg,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  title: {
    flex: 1,
    color: colors.text.inverse,
    fontSize: 22,
    fontWeight: '700',
  },
  close: {
    width: 36,
    height: 36,
    borderRadius: radii.full,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.navySoft,
  },
  body: {
    padding: spacing.lg,
  },
  search: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
  },
  spinner: {
    marginTop: spacing.xl,
  },
  list: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.sm,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border.default,
  },
  rowPressed: {
    opacity: 0.6,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: radii.full,
    backgroundColor: colors.tealSoft,
    alignItems: 'center',
    justifyContent: 'center',
  },
  avatarAdd: {
    backgroundColor: colors.surface.subtle,
    borderWidth: 1,
    borderColor: colors.border.strong,
    borderStyle: 'dashed',
  },
  avatarText: {
    color: colors.tealText,
    fontWeight: '700',
    fontSize: 14,
  },
  rowText: {
    flex: 1,
    gap: 2,
  },
  rowTitle: {
    fontWeight: '600',
  },
  empty: {
    paddingVertical: spacing.lg,
    textAlign: 'center',
  },
});
