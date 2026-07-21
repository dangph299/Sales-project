export interface CreateCategoryRequest {
  categoryCode: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
}
