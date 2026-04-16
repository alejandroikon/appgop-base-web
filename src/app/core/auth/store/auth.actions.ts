import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AuthUser, LoginCredentials } from '@shared/models';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    'Login': props<{ credentials: LoginCredentials }>(),
    'Login Success': props<{ user: AuthUser; accessToken: string; refreshToken: string }>(),
    'Login Failure': props<{ error: string }>(),
    'Logout': emptyProps(),
    'Logout Success': emptyProps(),
    'Restore Session': emptyProps(),
    'Session Expired': emptyProps(),
    'Clear Auth Error': emptyProps(),
    'Refresh Token': emptyProps(),
    'Refresh Token Success': props<{ accessToken: string; refreshToken: string; user: AuthUser }>(),
    'Refresh Token Failure': emptyProps(),
  },
});
