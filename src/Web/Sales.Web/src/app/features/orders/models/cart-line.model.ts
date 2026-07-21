import { ProductLookupResponse, ProductVariantLookupResponse } from '../../common/contracts/product-lookup.response';

/** A pending order line held in the UI before the order is created. */
export interface CartLine {
  product: ProductLookupResponse;
  variant: ProductVariantLookupResponse;
  quantity: number;
  discountPercent: number;
}

export function normalizeQuantity(quantity: number): number {
  return Math.max(1, quantity || 1);
}

export function normalizeDiscountPercent(discountPercent: number): number {
  return Math.min(100, Math.max(0, discountPercent || 0));
}

export function cartLineTotal(line: CartLine): number {
  return Math.round(line.variant.price * line.quantity * (1 - line.discountPercent / 100));
}

export function cartSubtotal(lines: readonly CartLine[]): number {
  return lines.reduce((total, line) => total + line.variant.price * line.quantity, 0);
}

export function cartGrandTotal(lines: readonly CartLine[]): number {
  return lines.reduce((total, line) => total + cartLineTotal(line), 0);
}
