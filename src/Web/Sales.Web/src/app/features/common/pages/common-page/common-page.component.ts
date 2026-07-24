import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { CommonStore } from '../../services/common-store.service';
import { colorListColumns, sizeListColumns } from './common-page.columns';

@Component({
  selector: 'app-common-page',
  standalone: true,
  imports: [CommonModule, DataTableComponent, TableCellTemplateDirective, NzCardModule, PageStateComponent],
  templateUrl: './common-page.component.html',
  styleUrl: './common-page.component.scss'
})
export class CommonPageComponent implements OnInit {
  private readonly common = inject(CommonStore);

  readonly colors = this.common.colors;
  readonly sizes = this.common.sizes;
  readonly loading = this.common.loading;
  readonly errorMessage = this.common.loadError;
  readonly colorColumns = colorListColumns;
  readonly sizeColumns = sizeListColumns;

  ngOnInit(): void {
    void this.common.ensureLoaded();
  }

  reload(): void {
    void this.common.reload();
  }
}
