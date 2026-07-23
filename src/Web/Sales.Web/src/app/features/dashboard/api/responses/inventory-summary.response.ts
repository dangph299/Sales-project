export interface InventorySummaryResponse {
  totalItems: number;
  totalQuantity: number;
  inStock: number;
  lowStock: number;
  outOfStock: number;
  lowStockThreshold: number;
}
