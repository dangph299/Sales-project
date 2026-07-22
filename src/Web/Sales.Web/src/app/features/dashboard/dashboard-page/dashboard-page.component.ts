import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { NzTableModule } from 'ng-zorro-antd/table';
import { DashboardChartComponent, DashboardChartPoint } from '../components/dashboard-chart/dashboard-chart.component';
import { DashboardSectionComponent } from '../components/dashboard-section/dashboard-section.component';
import { MetricCardComponent } from '../components/metric-card/metric-card.component';
import { PageStateComponent } from '../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../shared/components/status-tag/status-tag.component';
import { DateTimePipe } from '../../../shared/pipes/date-time.pipe';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { PriceRangePipe } from '../../../shared/pipes/price-range.pipe';
import { describeApiError } from '../../../shared/utilities/describe-api-error';
import { DashboardApiService } from '../api/dashboard-api.service';
import { DashboardMetrics, emptyDashboardMetrics } from '../models/dashboard-metrics.model';
import { RecentOrderRow, RecentProductRow } from '../models/dashboard-row.model';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    PageStateComponent,
    StatusTagComponent,
    DashboardChartComponent,
    DashboardSectionComponent,
    MetricCardComponent,
    DateTimePipe,
    MoneyPipe,
    PriceRangePipe,
    NzButtonModule,
    NzCardModule,
    NzIconModule,
    NzSkeletonModule,
    NzTableModule
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardApi = inject(DashboardApiService);

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly metrics = signal<DashboardMetrics>(emptyDashboardMetrics);
  readonly recentOrders = signal<RecentOrderRow[]>([]);
  readonly recentProducts = signal<RecentProductRow[]>([]);
  readonly chartOrders = signal<RecentOrderRow[]>([]);
  readonly lastUpdatedAt = signal<Date | null>(null);

  readonly lastUpdatedLabel = computed(() => {
    const lastUpdatedAt = this.lastUpdatedAt();
    if (!lastUpdatedAt) {
      return 'Not updated yet';
    }

    const seconds = Math.max(0, Math.floor((Date.now() - lastUpdatedAt.getTime()) / 1000));
    if (seconds < 60) {
      return 'Just now';
    }

    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) {
      return `${minutes} minute${minutes === 1 ? '' : 's'} ago`;
    }

    const hours = Math.floor(minutes / 60);
    return `${hours} hour${hours === 1 ? '' : 's'} ago`;
  });

  readonly revenueTrend = computed<DashboardChartPoint[]>(() => this.toRevenueTrend(this.chartOrders()));
  readonly ordersByStatus = computed<DashboardChartPoint[]>(() => this.toOrdersByStatus(this.chartOrders()));

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const snapshot = await this.dashboardApi.loadSnapshot();
      this.metrics.set(snapshot.metrics);
      this.recentOrders.set(snapshot.recentOrders);
      this.recentProducts.set(snapshot.recentProducts);
      this.chartOrders.set(snapshot.chartOrders);
      this.lastUpdatedAt.set(new Date());
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private toRevenueTrend(orders: RecentOrderRow[]): DashboardChartPoint[] {
    const today = new Date();
    return Array.from({ length: 7 }, (_, index) => {
      const date = new Date(today.getFullYear(), today.getMonth(), today.getDate() - (6 - index));
      const key = this.dateKey(date);
      return {
        label: date.toLocaleDateString(undefined, { weekday: 'short' }),
        value: orders
          .filter(order => this.dateKey(new Date(order.createdAt)) === key)
          .reduce((total, order) => total + order.total, 0),
        tone: 'info'
      };
    });
  }

  private toOrdersByStatus(orders: RecentOrderRow[]): DashboardChartPoint[] {
    const statusTotals = orders.reduce<Record<string, DashboardChartPoint>>((totals, order) => {
      const label = order.status.label;
      totals[label] = totals[label] || { label, value: 0, tone: order.status.tone };
      totals[label].value += 1;
      return totals;
    }, {});

    return Object.values(statusTotals);
  }

  private dateKey(date: Date): string {
    return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
  }
}
