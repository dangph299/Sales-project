/** Stock levels for one product variant, keyed by variant id and SKU. */
export interface InventoryResponse {
  productId: string;
  sku: string;
  available: number;
  reserved: number;
  version: number;
}

export interface InventoryBatchResponse {
  items: InventoryResponse[];
}
