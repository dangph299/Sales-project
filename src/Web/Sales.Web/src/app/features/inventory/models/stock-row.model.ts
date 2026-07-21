import {
  ProductLookupResponse,
  ProductVariantLookupResponse
} from '../../common/contracts/product-lookup.response';
import { InventoryResponse } from '../api/responses/inventory.response';

/** One table row: a sellable variant paired with its stock levels. */
export interface StockRow {
  product: ProductLookupResponse;
  variant: ProductVariantLookupResponse;
  inventory: InventoryResponse | null;
}

export function totalQuantity(row: StockRow): number {
  return (row.inventory?.available ?? 0) + (row.inventory?.reserved ?? 0);
}

/** Stock may only be received for Published products with Published variants. */
export function canAdjustStock(row: StockRow): boolean {
  return row.product.status === 'Published' && row.variant.status === 'Published';
}
