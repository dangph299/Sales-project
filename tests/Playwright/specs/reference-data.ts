import { uncategorizedCategoryId } from '../../../src/Web/Sales.Web/src/app/reference-data/category-reference-data';
import { seededColors } from '../../../src/Web/Sales.Web/src/app/reference-data/color-reference-data';
import { seededSizes } from '../../../src/Web/Sales.Web/src/app/reference-data/size-reference-data';

const blackColor = seededColors.find(color => color.code === 'BLK');
const mediumSize = seededSizes.find(size => size.code === 'M');

if (!blackColor || !mediumSize) {
  throw new Error('Expected seeded color and size were not found.');
}

const blackColorId = blackColor.id;
const mediumSizeId = mediumSize.id;

export { blackColorId, mediumSizeId, uncategorizedCategoryId };
