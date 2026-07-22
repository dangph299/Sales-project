import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';

export type MetricTone = 'success' | 'warning' | 'danger' | 'info';

@Component({
  selector: 'app-metric-card',
  standalone: true,
  imports: [CommonModule, NzCardModule, NzIconModule],
  templateUrl: './metric-card.component.html',
  styleUrl: './metric-card.component.scss'
})
export class MetricCardComponent {
  @Input({ required: true }) icon = '';
  @Input({ required: true }) title = '';
  @Input({ required: true }) metric: string | number = '';
  @Input() secondary = '';
  @Input() tone: MetricTone = 'info';
}
