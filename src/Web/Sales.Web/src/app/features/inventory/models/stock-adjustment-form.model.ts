export type StockAdjustmentOperation = 'receive' | 'adjust';

export interface StockAdjustmentFormModel {
  operation: StockAdjustmentOperation;
  quantityDelta: number;
  reason: string;
}

export function emptyStockAdjustmentForm(): StockAdjustmentFormModel {
  return {
    operation: 'receive',
    quantityDelta: 10,
    reason: ''
  };
}
