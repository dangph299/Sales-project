export type ListSortDirection = 'ascend' | 'descend' | null;

export interface ListQuerySort {
  field: string;
  direction: ListSortDirection;
}

export interface ListQueryState<TFilter> {
  pageIndex: number;
  pageSize: number;
  sort?: ListQuerySort;
  filters: TFilter;
}
