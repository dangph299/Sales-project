import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

const orderStatusDisplays: Readonly<Record<string, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  PendingInventory: { label: 'Pending inventory', tone: 'info' },
  Confirmed: { label: 'Confirmed', tone: 'success' },
  Cancelled: { label: 'Cancelled', tone: 'danger' },
  InventoryRejected: { label: 'Inventory rejected', tone: 'danger' }
};

export function dashboardOrderStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status, orderStatusDisplays);
}
