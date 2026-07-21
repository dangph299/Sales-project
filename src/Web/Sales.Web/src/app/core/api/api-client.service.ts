import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SessionService } from '../auth/session.service';
import { ApiClientError, ApiResponseReader } from './api-client-result';
import { ApiResult } from './api-result.model';
import { PagedResult } from './paged-result.model';

/** Values accepted as query-string parameters. Undefined, null and '' are dropped. */
export type QueryParameters = Record<string, string | number | boolean | undefined | null>;

/**
 * Transport-level HTTP access to the backend APIs.
 *
 * Deliberately narrow: callers name a base URL and a path, and get back the
 * unwrapped payload. Header construction, parameter construction, response-option
 * creation and API-error translation stay private here so that feature API
 * services never touch Angular HTTP primitives.
 */
@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(SessionService);

  get<T>(baseUrl: string, path: string, parameters?: QueryParameters): Promise<T> {
    return this.send<T>(firstValueFrom(this.http.get(this.url(baseUrl, path), this.options(parameters))));
  }

  /** GET that resolves to null when the resource does not exist (404). */
  async getOptional<T>(baseUrl: string, path: string, parameters?: QueryParameters): Promise<T | null> {
    try {
      return await this.get<T>(baseUrl, path, parameters);
    } catch (error) {
      if (error instanceof ApiClientError && error.status === 404) {
        return null;
      }

      throw error;
    }
  }

  getPage<T>(baseUrl: string, path: string, parameters?: QueryParameters): Promise<PagedResult<T>> {
    return this.get<PagedResult<T>>(baseUrl, path, parameters);
  }

  getWithEtag<T>(baseUrl: string, path: string, parameters?: QueryParameters): Promise<ApiResult<T>> {
    return this.sendWithEtag<T>(firstValueFrom(this.http.get(this.url(baseUrl, path), this.options(parameters))));
  }

  post<T>(baseUrl: string, path: string, body: unknown = {}): Promise<T> {
    return this.send<T>(firstValueFrom(this.http.post(this.url(baseUrl, path), body, this.options())));
  }

  postWithEtag<T>(baseUrl: string, path: string, body: unknown = {}, etag?: string): Promise<ApiResult<T>> {
    return this.sendWithEtag<T>(firstValueFrom(this.http.post(this.url(baseUrl, path), body, this.options(undefined, etag))));
  }

  put<T>(baseUrl: string, path: string, body: unknown = {}): Promise<T> {
    return this.send<T>(firstValueFrom(this.http.put(this.url(baseUrl, path), body, this.options())));
  }

  putWithEtag<T>(baseUrl: string, path: string, body: unknown = {}, etag?: string): Promise<ApiResult<T>> {
    return this.sendWithEtag<T>(firstValueFrom(this.http.put(this.url(baseUrl, path), body, this.options(undefined, etag))));
  }

  async delete(baseUrl: string, path: string): Promise<void> {
    try {
      const response = await firstValueFrom(this.http.delete(this.url(baseUrl, path), this.options()));
      ApiResponseReader.ensureSuccess<void>(ApiResponseReader.readSuccess<void>(response), false);
    } catch (error) {
      this.throwApiError(error);
    }
  }

  private async send<T>(request: Promise<HttpResponse<string>>): Promise<T> {
    try {
      const response = await request;
      return ApiResponseReader.ensureSuccess<T>(ApiResponseReader.readSuccess<T>(response), true);
    } catch (error) {
      this.throwApiError(error);
    }
  }

  private async sendWithEtag<T>(request: Promise<HttpResponse<string>>): Promise<ApiResult<T>> {
    try {
      const response = await request;
      const readResult = ApiResponseReader.readSuccess<T>(response);
      const body = ApiResponseReader.ensureSuccess<T>(readResult, true);
      return {
        body,
        etag: response.headers.get('ETag'),
        status: response.status,
        message: readResult.message,
        correlationId: readResult.correlationId
      };
    } catch (error) {
      this.throwApiError(error);
    }
  }

  private url(baseUrl: string, path: string): string {
    return `${baseUrl}${path}`;
  }

  private options(parameters?: QueryParameters, etag?: string): {
    headers: HttpHeaders;
    observe: 'response';
    params?: HttpParams;
    responseType: 'text';
  } {
    let headers = this.authHeaders();
    if (etag) {
      headers = headers.set('If-Match', etag);
    }

    const options: {
      headers: HttpHeaders;
      observe: 'response';
      params?: HttpParams;
      responseType: 'text';
    } = {
      headers,
      observe: 'response',
      responseType: 'text'
    };

    const params = this.buildParams(parameters);
    if (params) {
      options.params = params;
    }

    return options;
  }

  private authHeaders(): HttpHeaders {
    const token = this.session.accessToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private buildParams(parameters?: QueryParameters): HttpParams | null {
    if (!parameters) {
      return null;
    }

    let params = new HttpParams();
    Object.entries(parameters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && `${value}` !== '') {
        params = params.set(key, `${value}`);
      }
    });

    return params;
  }

  private throwApiError(error: unknown): never {
    if (error instanceof ApiClientError) {
      throw error;
    }

    throw new ApiClientError(ApiResponseReader.readFailure<unknown>(error));
  }
}
