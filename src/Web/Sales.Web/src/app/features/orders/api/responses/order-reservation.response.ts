/**
 * The Inventory reservation for a given order, read by the Orders detail panel.
 *
 * Keyed by order id and owned here rather than by the Inventory feature, so that
 * Orders does not depend on Inventory's internals.
 */
export interface OrderReservationLineResponse {
  productId?: string | null;
  productVariantId?: string | null;
  sku?: string | null;
  quantity?: number | null;
}

export interface OrderReservationResponse {
  orderId?: string | null;
  status?: string | null;
  lines?: OrderReservationLineResponse[] | null;
  expiresAt?: string | null;
  createdAt?: string | null;
}
