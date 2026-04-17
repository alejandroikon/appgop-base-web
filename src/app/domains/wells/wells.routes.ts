import { Routes } from '@angular/router';

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
      import('./features/well-form/well-form.component').then(
        (m) => m.WellFormComponent,
      ),
  },
  {
    // Ruta de detalle — debe ir ANTES de :id/edit para que Angular resuelva correctamente
    path: ':id',
    loadComponent: () =>
      import('./features/well-detail/well-detail.component').then(
        (m) => m.WellDetailComponent,
      ),
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./features/well-form/well-form.component').then(
        (m) => m.WellFormComponent,
      ),
  },
];
