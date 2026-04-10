import { Routes } from '@angular/router';
import { AuthLayoutComponent } from '@core/layout/auth-layout/auth-layout.component';
import { authGuard } from '@core/guards/auth.guard';
import { noAuthGuard } from '@core/guards/no-auth.guard';

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
  // Página principal — requiere sesión
  {
    path: '',
    pathMatch: 'full',
    canActivate: [authGuard],
    loadComponent: () =>
      import('@core/layout/home/home.component').then(
        (m) => m.HomeComponent
      ),
  },
  // Rutas protegidas — Dominios
  {
    path: 'wells',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./domains/wells/wells.routes').then((m) => m.wellsRoutes),
  },
  {
    path: 'operations',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./domains/operations/operations.routes').then(
        (m) => m.operationsRoutes
      ),
  },
  {
    path: 'production',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./domains/production/production.routes').then(
        (m) => m.productionRoutes
      ),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./domains/admin/admin.routes').then((m) => m.adminRoutes),
  },
  // Wildcard
  { path: '**', redirectTo: '' },
];
