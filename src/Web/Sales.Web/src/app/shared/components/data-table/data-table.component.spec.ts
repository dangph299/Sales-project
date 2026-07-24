import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { DataTableComponent } from './data-table.component';
import { TableCellTemplateDirective } from './table-cell-template.directive';
import { TableActionEvent, TableColumn, TablePageChange, TableSort } from '../../models/table.model';

interface TestRow {
  id: string;
  name: string;
  amount: number;
}

@Component({
  standalone: true,
  imports: [DataTableComponent, TableCellTemplateDirective],
  template: `
    <app-data-table
      title="Rows"
      [columns]="columns"
      [rows]="rows"
      [actions]="actions"
      [loading]="loading"
      [empty]="empty"
      [total]="total"
      [showPagination]="true"
      [sort]="sort"
      (sortChanged)="lastSort = $event"
      (pageChanged)="lastPage = $event"
      (actionClicked)="lastAction = $event">
      <ng-template appTableCell="name" let-row>
        <strong>{{ row.name }}</strong>
      </ng-template>
    </app-data-table>
  `
})
class HostComponent {
  columns: TableColumn<TestRow>[] = [
    { key: 'name', header: 'Name', sortable: true, type: 'custom' },
    { key: 'amount', header: 'Amount', type: 'currency', align: 'right' }
  ];
  rows: TestRow[] = [{ id: 'row-1', name: 'Alpha', amount: 1200 }];
  actions = [{ key: 'edit', label: 'Edit' }];
  loading = false;
  empty = false;
  total = 1;
  sort: TableSort | null = null;
  lastSort: TableSort | null = null;
  lastPage: TablePageChange | null = null;
  lastAction: TableActionEvent<TestRow> | null = null;
}

describe('DataTableComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent, NoopAnimationsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('renders dynamic columns, rows, and custom cells', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Name');
    expect(text).toContain('Amount');
    expect(text).toContain('Alpha');
    expect(fixture.debugElement.query(By.css('strong'))?.nativeElement.textContent).toContain('Alpha');
  });

  it('emits sort events', () => {
    fixture.debugElement.query(By.css('.sort-header')).nativeElement.click();

    expect(fixture.componentInstance.lastSort).toEqual({ key: 'name', direction: 'ascend' });
  });

  it('emits action events', () => {
    const buttons = fixture.debugElement.queryAll(By.css('button'));
    buttons.find(button => button.nativeElement.textContent.includes('Edit'))?.nativeElement.click();

    expect(fixture.componentInstance.lastAction?.actionKey).toBe('edit');
    expect(fixture.componentInstance.lastAction?.row.id).toBe('row-1');
  });

  it('shows loading without empty state', () => {
    fixture.componentInstance.loading = true;
    fixture.componentInstance.rows = [];
    fixture.componentInstance.empty = true;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Loading data');
    expect(text).not.toContain('No data available');
  });
});
