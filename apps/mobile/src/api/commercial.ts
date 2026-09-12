import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorized,
  sendAuthorizedJson,
} from '@/api/client';

/**
 * Who the booking is for (D-022). Organisers only. Never on the engagement
 * itself, so a member's screen has no field to render by mistake.
 */
export type Customer = {
  name: string | null;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  privateNotes: string | null;
  updatedAtUtc: string;
};

export type CustomerInput = Omit<Customer, 'updatedAtUtc'>;

/**
 * The money on one booking (D-008, D-016). Financial access only — the Owner,
 * and Admins the Owner has granted it. `balance` is agreed fee less deposit,
 * derived on the server so it cannot disagree with them.
 */
export type Finance = {
  quotedFee: number | null;
  agreedFee: number | null;
  depositAmount: number | null;
  depositReceivedOn: string | null;
  balance: number | null;
  balanceReceivedOn: string | null;
  isCustomerBalanceOutstanding: boolean;
  notes: string | null;
  updatedAtUtc: string;
};

export type FinanceInput = {
  quotedFee: number | null;
  agreedFee: number | null;
  depositAmount: number | null;
  depositReceivedOn: string | null;
  balanceReceivedOn: string | null;
  notes: string | null;
};

/** What one performer is owed, and whether it has been settled. */
export type PerformerPayment = {
  id: string;
  userId: string;
  displayName: string | null;
  amount: number;
  notes: string | null;
  paidOn: string | null;
  isPaid: boolean;
  createdAtUtc: string;
};

function isCustomer(value: unknown): value is Customer {
  return typeof value === 'object' && value !== null && 'updatedAtUtc' in value && 'name' in value;
}

function isFinance(value: unknown): value is Finance {
  return (
    typeof value === 'object' &&
    value !== null &&
    'isCustomerBalanceOutstanding' in value
  );
}

function isPayment(value: unknown): value is PerformerPayment {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'amount' in value &&
    'isPaid' in value
  );
}

function isPaymentList(value: unknown): value is PerformerPayment[] {
  return Array.isArray(value) && value.every(isPayment);
}

function base(organisationId: string, engagementId: string): string {
  return `/api/organisations/${organisationId}/engagements/${engagementId}`;
}

export function getCustomer(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<Customer> {
  return getAuthorizedJson(
    `${base(organisationId, engagementId)}/customer`,
    accessToken,
    isCustomer,
    signal,
  );
}

export function updateCustomer(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: CustomerInput,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/customer`,
    accessToken,
    'PUT',
    input,
  );
}

export function getFinance(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<Finance> {
  return getAuthorizedJson(
    `${base(organisationId, engagementId)}/finance`,
    accessToken,
    isFinance,
    signal,
  );
}

export function updateFinance(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: FinanceInput,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/finance`,
    accessToken,
    'PUT',
    input,
  );
}

export function listPayments(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  signal?: AbortSignal,
): Promise<PerformerPayment[]> {
  return getAuthorizedJson(
    `${base(organisationId, engagementId)}/payments`,
    accessToken,
    isPaymentList,
    signal,
  );
}

export function createPayment(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  input: { userId: string; amount: number; notes?: string | null },
): Promise<PerformerPayment> {
  return postAuthorizedJson(
    `${base(organisationId, engagementId)}/payments`,
    accessToken,
    { userId: input.userId, amount: input.amount, notes: input.notes ?? null },
    isPayment,
  );
}

export function updatePayment(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  paymentId: string,
  input: { amount: number; notes: string | null; paidOn: string | null },
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId, engagementId)}/payments/${paymentId}`,
    accessToken,
    'PUT',
    input,
  );
}

export function deletePayment(
  accessToken: string,
  organisationId: string,
  engagementId: string,
  paymentId: string,
): Promise<void> {
  return sendAuthorized(
    `${base(organisationId, engagementId)}/payments/${paymentId}`,
    accessToken,
    'DELETE',
  );
}
