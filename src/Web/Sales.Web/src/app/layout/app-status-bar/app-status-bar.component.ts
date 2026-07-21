import { CommonModule } from '@angular/common';
import { Component, Input, inject, signal } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { ApiClientError, ApiResponseReader } from '../../core/api/api-client-result';
import { SessionService } from '../../core/auth/session.service';
import { HealthApiService } from '../../core/health/health-api.service';
import { SignalrConnectionService } from '../../core/realtime/signalr-connection.service';

@Component({
  selector: 'app-status-bar',
  standalone: true,
  imports: [CommonModule, NzAlertModule, NzBadgeModule, NzButtonModule, NzIconModule, NzTagModule],
  templateUrl: './app-status-bar.component.html',
  styleUrl: './app-status-bar.component.scss'
})
export class AppStatusBarComponent {
  private readonly session = inject(SessionService);
  private readonly health = inject(HealthApiService);
  private readonly realtime = inject(SignalrConnectionService);

  @Input() errorMessage = '';

  readonly healthText = signal('Health check has not run.');
  readonly isAuthenticated = this.session.isAuthenticated;

  async checkHealth(): Promise<void> {
    this.healthText.set('Checking APIs...');
    try {
      await this.health.check();
      this.healthText.set('Sales API and Inventory API are ready.');
    } catch (error) {
      this.healthText.set(this.describeError(error));
    }
  }

  realtimeLabel(): string {
    switch (this.realtime.state()) {
      case 'Connected':
        return 'Live';
      case 'Connecting':
        return 'Connecting';
      case 'Reconnecting':
        return 'Reconnecting';
      default:
        return 'Offline';
    }
  }

  realtimeBadgeStatus(): 'success' | 'processing' | 'warning' | 'default' {
    switch (this.realtime.state()) {
      case 'Connected':
        return 'success';
      case 'Connecting':
        return 'processing';
      case 'Reconnecting':
        return 'warning';
      default:
        return 'default';
    }
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    if (error instanceof Error) {
      return error.message;
    }

    return 'Request failed.';
  }
}
