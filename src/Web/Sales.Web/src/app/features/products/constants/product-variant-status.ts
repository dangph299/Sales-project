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

/** Statuses a variant may move to next, including staying put. */
export function allowedProductVariantStatusTransitions(status: ProductVariantStatus): ProductVariantStatus[] {
  switch (status) {
    case 'Draft':
      return ['Draft', 'Published'];
    case 'Published':
      return ['Published', 'Discontinued'];
    case 'Discontinued':
      return ['Discontinued', 'Published'];
    default:
      return ['Draft', 'Published'];
  }
}

export function coerceProductVariantStatus(
  status: ProductVariantStatus,
  options: readonly ProductVariantStatus[]
): ProductVariantStatus {
  return options.includes(status) ? status : options[0];
}
