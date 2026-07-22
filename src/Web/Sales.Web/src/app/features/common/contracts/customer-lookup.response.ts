/** The narrow customer projection features other than Customers may consume. */
export interface CustomerLookupResponse {
  id: string;
  customerCode?: string | null;
  name: string;
  phone: string;
  status?: string | null;
}

/**
 * A single phone-autocomplete suggestion.
 *
 * Deliberately separate from `CustomerLookupResponse`: this is what
 * `/api/customers/lookup` returns per keystroke, carrying only the fields the
 * dropdown shows and the order form fills in.
 */
export interface CustomerPhoneSuggestionResponse {
  customerId: string;
  phone: string;
  name: string;
  email?: string | null;
  address?: string | null;
}
