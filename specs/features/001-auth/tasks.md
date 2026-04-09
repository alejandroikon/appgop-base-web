# Tasks: Módulo de Autenticación (001-auth)

**Input**: `specs/features/001-auth/spec.md` + `specs/features/001-auth/plan.md`
**Referencia visual**: `specs/features/001-auth/ref-login.png`
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

**⚠️ BLOQUEANTE**: No iniciar Phase 3 ni 4 hasta completar esta fase íntegramente.

- [ ] T010 [P] Crear datos mock `MOCK_USERS` y función `mockLogin()` — `src/app/core/auth/auth.mock.ts`
- [ ] T011 [BLOQUEANTE] Crear acciones NgRx: `login`, `loginSuccess`, `loginFailure`, `logout`, `logoutSuccess`, `sessionExpired`, `clearAuthError` — `src/app/core/auth/store/auth.actions.ts`
- [ ] T012 [BLOQUEANTE] Crear reducer `authReducer` con `AuthState` (user, isAuthenticated, isLoading, error) — `src/app/core/auth/store/auth.reducer.ts`
- [ ] T013 [BLOQUEANTE] Crear selectores: `selectCurrentUser`, `selectIsAuthenticated`, `selectCurrentTenant`, `selectAuthIsLoading`, `selectAuthError` — `src/app/core/auth/store/auth.selectors.ts`
- [ ] T014 [BLOQUEANTE] [US2] Crear effects: `login$`, `loginSuccess$`, `logout$`, `sessionExpired$` (navegación y sessionStorage exclusivamente en effects) — `src/app/core/auth/store/auth.effects.ts`
- [ ] T015 Crear barrel export del store (exporta `authFeature` + acciones + selectores) — `src/app/core/auth/store/index.ts`
- [ ] T016 [BLOQUEANTE] Expandir `AuthService`: agregar `login()`, `clearSession()`, `saveSession()`, eliminar navegación directa del `logout()` actual — `src/app/core/auth/auth.service.ts`
- [ ] T017 [P] [BLOQUEANTE] [US2] Crear `authGuard`: protege rutas autenticadas, redirige a `/login?returnUrl=...` leyendo `selectIsAuthenticated` del store — `src/app/core/guards/auth.guard.ts`
- [ ] T018 [P] [BLOQUEANTE] Crear `noAuthGuard`: redirige a `/` si ya hay sesión activa, leyendo `selectIsAuthenticated` del store — `src/app/core/guards/no-auth.guard.ts`
- [ ] T019 [BLOQUEANTE] Registrar `authFeature` en providers globales con `provideState(authFeature)` y `provideEffects(AuthEffects)` — `src/app/app.config.ts`

**Checkpoint**: Store NgRx funcional, guards operativos, `AuthService` con mock. Verificar que `ng build` compila sin errores y la app sigue funcionando como antes (sin cambio visual).

---

## Phase 3: US1 + US2 — Login Funcional + Cierre de Sesión

**Goal**: Usuario puede autenticarse con credenciales válidas, recibir feedback de errores, ser redirigido a `/`, y el sistema maneja logout/sesión expirada con notificación contextual.

**Independent Test**:
- Navegar a `/login` → formulario split-screen visible.
- Ingresar `operador@gop360.com` / `Oper123*` → redirige a `/`.
- Ingresar credenciales inválidas → muestra error inline.
- Navegar a `/login` con sesión activa → redirige a `/`.
- Despachar `sessionExpired()` desde NgRx DevTools → redirige a `/login?reason=session_expired` con notificación.

> **Referencia visual obligatoria:** Antes de implementar cualquier tarea de esta fase, leer la imagen `specs/features/001-auth/ref-login.png` para respetar la estructura, proporciones y disposición de elementos del layout y formulario.
>
> **Orden de creación**: Los componentes se crean ANTES que las rutas, porque `auth.routes.ts` importa dinámicamente los componentes y Angular resuelve los imports en compilación (AOT).

- [ ] T020 [P] [US1] Crear `AuthLayoutComponent` (shell split-screen, lee `environment.loginBgUrl`) — `src/app/core/layout/auth-layout/auth-layout.component.ts`
- [ ] T021 [P] [US1] Crear locale de la pantalla login con todos los textos (títulos, labels, placeholders, errores, acciones) — `src/app/core/auth/features/login/locale.ts`
- [ ] T022 [US1] Crear template del layout: panel izquierdo (1/3 overlay+texto) + panel derecho (2/3 logo+outlet+versión) — `src/app/core/layout/auth-layout/auth-layout.component.html`
- [ ] T023 [US1] Crear `LoginComponent`: formulario reactivo, Signals (`showPassword`), despacho de acciones NgRx, lectura de `?reason` query param para HU-002 — `src/app/core/auth/features/login/login.component.ts`
- [ ] T024 [US1] Crear template del login: campos con íconos, toggle contraseña, enlace ¿Olvidó?, botón con estado de carga — `src/app/core/auth/features/login/login.component.html`
- [ ] T025 [US1] [BLOQUEANTE] Crear rutas del módulo auth con `AuthLayoutComponent` como shell y lazy loading de `LoginComponent` (solo ruta `/login`) — `src/app/core/auth/auth.routes.ts`
- [ ] T026 [US1] [US2] [BLOQUEANTE] Modificar rutas raíz: agregar auth routes, `authGuard` en dominios existentes, `redirectTo: 'login'` como default — `src/app/app.routes.ts`
- [ ] T027 [US2] Agregar textos de sesión expirada y notificaciones de auth al locale global — `src/app/shared/locale/locale.ts`

**Checkpoint**: Flujo completo de login funcional. Verificar los 5 escenarios de HU-001 y los 2 escenarios de HU-002 (logout + sesión expirada → notificación en `/login`).

---

## Phase 4: US3 — Recuperación de Contraseña

**Goal**: Usuario puede solicitar recuperación de contraseña y recibe pantalla de confirmación neutral independientemente de si el correo existe.

**Independent Test**: Navegar a `/forgot-password`, ingresar cualquier correo válido → muestra pantalla de confirmación. Ingresar correo inválido → error inline. Presionar "Volver" → navega a `/login`.

> **Referencia visual:** Misma estructura split-screen de `specs/features/001-auth/ref-login.png`. Solo cambia el contenido del panel derecho (ver spec §5.5).

- [ ] T028 [P] [US3] Crear locale de recuperación de contraseña (título, subtítulo, labels, acciones, confirmación) — `src/app/core/auth/features/forgot-password/locale.ts`
- [ ] T029 [US3] Agregar método `forgotPassword(email: string): Observable<void>` al `AuthService` (mock con delay simulado) — `src/app/core/auth/auth.service.ts`
- [ ] T030 [US3] Crear `ForgotPasswordComponent`: formulario reactivo, Signals (`isLoading`, `isConfirmed`), llamada a `AuthService.forgotPassword()` — `src/app/core/auth/features/forgot-password/forgot-password.component.ts`
- [ ] T031 [US3] Crear template: formulario con campo email ↔ pantalla de confirmación (renderizado condicional con `@if`) — `src/app/core/auth/features/forgot-password/forgot-password.component.html`
- [ ] T032 [US3] Agregar ruta `/forgot-password` con lazy loading de `ForgotPasswordComponent` a las rutas auth existentes — `src/app/core/auth/auth.routes.ts`

**Checkpoint**: Flujo completo de forgot-password funcional. Verificar los 4 escenarios de HU-003, incluyendo la pantalla de confirmación neutral (RN-005).

---

## Phase 5: Polish & Validación Final

**Propósito**: Verificación transversal, calidad y compilación limpia antes de marcar la feature como completada.

- [ ] T033 [P] Verificar que todos los imports entre capas usan path aliases (`@core/*`, `@shared/*`, `@env/*`) y no rutas relativas — revisar todos los archivos creados
- [ ] T034 Ejecutar `ng build` y corregir todos los errores de compilación TypeScript — `angular.json` / todos los archivos modificados
- [ ] T035 [P] Verificar que ningún texto está hardcodeado en templates o componentes (todo debe venir de `locale.ts`) — archivos `*.html` y `*.ts` de la feature

**Checkpoint final**: `ng build` exitoso sin warnings. Los 4 usuarios mock pueden autenticarse. Los 3 flujos (login, logout por sesión expirada, forgot-password) funcionan end-to-end.

---

## Dependencies & Execution Order

### Dependencias entre Fases

```
Phase 1 (Setup)       → Sin dependencias. Iniciar inmediatamente.
Phase 2 (Foundational)→ Depende de Phase 1. BLOQUEA todas las historias.
Phase 3 (US1+US2)     → Depende de Phase 2 completa.
Phase 4 (US3)         → Depende de Phase 2 (AuthService base) y Phase 3 (auth.routes.ts + AuthLayout).
Phase 5 (Polish)      → Depende de Phase 3 + 4 completas.
```

### Dependencias Críticas Dentro de Fases

```
Phase 1:
  T004 depende de T002, T003
  T005 depende de T001–T004
  T007, T008, T009 dependen de T006

Phase 2:
  T012 depende de T011
  T013 depende de T012
  T014 depende de T010, T011, T012
  T015 depende de T011–T014
  T016 depende de T010, T012
  T017, T018 dependen de T013
  T019 depende de T015

Phase 3 (componentes ANTES de rutas):
  T022 depende de T020
  T023 depende de T021
  T024 depende de T023
  T025 depende de T020, T022, T023, T024 (componentes deben existir antes que las rutas)
  T026 depende de T017, T018, T019, T025

Phase 4:
  T029 depende de T016 (mismo archivo, agregar método)
  T030 depende de T025, T028, T029
  T031 depende de T030
  T032 depende de T025, T030, T031 (componente debe existir antes de agregar ruta)
```

### Oportunidades de Paralelismo

**Phase 1**: T001, T002, T003, T006 en paralelo → luego T004 → T005 → T007/T008/T009 en paralelo.

**Phase 2**: T010 y T011 en paralelo → T012 → T013 y T016 en paralelo → T014 → T015 → T017 y T018 en paralelo → T019.

**Phase 3**: T020 y T021 en paralelo → T022 → T023 → T024 → T025 → T026 → T027 (independiente, cierra la fase).

**Phase 4**: T028 en paralelo con inicio de T029 → T030 → T031 → T032.

---

## Implementation Blocks (Secuencia de Ejecución)

Cada bloque produce una aplicación compilable (`ng build` ✅) con resultado validable.

### Bloque 1 — Modelos y Entornos (Phase 1)

```
/speckit-implement T001, T002, T003, T006    # paralelas: tipos base + interfaz entorno
/speckit-implement T004                       # mapper (depende de T002, T003)
/speckit-implement T005                       # barrel export (depende de T001–T004)
/speckit-implement T007, T008, T009           # paralelas: valores entorno (dependen de T006)
```

**Validación**: `ng build` ✅ — tipos disponibles, sin cambio visual.

### Bloque 2 — Store + Service + Guards (Phase 2)

```
/speckit-implement T010, T011                 # paralelas: mock + acciones
/speckit-implement T012                       # reducer (depende de T011)
/speckit-implement T013, T016                 # paralelas: selectores + service (dependen de T012)
/speckit-implement T014                       # effects (depende de T010, T011, T012)
/speckit-implement T015                       # barrel store (depende de T011–T014)
/speckit-implement T017, T018                 # paralelas: guards (dependen de T013)
/speckit-implement T019                       # registrar en app.config (depende de T015)
```

**Validación**: `ng build` ✅ — AuthState visible en NgRx DevTools. App funciona igual que antes (sin cambio visual).

### Bloque 3 — Login Funcional + Logout (Phase 3)

```
/speckit-implement T020, T021                 # paralelas: AuthLayout.ts + login locale
/speckit-implement T022                       # AuthLayout.html (depende de T020)
/speckit-implement T023                       # LoginComponent.ts (depende de T021)
/speckit-implement T024                       # LoginComponent.html (depende de T023)
/speckit-implement T025                       # auth.routes.ts — solo /login (depende de T020–T024)
/speckit-implement T026                       # app.routes.ts — guards + redirect (depende de T025)
/speckit-implement T027                       # locale global — textos session expired
```

**Validación**: `ng build` ✅ — Login visible en `/login`. Credenciales mock funcionan. Session expired muestra notificación.

### Bloque 4 — Forgot Password (Phase 4)

```
/speckit-implement T028, T029                 # paralelas: locale + service method
/speckit-implement T030                       # ForgotPasswordComponent.ts (depende de T028, T029)
/speckit-implement T031                       # ForgotPasswordComponent.html (depende de T030)
/speckit-implement T032                       # agregar ruta /forgot-password (depende de T030, T031)
```

**Validación**: `ng build` ✅ — Flujo forgot-password completo desde enlace "¿Olvidó su contraseña?" en login.

### Bloque 5 — Polish (Phase 5)

```
/speckit-implement T033, T035                 # paralelas: aliases + textos hardcodeados
/speckit-implement T034                       # ng build final + correcciones
```

**Validación**: `ng build` ✅ — Todos los flujos end-to-end. Feature lista para PR.

---

## Implementation Strategy

### MVP (Solo US1 — Login funcional)

1. Completar Bloque 1 (Modelos y Entornos)
2. Completar Bloque 2 (Store + Service + Guards) ← **crítico**
3. Completar Bloque 3 (Login + Logout)
4. **VALIDAR**: Los 5 escenarios de HU-001 + 2 de HU-002 funcionan. `ng build` limpio.

### Entrega Incremental Completa

1. Bloque 1 + 2 → Base lista
2. Bloque 3 → Login + Logout funcional → **Demo MVP**
3. Bloque 4 → Forgot-password funcional → **Demo completo**
4. Bloque 5 → Validación final → **Feature lista para PR**

---

## Resumen

| Fase | Historia | Tareas | Paralelas | Archivos |
|---|---|---|---|---|
| Phase 1 — Setup | — | T001–T009 | 8/9 | 5 nuevos + 4 modif. |
| Phase 2 — Foundational | US1+US2 base | T010–T019 | 4/10 | 8 nuevos + 2 modif. |
| Phase 3 — Login+Logout | US1+US2 | T020–T027 | 2/8 | 6 nuevos + 2 modif. |
| Phase 4 — ForgotPw | US3 | T028–T032 | 1/5 | 3 nuevos + 2 modif. |
| Phase 5 — Polish | — | T033–T035 | 2/3 | — |
| **Total** | | **35 tareas** | **17 paralelas** | **22 nuevos + 10 modif.** |