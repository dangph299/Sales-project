import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { ApiClientError, ApiResponseReader } from '../../core/api/api-client-result';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { SessionService } from '../../core/auth/session.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule, NzButtonModule, NzFormModule, NzIconModule, NzInputModule, NzLayoutModule],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss'
})
export class AppHeaderComponent {
  private readonly auth = inject(AuthApiService);
  private readonly session = inject(SessionService);

  @Input() pageTitle = '';
  @Input() collapsed = false;
  @Output() collapsedChange = new EventEmitter<boolean>();
  @Output() signedOut = new EventEmitter<void>();
  @Output() errorMessageChange = new EventEmitter<string>();

  readonly userName = signal('admin');
  readonly password = signal('Admin123!');
  readonly authenticating = signal(false);

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

  logout(): void {
    this.auth.logout();
    this.signedOut.emit();
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
