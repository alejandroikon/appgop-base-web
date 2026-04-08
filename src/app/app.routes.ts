import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'wells',
    loadChildren: () =>
      import('./domains/wells/wells.routes').then(m => m.wellsRoutes),
  },
  {
    path: 'operations',
    loadChildren: () =>
      import('./domains/operations/operations.routes').then(m => m.operationsRoutes),
  },
  {
    path: 'production',
    loadChildren: () =>
      import('./domains/production/production.routes').then(m => m.productionRoutes),
  },
  {
    path: 'admin',
    loadChildren: () =>
      import('./domains/admin/admin.routes').then(m => m.adminRoutes),
  },
  { path: '', redirectTo: 'wells', pathMatch: 'full' },
  { path: '**', redirectTo: 'wells' },
];
