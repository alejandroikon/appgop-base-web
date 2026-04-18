import { Routes } from '@angular/router';
import { roleGuard } from '@core/guards/role.guard';

export const wellsRoutes: Routes = [
  {
    path: '',
    redirectTo: 'manage',
    pathMatch: 'full',
  },
  {
    path: 'manage',
    loadComponent: () =>
      import('./features/well-manage/well-manage.component').then(
        (m) => m.WellManageComponent,
      ),
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./features/well-create/well-create.component').then(
        (m) => m.WellCreateComponent,
      ),
    canActivate: [roleGuard(['OPERADOR', 'ADMIN'])],
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./features/well-detail/well-detail.component').then(
        (m) => m.WellDetailComponent,
      ),
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./features/well-create/well-create.component').then(
        (m) => m.WellCreateComponent,
      ),
    canActivate: [roleGuard(['OPERADOR', 'SUPERVISOR', 'ADMIN'])],
  },
];
