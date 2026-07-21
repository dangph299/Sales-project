import { Component, Input } from '@angular/core';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { StatusDisplay, StatusTone } from '../../models/status-display.model';

const toneColors: Record<StatusTone, string> = {
  success: 'success',
  warning: 'warning',
  danger: 'error',
  info: 'processing',
  neutral: 'default'
};

/** Presentation-only. Owns no status vocabulary; features supply the display. */
@Component({
  selector: 'app-status-tag',
  standalone: true,
  imports: [NzTagModule],
  template: `<nz-tag [nzColor]="color">{{ display.label }}</nz-tag>`
})
export class StatusTagComponent {
  @Input({ required: true }) display!: StatusDisplay;

  get color(): string {
    return toneColors[this.display.tone];
  }
}
