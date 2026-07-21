export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
}
