import {
  getAuthorizedJson,
  postAuthorizedJson,
  sendAuthorizedJson,
} from '@/api/client';

/**
 * One customer in the organisation's directory (D-022: organisers only).
 * The same family, venue or company across every booking they make, so a
 * returning customer is picked rather than retyped. `bookingCount` is the
 * "they have had us four times" number.
 */
export type Customer = {
  id: string;
  name: string;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  notes: string | null;
  bookingCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};

/** A booking as a line in a customer's history. */
export type CustomerBooking = {
  engagementId: string;
  title: string;
  status: string;
  startDate: string | null;
  venue: string | null;
};

export type CustomerDetail = {
  customer: Customer;
  bookings: CustomerBooking[];
};

export type SaveCustomerInput = {
  name: string;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  notes: string | null;
};

function isCustomer(value: unknown): value is Customer {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'name' in value &&
    'bookingCount' in value
  );
}

function isCustomerList(value: unknown): value is Customer[] {
  return Array.isArray(value) && value.every(isCustomer);
}

function isCustomerDetail(value: unknown): value is CustomerDetail {
  return (
    typeof value === 'object' &&
    value !== null &&
    'customer' in value &&
    'bookings' in value &&
    isCustomer((value as CustomerDetail).customer)
  );
}

function base(organisationId: string): string {
  return `/api/organisations/${organisationId}/customers`;
}

export function listCustomers(
  accessToken: string,
  organisationId: string,
  signal?: AbortSignal,
): Promise<Customer[]> {
  return getAuthorizedJson(base(organisationId), accessToken, isCustomerList, signal);
}

export function getCustomerDetail(
  accessToken: string,
  organisationId: string,
  customerId: string,
  signal?: AbortSignal,
): Promise<CustomerDetail> {
  return getAuthorizedJson(
    `${base(organisationId)}/${customerId}`,
    accessToken,
    isCustomerDetail,
    signal,
  );
}

export function createCustomer(
  accessToken: string,
  organisationId: string,
  input: SaveCustomerInput,
): Promise<Customer> {
  return postAuthorizedJson(base(organisationId), accessToken, input, isCustomer);
}

export function updateCustomer(
  accessToken: string,
  organisationId: string,
  customerId: string,
  input: SaveCustomerInput,
): Promise<void> {
  return sendAuthorizedJson(
    `${base(organisationId)}/${customerId}`,
    accessToken,
    'PUT',
    input,
  );
}
