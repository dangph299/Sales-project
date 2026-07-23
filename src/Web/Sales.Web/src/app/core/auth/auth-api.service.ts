import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { BehaviorSubject, Observable, catchError, filter, finalize, from, map, shareReplay, switchMap, take, throwError } from 'rxjs';
import { ApiClientService } from '../api/api-client.service';
import { ApiEndpointConfigurationService } from '../config/api-endpoint-configuration.service';
import { SessionService } from './session.service';
import { TokenResponse } from './token.response';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);
  private readonly session = inject(SessionService);
  private readonly notification = inject(NzNotificationService);
  private readonly router = inject(Router);
  private readonly refreshedAccessToken = new BehaviorSubject<string | null>(null);
  private refreshRequest$: Observable<TokenResponse> | null = null;

  async login(userName: string, password: string): Promise<TokenResponse> {
    const token = await this.client.post<TokenResponse>(
      this.endpoints.salesBase(),
      '/api/auth/login',
      { userName, password });

    this.session.setTokens(token);
    return token;
  }

  logout(): void {
    this.session.clear();
  }

  refreshAccessToken(): Observable<string> {
    const refreshToken = this.session.refreshToken();
    if (!refreshToken) {
      this.handleRefreshFailure();
      return throwError(() => new Error('Refresh token is missing.'));
    }

    if (!this.refreshRequest$) {
      this.refreshedAccessToken.next(null);
      this.refreshRequest$ = from(this.refresh(refreshToken)).pipe(
        catchError(error => {
          this.handleRefreshFailure();
          return throwError(() => error);
        }),
        finalize(() => {
          this.refreshRequest$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    return this.refreshRequest$.pipe(
      switchMap(() => this.refreshedAccessToken.pipe(
        filter((token): token is string => token !== null && token.trim().length > 0),
        take(1))),
      map(token => token)
    );
  }

  private async refresh(refreshToken: string): Promise<TokenResponse> {
    const token = await this.client.post<TokenResponse>(
      this.endpoints.salesBase(),
      '/api/auth/refresh-token',
      { refreshToken });

    this.session.setTokens(token);
    this.refreshedAccessToken.next(token.accessToken);
    return token;
  }

  private handleRefreshFailure(): void {
    if (this.session.isAuthenticated() || this.session.refreshToken().trim().length > 0) {
      this.notification.warning('Session Expired', 'Sign in again to continue.');
    }

    this.session.clear();
    void this.router.navigate(['/dashboard']);
  }
}
