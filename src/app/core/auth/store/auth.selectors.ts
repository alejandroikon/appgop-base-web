import { createSelector } from '@ngrx/store';
import { authFeature } from './auth.reducer';

export const selectAuthState = authFeature.selectAuthState;
export const selectCurrentUser = authFeature.selectUser;
export const selectIsAuthenticated = authFeature.selectIsAuthenticated;
export const selectAuthIsLoading = authFeature.selectIsLoading;
export const selectAuthError = authFeature.selectError;
export const selectAccessToken = authFeature.selectAccessToken;
export const selectRefreshToken = authFeature.selectRefreshToken;

export const selectCurrentTenant = createSelector(
  selectCurrentUser,
  (user) => user ? { tenantId: user.tenantId, tenantName: user.tenantName } : null
);
