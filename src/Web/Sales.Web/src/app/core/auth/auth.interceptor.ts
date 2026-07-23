import { HttpContextToken, HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { SessionService } from './session.service';
import { AuthApiService } from './auth-api.service';

const refreshAttempted = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(SessionService);
  const auth = inject(AuthApiService);
  const token = session.accessToken();
  const authorizedRequest = shouldAttachToken(request) && token
    ? addBearerToken(request, token)
    : request;

  return next(authorizedRequest).pipe(
    catchError(error => {
      if (!shouldRefresh(error, request, session)) {
        return throwError(() => error);
      }

      return auth.refreshAccessToken().pipe(
        switchMap(accessToken => next(addBearerToken(
          request.clone({ context: request.context.set(refreshAttempted, true) }),
          accessToken))));
    })
  );
};

function shouldAttachToken(request: HttpRequest<unknown>): boolean {
  return !isAuthEndpoint(request);
}

function shouldRefresh(error: unknown, request: HttpRequest<unknown>, session: SessionService): boolean {
  return error instanceof HttpErrorResponse &&
    error.status === 401 &&
    !request.context.get(refreshAttempted) &&
    !isAuthEndpoint(request) &&
    session.refreshToken().trim().length > 0;
}

function isAuthEndpoint(request: HttpRequest<unknown>): boolean {
  return /\/api\/auth\/(login|refresh|refresh-token)(\?|$)/.test(request.url);
}

function addBearerToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}
