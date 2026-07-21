import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../api/api-client.service';
import { ApiEndpointConfigurationService } from '../config/api-endpoint-configuration.service';
import { SessionService } from './session.service';
import { TokenResponse } from './token.response';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);
  private readonly session = inject(SessionService);

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
}
