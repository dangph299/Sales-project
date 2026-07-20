import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { getStatusDisplay } from './status-display';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="status-badge" [class]="toneClass" [attr.aria-label]="display.label">
      {{ display.label }}
    </span>
  `
})
export class StatusBadgeComponent {
  @Input() status: string | null | undefined;

  get display(): { label: string; tone: string } {
    return getStatusDisplay(this.status);
  }

  get toneClass(): string {
    return `status-badge ${this.display.tone}`;
  }
}
