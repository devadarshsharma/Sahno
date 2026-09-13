import { useRouter } from 'expo-router';
import { StyleSheet } from 'react-native';

import { CustomerForm } from '@/components/customer-form';
import { Card, Screen } from '@/components/ui';
import { spacing } from '@/theme';

/** Add a customer to the directory, then open them. */
export default function NewCustomer() {
  const router = useRouter();

  return (
    <Screen
      scroll
      hero={{
        title: 'New customer',
        subtitle: 'Once they are here, every booking for them is a tap, not a form.',
      }}
    >
      <Card style={styles.card}>
        <CustomerForm
          onSaved={(customer) => router.replace(`/customers/${customer.id}`)}
          onCancel={() => router.back()}
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
