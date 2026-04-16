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

- [ ] T001 [BLOQUEANTE] [HU-009] Actualizar `auth-user.dto.ts` — eliminar `AuthUserDTO` (snake_case); agregar `UserProfileDTO` (camelCase: id, email, name, role, tenantId, tenantName), `TokenResponseDTO` (accessToken, refreshToken, expiresIn, user: UserProfileDTO), `LoginRequestDTO` (email, password), `RefreshTokenRequestDTO` (refreshToken); mantener `LoginCredentials` como alias — `src/app/shared/models/auth-user.dto.ts`
- [ ] T002 [HU-009] Actualizar `auth-user.mapper.ts` — reemplazar `mapAuthUserDTOToModel(dto: AuthUserDTO)` por `mapUserProfileDTOToModel(dto: UserProfileDTO)`: mapeo directo camelCase→camelCase, solo castea `role` a `UserRole` — `src/app/shared/models/auth-user.mapper.ts`
- [ ] T003 Actualizar `index.ts` — ajustar barrel exports: reemplazar `AuthUserDTO` por `UserProfileDTO`, agregar export de `TokenResponseDTO`, `LoginRequestDTO`, `RefreshTokenRequestDTO`; mantener export de `mapUserProfileDTOToModel` — `src/app/shared/models/index.ts`

**Checkpoint:** `ng build` → exit code 0. Pueden haber errores de compilación por archivos que aún importan `AuthUserDTO` o `mapAuthUserDTOToModel` — se corrigen en bloques siguientes.

---

## Bloque 2 — Service y Endpoints (ng build)

**Propósito:** Actualizar `AuthService` para hacer llamadas HTTP reales al backend y agregar el endpoint `/auth/me`.

**⚠️ BLOQUEANTE:** El service es usado por effects y interceptors.

- [ ] T004 [HU-009] Actualizar `api-endpoints.ts` — agregar `me: \`\${hosts.gopApi}/api/v1/auth/me\``; eliminar `logout` (no existe en el contrato) — `src/app/core/http/api-endpoints.ts`
- [ ] T005 [BLOQUEANTE] [HU-009] [HU-010] [HU-011] Actualizar `auth.service.ts` — reemplazar método `login()`: ahora usa `this.http.post<TokenResponseDTO>(API.auth.login, ...)` (trim+lowercase email en el request); agregar método `refresh(refreshToken: string): Observable<TokenResponseDTO>` que llama a `API.auth.refresh`; agregar método `me(): Observable<UserProfileDTO>` que llama a `API.auth.me`; reemplazar `saveSession(user)` por `saveTokens(accessToken, refreshToken)` + `saveUser(user)` que guarda user en sessionStorage; renombrar `getToken()` → `getAccessToken()` que lee `sessionStorage.getItem('accessToken')`; agregar `getRefreshToken()` que lee `sessionStorage.getItem('refreshToken')`; mantener `clearSession()`, `getStoredUser()` y `forgotPassword()` sin cambios — `src/app/core/auth/auth.service.ts`

**Checkpoint:** `ng build` → puede tener errores por consumers del viejo `getToken()` — se corrigen en bloque siguiente.

---

## Bloque 3 — Store (ng build)

**Propósito:** Extender el estado NgRx con tokens JWT, agregar acciones de refresh y modificar effects para consumir la API real.

- [ ] T006 [P] [HU-010] Actualizar `auth.actions.ts` — agregar 3 acciones nuevas al `createActionGroup`: `'Refresh Token': emptyProps()`, `'Refresh Token Success': props<{ accessToken: string; refreshToken: string; user: AuthUser }>()`, `'Refresh Token Failure': emptyProps()`; modificar `'Login Success'` para agregar props `accessToken` y `refreshToken` — `src/app/core/auth/store/auth.actions.ts`
- [ ] T007 [HU-009] Actualizar `auth.reducer.ts` — extender `AuthState` con `accessToken: string | null` y `refreshToken: string | null` (initial null); agregar handlers: `on(loginSuccess)` → setea también `accessToken` y `refreshToken`, `on(refreshTokenSuccess)` → actualiza `accessToken`, `refreshToken` y `user`, `on(logoutSuccess)` → limpia tokens a null — `src/app/core/auth/store/auth.reducer.ts`
- [ ] T008 [P] Actualizar `auth.selectors.ts` — agregar selectores: `selectAccessToken` (authFeature.selectAccessToken), `selectRefreshToken` (authFeature.selectRefreshToken) — `src/app/core/auth/store/auth.selectors.ts`
- [ ] T009 [BLOQUEANTE] [HU-009] [HU-010] [HU-011] Actualizar `auth.effects.ts` — modificar `login$`: consumir `TokenResponseDTO`, mapear `response.user` con `mapUserProfileDTOToModel()`, despachar `loginSuccess` con `{ user, accessToken: response.accessToken, refreshToken: response.refreshToken }`; modificar `loginSuccess$`: llamar `authService.saveTokens(accessToken, refreshToken)` en vez de `saveSession(user)`, guardar user por separado; modificar `restoreSession$`: si `authService.getAccessToken()` existe, llamar `authService.me()` → mapear → despachar `loginSuccess` con tokens del storage; si falla → `logoutSuccess`; agregar `refreshToken$` (NUEVO): lee refreshToken del storage → `authService.refresh()` → despacha `refreshTokenSuccess` → en error despacha `refreshTokenFailure` y luego `sessionExpired` — `src/app/core/auth/store/auth.effects.ts`

**Checkpoint:** `ng build` → exit code 0. Store actualizado con tokens.

---

## Bloque 4 — Interceptors (ng build)

**Propósito:** Crear el interceptor de refresh automático, actualizar el auth interceptor para usar `getAccessToken()` y modificar el error interceptor para excluir URLs de auth.

- [ ] T010 [HU-010] Crear `token-refresh.interceptor.ts` — `HttpInterceptorFn` que: en caso de error 401, verifica que la URL NO sea `/auth/login` ni `/auth/refresh` (evita loops), verifica que exista refreshToken en storage, llama `AuthService.refresh()` una sola vez, si éxito → guarda nuevos tokens y reintenta la request original con nuevo accessToken, si falla → re-lanza el error 401 para que lo capture el error interceptor — `src/app/core/http/token-refresh.interceptor.ts`
- [ ] T011 [HU-009] Actualizar `auth.interceptor.ts` — cambiar `inject(AuthService).getToken()` por `inject(AuthService).getAccessToken()` — `src/app/core/http/auth.interceptor.ts`
- [ ] T012 [HU-009] Actualizar `error.interceptor.ts` — en el case 401: agregar guard que excluye URLs de auth (`req.url.includes('/auth/login')` o `/auth/refresh`); solo despachar `sessionExpired()` si NO es URL de auth (el refresh interceptor se encarga del retry) — `src/app/core/http/error.interceptor.ts`
- [ ] T013 [BLOQUEANTE] Actualizar `app.config.ts` — agregar `tokenRefreshInterceptor` en la cadena de interceptores: posición 3, después de `authInterceptor` y antes de `errorInterceptor`; orden final: `mockInterceptor` → `authInterceptor` → `tokenRefreshInterceptor` → `errorInterceptor` — `src/app/app.config.ts`

**Checkpoint:** `ng build` → exit code 0. Si backend está corriendo con `useMocks: false`: login → obtiene JWT real → /me funciona → refresh intercepta 401 y renueva.

---

## Bloque 5 — Mocks (ng build)

**Propósito:** Actualizar los datos mock para consistencia con el backend (`@gop.co`) y crear mock handlers para el interceptor de mocks, garantizando que `useMocks: true` sigue funcionando.

- [ ] T014 [P] Actualizar `auth.mock.ts` — cambiar los 4 emails de `@gop360.com` a `@gop.co`: `admin@gop.co`, `supervisor@gop.co`, `operador@gop.co`, `auditor@gop.co`; ajustar IDs a formato UUID para consistencia con backend; mantener estructura `MOCK_USERS` y función `mockLogin()` — `src/app/core/auth/auth.mock.ts`
- [ ] T015 [HU-009] [HU-011] Crear `auth.mock-handlers.ts` — array `authMockHandlers: MockHandler[]` con 2 handlers: (1) POST `/api/v1/auth/login` → valida email/password contra MOCK_USERS, retorna `HttpResponse<TokenResponseDTO>` con accessToken mock, refreshToken mock, expiresIn 1800 y user; (2) GET `/api/v1/auth/me` → extrae token del header Authorization, retorna `HttpResponse<UserProfileDTO>` con datos del usuario mockeado — `src/app/core/auth/mocks/auth.mock-handlers.ts`
- [ ] T016 Actualizar `mock.registry.ts` — importar `authMockHandlers` desde `@core/auth/mocks/auth.mock-handlers` y spread en `MOCK_HANDLERS` array — `src/app/core/http/mock.registry.ts`

**Checkpoint:** `ng build` → exit code 0. Con `useMocks: true`: login con `admin@gop.co` / `Admin123*` funciona end-to-end.

---

## Bloque 6 — Polish y Validación (ng build)

**Propósito:** Verificación transversal, corrección de imports rotos y validación de ambos modos (mock y API real).

- [ ] T017 [P] Verificar que todos los imports de `AuthUserDTO` y `mapAuthUserDTOToModel` en el codebase se actualizaron a `UserProfileDTO` y `mapUserProfileDTOToModel` — buscar en todos los archivos `*.ts`
- [ ] T018 [P] Verificar que todos los imports entre capas usan path aliases (`@core/*`, `@shared/*`, `@env/*`) y no rutas relativas largas — revisar archivos creados y modificados
- [ ] T019 [P] Verificar que ningún texto nuevo está hardcodeado en templates o componentes — todo debe venir de `locale.ts`
- [ ] T020 Ejecutar `ng build` y corregir todos los errores de compilación TypeScript — todos los archivos modificados

**Checkpoint final:** `ng build` exitoso sin warnings. Los 4 usuarios pueden autenticarse con `useMocks: true`. Si backend disponible, también con `useMocks: false`.

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

### Dependencias Dentro de Bloques

```
Bloque 1:
  T001 BLOQUEANTE (DTOs usados por todo)
  T002 depende de T001 (mapper usa UserProfileDTO)
  T003 depende de T001, T002 (barrel re-exporta todo)

Bloque 2:
  T004 independiente (api-endpoints)
  T005 depende de T004 (AuthService usa API endpoints)

Bloque 3:
  T006, T008 paralelos (actions y selectors son independientes)
  T007 depende de T006 (reducer maneja las nuevas actions)
  T009 depende de T006, T007 (effects dependen de actions y reducer shape)

Bloque 4:
  T010 independiente (nuevo interceptor, solo depende de AuthService del B2)
  T011 independiente (cambio menor en auth.interceptor)
  T012 independiente (cambio menor en error.interceptor)
  T013 depende de T010, T011, T012 (app.config registra los 3 interceptors)

Bloque 5:
  T014 independiente (solo cambia emails en mock data)
  T015 depende de T014 (mock handlers usan MOCK_USERS)
  T016 depende de T015 (registry importa handlers)

Bloque 6:
  T017, T018, T019 paralelos (verificaciones independientes)
  T020 depende de T017-T019 (build final corrige lo encontrado)
```

### Oportunidades de Paralelismo

**Bloque 1:** T001 → T002 → T003 (secuencial).

**Bloque 2:** T004 y T005 en paralelo (T005 depende de T004 pero el import de API es estático).

**Bloque 3:** T006 y T008 en paralelo → T007 → T009.

**Bloque 4:** T010, T011, T012 en paralelo → T013.

**Bloque 5:** T014 → T015 → T016 (secuencial). Puede ejecutarse en paralelo con B2-B4.

**Bloque 6:** T017, T018, T019 en paralelo → T020.

---

## Implementation Blocks (Secuencia de Ejecución)

### Bloque 1 — DTOs y Mapper

```
T001                  # auth-user.dto.ts (nuevas interfaces)
T002                  # auth-user.mapper.ts (nueva función)
T003                  # index.ts (barrel exports)
```
**Validación:** `ng build` ✅ (puede tener warnings por imports no actualizados aún)

### Bloque 2 — Service y Endpoints

```
T004                  # api-endpoints.ts
T005                  # auth.service.ts (HTTP calls reales)
```
**Validación:** `ng build` ✅

### Bloque 3 — Store

```
T006, T008            # paralelas: auth.actions.ts, auth.selectors.ts
T007                  # auth.reducer.ts
T009                  # auth.effects.ts
```
**Validación:** `ng build` ✅

### Bloque 4 — Interceptors

```
T010, T011, T012      # paralelas: token-refresh.interceptor, auth.interceptor, error.interceptor
T013                  # app.config.ts (registrar interceptor)
```
**Validación:** `ng build` ✅

### Bloque 5 — Mocks

```
T014                  # auth.mock.ts (emails @gop.co)
T015                  # auth.mock-handlers.ts (formato MockHandler)
T016                  # mock.registry.ts (registrar handlers)
```
**Validación:** `ng build` ✅

### Bloque 6 — Polish

```
T017, T018, T019      # paralelas: verificaciones de imports, aliases, textos
T020                  # ng build final + correcciones
```
**Validación:** `ng build` ✅ — Feature lista para PR.

---

## Resumen

| Bloque | Propósito | Tareas | Archivos nuevos | Archivos modificados | Verificación |
|---|---|---|---|---|---|
| B1 — DTOs | Actualizar interfaces al contrato | T001–T003 (3) | 0 | 3 | `ng build` |
| B2 — Service | HTTP calls reales | T004–T005 (2) | 0 | 2 | `ng build` |
| B3 — Store | Tokens en NgRx + refresh | T006–T009 (4) | 0 | 4 | `ng build` |
| B4 — Interceptors | Refresh automático en 401 | T010–T013 (4) | 1 | 3 | `ng build` |
| B5 — Mocks | Fallback con emails @gop.co | T014–T016 (3) | 1 | 2 | `ng build` |
| B6 — Polish | Verificación y build limpio | T017–T020 (4) | 0 | varios | `ng build` |
| **Total** | | **20 tareas** | **2 nuevos** | **14 modif** | |
