import { delay, Observable, of, throwError } from 'rxjs';
import { AuthUser } from '@shared/models';
import { UserRole } from '@shared/models';

interface MockUser {
  email: string;
  password: string;
  id: string;
  name: string;
  role: UserRole;
  tenantId: string;
  tenantName: string;
}

const MOCK_USERS: MockUser[] = [
  {
    email: 'admin@gop360.com',
    password: 'Admin123*',
    id: 'usr-001',
    name: 'Administrador ANH',
    role: 'ADMIN',
    tenantId: 'tenant-anh',
    tenantName: 'Agencia Nacional de Hidrocarburos',
  },
  {
    email: 'supervisor@gop360.com',
    password: 'Super123*',
    id: 'usr-002',
    name: 'Supervisor Ecopetrol',
    role: 'SUPERVISOR',
    tenantId: 'tenant-ecopetrol',
    tenantName: 'Ecopetrol S.A.',
  },
  {
    email: 'operador@gop360.com',
    password: 'Oper123*',
    id: 'usr-003',
    name: 'Operador Ecopetrol',
    role: 'OPERADOR',
    tenantId: 'tenant-ecopetrol',
    tenantName: 'Ecopetrol S.A.',
  },
  {
    email: 'auditor@gop360.com',
    password: 'Audit123*',
    id: 'usr-004',
    name: 'Auditor ANH',
    role: 'AUDITOR',
    tenantId: 'tenant-anh',
    tenantName: 'Agencia Nacional de Hidrocarburos',
  },
];

export function mockLogin(email: string, password: string): Observable<AuthUser> {
  const normalizedEmail = email.trim().toLowerCase();
  const user = MOCK_USERS.find(
    (u) => u.email === normalizedEmail && u.password === password
  );

  if (user) {
    const { password: _, ...authUser } = user;
    return of(authUser).pipe(delay(800));
  }

  return throwError(() => 'Correo o contraseña incorrectos. Verifique sus datos.').pipe(delay(800));
}
