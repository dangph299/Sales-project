export interface CustomerFormModel {
  name: string;
  phone: string;
  email: string;
  address: string;
}

/**
 * Creates a blank customer form. Name and phone start empty rather than carrying placeholder
 * values: phone is unique per customer, so a generated default risks colliding with a real record.
 * `saveCustomer` already refuses to submit either field empty.
 */
export function emptyCustomerForm(): CustomerFormModel {
  return {
    name: '',
    phone: '',
    email: '',
    address: ''
  };
}
