import { Routes } from '@angular/router';
import { AuthLayoutComponent } from '@core/layout/auth-layout/auth-layout.component';
import { MainLayoutComponent } from '@core/layout/main-layout/main-layout.component';
import { authGuard } from '@core/guards/auth.guard';
import { noAuthGuard } from '@core/guards/no-auth.guard';
import { roleGuard } from '@core/guards/role.guard';

export const routes: Routes = [
  // Rutas públicas — Auth Layout (sin Sidebar ni TopHeader)
  {
    path: 'login',
    component: AuthLayoutComponent,
    canActivate: [noAuthGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('@core/auth/features/login/login.component').then(
            (m) => m.LoginComponent
          ),
      },
    ],
  },
  {
    path: 'forgot-password',
    component: AuthLayoutComponent,
    canActivate: [noAuthGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('@core/auth/features/forgot-password/forgot-password.component').then(
            (m) => m.ForgotPasswordComponent
          ),
      },
    ],
  },

  // Rutas autenticadas — MainLayout como shell padre
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      // Dashboard (ruta por defecto)
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('@core/layout/home/home.component').then(
            (m) => m.HomeComponent
          ),
      },
      // Dominios
      {
        path: 'wells',
        loadChildren: () =>
          import('./domains/wells/wells.routes').then((m) => m.wellsRoutes),
      },
      {
        path: 'operations',
        loadChildren: () =>
          import('./domains/operations/operations.routes').then(
            (m) => m.operationsRoutes
          ),
      },
      {
        path: 'production',
        loadChildren: () =>
          import('./domains/production/production.routes').then(
            (m) => m.productionRoutes
          ),
      },
      {
        path: 'admin',
        canActivate: [roleGuard(['ADMIN', 'AUDITOR'])],
        loadChildren: () =>
          import('./domains/admin/admin.routes').then((m) => m.adminRoutes),
      },
    ],
  },

  // Wildcard
  { path: '**', redirectTo: '' },
];
