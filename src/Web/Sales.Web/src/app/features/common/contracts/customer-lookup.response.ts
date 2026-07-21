/** The narrow customer projection features other than Customers may consume. */
export interface CustomerLookupResponse {
  id: string;
  customerCode?: string | null;
  name: string;
  phone: string;
  status?: string | null;
}
