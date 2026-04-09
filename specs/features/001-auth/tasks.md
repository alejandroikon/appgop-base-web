# Tasks: Módulo de Autenticación (001-auth)

**Input**: `specs/features/001-auth/spec.md` + `specs/features/001-auth/plan.md`
**Referencia arquitectónica**: `CONSTITUTION.md`

**Formato**: `[ID] [P?] [Story?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — no tiene dependencias de tareas incompletas en la misma fase
- **[US1]**: HU-001 Inicio de Sesión | **[US2]**: HU-002 Cierre de Sesión | **[US3]**: HU-003 Recuperación de Contraseña
- **[BLOQUEANTE]**: Ninguna historia puede avanzar sin que esta tarea esté completada

---

## Phase 1: Setup — Modelos Compartidos y Entorno

**Propósito**: Definir los tipos base (`AuthUser`, `UserRole`, `LoginCredentials`) y extender la configuración de entornos. Sin dependencias entre sí — todas paralelas.

- [ ] T001 [P] Crear type `UserRole` — `src/app/shared/models/user-role.model.ts`
- [ ] T002 [P] Crear interface `AuthUser` (incluye `tenantId`, `tenantName`) — `src/app/shared/models/auth-user.model.ts`
- [ ] T003 [P] Crear interfaces `AuthUserDTO` y `LoginCredentials` — `src/app/shared/models/auth-user.dto.ts`
- [ ] T004 Crear función pura `mapAuthUserDTOToModel` — `src/app/shared/models/auth-user.mapper.ts`
- [ ] T005 Crear barrel export del grupo de modelos — `src/app/shared/models/index.ts`
- [ ] T006 [P] Agregar campo `loginBgUrl: string` a la interfaz de entorno — `src/environments/environment.interface.ts`
- [ ] T007 [P] Agregar valor `loginBgUrl` al entorno de desarrollo — `src/environments/environment.ts`
- [ ] T008 [P] Agregar valor `loginBgUrl` al entorno QA — `src/environments/environment.qa.ts`
- [ ] T009 [P] Agregar valor `loginBgUrl` al entorno producción — `src/environments/environment.prod.ts`

**Checkpoint**: Modelos tipados disponibles y configuración de entornos completa. Verificar que `ng build` no arroja errores.

---

## Phase 2: Foundational — NgRx Auth Store + AuthService + Guards

**Propósito**: Infraestructura de sesión global que TODAS las historias requieren. Ningún componente puede implementarse sin que esta fase esté completa.

**⚠️ BLOQUEANTE**: No iniciar Phase 3, 4 ni 5 hasta completar esta fase íntegramente.

- [ ] T010 [P] Crear datos mock `MOCK_USERS` y función `mockLogin()` — `src/app/core/auth/auth.mock.ts`
- [ ] T011 [BLOQUEANTE] Crear acciones NgRx: `login`, `loginSuccess`, `loginFailure`, `logout`, `logoutSuccess`, `sessionExpired`, `clearAuthError` — `src/app/core/auth/store/auth.actions.ts`
- [ ] T012 [BLOQUEANTE] Crear reducer `authReducer` con `AuthState` (user, isAuthenticated, isLoading, error) — `src/app/core/auth/store/auth.reducer.ts`
- [ ] T013 [BLOQUEANTE] Crear selectores: `selectCurrentUser`, `selectIsAuthenticated`, `selectCurrentTenant`, `selectAuthIsLoading`, `selectAuthError` — `src/app/core/auth/store/auth.selectors.ts`
- [ ] T014 [BLOQUEANTE] [US2] Crear effects: `login$`, `loginSuccess$`, `logout$`, `sessionExpired$` (navegación y sessionStorage exclusivamente en effects) — `src/app/core/auth/store/auth.effects.ts`
- [ ] T015 Crear barrel export del store (exporta `authFeature` + acciones + selectores) — `src/app/core/auth/store/index.ts`
- [ ] T016 [BLOQUEANTE] Expandir `AuthService`: agregar `login()`, `clearSession()`, `saveSession()`, eliminar navegación directa del `logout()` actual — `src/app/core/auth/auth.service.ts`
- [ ] T017 [P] [BLOQUEANTE] [US2] Crear `authGuard`: protege rutas autenticadas, redirige a `/login?returnUrl=...` leyendo `selectIsAuthenticated` del store — `src/app/core/guards/auth.guard.ts`
- [ ] T018 [P] [BLOQUEANTE] Crear `noAuthGuard`: redirige a `/` si ya hay sesión activa, leyendo `selectIsAuthenticated` del store — `src/app/core/guards/no-auth.guard.ts`
- [ ] T019 [BLOQUEANTE] Registrar `authFeature` en providers globales con `provideState(authFeature)` — `src/app/app.config.ts`

**Checkpoint**: Store NgRx funcional, guards operativos, `AuthService` con mock. Verificar que `ng build` compila sin errores.

---

## Phase 3: US1 — Inicio de Sesión (Login)

**Goal**: Usuario puede autenticarse con credenciales válidas, recibir feedback de errores y ser redirigido a `/`.

**Independent Test**: Navegar a `/login`, ingresar `operador@gop360.com` / `Oper123*` → redirige a `/`. Ingresar credenciales inválidas → muestra error inline. Navegar a `/login` con sesión activa → redirige a `/`.

- [ ] T020 [P] [US1] Crear `AuthLayoutComponent` (shell split-screen, lee `environment.loginBgUrl`) — `src/app/core/layout/auth-layout/auth-layout.component.ts`
- [ ] T021 [P] [US1] Crear template del layout: panel izquierdo (1/3 overlay+texto) + panel derecho (2/3 logo+outlet+versión) — `src/app/core/layout/auth-layout/auth-layout.component.html`
- [ ] T022 [US1] [BLOQUEANTE] Crear rutas del módulo auth con `AuthLayoutComponent` como shell y lazy loading de páginas hijas — `src/app/core/auth/auth.routes.ts`
- [ ] T023 [US1] [BLOQUEANTE] Modificar rutas raíz: agregar auth routes, `authGuard` en dominios existentes, `redirectTo: 'login'` como default — `src/app/app.routes.ts`
- [ ] T024 [P] [US1] Crear locale de la pantalla login con todos los textos (títulos, labels, placeholders, errores, acciones) — `src/app/core/auth/features/login/locale.ts`
- [ ] T025 [US1] Crear `LoginComponent`: formulario reactivo, Signals (`showPassword`), despacho de acciones NgRx, lectura de `?reason` query param para HU-002 — `src/app/core/auth/features/login/login.component.ts`
- [ ] T026 [US1] Crear template del login: campos con íconos, toggle contraseña, enlace ¿Olvidó?, botón con estado de carga — `src/app/core/auth/features/login/login.component.html`

**Checkpoint**: Flujo completo de login funcional. Verificar los 5 escenarios de HU-001 y el escenario 2 de HU-002 (sesión expirada → toast en `/login`).

---

## Phase 4: US2 — Cierre de Sesión

**Goal**: El store limpia la sesión, el guard detecta expiración y el usuario recibe feedback contextual en la pantalla de login.

**Independent Test**: Con sesión activa, despachar `logout()` desde devtools NgRx → sessionStorage vacío, redirige a `/login`. Simular `sessionExpired()` → redirige a `/login?reason=session_expired` y aparece notificación.

> **Nota**: Los effects `logout$` y `sessionExpired$` fueron implementados en T014 (Phase 2) y el guard de expiración en T017. Esta fase agrega únicamente los textos de notificación globales para cerrar la historia.

- [ ] T027 [US2] Agregar textos de sesión expirada y notificaciones de auth al locale global — `src/app/shared/locale/locale.ts`

**Checkpoint**: Despachar `logout()` y `sessionExpired()` produce la navegación y notificaciones correctas según HU-002.

---

## Phase 5: US3 — Recuperación de Contraseña

**Goal**: Usuario puede solicitar recuperación de contraseña y recibe pantalla de confirmación neutral independientemente de si el correo existe.

**Independent Test**: Navegar a `/forgot-password`, ingresar cualquier correo válido → muestra pantalla de confirmación. Ingresar correo inválido → error inline. Presionar "Volver" → navega a `/login`.

- [ ] T028 [P] [US3] Crear locale de recuperación de contraseña (título, subtítulo, labels, acciones, confirmación) — `src/app/core/auth/features/forgot-password/locale.ts`
- [ ] T029 [US3] Agregar método `forgotPassword(email: string): Observable<void>` al `AuthService` (mock con delay simulado) — `src/app/core/auth/auth.service.ts`
- [ ] T030 [US3] Crear `ForgotPasswordComponent`: formulario reactivo, Signals (`isLoading`, `isConfirmed`), llamada a `AuthService.forgotPassword()` — `src/app/core/auth/features/forgot-password/forgot-password.component.ts`
- [ ] T031 [US3] Crear template: formulario con campo email ↔ pantalla de confirmación (renderizado condicional con `@if`) — `src/app/core/auth/features/forgot-password/forgot-password.component.html`

**Checkpoint**: Flujo completo de forgot-password funcional. Verificar los 4 escenarios de HU-003, incluyendo la pantalla de confirmación neutral (RN-005).

---

## Phase 6: Polish & Validación Final

**Propósito**: Verificación transversal, calidad y compilación limpia antes de marcar la feature como completada.

- [ ] T032 [P] Verificar que todos los imports entre capas usan path aliases (`@core/*`, `@shared/*`, `@env/*`) y no rutas relativas — revisar todos los archivos creados
- [ ] T033 Ejecutar `ng build` y corregir todos los errores de compilación TypeScript — `angular.json` / todos los archivos modificados
- [ ] T034 [P] Verificar que ningún texto está hardcodeado en templates o componentes (todo debe venir de `locale.ts`) — archivos `*.html` y `*.ts` de la feature

**Checkpoint final**: `ng build` exitoso sin warnings. Los 4 usuarios mock pueden autenticarse. Los 3 flujos (login, logout por sesión expirada, forgot-password) funcionan end-to-end.

---

## Dependencies & Execution Order

### Dependencias entre Fases

```
Phase 1 (Setup)       → Sin dependencias. Iniciar inmediatamente.
Phase 2 (Foundational)→ Depende de Phase 1. BLOQUEA todas las historias.
Phase 3 (US1 Login)   → Depende de Phase 2 completa.
Phase 4 (US2 Logout)  → Depende de Phase 2 (effects/guard ya implementados) y Phase 3 (locale global).
Phase 5 (US3 ForgotPw)→ Depende de Phase 2 (AuthService base) y Phase 3 (auth.routes.ts + AuthLayout).
Phase 6 (Polish)      → Depende de Phase 3 + 4 + 5 completas.
```

### Dependencias Críticas Dentro de Fases

```
T004 depende de T002, T003
T005 depende de T001–T004
T007, T008, T009 dependen de T006
T012 depende de T011
T013 depende de T012
T014 depende de T010, T011, T012
T015 depende de T011–T014
T016 depende de T010, T012
T017, T018 dependen de T013
T019 depende de T015
T021 depende de T020
T022 depende de T017, T018, T020
T023 depende de T019, T022
T025 depende de T022, T024
T026 depende de T025
T029 depende de T016 (mismo archivo, agregar método)
T030 depende de T022, T028, T029
T031 depende de T030
```

### Oportunidades de Paralelismo

**Phase 1**: T001, T002, T003, T006 en paralelo → luego T004 → T005 → T007/T008/T009 en paralelo.

**Phase 2**: T010 y T011 en paralelo → T012 → T013 y T016 en paralelo → T014 → T015 → T017 y T018 en paralelo → T019.

**Phase 3**: T020/T021 y T024 en paralelo (una vez Phase 2 completa) → T022 → T023 → T025 → T026.

**Phase 5**: T028 en paralelo con inicio de T029 → T030 → T031.

---

## Implementation Strategy

### MVP (Solo US1 — Login funcional)

1. Completar Phase 1 (Setup)
2. Completar Phase 2 (Foundational) ← **crítico**
3. Completar Phase 3 (US1 Login)
4. **VALIDAR**: Los 5 escenarios de HU-001 funcionan. `ng build` limpio.

### Entrega Incremental Completa

1. Phase 1 + 2 → Base lista
2. Phase 3 → Login funcional → **Demo MVP**
3. Phase 4 → Logout/sesión expirada funcional
4. Phase 5 → Forgot-password funcional → **Demo completo**
5. Phase 6 → Validación final → **Feature lista para PR**

---

## Resumen

| Fase | Historia | Tareas | Paralelas | Archivos |
|---|---|---|---|---|
| Phase 1 — Setup | — | T001–T009 | 8/9 | 9 (5 nuevos + 4 modif.) |
| Phase 2 — Foundational | US1+US2 base | T010–T019 | 4/10 | 10 (7 nuevos + 3 modif.) |
| Phase 3 — Login | US1 | T020–T026 | 3/7 | 7 (6 nuevos + 1 modif.) |
| Phase 4 — Logout | US2 | T027 | 0/1 | 1 (modif.) |
| Phase 5 — ForgotPw | US3 | T028–T031 | 1/4 | 4 (3 nuevos + 1 modif.) |
| Phase 6 — Polish | — | T032–T034 | 2/3 | — |
| **Total** | | **34 tareas** | **18 paralelas** | **21 nuevos + 9 modif.** |
