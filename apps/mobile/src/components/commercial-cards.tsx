import { useState } from 'react';
import { Linking, Pressable, StyleSheet, View } from 'react-native';

import {
  createPayment,
  deletePayment,
  updateCustomer,
  updateFinance,
  updatePayment,
  type Customer,
  type Finance,
  type PerformerPayment,
} from '@/api/commercial';
import type { Participant } from '@/api/availability';
import { Button, Card, DateField, Text, TextInput } from '@/components/ui';
import { useAvailability } from '@/hooks/use-availability';
import {
  useCommercialMutation,
  useCustomer,
  useFinance,
  useFinancialAccess,
  usePayments,
} from '@/hooks/use-commercial';
import { colors, radii, spacing } from '@/theme';

/**
 * Who the booking is for, and the organiser's private notes (D-047 §7 Admin,
 * D-022). Every organiser; never a member. The query is off for members and
 * the card is never mounted for them, so there is no state in which this
 * renders for the wrong person.
 */
export function CustomerCard({ engagementId }: { engagementId: string }) {
  const customerQuery = useCustomer(engagementId);
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!customerQuery.isSuccess) {
    return null;
  }

  const customer = customerQuery.data;
  const empty =
    !customer.name && !customer.contactName && !customer.phone && !customer.email;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Customer</Text>
      <Text color="secondary" variant="bodySmall">
        Who this is for. Members never see any of this.
      </Text>

      {editing ? (
        <CustomerForm
          engagementId={engagementId}
          customer={customer}
          onDone={() => setEditing(false)}
          onError={setError}
        />
      ) : (
        <>
          {empty ? (
            <Text color="muted" variant="bodySmall">
              Nothing recorded yet.
            </Text>
          ) : (
            <View style={styles.rows}>
              {customer.name ? <Detail label="Customer" value={customer.name} /> : null}
              {customer.contactName ? (
                <Detail label="Contact" value={customer.contactName} />
              ) : null}
              {customer.phone ? (
                <Detail
                  label="Phone"
                  value={customer.phone}
                  onPress={() => Linking.openURL(`tel:${customer.phone}`)}
                />
              ) : null}
              {customer.email ? (
                <Detail
                  label="Email"
                  value={customer.email}
                  onPress={() => Linking.openURL(`mailto:${customer.email}`)}
                />
              ) : null}
            </View>
          )}

          {customer.privateNotes ? (
            <View style={styles.notes}>
              <Text variant="label" color="secondary">
                Private notes
              </Text>
              <Text variant="bodySmall">{customer.privateNotes}</Text>
            </View>
          ) : null}

          <Button
            label={empty ? 'Add customer details' : 'Edit'}
            variant="secondary"
            onPress={() => setEditing(true)}
          />
        </>
      )}

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function CustomerForm({
  engagementId,
  customer,
  onDone,
  onError,
}: {
  engagementId: string;
  customer: Customer;
  onDone: () => void;
  onError: (message: string) => void;
}) {
  const [name, setName] = useState(customer.name ?? '');
  const [contactName, setContactName] = useState(customer.contactName ?? '');
  const [phone, setPhone] = useState(customer.phone ?? '');
  const [email, setEmail] = useState(customer.email ?? '');
  const [privateNotes, setPrivateNotes] = useState(customer.privateNotes ?? '');

  const save = useCommercialMutation<void>((accessToken, organisationId) =>
    updateCustomer(accessToken, organisationId, engagementId, {
      name: name.trim() || null,
      contactName: contactName.trim() || null,
      phone: phone.trim() || null,
      email: email.trim() || null,
      privateNotes: privateNotes.trim() || null,
    }),
  );

  return (
    <View style={styles.form}>
      <TextInput label="Customer" placeholder="e.g. The Khan family" value={name} onChangeText={setName} />
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
        label="Private notes"
        placeholder="How it came in, what was said, what to remember"
        value={privateNotes}
        onChangeText={setPrivateNotes}
        multiline
      />
      <Button
        label="Save"
        loading={save.isPending}
        onPress={() =>
          save.mutate(undefined, {
            onSuccess: onDone,
            onError: () => onError('Could not save that.'),
          })
        }
      />
      <Button label="Cancel" variant="ghost" onPress={onDone} />
    </View>
  );
}

/**
 * The money (D-008, D-016). Rendered only with financial access — the Owner,
 * or an Admin the Owner has granted it. Balance is derived server-side so it
 * can never disagree with the fee and deposit it comes from.
 */
export function FinanceCard({ engagementId }: { engagementId: string }) {
  const allowed = useFinancialAccess();
  const financeQuery = useFinance(engagementId);
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!allowed || !financeQuery.isSuccess) {
    return null;
  }

  const finance = financeQuery.data;
  const nothing =
    finance.quotedFee === null && finance.agreedFee === null && finance.depositAmount === null;

  return (
    <Card style={styles.card}>
      <Text variant="subheading">Money</Text>
      <Text color="secondary" variant="bodySmall">
        {finance.isCustomerBalanceOutstanding
          ? 'The customer still owes something.'
          : nothing
            ? 'Nothing agreed yet.'
            : 'Settled with the customer.'}
      </Text>

      {editing ? (
        <FinanceForm
          engagementId={engagementId}
          finance={finance}
          onDone={() => setEditing(false)}
          onError={setError}
        />
      ) : (
        <>
          {!nothing ? (
            <View style={styles.rows}>
              {finance.quotedFee !== null ? (
                <Detail label="Quoted" value={money(finance.quotedFee)} />
              ) : null}
              {finance.agreedFee !== null ? (
                <Detail label="Agreed" value={money(finance.agreedFee)} />
              ) : null}
              {finance.depositAmount !== null ? (
                <Detail
                  label="Deposit"
                  value={`${money(finance.depositAmount)}${finance.depositReceivedOn ? ' · received' : ' · not yet received'}`}
                  tone={finance.depositReceivedOn ? 'muted' : 'error'}
                />
              ) : null}
              {finance.balance !== null ? (
                <Detail
                  label="Balance"
                  value={`${money(finance.balance)}${finance.balanceReceivedOn ? ' · received' : finance.balance > 0 ? ' · not yet received' : ''}`}
                  tone={finance.balanceReceivedOn || finance.balance <= 0 ? 'muted' : 'error'}
                />
              ) : null}
            </View>
          ) : null}

          {finance.notes ? (
            <View style={styles.notes}>
              <Text variant="label" color="secondary">
                Notes
              </Text>
              <Text variant="bodySmall">{finance.notes}</Text>
            </View>
          ) : null}

          <Button
            label={nothing ? 'Record the fee' : 'Edit'}
            variant="secondary"
            onPress={() => setEditing(true)}
          />
        </>
      )}

      <PaymentsSection engagementId={engagementId} onError={setError} />

      {error ? (
        <Text color="error" variant="bodySmall">
          {error}
        </Text>
      ) : null}
    </Card>
  );
}

function FinanceForm({
  engagementId,
  finance,
  onDone,
  onError,
}: {
  engagementId: string;
  finance: Finance;
  onDone: () => void;
  onError: (message: string) => void;
}) {
  const [quoted, setQuoted] = useState(amountText(finance.quotedFee));
  const [agreed, setAgreed] = useState(amountText(finance.agreedFee));
  const [deposit, setDeposit] = useState(amountText(finance.depositAmount));
  const [depositReceivedOn, setDepositReceivedOn] = useState(finance.depositReceivedOn);
  const [balanceReceivedOn, setBalanceReceivedOn] = useState(finance.balanceReceivedOn);
  const [notes, setNotes] = useState(finance.notes ?? '');

  const save = useCommercialMutation<void>((accessToken, organisationId) =>
    updateFinance(accessToken, organisationId, engagementId, {
      quotedFee: parseAmount(quoted),
      agreedFee: parseAmount(agreed),
      depositAmount: parseAmount(deposit),
      depositReceivedOn,
      balanceReceivedOn,
      notes: notes.trim() || null,
    }),
  );

  return (
    <View style={styles.form}>
      <TextInput label="Quoted fee" value={quoted} onChangeText={setQuoted} keyboardType="decimal-pad" />
      <TextInput label="Agreed fee" value={agreed} onChangeText={setAgreed} keyboardType="decimal-pad" />
      <TextInput label="Deposit" value={deposit} onChangeText={setDeposit} keyboardType="decimal-pad" />
      <DateField
        label="Deposit received on"
        value={depositReceivedOn}
        onChange={setDepositReceivedOn}
        placeholder="Not yet"
        clearable
      />
      <DateField
        label="Balance received on"
        value={balanceReceivedOn}
        onChange={setBalanceReceivedOn}
        placeholder="Not yet"
        clearable
      />
      <TextInput
        label="Notes"
        placeholder="Payment terms, who to invoice"
        value={notes}
        onChangeText={setNotes}
        multiline
      />
      <Button
        label="Save"
        loading={save.isPending}
        onPress={() => {
          if ([quoted, agreed, deposit].some((text) => text.trim() && parseAmount(text) === null)) {
            onError('Amounts need to be numbers.');
            return;
          }
          save.mutate(undefined, {
            onSuccess: onDone,
            onError: () => onError('Could not save that. Amounts cannot be negative.'),
          });
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onDone} />
    </View>
  );
}

/**
 * What each performer is owed. An unpaid row outlives the event — it keeps
 * appearing in the organiser's attention list until it is marked paid, which
 * is the whole reason it exists (Slice 11 criteria).
 */
function PaymentsSection({
  engagementId,
  onError,
}: {
  engagementId: string;
  onError: (message: string) => void;
}) {
  const paymentsQuery = usePayments(engagementId);
  const lineupQuery = useAvailability(engagementId);
  const [adding, setAdding] = useState(false);

  const settle = useCommercialMutation<{ payment: PerformerPayment; paidOn: string | null }>(
    (accessToken, organisationId, args) =>
      updatePayment(accessToken, organisationId, engagementId, args.payment.id, {
        amount: args.payment.amount,
        notes: args.payment.notes,
        paidOn: args.paidOn,
      }),
  );

  const remove = useCommercialMutation<string>((accessToken, organisationId, paymentId) =>
    deletePayment(accessToken, organisationId, engagementId, paymentId),
  );

  if (!paymentsQuery.isSuccess) {
    return null;
  }

  const payments = paymentsQuery.data;
  const unpaid = payments.filter((payment) => !payment.isPaid);
  const lineup = (lineupQuery.data?.participants ?? []).filter((row) => row.isActive);

  return (
    <View style={styles.section}>
      <Text variant="label" color="secondary">
        Performer payments
      </Text>
      <Text variant="caption" color={unpaid.length > 0 ? 'error' : 'muted'}>
        {payments.length === 0
          ? 'Nobody recorded yet.'
          : unpaid.length === 0
            ? 'Everyone paid.'
            : `${unpaid.length} still to pay · ${money(unpaid.reduce((sum, row) => sum + row.amount, 0))}`}
      </Text>

      {payments.map((payment) => (
        <View key={payment.id} style={styles.paymentRow}>
          <Pressable
            accessibilityRole="checkbox"
            accessibilityState={{ checked: payment.isPaid }}
            accessibilityLabel={`${payment.displayName ?? 'Performer'} paid`}
            disabled={settle.isPending}
            onPress={() =>
              settle.mutate(
                { payment, paidOn: payment.isPaid ? null : today() },
                { onError: () => onError('Could not save that.') },
              )
            }
            style={[styles.tick, payment.isPaid ? styles.tickDone : styles.tickOutstanding]}
          >
            <Text variant="caption" style={styles.tickMark}>
              {payment.isPaid ? '✓' : ''}
            </Text>
          </Pressable>
          <View style={styles.paymentBody}>
            <Text variant="bodySmall" color={payment.isPaid ? 'muted' : 'primary'}>
              {payment.displayName ?? 'Someone'} · {money(payment.amount)}
            </Text>
            <Text variant="caption" color="muted">
              {payment.isPaid ? `Paid ${payment.paidOn}` : 'Not yet paid'}
              {payment.notes ? ` · ${payment.notes}` : ''}
            </Text>
          </View>
          <Text
            variant="caption"
            color="muted"
            onPress={
              remove.isPending
                ? undefined
                : () =>
                    remove.mutate(payment.id, {
                      onError: () => onError('Could not remove that.'),
                    })
            }
            suppressHighlighting
          >
            Remove
          </Text>
        </View>
      ))}

      {adding ? (
        <AddPayment
          engagementId={engagementId}
          lineup={lineup}
          onDone={() => setAdding(false)}
          onError={onError}
        />
      ) : (
        <Button label="Add a payment" variant="ghost" onPress={() => setAdding(true)} />
      )}
    </View>
  );
}

function AddPayment({
  engagementId,
  lineup,
  onDone,
  onError,
}: {
  engagementId: string;
  lineup: Participant[];
  onDone: () => void;
  onError: (message: string) => void;
}) {
  const [chosen, setChosen] = useState<string | null>(null);
  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');

  // The lineup can arrive after the form opens, so the default is derived
  // rather than copied into state once and left stale.
  const userId = chosen ?? lineup[0]?.userId ?? null;

  const create = useCommercialMutation<{ userId: string; amount: number; notes: string | null }>(
    (accessToken, organisationId, args) =>
      createPayment(accessToken, organisationId, engagementId, args),
  );

  return (
    <View style={styles.form}>
      <View>
        <Text variant="label" color="secondary">
          Who
        </Text>
        <View style={styles.chips}>
          {lineup.map((participant) => (
            <Chip
              key={participant.userId}
              label={participant.displayName ?? 'Someone'}
              selected={userId === participant.userId}
              onPress={() => setChosen(participant.userId)}
            />
          ))}
        </View>
        {lineup.length === 0 ? (
          <Text variant="caption" color="muted">
            Nobody is on this event yet. Payments go to people on the lineup.
          </Text>
        ) : null}
      </View>
      <TextInput label="Amount" value={amount} onChangeText={setAmount} keyboardType="decimal-pad" />
      <TextInput label="Notes" placeholder="e.g. Tabla, plus travel" value={notes} onChangeText={setNotes} />
      <Button
        label="Add"
        loading={create.isPending}
        onPress={() => {
          const parsed = parseAmount(amount);
          if (userId === null || parsed === null || parsed <= 0) {
            onError('A payment needs a performer and an amount.');
            return;
          }
          create.mutate(
            { userId, amount: parsed, notes: notes.trim() || null },
            { onSuccess: onDone, onError: () => onError('Could not add that.') },
          );
        }}
      />
      <Button label="Cancel" variant="ghost" onPress={onDone} />
    </View>
  );
}

function Detail({
  label,
  value,
  tone = 'primary',
  onPress,
}: {
  label: string;
  value: string;
  tone?: 'primary' | 'muted' | 'error';
  onPress?: () => void;
}) {
  return (
    <View style={styles.detailRow}>
      <Text variant="bodySmall" color="secondary" style={styles.detailLabel}>
        {label}
      </Text>
      <Text
        variant="bodySmall"
        color={onPress ? 'accent' : tone}
        style={styles.detailValue}
        onPress={onPress}
        suppressHighlighting
      >
        {value}
      </Text>
    </View>
  );
}

function Chip({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected }}
      onPress={onPress}
      style={[styles.chip, selected ? styles.chipSelected : null]}
    >
      <Text variant="caption" color={selected ? 'inverse' : 'secondary'}>
        {label}
      </Text>
    </Pressable>
  );
}

/** Two decimals, thousands grouped, no currency symbol — Sahno does not know the currency. */
function money(amount: number): string {
  return amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function amountText(amount: number | null): string {
  return amount === null ? '' : String(amount);
}

function parseAmount(text: string): number | null {
  const trimmed = text.trim().replace(/,/g, '');
  if (trimmed.length === 0) {
    return null;
  }
  const value = Number(trimmed);
  return Number.isFinite(value) ? value : null;
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

const styles = StyleSheet.create({
  card: {
    gap: spacing.md,
    marginBottom: spacing.lg,
  },
  rows: {
    gap: spacing.xs,
  },
  detailRow: {
    flexDirection: 'row',
    gap: spacing.md,
  },
  detailLabel: {
    width: 72,
  },
  detailValue: {
    flex: 1,
  },
  notes: {
    gap: 2,
  },
  form: {
    gap: spacing.md,
    paddingTop: spacing.sm,
    borderTopWidth: 1,
    borderTopColor: colors.border.default,
  },
  section: {
    gap: spacing.sm,
    paddingTop: spacing.md,
    borderTopWidth: 1,
    borderTopColor: colors.border.default,
  },
  paymentRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  paymentBody: {
    flex: 1,
    gap: 2,
  },
  chips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
    marginTop: spacing.xs,
  },
  chip: {
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    borderRadius: radii.full,
    borderWidth: 1,
    borderColor: colors.border.strong,
    minHeight: 34,
    justifyContent: 'center',
  },
  chipSelected: {
    backgroundColor: colors.interactive.primary,
    borderColor: colors.interactive.primary,
  },
  tick: {
    width: 24,
    height: 24,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
  },
  tickMark: {
    color: colors.text.inverse,
    lineHeight: 14,
  },
  tickDone: {
    backgroundColor: colors.tealText,
    borderColor: colors.tealText,
  },
  tickOutstanding: {
    backgroundColor: 'transparent',
    borderColor: colors.border.strong,
  },
});
