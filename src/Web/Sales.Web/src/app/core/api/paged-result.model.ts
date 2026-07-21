/**
 * The single paging contract used by the backend.
 *
 * Mirrors BuildingBlocks.Application.PagedResult<T>
 * (Items, Page, PageSize, Total) serialized as camelCase.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}
