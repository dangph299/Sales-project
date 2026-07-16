import { HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ApiClientError, ApiResponseReader } from './api-client-result';
import { PagedResponse } from './models';

describe('ApiResponseReader', () => {
  it('reads 200 OK with ApiResponse<T>', () => {
    const response = ok(200, '{"success":true,"message":"Loaded.","correlationId":"c-1","data":{"id":"p-1"}}');

    const result = ApiResponseReader.readSuccess<{ id: string }>(response);
    const data = ApiResponseReader.ensureSuccess(result, true);

    expect(data.id).toBe('p-1');
    expect(result.message).toBe('Loaded.');
    expect(result.correlationId).toBe('c-1');
  });

  it('reads 201 Created with ApiResponse<T>', () => {
    const response = ok(201, '{"success":true,"data":{"id":"o-1"}}');

    const result = ApiResponseReader.readSuccess<{ id: string }>(response);
    const data = ApiResponseReader.ensureSuccess(result, true);

    expect(data.id).toBe('o-1');
    expect(result.status).toBe(201);
  });

  it('treats 204 No Content as success without reading data', () => {
    const response = ok(204, null);

    const result = ApiResponseReader.readSuccess<void>(response);

    expect(result.isSuccess).toBeTrue();
    expect(result.data).toBeNull();
  });

  it('reads paginated ApiResponse<T>', () => {
    const body = '{"success":true,"data":{"items":[{"id":"p-1"}],"pageNumber":1,"pageSize":20,"totalCount":1,"totalPages":1,"hasPreviousPage":false,"hasNextPage":false}}';
    const response = ok(200, body);

    const result = ApiResponseReader.readSuccess<PagedResponse<{ id: string }>>(response);
    const data = ApiResponseReader.ensureSuccess(result, true);

    expect(data.items.length).toBe(1);
    expect(data.totalCount).toBe(1);
    expect(data.hasNextPage).toBeFalse();
  });

  it('reads 400 validation errors', () => {
    const error = httpError(400, 'Bad Request', '{"status":400,"errorCode":"validation","message":"Validation failed.","traceId":"t-1","correlationId":"c-1","validationErrors":[{"field":"Name","message":"Name is required.","code":"NotEmpty"}]}');

    const result = ApiResponseReader.readFailure(error);

    expect(result.isSuccess).toBeFalse();
    expect(result.validationErrors[0].field).toBe('Name');
    expect(result.validationErrors[0].message).toBe('Name is required.');
    expect(result.traceId).toBe('t-1');
    expect(result.correlationId).toBe('c-1');
  });

  it('reads 404 not found errors', () => {
    const error = httpError(404, 'Not Found', '{"status":404,"errorCode":"not_found","message":"The resource was not found."}');

    const result = ApiResponseReader.readFailure(error);

    expect(result.status).toBe(404);
    expect(result.errorCode).toBe('not_found');
    expect(ApiResponseReader.failureMessages(result)).toEqual(['The resource was not found.']);
  });

  it('reads 409 conflict errors without treating them as validation', () => {
    const error = httpError(409, 'Conflict', '{"status":409,"errorCode":"concurrency_conflict","message":"Reload the latest data.","errors":[{"code":"retryable","message":"False"}]}');

    const result = ApiResponseReader.readFailure(error);

    expect(result.status).toBe(409);
    expect(result.validationErrors.length).toBe(0);
    expect(result.errors[0].code).toBe('retryable');
    expect(ApiResponseReader.failureMessages(result)).toEqual(['Reload the latest data.']);
  });

  it('reads 500 internal server errors', () => {
    const error = httpError(500, 'Internal Server Error', '{"status":500,"errorCode":"internal_server_error","message":"Unexpected error.","traceId":"t-500"}');

    const result = ApiResponseReader.readFailure(error);

    expect(result.status).toBe(500);
    expect(result.traceId).toBe('t-500');
    expect(ApiResponseReader.formatFailure(result)).toContain('Unexpected error.');
  });

  it('handles empty response bodies', () => {
    const response = ok(200, '');

    const result = ApiResponseReader.readSuccess<string>(response);

    expect(result.isSuccess).toBeTrue();
    expect(result.data).toBeNull();
  });

  it('handles malformed JSON response bodies', () => {
    const response = ok(200, '<html>');

    const result = ApiResponseReader.readSuccess<string>(response);

    expect(result.isSuccess).toBeFalse();
    expect(ApiResponseReader.failureMessages(result)[0]).toContain('could not be read');
  });

  it('reports successful responses with null data when data is required', () => {
    const response = ok(200, '{"success":true,"data":null}');
    const result = ApiResponseReader.readSuccess<string>(response);

    expect(() => ApiResponseReader.ensureSuccess(result, true)).toThrowError(ApiClientError);
  });

  it('maps correlation ID and trace ID from error responses', () => {
    const error = httpError(400, 'Bad Request', '{"status":400,"errorCode":"bad_request","message":"Bad request.","traceId":"trace-1","correlationId":"corr-1"}');

    const result = ApiResponseReader.readFailure(error);

    expect(result.traceId).toBe('trace-1');
    expect(result.correlationId).toBe('corr-1');
    expect(ApiResponseReader.diagnostics(result)).toEqual(['Correlation ID: corr-1', 'Trace ID: trace-1']);
  });
});

function ok(status: number, body: string | null): HttpResponse<string> {
  return new HttpResponse<string>({
    body,
    status,
    statusText: status === 201 ? 'Created' : 'OK'
  });
}

function httpError(status: number, statusText: string, body: string): HttpErrorResponse {
  return new HttpErrorResponse({
    error: body,
    headers: new HttpHeaders({ 'content-type': 'application/json' }),
    status,
    statusText
  });
}
