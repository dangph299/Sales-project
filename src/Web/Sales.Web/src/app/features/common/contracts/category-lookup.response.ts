/**
 * Category as published by the lookup endpoint. `categoryCode` is the stable business identifier
 * that UI logic matches on; `id` is the persistence identifier that write requests must submit.
 */
export interface CategoryLookupResponse {
  id: string;
  categoryCode: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
  status: string;
}
