import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-page-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section *ngIf="loading" class="state-panel">
      <div class="spinner" aria-hidden="true"></div>
      <p>{{ loadingText }}</p>
    </section>

    <section *ngIf="!loading && errorMessage" class="state-panel error">
      <strong>{{ errorMessage }}</strong>
      <p *ngIf="diagnostics">{{ diagnostics }}</p>
      <button type="button" class="secondary" (click)="retry.emit()">Thu lai</button>
    </section>

    <section *ngIf="!loading && !errorMessage && empty" class="state-panel">
      <strong>{{ emptyTitle }}</strong>
      <p>{{ emptyText }}</p>
    </section>
  `
})
export class PageStateComponent {
  @Input() loading = false;
  @Input() empty = false;
  @Input() loadingText = 'Dang tai du lieu...';
  @Input() emptyTitle = 'Chua co du lieu';
  @Input() emptyText = 'Hay tao ban ghi dau tien de bat dau.';
  @Input() errorMessage = '';
  @Input() diagnostics = '';
  @Output() retry = new EventEmitter<void>();
}
