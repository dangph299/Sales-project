export type StockState = 'available' | 'low' | 'out';

export const lowStockThreshold = 5;

export function stockStateOf(available: number | null | undefined, threshold = lowStockThreshold): StockState {
  const quantity = available ?? 0;
  if (quantity <= 0) {
    return 'out';
  }

  return quantity <= threshold ? 'low' : 'available';
}

export const stockStateLabels: Readonly<Record<StockState, string>> = {
  available: 'Available',
  low: 'Low stock',
  out: 'Out of stock'
};
