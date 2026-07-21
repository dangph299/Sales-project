import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/** String union, not a TS enum: values are backend codes and must match exactly. */
export type OrderStatus = 'Draft' | 'PendingInventory' | 'Confirmed' | 'Cancelled' | 'InventoryRejected';

export const orderStatusDisplays: Readonly<Record<OrderStatus, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  PendingInventory: { label: 'Pending inventory', tone: 'info' },
  Confirmed: { label: 'Confirmed', tone: 'success' },
  Cancelled: { label: 'Cancelled', tone: 'danger' },
  InventoryRejected: { label: 'Inventory rejected', tone: 'danger' }
};

export function orderStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status, orderStatusDisplays);
}

/** User-facing explanation of a realtime status transition. */
export function describeOrderStatusChange(status: string): string {
  switch (status) {
    case 'Confirmed':
      return 'The order was confirmed and inventory was reserved.';
    case 'InventoryRejected':
      return 'The order could not be confirmed because Inventory rejected the reservation.';
    case 'Cancelled':
      return 'The order was cancelled.';
    default:
      return 'The order has new data.';
  }
}
