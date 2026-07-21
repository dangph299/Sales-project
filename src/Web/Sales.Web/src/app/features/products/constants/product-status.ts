import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/** String union, not a TS enum: values are backend codes and must match exactly. */
export type ProductStatus = 'Draft' | 'Published' | 'Discontinued';

export const productStatusDisplays: Readonly<Record<ProductStatus, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' }
};

export function productStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status, productStatusDisplays);
}
