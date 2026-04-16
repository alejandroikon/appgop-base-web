import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';

export const tokenRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthUrl =
        req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

      if (error.status !== 401 || isAuthUrl) {
        return throwError(() => error);
      }

      const storedRefreshToken = authService.getRefreshToken();
      if (!storedRefreshToken) {
        return throwError(() => error);
      }

      return authService.refresh(storedRefreshToken).pipe(
        switchMap((response) => {
          authService.saveTokens(response.accessToken, response.refreshToken);
          const retryReq = req.clone({
            setHeaders: { Authorization: `Bearer ${response.accessToken}` },
          });
          return next(retryReq);
        }),
        catchError(() => throwError(() => error))
      );
    })
  );
};
