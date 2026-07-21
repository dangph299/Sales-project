/** Create and update share the same payload shape on the Sales API. */
export interface SaveCustomerRequest {
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
}
