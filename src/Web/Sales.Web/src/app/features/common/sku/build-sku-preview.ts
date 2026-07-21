import { ColorResponse } from '../contracts/color.response';
import { SizeResponse } from '../contracts/size.response';

/**
 * Mirrors the backend SKU convention: PRODUCTCODE-COLOR-SIZE.
 *
 * Takes the loaded lookups rather than reaching for them, so the preview stays a pure function of
 * what the caller has actually resolved from the backend.
 */
export function buildSkuPreview(
  productCode: string,
  colorId: string,
  sizeId: string,
  colors: readonly ColorResponse[],
  sizes: readonly SizeResponse[]): string {
  const color = colors.find(candidate => candidate.id === colorId);
  const size = sizes.find(candidate => candidate.id === sizeId);
  const normalizedProductCode = productCode.trim().toUpperCase();

  if (!normalizedProductCode || !color || !size) {
    return '-';
  }

  return `${normalizedProductCode}-${color.code}-${size.code}`;
}
