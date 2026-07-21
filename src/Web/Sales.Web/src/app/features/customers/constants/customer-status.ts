import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/** String union, not a TS enum: values are backend codes and must match exactly. */
export type CustomerStatus = 'Normal' | 'Suspended' | 'Blocked';

export const customerStatusDisplays: Readonly<Record<CustomerStatus, StatusDisplay>> = {
  Normal: { label: 'Normal', tone: 'success' },
  Suspended: { label: 'Suspended', tone: 'warning' },
  Blocked: { label: 'Blocked', tone: 'danger' }
};

export function customerStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status ?? 'Normal', customerStatusDisplays);
}
