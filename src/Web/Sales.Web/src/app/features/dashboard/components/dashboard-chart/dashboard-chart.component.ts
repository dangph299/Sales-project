import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { NzEmptyModule } from 'ng-zorro-antd/empty';

export interface DashboardChartPoint {
  label: string;
  value: number;
  tone?: 'success' | 'warning' | 'danger' | 'info' | 'neutral';
}

@Component({
  selector: 'app-dashboard-chart',
  standalone: true,
  imports: [CommonModule, NzEmptyModule],
  templateUrl: './dashboard-chart.component.html',
  styleUrl: './dashboard-chart.component.scss'
})
export class DashboardChartComponent {
  @Input({ required: true }) points: DashboardChartPoint[] = [];
  @Input() chartType: 'bars' | 'columns' = 'columns';
  @Input() emptyTitle = 'No chart data';
  @Input() emptyText = 'Data will appear after records are created.';

  get hasData(): boolean {
    return this.points.some(point => point.value > 0);
  }

  barSize(point: DashboardChartPoint): number {
    const max = Math.max(...this.points.map(item => item.value), 1);
    return Math.max(6, Math.round((point.value / max) * 100));
  }
}
