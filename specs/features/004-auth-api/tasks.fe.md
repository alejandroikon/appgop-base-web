# Tasks Frontend: Integración Auth API Real (004-auth-api)

**Input:** `specs/features/004-auth-api/spec.md` + `specs/features/004-auth-api/plan.fe.md`
**Contrato:** `specs/features/004-auth-api/contract.yml`
**Constitución:** `CONSTITUTION.md` (frontend)

**Formato:** `[ID] [P?] [HU?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — sin dependencias de tareas incompletas en el mismo bloque
- **[HU-N]**: Historia de usuario de referencia en spec.md
- **[BLOQUEANTE]**: Tareas posteriores no pueden iniciar sin esta completada

---

## Bloque 1 — DTOs y Mapper (ng build)

**Propósito:** Actualizar las interfaces DTO para reflejar el contrato camelCase del backend y simplificar el mapper. Eliminar el viejo `AuthUserDTO` snake_case.

- [x] T001 [BLOQUEANTE] [HU-009] Actualizar `auth-user.dto.ts` — eliminar `AuthUserDTO` (snake_case); agregar `UserProfileDTO` (camelCase: id, email, name, role, tenantId, tenantName), `TokenResponseDTO` (accessToken, refreshToken, expiresIn, user: UserProfileDTO), `LoginRequestDTO` (email, password), `RefreshTokenRequestDTO` (refreshToken); mantener `LoginCredentials` como alias — `src/app/shared/models/auth-user.dto.ts`
- [x] T002 [HU-009] Actualizar `auth-user.mapper.ts` — reemplazar `mapAuthUserDTOToModel(dto: AuthUserDTO)` por `mapUserProfileDTOToModel(dto: UserProfileDTO)`: mapeo directo camelCase→camelCase, solo castea `role` a `UserRole` — `src/app/shared/models/auth-user.mapper.ts`
- [x] T003 Actualizar `index.ts` — ajustar barrel exports: reemplazar `AuthUserDTO` por `UserProfileDTO`, agregar export de `TokenResponseDTO`, `LoginRequestDTO`, `RefreshTokenRequestDTO`; mantener export de `mapUserProfileDTOToModel` — `src/app/shared/models/index.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 2 — Service y Endpoints (ng build)

**Propósito:** Actualizar `AuthService` para hacer llamadas HTTP reales al backend y agregar el endpoint `/auth/me`.

**⚠️ BLOQUEANTE:** El service es usado por effects y interceptors.

- [x] T001b [EMERGENTE] Agregar `useMocks: boolean` a `AppEnvironment` e inicializar en `environment.ts`, `environment.qa.ts`, `environment.prod.ts` — campo requerido por el mock interceptor.
- [x] T004 [HU-009] Actualizar `api-endpoints.ts` — agregar `me: \`\${hosts.gopApi}/api/v1/auth/me\``; eliminar `logout` (no existe en el contrato) — `src/app/core/http/api-endpoints.ts`
- [x] T005 [BLOQUEANTE] [HU-009] [HU-010] [HU-011] Actualizar `auth.service.ts` — reemplazar método `login()`: ahora usa `this.http.post<TokenResponseDTO>(API.auth.login, ...)` (trim+lowercase email en el request); agregar método `refresh(refreshToken: string): Observable<TokenResponseDTO>` que llama a `API.auth.refresh`; agregar método `me(): Observable<UserProfileDTO>` que llama a `API.auth.me`; reemplazar `saveSession(user)` por `saveTokens(accessToken, refreshToken)` + `saveUser(user)` que guarda user en sessionStorage; renombrar `getToken()` → `getAccessToken()` que lee `sessionStorage.getItem('accessToken')`; agregar `getRefreshToken()` que lee `sessionStorage.getItem('refreshToken')`; mantener `clearSession()`, `getStoredUser()` y `forgotPassword()` sin cambios — `src/app/core/auth/auth.service.ts`

**Checkpoint:** `ng build` ✅ (parcial — errors en effects se resuelven en B3)

---

## Bloque 3 — Store (ng build)

**Propósito:** Extender el estado NgRx con tokens JWT, agregar acciones de refresh y modificar effects para consumir la API real.

- [x] T006 [P] [HU-010] Actualizar `auth.actions.ts` — agregar 3 acciones nuevas al `createActionGroup`: `'Refresh Token': emptyProps()`, `'Refresh Token Success': props<{ accessToken: string; refreshToken: string; user: AuthUser }>()`, `'Refresh Token Failure': emptyProps()`; modificar `'Login Success'` para agregar props `accessToken` y `refreshToken` — `src/app/core/auth/store/auth.actions.ts`
- [x] T007 [HU-009] Actualizar `auth.reducer.ts` — extender `AuthState` con `accessToken: string | null` y `refreshToken: string | null` (initial null); agregar handlers: `on(loginSuccess)` → setea también `accessToken` y `refreshToken`, `on(refreshTokenSuccess)` → actualiza `accessToken`, `refreshToken` y `user`, `on(logoutSuccess)` → limpia tokens a null — `src/app/core/auth/store/auth.reducer.ts`
- [x] T008 [P] Actualizar `auth.selectors.ts` — agregar selectores: `selectAccessToken` (authFeature.selectAccessToken), `selectRefreshToken` (authFeature.selectRefreshToken) — `src/app/core/auth/store/auth.selectors.ts`
- [x] T009 [BLOQUEANTE] [HU-009] [HU-010] [HU-011] Actualizar `auth.effects.ts` — modificar `login$`: consumir `TokenResponseDTO`, mapear `response.user` con `mapUserProfileDTOToModel()`, despachar `loginSuccess` con `{ user, accessToken: response.accessToken, refreshToken: response.refreshToken }`; modificar `loginSuccess$`: llamar `authService.saveTokens(accessToken, refreshToken)` en vez de `saveSession(user)`, guardar user por separado; modificar `restoreSession$`: si `authService.getAccessToken()` existe, llamar `authService.me()` → mapear → despachar `loginSuccess` con tokens del storage; si falla → `logoutSuccess`; agregar `refreshToken$` (NUEVO): lee refreshToken del storage → `authService.refresh()` → despacha `refreshTokenSuccess` → en error despacha `refreshTokenFailure` y luego `sessionExpired` — `src/app/core/auth/store/auth.effects.ts`
- [x] Actualizar `auth.store/index.ts` — exportar `selectAccessToken`, `selectRefreshToken` — `src/app/core/auth/store/index.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 4 — Interceptors (ng build)

**Propósito:** Crear el interceptor de refresh automático, actualizar el auth interceptor para usar `getAccessToken()` y modificar el error interceptor para excluir URLs de auth.

- [x] T009b [EMERGENTE] Crear `mock.interceptor.ts` — infraestructura del sistema de mocks; define `MockHandler` interface y el `HttpInterceptorFn` que lo ejecuta condicionalmente según `useMocks` — `src/app/core/http/mock.interceptor.ts`
- [x] T009c [EMERGENTE] Crear `mock.registry.ts` — registro central vacío de handlers; se llena en B5 — `src/app/core/http/mock.registry.ts`
- [x] T010 [HU-010] Crear `token-refresh.interceptor.ts` — `HttpInterceptorFn` que: en caso de error 401, verifica que la URL NO sea `/auth/login` ni `/auth/refresh` (evita loops), verifica que exista refreshToken en storage, llama `AuthService.refresh()` una sola vez, si éxito → guarda nuevos tokens y reintenta la request original con nuevo accessToken, si falla → re-lanza el error 401 para que lo capture el error interceptor — `src/app/core/http/token-refresh.interceptor.ts`
- [x] T011 [HU-009] Actualizar `auth.interceptor.ts` — cambiar `inject(AuthService).getToken()` por `inject(AuthService).getAccessToken()` — `src/app/core/http/auth.interceptor.ts`
- [x] T012 [HU-009] Actualizar `error.interceptor.ts` — en el case 401: agregar guard que excluye URLs de auth (`req.url.includes('/auth/login')` o `/auth/refresh`); solo despachar `sessionExpired()` si NO es URL de auth (el refresh interceptor se encarga del retry) — `src/app/core/http/error.interceptor.ts`
- [x] T013 [BLOQUEANTE] Actualizar `app.config.ts` — agregar `tokenRefreshInterceptor` en la cadena de interceptores: posición 3, después de `authInterceptor` y antes de `errorInterceptor`; orden final: `mockInterceptor` → `authInterceptor` → `tokenRefreshInterceptor` → `errorInterceptor` — `src/app/app.config.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 5 — Mocks (ng build)

**Propósito:** Actualizar los datos mock para consistencia con el backend (`@gop.co`) y crear mock handlers para el interceptor de mocks, garantizando que `useMocks: true` sigue funcionando.

- [x] T014 [P] Actualizar `auth.mock.ts` — cambiar los 4 emails de `@gop360.com` a `@gop.co`: `admin@gop.co`, `supervisor@gop.co`, `operador@gop.co`, `auditor@gop.co`; ajustar IDs a formato UUID para consistencia con backend; mantener estructura `MOCK_USERS` y función `mockLogin()`; exportar `MOCK_USERS` como named export — `src/app/core/auth/auth.mock.ts`
- [x] T015 [HU-009] [HU-011] Crear `auth.mock-handlers.ts` — array `authMockHandlers: MockHandler[]` con 3 handlers: (1) POST `/api/v1/auth/login` → valida email/password contra MOCK_USERS, retorna `HttpResponse<TokenResponseDTO>` con accessToken mock, refreshToken mock, expiresIn 1800 y user; (2) POST `/api/v1/auth/refresh` → valida refreshToken y retorna nuevo par; (3) GET `/api/v1/auth/me` → extrae token del header Authorization, retorna `HttpResponse<UserProfileDTO>` con datos del usuario mockeado — `src/app/core/auth/mocks/auth.mock-handlers.ts`
- [x] T016 Actualizar `mock.registry.ts` — importar `authMockHandlers` desde `@core/auth/mocks/auth.mock-handlers` y spread en `MOCK_HANDLERS` array — `src/app/core/http/mock.registry.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 6 — Polish y Validación (ng build)

**Propósito:** Verificación transversal, corrección de imports rotos y validación de ambos modos (mock y API real).

- [x] T017 [P] Verificar que todos los imports de `AuthUserDTO` y `mapAuthUserDTOToModel` en el codebase se actualizaron a `UserProfileDTO` y `mapUserProfileDTOToModel` — buscar en todos los archivos `*.ts` → ✅ Sin referencias al DTO viejo
- [x] T018 [P] Verificar que todos los imports entre capas usan path aliases (`@core/*`, `@shared/*`, `@env/*`) y no rutas relativas largas — revisar archivos creados y modificados → ✅ Sin rutas relativas entre capas
- [x] T019 [P] Verificar que ningún texto nuevo está hardcodeado en templates o componentes — todo debe venir de `locale.ts` → ✅ Archivos de esta feature son infraestructura sin templates
- [x] T020 Ejecutar `ng build` y corregir todos los errores de compilación TypeScript — todos los archivos modificados → ✅ `ng build` exitoso sin errores

**Checkpoint final:** `ng build` ✅ — Feature lista para commit.

---

## Tareas Emergentes Registradas

| ID | Origen | Descripción |
|---|---|---|
| T001b | Entre B1/B2 | `useMocks: boolean` faltante en `AppEnvironment` — requerido por `mock.interceptor.ts` |
| T009b | Entre B3/B4 | Crear `mock.interceptor.ts` — infraestructura del sistema de mocks no existía en el repo |
| T009c | Entre B3/B4 | Crear `mock.registry.ts` vacío — requerido por `mock.interceptor.ts` antes de B5 |

---

## Dependencies & Execution Order

### Dependencias entre Bloques

```
Bloque 1 (DTOs)           → Sin dependencias internas. Primer paso.
Bloque 2 (Service)        → Depende de Bloque 1 (DTOs usados por AuthService).
Bloque 3 (Store)          → Depende de Bloque 2 (AuthService consumido por effects).
Bloque 4 (Interceptors)   → Depende de Bloque 2 + Bloque 3 (AuthService + actions).
Bloque 5 (Mocks)          → Depende de Bloque 1 (DTOs para mock responses).
Bloque 6 (Polish)         → Depende de Bloques 1-5 completos.
```

### Diagrama

```
B1 ──→ B2 ──→ B3 ──→ B4 ──→ B6
 │                            ↑
 └──→ B5 ────────────────────┘
```

B5 (Mocks) puede ejecutarse en paralelo con B2-B4, ya que solo depende de B1 (DTOs).
