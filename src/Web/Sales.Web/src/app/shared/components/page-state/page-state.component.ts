import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSpinModule } from 'ng-zorro-antd/spin';

@Component({
  selector: 'app-page-state',
  standalone: true,
  imports: [CommonModule, NzAlertModule, NzButtonModule, NzEmptyModule, NzSpinModule],
  template: `
    <nz-spin *ngIf="loading" [nzTip]="loadingText"></nz-spin>

    <section *ngIf="!loading && errorMessage" class="state-panel">
      <nz-alert nzType="error" [nzMessage]="errorMessage" nzShowIcon></nz-alert>
      <p *ngIf="diagnostics">{{ diagnostics }}</p>
      <button nz-button type="button" (click)="retry.emit()">Retry</button>
    </section>

    <section *ngIf="!loading && !errorMessage && empty" class="state-panel">
      <nz-empty [nzNotFoundContent]="emptyTitle">
        <span>{{ emptyText }}</span>
      </nz-empty>
    </section>
  `
})
export class PageStateComponent {
  @Input() loading = false;
  @Input() empty = false;
  @Input() loadingText = 'Loading data...';
  @Input() emptyTitle = 'No data available';
  @Input() emptyText = 'Create the first record to get started.';
  @Input() errorMessage = '';
  @Input() diagnostics = '';
  @Output() retry = new EventEmitter<void>();
}
