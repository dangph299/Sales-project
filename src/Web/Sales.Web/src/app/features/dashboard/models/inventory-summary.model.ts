export interface InventorySummary {
  totalItems: number;
  totalQuantity: number;
  inStock: number;
  lowStock: number;
  outOfStock: number;
  lowStockThreshold: number;
}

export const emptyInventorySummary: InventorySummary = {
  totalItems: 0,
  totalQuantity: 0,
  inStock: 0,
  lowStock: 0,
  outOfStock: 0,
  lowStockThreshold: 0
};
