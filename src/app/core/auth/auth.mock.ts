import { delay, Observable, of, throwError } from 'rxjs';
import { AuthUser, UserRole } from '@shared/models';

interface MockUser {
  email: string;
  password: string;
  id: string;
  name: string;
  role: UserRole;
  tenantId: string;
  tenantName: string;
}

export const MOCK_USERS: MockUser[] = [
  {
    email: 'admin@gop360.com',
    password: 'Admin123!',
    id: '550e8400-e29b-41d4-a716-446655440001',
    name: 'Administrador ANH',
    role: 'ADMIN',
    tenantId: '1',
    tenantName: 'Agencia Nacional de Hidrocarburos',
  },
  {
    email: 'supervisor@gop360.com',
    password: 'Super123!',
    id: '550e8400-e29b-41d4-a716-446655440002',
    name: 'Supervisor Ecopetrol',
    role: 'SUPERVISOR',
    tenantId: '2',
    tenantName: 'Ecopetrol S.A.',
  },
  {
    email: 'operador@gop360.com',
    password: 'Oper123!',
    id: '550e8400-e29b-41d4-a716-446655440003',
    name: 'Operador Ecopetrol',
    role: 'OPERADOR',
    tenantId: '2',
    tenantName: 'Ecopetrol S.A.',
  },
  {
    email: 'auditor@gop360.com',
    password: 'Audit123!',
    id: '550e8400-e29b-41d4-a716-446655440004',
    name: 'Auditor ANH',
    role: 'AUDITOR',
    tenantId: '1',
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
