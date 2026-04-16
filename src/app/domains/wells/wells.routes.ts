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
    path: ':id/edit',
    loadComponent: () =>
      import('./features/well-form/well-form.component').then(
        (m) => m.WellFormComponent,
      ),
  },
];
