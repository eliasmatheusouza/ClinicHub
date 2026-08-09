import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const isAuthenticationRequest = request.url.includes('/auth/login') || request.url.includes('/auth/refresh');
  const authorizedRequest = !isAuthenticationRequest && auth.getAccessToken()
    ? request.clone({ setHeaders: { Authorization: `Bearer ${auth.getAccessToken()}` } })
    : request;

  return next(authorizedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isAuthenticationRequest) return throwError(() => error);

      return auth.refresh().pipe(
        switchMap((tokens) => next(request.clone({ setHeaders: { Authorization: `Bearer ${tokens.accessToken}` } }))),
        catchError(() => throwError(() => error))
      );
    })
  );
};
