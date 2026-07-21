import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/** String union, not a TS enum: values are backend codes and must match exactly. */
export type ProductVariantStatus = 'Draft' | 'Published' | 'Discontinued';

export const productVariantStatusDisplays: Readonly<Record<ProductVariantStatus, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' }
};

export function productVariantStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status, productVariantStatusDisplays);
}
