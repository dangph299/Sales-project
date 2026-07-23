import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { ApiClientService } from '../api/api-client.service';
import { ApiEndpointConfigurationService } from '../config/api-endpoint-configuration.service';
import { authInterceptor } from './auth.interceptor';
import { SessionService } from './session.service';
import { TokenResponse } from './token.response';

describe('authInterceptor', () => {
  let client: ApiClientService;
  let http: HttpTestingController;
  let session: SessionService;
  let notification: jasmine.SpyObj<NzNotificationService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    notification = jasmine.createSpyObj<NzNotificationService>('NzNotificationService', ['warning']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.resolveTo(true);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: ApiEndpointConfigurationService, useValue: { salesBase: () => '/sales-api' } },
        { provide: NzNotificationService, useValue: notification },
        { provide: Router, useValue: router }
      ]
    });

    client = TestBed.inject(ApiClientService);
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(SessionService);
  });

  afterEach(() => {
    http.verify();
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  });

  it('refreshes the token and retries the original request after a 401', async () => {
    session.setTokens(token('old-access', 'old-refresh'));

    const result = client.get<string[]>('/sales-api', '/api/categories/');
    const original = http.expectOne('/sales-api/api/categories/');
    expect(original.request.headers.get('Authorization')).toBe('Bearer old-access');
    original.flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    const refresh = http.expectOne('/sales-api/api/auth/refresh-token');
    expect(refresh.request.headers.has('Authorization')).toBeFalse();
    expect(refresh.request.body).toEqual({ refreshToken: 'old-refresh' });
    refresh.flush(success(token('new-access', 'new-refresh')));
    await flushPromises();

    const retry = http.expectOne('/sales-api/api/categories/');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-access');
    retry.flush(success(['categories']));

    expect(await result).toEqual(['categories']);
    expect(session.accessToken()).toBe('new-access');
    expect(session.refreshToken()).toBe('new-refresh');
  });

  it('shares one refresh request across simultaneous 401 responses', async () => {
    session.setTokens(token('old-access', 'old-refresh'));

    const first = client.get<string[]>('/sales-api', '/api/categories/');
    const second = client.get<string[]>('/sales-api', '/api/products/');
    http.expectOne('/sales-api/api/categories/').flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/sales-api/api/products/').flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    const refreshRequests = http.match('/sales-api/api/auth/refresh-token');
    expect(refreshRequests.length).toBe(1);
    refreshRequests[0].flush(success(token('new-access', 'new-refresh')));
    await flushPromises();

    const retries = http.match(request =>
      request.url === '/sales-api/api/categories/' || request.url === '/sales-api/api/products/');
    expect(retries.length).toBe(2);
    for (const retry of retries) {
      expect(retry.request.headers.get('Authorization')).toBe('Bearer new-access');
      retry.flush(success([retry.request.url]));
    }

    expect(await first).toEqual(['/sales-api/api/categories/']);
    expect(await second).toEqual(['/sales-api/api/products/']);
  });

  it('clears authentication state and navigates when refresh fails', async () => {
    session.setTokens(token('old-access', 'old-refresh'));

    const result = client.get<string[]>('/sales-api', '/api/categories/');
    http.expectOne('/sales-api/api/categories/').flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/sales-api/api/auth/refresh-token').flush({ message: 'invalid refresh token' }, { status: 401, statusText: 'Unauthorized' });

    await expectAsync(result).toBeRejected();
    expect(session.accessToken()).toBe('');
    expect(session.refreshToken()).toBe('');
    expect(notification.warning).toHaveBeenCalledOnceWith('Session Expired', 'Sign in again to continue.');
    expect(router.navigate).toHaveBeenCalledOnceWith(['/dashboard']);
  });
});

function success<T>(data: T): { success: true; data: T } {
  return { success: true, data };
}

function token(accessToken: string, refreshToken: string): TokenResponse {
  return {
    accessToken,
    expiresIn: 1800,
    refreshToken
  };
}

async function flushPromises(): Promise<void> {
  await Promise.resolve();
  await Promise.resolve();
}
