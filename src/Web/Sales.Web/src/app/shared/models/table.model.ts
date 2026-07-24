import { TemplateRef } from '@angular/core';

export type TableSortDirection = 'ascend' | 'descend' | null;
export type TableColumnAlign = 'left' | 'center' | 'right';
export type TableColumnType = 'text' | 'number' | 'currency' | 'dateTime' | 'boolean' | 'custom' | 'actions';

export interface TableSort {
  key: string;
  direction: TableSortDirection;
}

export interface TablePageChange {
  pageIndex: number;
  pageSize: number;
}

export interface TableQuery {
  pageIndex: number;
  pageSize: number;
  sort?: TableSort;
  filters?: Record<string, unknown>;
}

export interface TableColumn<T> {
  key: Extract<keyof T, string> | string;
  header: string;
  type?: TableColumnType;
  width?: string;
  minWidth?: string;
  maxWidth?: string;
  sortable?: boolean;
  filterable?: boolean;
  hidden?: boolean;
  align?: TableColumnAlign;
  headerAlign?: TableColumnAlign;
  wrap?: boolean;
  valueAccessor?: (row: T) => unknown;
  valueFormatter?: (row: T) => string;
  cssClass?: string | ((row: T) => string);
  headerCssClass?: string;
  tooltip?: (row: T) => string;
  comparator?: (left: T, right: T) => number;
}

export interface TableAction<T> {
  key: string;
  label: string;
  danger?: boolean;
  disabled?: (row: T) => boolean;
  hidden?: (row: T) => boolean;
}

export interface TableActionEvent<T> {
  actionKey: string;
  row: T;
}

export interface TableCellContext<T> {
  $implicit: T;
  row: T;
  column: TableColumn<T>;
  value: unknown;
}

export interface TableHeaderContext<T> {
  $implicit: TableColumn<T>;
  column: TableColumn<T>;
}

export interface TableTemplate<T> {
  key: string;
  template: TemplateRef<TableCellContext<T>>;
}
