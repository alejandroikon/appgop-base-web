import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectCurrentUser } from '@core/auth/store';
import { UserRole } from '@shared/models';

export const roleGuard = (allowedRoles: UserRole[]): CanActivateFn => () => {
  const store = inject(Store);
  const router = inject(Router);
  return store.select(selectCurrentUser).pipe(
    take(1),
    map(user =>
      user && allowedRoles.includes(user.role)
        ? true
        : router.createUrlTree(['/dashboard'])
    ),
  );
};
