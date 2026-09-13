import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import { createCustomer, updateCustomer, type Customer } from '@/api/customers';
import { Button, Text, TextInput } from '@/components/ui';
import { useCustomerMutation } from '@/hooks/use-customers';
import { spacing } from '@/theme';

/**
 * Add or edit one customer in the directory. The same form serves the
 * directory, the picker's "add new" path, and the customer page, so a
 * customer looks the same wherever they were first typed in.
 */
export function CustomerForm({
  customer,
  submitLabel = customer ? 'Save' : 'Add customer',
  onSaved,
  onCancel,
}: {
  /** Absent means create. */
  customer?: Customer;
  submitLabel?: string;
  onSaved: (customer: Customer) => void;
  onCancel: () => void;
}) {
  const [name, setName] = useState(customer?.name ?? '');
  const [contactName, setContactName] = useState(customer?.contactName ?? '');
  const [phone, setPhone] = useState(customer?.phone ?? '');
  const [email, setEmail] = useState(customer?.email ?? '');
  const [notes, setNotes] = useState(customer?.notes ?? '');
  const [error, setError] = useState<string | null>(null);

  const save = useCustomerMutation<void, Customer>(async (accessToken, organisationId) => {
    const input = {
      name: name.trim(),
      contactName: contactName.trim() || null,
      phone: phone.trim() || null,
      email: email.trim() || null,
      notes: notes.trim() || null,
    };
    if (customer) {
      await updateCustomer(accessToken, organisationId, customer.id, input);
      return { ...customer, ...input };
    }
    return createCustomer(accessToken, organisationId, input);
  });

  const submit = () => {
    if (!name.trim()) {
      setError('A customer needs a name.');
      return;
    }
    setError(null);
    save.mutate(undefined, {
      onSuccess: onSaved,
      onError: () => setError('Could not save that. Check your connection and try again.'),
    });
  };

  return (
    <View style={styles.form}>
      <TextInput
        label="Customer"
        placeholder="e.g. The Khan family, University of Canberra"
        value={name}
        onChangeText={setName}
        error={error === 'A customer needs a name.' ? error : undefined}
        autoFocus={!customer}
      />
      <TextInput
        label="Contact person"
        placeholder="Who to ring"
        value={contactName}
        onChangeText={setContactName}
      />
      <TextInput label="Phone" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
      <TextInput
        label="Email"
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
      />
      <TextInput
        label="Notes"
        placeholder="How they like things, who signs off, what to remember"
        value={notes}
        onChangeText={setNotes}
        multiline
      />

      {error && error !== 'A customer needs a name.' ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}

      <Button label={submitLabel} loading={save.isPending} onPress={submit} />
      <Button label="Cancel" variant="ghost" onPress={onCancel} disabled={save.isPending} />
    </View>
  );
}

const styles = StyleSheet.create({
  form: {
    gap: spacing.md,
  },
});
