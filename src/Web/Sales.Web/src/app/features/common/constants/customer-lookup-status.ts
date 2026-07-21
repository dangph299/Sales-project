import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/**
 * Display mapping for customer statuses as seen through the lookup boundary.
 *
 * Published here rather than imported from the Customers feature so that
 * consumers of the lookup do not reach into another feature's internals.
 */
const customerLookupStatusDisplays: Readonly<Record<string, StatusDisplay>> = {
  Normal: { label: 'Normal', tone: 'success' },
  Suspended: { label: 'Suspended', tone: 'warning' },
  Blocked: { label: 'Blocked', tone: 'danger' }
};

export function customerLookupStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status ?? 'Normal', customerLookupStatusDisplays);
}
