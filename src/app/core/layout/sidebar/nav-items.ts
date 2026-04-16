import { UserRole } from '@shared/models';

export interface NavItem {
  key:       string;
  icon?:     string;
  route?:    string;
  roles:     UserRole[];
  children?: NavItem[];
}

export const NAV_ITEMS: NavItem[] = [
  {
    key: 'dashboard',
    icon: 'pi pi-objects-column',
    route: '/dashboard',
    roles: ['ADMIN', 'SUPERVISOR', 'OPERADOR', 'AUDITOR'],
  },
  {
    key: 'wells',
    icon: 'pi pi-map-marker',
    route: '/wells',
    roles: ['ADMIN', 'SUPERVISOR', 'OPERADOR', 'AUDITOR'],
  },
  {
    key: 'admin',
    icon: 'pi pi-shield',
    roles: ['ADMIN', 'AUDITOR'],
    children: [
      {
        key: 'adminUsers',
        route: '/admin/users',
        roles: ['ADMIN'],
      },
      {
        key: 'adminAuditLogs',
        route: '/admin/audit-logs',
        roles: ['ADMIN', 'AUDITOR'],
      },
    ],
  },
];