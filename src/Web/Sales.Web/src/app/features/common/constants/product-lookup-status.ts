import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/**
 * Display mapping for product and variant statuses as seen through the lookup
 * boundary. Published here rather than imported from the Products feature so
 * that consumers of the lookup do not reach into another feature's internals.
 */
const productLookupStatusDisplays: Readonly<Record<string, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' }
};

export function productLookupStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status, productLookupStatusDisplays);
}
