import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { ApiError, ApiErrorResponse, ApiResponse, ValidationError } from './models';

export interface ApiClientResult<T> {
  isSuccess: boolean;
  status: number;
  statusText?: string;
  data: T | null;
  message?: string | null;
  correlationId?: string | null;
  traceId?: string | null;
  errorCode?: string | null;
  errors: ApiError[];
  validationErrors: ValidationError[];
}

export class ApiClientError extends Error {
  readonly result: ApiClientResult<unknown>;
  readonly status: number;

  constructor(result: ApiClientResult<unknown>) {
    super(ApiResponseReader.formatFailure(result));
    this.name = 'ApiClientError';
    this.result = result;
    this.status = result.status;
  }
}

export class ApiResponseReader {
  static readSuccess<T>(response: HttpResponse<string>): ApiClientResult<T> {
    if (response.status === 204) {
      return {
        isSuccess: true,
        status: response.status,
        statusText: response.statusText,
        data: null,
        errors: [],
        validationErrors: []
      };
    }

    if (!response.body || response.body.trim() === '') {
      return {
        isSuccess: true,
        status: response.status,
        statusText: response.statusText,
        data: null,
        errors: [],
        validationErrors: []
      };
    }

    const parsed = this.parseJson(response.body);
    if (!parsed.ok) {
      return this.clientFailure<T>(
        response.status,
        response.statusText,
        'The server returned a response that could not be read.',
        null,
        null);
    }

    const envelope = parsed.value as Partial<ApiResponse<T>>;
    const succeeded = envelope.success === true || envelope.succeeded === true;
    if (!succeeded) {
      return {
        isSuccess: false,
        status: response.status,
        statusText: response.statusText,
        message: envelope.message || 'The server reported that the request failed.',
        correlationId: envelope.correlationId || null,
        data: null,
        errors: [],
        validationErrors: []
      };
    }

    return {
      isSuccess: true,
      status: response.status,
      statusText: response.statusText,
      data: envelope.data ?? null,
      message: envelope.message || null,
      correlationId: envelope.correlationId || null,
      errors: [],
      validationErrors: []
    };
  }

  static readFailure<T>(error: unknown): ApiClientResult<T> {
    if (error instanceof HttpErrorResponse) {
      return this.readHttpError<T>(error);
    }

    if (error instanceof Error) {
        return this.clientFailure<T>(0, '', error.message, null, null);
    }

    return this.clientFailure<T>(0, '', String(error), null, null);
  }

  static ensureSuccess<T>(result: ApiClientResult<T>, requireData: true): NonNullable<T>;
  static ensureSuccess<T>(result: ApiClientResult<T>, requireData: false): T | null;
  static ensureSuccess<T>(result: ApiClientResult<T>, requireData: boolean): NonNullable<T> | T | null {
    if (!result.isSuccess) {
      throw new ApiClientError(result);
    }

    if (requireData && result.data == null) {
      const failure = this.clientFailure<T>(
        result.status,
        result.statusText || '',
        'The server returned a successful response without data.',
        result.correlationId || null,
        result.traceId || null);
      throw new ApiClientError(failure);
    }

    return result.data;
  }

  static formatFailure(result: ApiClientResult<unknown>): string {
    const messages = this.failureMessages(result);
    const diagnostics = this.diagnostics(result);

    if (diagnostics.length === 0) {
      return messages.join(' ');
    }

    return `${messages.join(' ')} ${diagnostics.join(' ')}`;
  }

  static failureMessages(result: ApiClientResult<unknown>): string[] {
    const messages: string[] = [];

    for (const validationError of result.validationErrors) {
      const field = validationError.field ? `${validationError.field}: ` : '';
      const message = `${field}${validationError.message}`;
      if (!messages.includes(message)) {
        messages.push(message);
      }
    }

    if (messages.length > 0) {
      return messages;
    }

    for (const error of result.errors) {
      if (this.isDiagnosticError(error)) {
        continue;
      }

      if (error.message && !messages.includes(error.message)) {
        messages.push(error.message);
      }
    }

    if (messages.length > 0) {
      return messages;
    }

    if (result.message && result.message.trim() !== '') {
      return [result.message];
    }

    if (result.statusText && result.statusText.trim() !== '') {
      return [`${result.status} ${result.statusText}`.trim()];
    }

    return ['Request failed.'];
  }

  static diagnostics(result: ApiClientResult<unknown>): string[] {
    const diagnostics: string[] = [];

    if (result.correlationId) {
      diagnostics.push(`Correlation ID: ${result.correlationId}`);
    }

    if (result.traceId) {
      diagnostics.push(`Trace ID: ${result.traceId}`);
    }

    return diagnostics;
  }

  private static readHttpError<T>(error: HttpErrorResponse): ApiClientResult<T> {
    if (typeof error.error === 'string' && error.error.trim() !== '') {
      const parsed = this.parseJson(error.error);
      if (parsed.ok) {
        return this.fromApiError<T>(error.status, error.statusText, parsed.value as Partial<ApiErrorResponse>);
      }
    }

    if (error.error && typeof error.error === 'object') {
      return this.fromApiError<T>(error.status, error.statusText, error.error as Partial<ApiErrorResponse>);
    }

    if (error.error instanceof Error) {
      return this.clientFailure<T>(error.status, error.statusText, 'The server returned a response that could not be read.', null, null);
    }

    return this.clientFailure<T>(error.status, error.statusText, error.message || error.statusText || 'Request failed.', null, null);
  }

  private static isDiagnosticError(error: ApiError): boolean {
    return error.code === 'retryable' || error.code === 'current_version';
  }

  private static fromApiError<T>(
    status: number,
    statusText: string,
    error: Partial<ApiErrorResponse>): ApiClientResult<T> {
    return {
      isSuccess: false,
      status: error.status || status,
      statusText,
      data: null,
      message: error.message || null,
      traceId: error.traceId || null,
      correlationId: error.correlationId || null,
      errorCode: error.errorCode || null,
      errors: error.errors || [],
      validationErrors: error.validationErrors || []
    };
  }

  private static clientFailure<T>(
    status: number,
    statusText: string,
    message: string,
    correlationId: string | null,
    traceId: string | null): ApiClientResult<T> {
    return {
      isSuccess: false,
      status,
      statusText,
      data: null,
      message,
      correlationId,
      traceId,
      errors: [],
      validationErrors: []
    };
  }

  private static parseJson(text: string): { ok: true; value: unknown } | { ok: false } {
    try {
      return { ok: true, value: JSON.parse(text) };
    } catch {
      return { ok: false };
    }
  }
}
