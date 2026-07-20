export { uncategorizedCategoryId } from './category-reference-data';
export { seededColors } from './color-reference-data';
export { seededSizes } from './size-reference-data';

import { seededColors } from './color-reference-data';
import { seededSizes } from './size-reference-data';

export function buildSkuPreview(productCode: string, colorId: string, sizeId: string): string {
  const color = seededColors.find(colorOption => colorOption.id === colorId);
  const size = seededSizes.find(sizeOption => sizeOption.id === sizeId);
  const normalizedProductCode = productCode.trim().toUpperCase();
  if (!normalizedProductCode || !color || !size) {
    return '-';
  }

  return `${normalizedProductCode}-${color.code}-${size.code}`;
}
