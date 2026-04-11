import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AuthUser, LoginCredentials } from '@shared/models';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    'Login': props<{ credentials: LoginCredentials }>(),
    'Login Success': props<{ user: AuthUser }>(),
    'Login Failure': props<{ error: string }>(),
    'Logout': emptyProps(),
    'Logout Success': emptyProps(),
    'Restore Session': emptyProps(),
    'Session Expired': emptyProps(),
    'Clear Auth Error': emptyProps(),
  },
});
