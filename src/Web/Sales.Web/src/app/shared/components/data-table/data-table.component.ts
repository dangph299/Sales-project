import { CommonModule } from '@angular/common';
import {
  AfterContentInit,
  Component,
  ContentChildren,
  EventEmitter,
  Input,
  Output,
  QueryList,
  TemplateRef,
  signal
} from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { DateTimePipe } from '../../pipes/date-time.pipe';
import { MoneyPipe } from '../../pipes/money.pipe';
import {
  TableAction,
  TableActionEvent,
  TableColumn,
  TablePageChange,
  TableSort,
  TableSortDirection
} from '../../models/table.model';
import { PageStateComponent } from '../page-state/page-state.component';
import { TableCellTemplateDirective } from './table-cell-template.directive';

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [
    CommonModule,
    DateTimePipe,
    MoneyPipe,
    PageStateComponent,
    TableCellTemplateDirective,
    NzButtonModule,
    NzCardModule,
    NzDropDownModule,
    NzEmptyModule,
    NzMenuModule,
    NzTableModule,
    NzToolTipModule
  ],
  templateUrl: './data-table.component.html',
  styleUrl: './data-table.component.scss'
})
export class DataTableComponent<T extends object> implements AfterContentInit {
  @Input({ required: true }) columns: TableColumn<T>[] = [];
  @Input({ required: true }) rows: T[] = [];
  @Input() actions: TableAction<T>[] = [];
  @Input() rowKey: Extract<keyof T, string> | string | ((row: T) => string) = 'id';
  @Input() selectedRowKey = '';
  @Input() loading = false;
  @Input() errorMessage = '';
  @Input() emptyTitle = 'No data available';
  @Input() emptyText = 'Create the first record to get started.';
  @Input() empty = false;
  @Input() title = '';
  @Input() recordLabel = 'records';
  @Input() showCard = true;
  @Input() cardClass = '';
  @Input() cardExtraText = '';
  @Input() tableClass = '';
  @Input() size: 'small' | 'middle' | 'default' = 'small';
  @Input() scrollX = '';
  @Input() scrollY = '';
  @Input() showTableWhenEmpty = false;
  @Input() showPagination = false;
  @Input() frontPagination = false;
  @Input() total = 0;
  @Input() pageIndex = 1;
  @Input() pageSize = 20;
  @Input() pageSizeOptions: number[] = [10, 20, 50];
  @Input() showSizeChanger = true;
  @Input() sort: TableSort | null = null;
  @Input() clickableRows = false;
  @Input() tableRole: string | null = null;
  @Input() rowClass: (row: T) => string = () => '';
  @Input() rowAriaLevel: (row: T) => number | null = () => null;
  @Input() rowAriaExpanded: (row: T) => boolean | null = () => null;

  @Output() sortChanged = new EventEmitter<TableSort>();
  @Output() pageChanged = new EventEmitter<TablePageChange>();
  @Output() actionClicked = new EventEmitter<TableActionEvent<T>>();
  @Output() rowClicked = new EventEmitter<T>();
  @Output() retry = new EventEmitter<void>();

  @ContentChildren(TableCellTemplateDirective) private readonly projectedCells?: QueryList<TableCellTemplateDirective<T>>;

  private readonly cellTemplates = signal(new Map<string, TemplateRef<unknown>>());

  ngAfterContentInit(): void {
    this.refreshTemplates();
    this.projectedCells?.changes.subscribe(() => this.refreshTemplates());
  }

  changeSort(column: TableColumn<T>): void {
    if (!column.sortable || this.loading) {
      return;
    }

    const currentDirection = this.sort?.key === column.key ? this.sort.direction : null;
    const nextDirection = this.nextDirection(currentDirection);
    this.sortChanged.emit({ key: column.key, direction: nextDirection });
  }

  changePageIndex(nextPageIndex: number): void {
    this.pageChanged.emit({ pageIndex: nextPageIndex, pageSize: this.pageSize });
  }

  changePageSize(nextPageSize: number): void {
    this.pageChanged.emit({ pageIndex: 1, pageSize: nextPageSize });
  }

  sortIndicator(column: TableColumn<T>): string {
    if (!column.sortable || this.sort?.key !== column.key || !this.sort.direction) {
      return '';
    }

    return this.sort.direction === 'ascend' ? '↑' : '↓';
  }

  ariaSort(column: TableColumn<T>): 'ascending' | 'descending' | 'none' {
    if (this.sort?.key !== column.key || !this.sort.direction) {
      return 'none';
    }

    return this.sort.direction === 'ascend' ? 'ascending' : 'descending';
  }

  value(row: T, column: TableColumn<T>): unknown {
    if (column.valueFormatter) {
      return column.valueFormatter(row);
    }

    if (column.valueAccessor) {
      return column.valueAccessor(row);
    }

    return row[column.key as keyof T];
  }

  width(column: TableColumn<T>): string | null {
    return column.width ?? null;
  }

  visibleColumns(): TableColumn<T>[] {
    return this.columns.filter(column => !column.hidden);
  }

  hasRows(): boolean {
    return this.rows.length > 0;
  }

  shouldShowState(): boolean {
    return this.loading || !!this.errorMessage || this.empty || (!this.showTableWhenEmpty && !this.hasRows());
  }

  shouldShowTable(): boolean {
    return !this.loading && !this.errorMessage && !this.empty && (this.hasRows() || this.showTableWhenEmpty);
  }

  totalText(): string {
    return `${this.total} ${this.recordLabel}`;
  }

  cardExtra(): string {
    return this.cardExtraText || this.totalText();
  }

  numberValue(row: T, column: TableColumn<T>): number | null {
    const rawValue = this.value(row, column);
    return typeof rawValue === 'number' ? rawValue : null;
  }

  textValue(row: T, column: TableColumn<T>): string {
    const rawValue = this.value(row, column);
    return rawValue === null || rawValue === undefined ? '' : String(rawValue);
  }

  cellClass(row: T, column: TableColumn<T>): string {
    const classes = [
      column.align === 'right' ? 'number-column' : '',
      column.align === 'center' ? 'center-column' : '',
      column.wrap === true ? 'wrap-cell' : '',
      column.wrap === false ? 'nowrap-cell' : '',
      typeof column.cssClass === 'function' ? column.cssClass(row) : column.cssClass ?? ''
    ];

    return classes.filter(Boolean).join(' ');
  }

  headerClass(column: TableColumn<T>): string {
    const headerAlign = column.headerAlign ?? column.align;
    const classes = [
      headerAlign === 'right' ? 'number-column' : '',
      headerAlign === 'center' ? 'center-column' : '',
      column.headerCssClass ?? ''
    ];

    return classes.filter(Boolean).join(' ');
  }

  templateFor(column: TableColumn<T>): TemplateRef<unknown> | null {
    return this.cellTemplates().get(column.key) ?? null;
  }

  rowIdentity(row: T): string {
    if (typeof this.rowKey === 'function') {
      return this.rowKey(row);
    }

    const value = row[this.rowKey as keyof T];
    return value === null || value === undefined ? '' : String(value);
  }

  visibleActions(row: T): TableAction<T>[] {
    return this.actions.filter(action => !action.hidden?.(row));
  }

  emitAction(action: TableAction<T>, row: T, event: Event): void {
    event.stopPropagation();
    if (action.disabled?.(row)) {
      return;
    }

    this.actionClicked.emit({ actionKey: action.key, row });
  }

  emitRow(row: T): void {
    if (this.clickableRows) {
      this.rowClicked.emit(row);
    }
  }

  private refreshTemplates(): void {
    const templates = new Map<string, TemplateRef<unknown>>();
    for (const cell of this.projectedCells ?? []) {
      templates.set(cell.key, cell.template);
    }

    this.cellTemplates.set(templates);
  }

  private nextDirection(direction: TableSortDirection): TableSortDirection {
    if (direction === null) {
      return 'ascend';
    }

    return direction === 'ascend' ? 'descend' : null;
  }
}
