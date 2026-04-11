import { Routes } from '@angular/router';
import { roleGuard } from '@core/guards/role.guard';

export const adminRoutes: Routes = [
  { path: '', redirectTo: 'users', pathMatch: 'full' },
  {
    path: 'users',
    canActivate: [roleGuard(['ADMIN'])],
    loadComponent: () =>
      import('./features/users-placeholder/users-placeholder.component').then(
        (m) => m.UsersPlaceholderComponent
      ),
  },
  {
    path: 'audit-logs',
    canActivate: [roleGuard(['ADMIN', 'AUDITOR'])],
    loadComponent: () =>
      import('./features/audit-logs-placeholder/audit-logs-placeholder.component').then(
        (m) => m.AuditLogsPlaceholderComponent
      ),
  },
];