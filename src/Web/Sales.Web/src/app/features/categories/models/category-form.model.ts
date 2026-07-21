import { CategoryStatus } from '../constants/category-status';

export interface CategoryFormModel {
  categoryCode: string;
  name: string;
  description: string;
  parentCategoryId: string;
  sortOrder: number;
  status: CategoryStatus;
}

export function emptyCategoryForm(): CategoryFormModel {
  return {
    categoryCode: `CAT${Math.floor(100 + Math.random() * 899)}`,
    name: 'New Category',
    description: '',
    parentCategoryId: '',
    sortOrder: 10,
    status: 'Draft'
  };
}
