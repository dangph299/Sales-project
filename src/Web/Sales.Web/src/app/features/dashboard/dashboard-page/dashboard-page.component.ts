import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzListModule } from 'ng-zorro-antd/list';
import { NzTableModule } from 'ng-zorro-antd/table';
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
    DateTimePipe,
    MoneyPipe,
    PriceRangePipe,
    NzButtonModule,
    NzCardModule,
    NzListModule,
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

  readonly systemAlerts = [
    'The dashboard currently summarizes small pages from list endpoints; a backend aggregation API should be added.',
    'Operations APIs for outbox, inbox, and dead letters are not exposed, so operational actions are not shown.'
  ];

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
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }
}
