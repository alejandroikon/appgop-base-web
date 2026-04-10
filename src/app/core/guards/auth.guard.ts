import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, take } from 'rxjs';
import { selectIsAuthenticated } from '@core/auth/store';

export const authGuard: CanActivateFn = (route) => {
  const store = inject(Store);
  const router = inject(Router);

  return store.select(selectIsAuthenticated).pipe(
    take(1),
    map((isAuthenticated) => {
      if (isAuthenticated) {
        return true;
      }
      const returnUrl = route.url.map((s) => s.path).join('/');
      return router.createUrlTree(['/login'], {
        queryParams: returnUrl ? { returnUrl } : {},
      });
    })
  );
};
