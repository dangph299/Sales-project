/** Which end of an order's customer phone number the search term must match. */
export type OrderCustomerPhoneMatchMode = 'Prefix' | 'Suffix';

export const orderCustomerPhoneMatchModes: { value: OrderCustomerPhoneMatchMode; label: string }[] = [
  { value: 'Prefix', label: 'Starts with' },
  { value: 'Suffix', label: 'Ends with' }
];
