import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap, tap } from 'rxjs';
import { AuthActions } from './auth.actions';
import { AuthService } from '../auth.service';
import { mapUserProfileDTOToModel } from '@shared/models';

export const restoreSession$ = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.restoreSession),
      switchMap(() => {
        const accessToken = authService.getAccessToken();
        if (!accessToken) {
          return of(AuthActions.logoutSuccess());
        }
        return authService.me().pipe(
          map((dto) => {
            const user = mapUserProfileDTOToModel(dto);
            const refreshToken = authService.getRefreshToken() ?? '';
            return AuthActions.loginSuccess({ user, accessToken, refreshToken });
          }),
          catchError(() => of(AuthActions.logoutSuccess()))
        );
      })
    ),
  { functional: true }
);

export const login$ = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.login),
      exhaustMap(({ credentials }) =>
        authService.login(credentials).pipe(
          map((response) => {
            const user = mapUserProfileDTOToModel(response.user);
            return AuthActions.loginSuccess({
              user,
              accessToken: response.accessToken,
              refreshToken: response.refreshToken,
            });
          }),
          catchError((error: unknown) => {
            const message =
              error instanceof Error ? error.message : 'Correo o contraseña incorrectos. Verifique sus datos.';
            return of(AuthActions.loginFailure({ error: message }));
          })
        )
      )
    ),
  { functional: true }
);

export const loginSuccess$ = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService), router = inject(Router)) =>
    actions$.pipe(
      ofType(AuthActions.loginSuccess),
      tap(({ user, accessToken, refreshToken }) => {
        authService.saveTokens(accessToken, refreshToken);
        authService.saveUser(user);
        router.navigate(['/']);
      })
    ),
  { functional: true, dispatch: false }
);

export const refreshToken$ = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService)) =>
    actions$.pipe(
      ofType(AuthActions.refreshToken),
      switchMap(() => {
        const storedRefreshToken = authService.getRefreshToken();
        if (!storedRefreshToken) {
          return of(AuthActions.refreshTokenFailure(), AuthActions.sessionExpired());
        }
        return authService.refresh(storedRefreshToken).pipe(
          map((response) => {
            const user = mapUserProfileDTOToModel(response.user);
            authService.saveTokens(response.accessToken, response.refreshToken);
            authService.saveUser(user);
            return AuthActions.refreshTokenSuccess({
              user,
              accessToken: response.accessToken,
              refreshToken: response.refreshToken,
            });
          }),
          catchError(() => of(AuthActions.refreshTokenFailure(), AuthActions.sessionExpired()))
        );
      })
    ),
  { functional: true }
);

export const logout$ = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService), router = inject(Router)) =>
    actions$.pipe(
      ofType(AuthActions.logout),
      tap(() => {
        authService.clearSession();
        router.navigate(['/login']);
      }),
      map(() => AuthActions.logoutSuccess())
    ),
  { functional: true }
);

export const sessionExpired$ = createEffect(
  (actions$ = inject(Actions), authService = inject(AuthService), router = inject(Router)) =>
    actions$.pipe(
      ofType(AuthActions.sessionExpired),
      tap(() => {
        authService.clearSession();
        router.navigate(['/login'], { queryParams: { reason: 'session_expired' } });
      }),
      map(() => AuthActions.logoutSuccess())
    ),
  { functional: true }
);
