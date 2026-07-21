import { Injectable, computed, signal } from '@angular/core';
import { TokenResponse } from './token.response';

const accessTokenStorageKey = 'accessToken';
const refreshTokenStorageKey = 'refreshToken';

/**
 * Single source of truth for the current session's tokens.
 *
 * Holds state only. Acquiring tokens is AuthApiService's job, which keeps this
 * service free of a dependency on ApiClientService (which itself reads the
 * access token to build auth headers).
 */
@Injectable({ providedIn: 'root' })
export class SessionService {
  readonly accessToken = signal(localStorage.getItem(accessTokenStorageKey) || '');
  readonly refreshToken = signal(localStorage.getItem(refreshTokenStorageKey) || '');

  readonly isAuthenticated = computed(() => this.accessToken().trim().length > 0);

  setTokens(token: TokenResponse): void {
    this.accessToken.set(token.accessToken);
    this.refreshToken.set(token.refreshToken);
    localStorage.setItem(accessTokenStorageKey, token.accessToken);
    localStorage.setItem(refreshTokenStorageKey, token.refreshToken);
  }

  clear(): void {
    this.accessToken.set('');
    this.refreshToken.set('');
    localStorage.removeItem(accessTokenStorageKey);
    localStorage.removeItem(refreshTokenStorageKey);
  }
}
