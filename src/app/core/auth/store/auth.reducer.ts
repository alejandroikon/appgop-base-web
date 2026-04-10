import { createFeature, createReducer, on } from '@ngrx/store';
import { AuthUser } from '@shared/models';
import { AuthActions } from './auth.actions';

export interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
};

export const authFeature = createFeature({
  name: 'auth',
  reducer: createReducer(
    initialState,
    on(AuthActions.login, (state) => ({
      ...state,
      isLoading: true,
      error: null,
    })),
    on(AuthActions.loginSuccess, (state, { user }) => ({
      ...state,
      user,
      isAuthenticated: true,
      isLoading: false,
      error: null,
    })),
    on(AuthActions.loginFailure, (state, { error }) => ({
      ...state,
      isLoading: false,
      error,
    })),
    on(AuthActions.logoutSuccess, () => initialState),
    on(AuthActions.clearAuthError, (state) => ({
      ...state,
      error: null,
    })),
  ),
});
