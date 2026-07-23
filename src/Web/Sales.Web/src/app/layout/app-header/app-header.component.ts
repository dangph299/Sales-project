import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { ApiClientError, ApiResponseReader } from '../../core/api/api-client-result';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { SessionService } from '../../core/auth/session.service';
import { HealthApiService } from '../../core/health/health-api.service';
import { SignalrConnectionService } from '../../core/realtime/signalr-connection.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NzBadgeModule,
    NzButtonModule,
    NzDropDownModule,
    NzFormModule,
    NzIconModule,
    NzInputModule,
    NzLayoutModule,
    NzMenuModule
  ],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss'
})
export class AppHeaderComponent {
  private readonly auth = inject(AuthApiService);
  private readonly session = inject(SessionService);
  private readonly health = inject(HealthApiService);
  private readonly realtime = inject(SignalrConnectionService);

  @Input() pageTitle = '';
  @Input() collapsed = false;
  @Output() collapsedChange = new EventEmitter<boolean>();
  @Output() signedOut = new EventEmitter<void>();
  @Output() errorMessageChange = new EventEmitter<string>();

  readonly userName = signal('admin');
  readonly password = signal('Admin123!');
  readonly authenticating = signal(false);
  readonly checkingHealth = signal(false);
  readonly apiStatus = signal<'unknown' | 'ready' | 'error'>('unknown');

  readonly isAuthenticated = this.session.isAuthenticated;

  toggleSidebar(): void {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }

  async login(): Promise<void> {
    this.authenticating.set(true);
    this.errorMessageChange.emit('');
    try {
      await this.auth.login(this.userName(), this.password());
    } catch (error) {
      this.errorMessageChange.emit(this.describeError(error));
    } finally {
      this.authenticating.set(false);
    }
  }

  refreshToken(): void {
    this.authenticating.set(true);
    this.errorMessageChange.emit('');
    this.auth.refreshAccessToken().subscribe({
      next: () => this.authenticating.set(false),
      error: error => {
        this.errorMessageChange.emit(this.describeError(error));
        this.authenticating.set(false);
      }
    });
  }

  logout(): void {
    this.auth.logout();
    this.signedOut.emit();
  }

  async checkHealth(): Promise<void> {
    this.checkingHealth.set(true);
    this.errorMessageChange.emit('');
    try {
      await this.health.check();
      this.apiStatus.set('ready');
    } catch (error) {
      this.apiStatus.set('error');
      this.errorMessageChange.emit(this.describeError(error));
    } finally {
      this.checkingHealth.set(false);
    }
  }

  apiLabel(): string {
    switch (this.apiStatus()) {
      case 'ready':
        return 'API ready';
      case 'error':
        return 'API issue';
      default:
        return 'API';
    }
  }

  apiBadgeStatus(): 'success' | 'error' | 'default' {
    switch (this.apiStatus()) {
      case 'ready':
        return 'success';
      case 'error':
        return 'error';
      default:
        return 'default';
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
