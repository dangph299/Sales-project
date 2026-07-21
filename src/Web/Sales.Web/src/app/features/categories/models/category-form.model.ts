import { CategoryStatus } from '../constants/category-status';

export interface CategoryFormModel {
  categoryCode: string;
  name: string;
  description: string;
  parentCategoryId: string;
  sortOrder: number;
  status: CategoryStatus;
}

/**
 * Creates a blank category form.
 *
 * `name` starts empty on purpose: category names are unique per parent (and globally unique at the
 * root), so defaulting it to a fixed string made every create after the first fail with a 409.
 * `saveCategory` already refuses to submit an empty name.
 */
export function emptyCategoryForm(): CategoryFormModel {
  return {
    categoryCode: `CAT${Math.floor(100 + Math.random() * 899)}`,
    name: '',
    description: '',
    parentCategoryId: '',
    sortOrder: 10,
    status: 'Draft'
  };
}
