/**
 * The customer details sent with an order.
 *
 * The same shape serves create and edit, but they mean different things: on
 * create the backend resolves the phone number to a customer or creates one, on
 * edit it only rewrites the order's own snapshot.
 */
export interface OrderCustomerRequest {
  name: string;
  phone: string;
  email?: string | null;
  address?: string | null;
}
