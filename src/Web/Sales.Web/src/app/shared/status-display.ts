export type StatusTone = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

export interface StatusDisplay {
  label: string;
  tone: StatusTone;
}

const statusDisplays: Record<string, StatusDisplay> = {
  Normal: { label: 'Normal', tone: 'success' },
  Suspended: { label: 'Suspended', tone: 'warning' },
  Blocked: { label: 'Blocked', tone: 'danger' },
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' },
  Archived: { label: 'Archived', tone: 'neutral' },
  PendingInventory: { label: 'Pending inventory', tone: 'info' },
  Confirmed: { label: 'Confirmed', tone: 'success' },
  Cancelled: { label: 'Cancelled', tone: 'danger' },
  InventoryRejected: { label: 'Inventory rejected', tone: 'danger' }
};

export function getStatusDisplay(status: string | null | undefined): StatusDisplay {
  if (!status) {
    return { label: 'Unknown', tone: 'neutral' };
  }

  return statusDisplays[status] ?? { label: status, tone: 'neutral' };
}
