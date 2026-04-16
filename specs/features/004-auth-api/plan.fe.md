# Plan Frontend: Integración Auth API Real

**Feature ID:** 004-auth-api
**Spec de referencia:** `specs/features/004-auth-api/spec.md`
**Contrato de referencia:** `specs/features/004-auth-api/contract.yml`
**Constitución de referencia:** `CONSTITUTION.md` (frontend)
**Depende de:** 001-auth (implementado), 003-backend-scaffold (implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Resumen del Cambio

Esta feature migra el módulo de autenticación del frontend de **mock local** a **API real**. El flujo de NgRx (actions, reducer, selectors) se mantiene intacto. Los cambios se concentran en:

1. **`AuthService`** — Reemplazar `mockLogin()` por llamadas HTTP reales a `/api/v1/auth/login`, `/auth/refresh` y `/auth/me`
2. **DTOs** — Actualizar para reflejar el contrato camelCase del backend
3. **Effects** — Agregar lógica de almacenamiento de tokens JWT reales y refresh automático
4. **Interceptor de errores** — Agregar intento de refresh antes de despachar `sessionExpired()`
5. **Mock data** — Actualizar emails a `@gop.co` para consistencia con el backend seed
6. **Endpoints** — Agregar `/auth/me` a `api-endpoints.ts`

El objetivo es que el frontend funcione con **ambos modos**: mock (cuando `useMocks: true`) y API real (cuando `useMocks: false`). El interceptor de mocks (ya existente) maneja la transición de forma transparente.

---

## 2. Archivos Afectados

### 2.1. Archivos a CREAR

```
src/app/
├── core/
│   ├── auth/
│   │   └── mocks/
│   │       └── auth.mock-handlers.ts                  # Mock handlers para interceptor (migrar datos de auth.mock.ts al formato MockHandler)
│   └── http/
│       └── token-refresh.interceptor.ts               # Interceptor que intenta refresh en 401 antes de sessionExpired
```

### 2.2. Archivos a MODIFICAR

```
src/app/
├── core/
│   ├── auth/
│   │   ├── auth.service.ts                            # Reemplazar mock por HttpClient calls reales
│   │   ├── auth.mock.ts                               # Actualizar emails @gop360.com → @gop.co
│   │   └── store/
│   │       ├── auth.actions.ts                        # + acciones: refreshToken, refreshTokenSuccess, refreshTokenFailure
│   │       ├── auth.reducer.ts                        # + estado: accessToken, refreshToken en AuthState
│   │       ├── auth.effects.ts                        # + effect refreshToken$, modificar loginSuccess$ y restoreSession$
│   │       └── auth.selectors.ts                      # + selectAccessToken, selectRefreshToken
│   └── http/
│       ├── api-endpoints.ts                           # + auth.me endpoint
│       └── error.interceptor.ts                       # Excluir /auth/refresh y /auth/login de interceptar 401
├── shared/
│   └── models/
│       ├── auth-user.dto.ts                           # Actualizar a camelCase (reflejar contract.yml)
│       ├── auth-user.mapper.ts                        # Simplificar (camelCase → camelCase = casi identity)
│       └── index.ts                                   # + export TokenResponseDTO
└── app.config.ts                                      # + registrar tokenRefreshInterceptor en la cadena
```

---

## 3. Diseño Detallado

### 3.1. DTOs Actualizados

El contrato (`contract.yml`) define properties en **camelCase** que el backend serializa directamente. Los DTOs frontend se alinean:

```typescript
// shared/models/auth-user.dto.ts — ACTUALIZADO

/** Refleja contract.yml → schemas/UserProfile */
export interface UserProfileDTO {
  id: string;
  email: string;
  name: string;
  role: string;
  tenantId: string;
  tenantName: string;
}

/** Refleja contract.yml → schemas/TokenResponse */
export interface TokenResponseDTO {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserProfileDTO;
}

/** Refleja contract.yml → schemas/LoginRequest */
export interface LoginRequestDTO {
  email: string;
  password: string;
}

/** Refleja contract.yml → schemas/RefreshTokenRequest */
export interface RefreshTokenRequestDTO {
  refreshToken: string;
}

/** Alias para compatibilidad con el store (no cambia) */
export interface LoginCredentials {
  email: string;
  password: string;
}
```

> **Cambio clave:** Se elimina `AuthUserDTO` (snake_case) y se reemplaza por `UserProfileDTO` (camelCase). La interfaz `LoginCredentials` se mantiene como alias para el store.

### 3.2. Mapper Actualizado

```typescript
// shared/models/auth-user.mapper.ts — ACTUALIZADO
import { AuthUser } from './auth-user.model';
import { UserProfileDTO } from './auth-user.dto';
import { UserRole } from './user-role.model';

export function mapUserProfileDTOToModel(dto: UserProfileDTO): AuthUser {
  return {
    id: dto.id,
    email: dto.email,
    name: dto.name,
    role: dto.role as UserRole,
    tenantId: dto.tenantId,
    tenantName: dto.tenantName,
  };
}
```

> El mapper se simplifica porque el contrato ya usa camelCase. Sigue existiendo para mantener el patrón DTO → Model y porque `role` necesita cast a `UserRole`.

### 3.3. AuthService — Llamadas HTTP Reales

```typescript
// core/auth/auth.service.ts — ACTUALIZADO
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  login(credentials: LoginCredentials): Observable<TokenResponseDTO> {
    return this.http.post<TokenResponseDTO>(API.auth.login, {
      email: credentials.email.trim().toLowerCase(),
      password: credentials.password,
    } satisfies LoginRequestDTO);
  }

  refresh(refreshToken: string): Observable<TokenResponseDTO> {
    return this.http.post<TokenResponseDTO>(API.auth.refresh, {
      refreshToken,
    } satisfies RefreshTokenRequestDTO);
  }

  me(): Observable<UserProfileDTO> {
    return this.http.get<UserProfileDTO>(API.auth.me);
  }

  saveTokens(accessToken: string, refreshToken: string): void {
    sessionStorage.setItem('accessToken', accessToken);
    sessionStorage.setItem('refreshToken', refreshToken);
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken');
  }

  clearSession(): void {
    sessionStorage.clear();
  }

  // Se mantiene para el mock handler (cuando useMocks: true)
  getStoredUser(): AuthUser | null { ... }
}
```

> **Cambios:** `login()` retorna `TokenResponseDTO` (no `AuthUser`). Se agregan `refresh()`, `me()`, `saveTokens()`, `getAccessToken()`, `getRefreshToken()`. Se renombra `getToken()` → `getAccessToken()`.

### 3.4. Auth State — Nuevas propiedades

```typescript
// auth.reducer.ts — AuthState extendido
export interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  accessToken: string | null;    // NUEVO
  refreshToken: string | null;   // NUEVO
}
```

### 3.5. Auth Actions — Nuevas acciones

```typescript
// auth.actions.ts — Nuevas acciones
events: {
  // ... existentes ...
  'Refresh Token': emptyProps(),
  'Refresh Token Success': props<{ accessToken: string; refreshToken: string; user: AuthUser }>(),
  'Refresh Token Failure': emptyProps(),
}
```

### 3.6. Auth Effects — Modificaciones

**`login$`** — Ahora consume `TokenResponseDTO`:
```
login$ → authService.login(credentials)
  → map(response) → mapUserProfileDTOToModel(response.user)
  → dispatch(loginSuccess({ user, accessToken, refreshToken }))
```

**`loginSuccess$`** — Almacena tokens reales:
```
loginSuccess$ → authService.saveTokens(accessToken, refreshToken)
  → router.navigate(['/'])
```

**`restoreSession$`** — Llama a `/auth/me` si hay token almacenado:
```
restoreSession$ → authService.getAccessToken()
  → si existe: authService.me() → mapUserProfileDTOToModel → loginSuccess
  → si no existe: logoutSuccess
```

**`refreshToken$`** (NUEVO):
```
refreshToken$ → authService.getRefreshToken()
  → authService.refresh(refreshToken)
  → dispatch(refreshTokenSuccess({ ... }))
  → catchError → dispatch(refreshTokenFailure()) → dispatch(sessionExpired())
```

### 3.7. Token Refresh Interceptor

```typescript
// core/http/token-refresh.interceptor.ts — NUEVO
// Se registra DESPUÉS de authInterceptor y ANTES de errorInterceptor
//
// Flujo:
// 1. Si la request falla con 401
// 2. Y la URL NO es /auth/login ni /auth/refresh (evita loops)
// 3. Y hay un refreshToken almacenado
// 4. Intentar refresh una sola vez
// 5. Si refresh exitoso: reintentar la request original con el nuevo accessToken
// 6. Si refresh falla: dejar que el 401 pase al errorInterceptor → sessionExpired()
```

**Orden de interceptores en `app.config.ts`:**
```typescript
provideHttpClient(
  withInterceptors([
    mockInterceptor,          // 1. Mocks (si useMocks: true)
    authInterceptor,          // 2. Inyecta Bearer token
    tokenRefreshInterceptor,  // 3. NUEVO: intenta refresh en 401
    errorInterceptor,         // 4. Muestra toast / despacha sessionExpired
  ])
)
```

### 3.8. Error Interceptor — Modificación

El `error.interceptor.ts` actual despacha `sessionExpired()` en todo 401. Con el nuevo flujo, el `tokenRefreshInterceptor` se encarga de intentar el refresh. Solo los 401 que pasen por el refresh interceptor sin éxito llegarán al error interceptor.

**Cambio mínimo:** Excluir las URLs de auth del dispatch de `sessionExpired()`:

```typescript
case 401: {
  const isAuthUrl = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');
  if (!isAuthUrl) {
    inject(Store).dispatch(AuthActions.sessionExpired());
  }
  break;
}
```

### 3.9. API Endpoints — Agregar /auth/me

```typescript
// core/http/api-endpoints.ts
auth: {
  login:   `${hosts.gopApi}/api/v1/auth/login`,
  refresh: `${hosts.gopApi}/api/v1/auth/refresh`,
  me:      `${hosts.gopApi}/api/v1/auth/me`,         // NUEVO
},
```

> La ruta `auth.logout` existente se elimina (no hay endpoint server-side de logout en este contrato).

### 3.10. Auth Interceptor — Actualización menor

```typescript
// core/http/auth.interceptor.ts — actualizar nombre del método
const token = inject(AuthService).getAccessToken(); // antes: getToken()
```

### 3.11. Mock Handlers (modo fallback)

Para que el frontend siga funcionando con `useMocks: true`, se migra la lógica de `auth.mock.ts` al formato `MockHandler` del interceptor de mocks (ver CONSTITUTION.md §13.4):

```typescript
// core/auth/mocks/auth.mock-handlers.ts
export const authMockHandlers: MockHandler[] = [
  {
    urlPattern: /\/api\/v1\/auth\/login$/,
    method: 'POST',
    handle: (req) => { /* valida contra MOCK_USERS, retorna TokenResponseDTO mock */ }
  },
  {
    urlPattern: /\/api\/v1\/auth\/me$/,
    method: 'GET',
    handle: (req) => { /* retorna UserProfile mock basado en token del header */ }
  },
];
```

Y se registran en `core/http/mock.registry.ts`:
```typescript
import { authMockHandlers } from '@core/auth/mocks/auth.mock-handlers';
export const MOCK_HANDLERS: MockHandler[] = [...authMockHandlers];
```

---

## 4. Mock Data — Actualización de Emails

```typescript
// auth.mock.ts — Actualizar los 4 usuarios
// @gop360.com → @gop.co (alineado con backend seed spec.md §4)
{ email: 'admin@gop.co',       password: 'Admin123*',  ... },
{ email: 'supervisor@gop.co',  password: 'Super123*',  ... },
{ email: 'operador@gop.co',    password: 'Oper123*',   ... },
{ email: 'auditor@gop.co',     password: 'Audit123*',  ... },
```

---

## 5. Restricciones de Implementación (CONSTITUTION.md)

| Regla | Aplicación |
|---|---|
| §4 — Signals para estado local | `isLoading`, `showPassword` siguen siendo Signals (sin cambio) |
| §4 — NgRx para estado global | `accessToken`, `refreshToken`, `user` en NgRx (cruzan features) |
| §4.1 — Navegación en Effects | `loginSuccess$` y `sessionExpired$` navegan desde effects (sin cambio) |
| §5 — Errores en interceptor | El `errorInterceptor` sigue siendo el único punto de manejo de errores HTTP |
| §6.3 — Patrón DTO/Mapper | `UserProfileDTO` → `mapUserProfileDTOToModel()` → `AuthUser` |
| §13 — Mocks por interceptor | Los mock handlers usan el patrón `MockHandler` del interceptor, no llamadas directas |

---

## 6. Environment — Configuración

```typescript
// environment.ts (DEV)
hosts: {
  gopApi: 'http://localhost:5000',  // Backend .NET en puerto 5000
},
featureFlags: {
  useMocks: false,                  // Cambiar a false cuando el backend esté corriendo
},
```

> Mientras el backend no esté disponible, `useMocks: true` mantiene el comportamiento actual sin cambios.

---

## 7. Bloques Compilables

### Bloque 1 — DTOs y Mapper (ng build)
- Actualizar `auth-user.dto.ts` (nuevas interfaces, eliminar `AuthUserDTO` snake_case)
- Actualizar `auth-user.mapper.ts` (nueva función `mapUserProfileDTOToModel`)
- Actualizar `index.ts` (barrel exports)

### Bloque 2 — Service y Endpoints (ng build)
- Actualizar `api-endpoints.ts` (agregar `me`, eliminar `logout`)
- Actualizar `auth.service.ts` (HTTP calls reales, nuevos métodos)

### Bloque 3 — Store (ng build)
- Actualizar `auth.actions.ts` (+ refresh actions)
- Actualizar `auth.reducer.ts` (+ accessToken, refreshToken en state)
- Actualizar `auth.selectors.ts` (+ selectAccessToken, selectRefreshToken)
- Actualizar `auth.effects.ts` (+ refreshToken$, modificar loginSuccess$, restoreSession$)

### Bloque 4 — Interceptors (ng build)
- Crear `token-refresh.interceptor.ts`
- Actualizar `auth.interceptor.ts` (getAccessToken)
- Actualizar `error.interceptor.ts` (excluir auth URLs de sessionExpired)
- Actualizar `app.config.ts` (registrar nuevo interceptor)

### Bloque 5 — Mocks (ng build)
- Actualizar `auth.mock.ts` (emails @gop.co)
- Crear `auth.mock-handlers.ts` (formato MockHandler)
- Actualizar `mock.registry.ts` (registrar auth handlers)

### Bloque 6 — Polish (ng build)
- Verificar imports y path aliases
- Verificar que `useMocks: true` sigue funcionando (fallback)
- Verificar que `useMocks: false` con backend corriendo funciona end-to-end
