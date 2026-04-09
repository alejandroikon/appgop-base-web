# Plan: Módulo de Autenticación (Auth)

**Feature ID:** 001-auth
**Spec de referencia:** `specs/features/001-auth/spec.md`
**Referencia visual:** `specs/features/001-auth/ref-login.png`
**Estado:** Listo para implementación

---

## 1. Resumen Arquitectónico

Este módulo implementa el ciclo completo de autenticación del prototipo GOP 360°. Dado que la sesión del usuario es estado **global** que cruza todos los dominios (guards en todas las rutas, TopHeader, audit logs, RBAC), se utiliza **NgRx** para gestionar el estado de sesión. La lógica de UI local (loading, show/hide password, estado de confirmación) se gestiona con **Signals**.

Las páginas de login y recuperación de contraseña se consideran funcionalidades del sistema de autenticación (`core/auth/`) y no de los dominios de negocio (`domains/`). El layout visual split-screen se construye en `core/layout/auth-layout/`.

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
src/
├── app/
│   ├── core/
│   │   ├── auth/
│   │   │   ├── auth.mock.ts                                   # Datos simulados de usuarios (mock RBAC)
│   │   │   ├── auth.routes.ts                                 # Rutas lazy del módulo auth (/login, /forgot-password)
│   │   │   ├── store/
│   │   │   │   ├── auth.actions.ts                            # Acciones NgRx: login, loginSuccess, loginFailure, logout, sessionExpired
│   │   │   │   ├── auth.reducer.ts                            # Reducer: AuthState shape y transiciones
│   │   │   │   ├── auth.selectors.ts                          # Selectores: selectIsAuthenticated, selectCurrentUser, selectAuthError
│   │   │   │   ├── auth.effects.ts                            # Effects: login$, loginSuccess$, logout$, sessionExpired$
│   │   │   │   └── index.ts                                   # Barrel: exporta feature state + todos los artefactos del store
│   │   │   └── features/
│   │   │       ├── login/
│   │   │       │   ├── locale.ts                              # Textos exclusivos de la pantalla de login
│   │   │       │   ├── login.component.ts                     # Smart component: formulario reactivo + despacho de acciones NgRx
│   │   │       │   └── login.component.html                   # Template split-screen: conecta al AuthLayoutComponent (panel derecho)
│   │   │       └── forgot-password/
│   │   │           ├── locale.ts                              # Textos exclusivos de la pantalla de recuperación
│   │   │           ├── forgot-password.component.ts           # Smart component: formulario reactivo + estado de confirmación (Signal)
│   │   │           └── forgot-password.component.html         # Template: formulario ↔ pantalla de confirmación (renderizado condicional)
│   │   ├── guards/
│   │   │   ├── auth.guard.ts                                  # Protege rutas autenticadas: redirige a /login si no hay sesión activa
│   │   │   └── no-auth.guard.ts                               # Protege rutas públicas: redirige a / si ya hay sesión activa
│   │   └── layout/
│   │       └── auth-layout/
│   │           ├── auth-layout.component.ts                   # Contenedor split-screen (layout shell sin Sidebar ni TopHeader)
│   │           └── auth-layout.component.html                 # Template: panel izquierdo (marca) + panel derecho (<router-outlet>)
│   └── shared/
│       └── models/
│           ├── user-role.model.ts                             # Type UserRole: 'ADMIN' | 'SUPERVISOR' | 'OPERADOR' | 'AUDITOR'
│           ├── auth-user.model.ts                             # Interface AuthUser: id, email, name, role
│           ├── auth-user.dto.ts                               # Interface AuthUserDTO (contrato API futuro) + LoginCredentials
│           ├── auth-user.mapper.ts                            # mapAuthUserDTOToModel(): función pura de transformación
│           └── index.ts                                       # Barrel export del grupo de modelos
```

### 2.2. Archivos a MODIFICAR

```
src/
├── app/
│   ├── app.routes.ts                                          # + rutas auth, + authGuard en rutas existentes, + redirectTo /login
│   ├── app.config.ts                                          # + provideState(authFeature) para registrar el feature state NgRx
│   └── core/
│       └── auth/
│           └── auth.service.ts                                # + mock login/forgotPassword, + dispatch NgRx, + lógica de sesión completa
└── environments/
    ├── environment.interface.ts                               # + campo loginBgUrl: string
    ├── environment.ts                                         # + loginBgUrl: 'assets/images/login-bg.jpg' (placeholder local)
    ├── environment.qa.ts                                      # + loginBgUrl con URL de QA
    └── environment.prod.ts                                    # + loginBgUrl con URL de producción
```

---

## 3. Árbol de Componentes

```
AuthLayoutComponent  [core/layout/auth-layout/]   ← Shell layout (Dumb - recibe solo configuración)
├── Panel Izquierdo (1/3) — renderizado directo en template, sin componente hijo
│   └── [image bg desde environment.loginBgUrl] + overlay + línea dorada + título + subtexto
└── Panel Derecho (2/3) — <router-outlet>
    ├── LoginComponent         [core/auth/features/login/]        ← Smart
    └── ForgotPasswordComponent [core/auth/features/forgot-password/] ← Smart
```

**Smart vs Dumb:**
- `AuthLayoutComponent` → Dumb. Recibe la URL de la imagen como `input()` desde el entorno, renderiza la estructura fija.
- `LoginComponent` → Smart. Lee estado NgRx, despacha acciones, gestiona el formulario reactivo.
- `ForgotPasswordComponent` → Smart. Llama al `AuthService`, gestiona el Signal de confirmación.

---

## 4. Diseño del Estado NgRx (Auth Feature State)

### 4.1. Shape del Estado

```typescript
// core/auth/store/auth.reducer.ts
interface AuthState {
  user:            AuthUser | null;   // Usuario autenticado; null si no hay sesión
  isAuthenticated: boolean;           // true cuando user !== null y token válido
  isLoading:       boolean;           // true durante el proceso de login
  error:           string | null;     // Mensaje de error de credenciales (HU-001 Esc.2)
}

const initialState: AuthState = {
  user:            null,
  isAuthenticated: false,
  isLoading:       false,
  error:           null,
};
```

### 4.2. Acciones

```typescript
// core/auth/store/auth.actions.ts
login({ credentials: LoginCredentials })     // Iniciado por LoginComponent al enviar el formulario
loginSuccess({ user: AuthUser })             // Emitido por el Effect tras login exitoso en el mock
loginFailure({ error: string })              // Emitido por el Effect tras credenciales inválidas
logout()                                     // Iniciado por el botón "Cerrar Sesión" (TopHeader - feature futura)
logoutSuccess()                              // Emitido tras limpiar la sesión
sessionExpired()                             // Emitido por el error.interceptor (401) o guard al detectar token expirado
clearAuthError()                             // Emitido al enfocar los campos tras un error (limpia el mensaje inline)
```

### 4.3. Effects y flujo de datos

```
LoginComponent.submit()
  → dispatch(login({ credentials }))
    → [auth.effects.ts] login$
        → authService.login(credentials)         ← llamada al mock
            ✓ éxito  → dispatch(loginSuccess({ user }))
                         → [loginSuccess$] sessionStorage.setItem('token') + navigate('/')
            ✗ error  → dispatch(loginFailure({ error: 'Correo o contraseña incorrectos...' }))
                         → LoginComponent muestra error inline vía selector selectAuthError

logout() / sessionExpired()
  → [auth.effects.ts] logout$ / sessionExpired$
      → authService.clearSession()               ← limpia sessionStorage
      → navigate('/login') [+ ?reason= si sessionExpired]
      → dispatch(logoutSuccess())
```

### 4.4. Selectores

```typescript
// core/auth/store/auth.selectors.ts
selectAuthState         // feature state raíz
selectCurrentUser       // AuthUser | null — incluye tenantId y tenantName
selectIsAuthenticated   // boolean
selectCurrentTenant     // { tenantId: string; tenantName: string } | null — para consumo en dominios futuros
selectAuthIsLoading     // boolean — controla el estado Enviando del botón
selectAuthError         // string | null — controla el mensaje de error inline
```

---

## 5. Diseño de Formularios Reactivos

### 5.1. Login Form

```typescript
// Definido en LoginComponent con ReactiveFormsModule
loginForm = new FormGroup({
  email:    new FormControl('', { validators: [Validators.required, Validators.email],    nonNullable: true }),
  password: new FormControl('', { validators: [Validators.required],                      nonNullable: true }),
});
```

**Reglas de negocio aplicadas en el componente (no en el formulario):**
- Trim de email antes de dispatch (RN-001, EC-001, EC-003)
- Normalización a minúsculas del email antes de dispatch (RN-001, EC-008)
- Deshabilitar formulario durante el estado `isLoading` (EC-007 — evita doble submit)

**Signals de UI local:**
```typescript
showPassword = signal(false);   // toggle show/hide contraseña (§5.6 spec)
```

### 5.2. Forgot Password Form

```typescript
forgotForm = new FormGroup({
  email: new FormControl('', { validators: [Validators.required, Validators.email], nonNullable: true }),
});
```

**Signals de UI local:**
```typescript
isLoading    = signal(false);   // estado Enviando del botón
isConfirmed  = signal(false);   // true → reemplaza el formulario por el mensaje de confirmación (§5.5 spec)
```

---

## 6. Diseño del Auth Layout (Split-Screen)

> **Referencia visual obligatoria:** Ver `specs/features/001-auth/ref-login.png` para estructura, proporciones y disposición exacta de elementos.

El `AuthLayoutComponent` es un contenedor estructural sin lógica de negocio. Implementa la proporción 1/3 — 2/3 usando clases de utilidad CSS.

```
AuthLayoutComponent
  ├── <div class="auth-panel-left">         ← 1/3, oculto en móvil
  │   ├── [img background via CSS]          ← background-image desde environment.loginBgUrl
  │   ├── <div class="overlay">             ← color azul navy + opacidad
  │   ├── <span class="accent-line">        ← línea dorada horizontal
  │   ├── <h1>"Bienvenido a GOP 360°"</h1>
  │   └── <p>subtexto descriptivo</p>
  └── <div class="auth-panel-right">        ← 2/3, 100% en móvil
      ├── <img src="assets/icons/login-logo.png"> ← zona superior
      ├── <router-outlet />                 ← zona central (LoginComponent o ForgotPasswordComponent)
      └── <span>"GOP 360° v1.0.0"</span>   ← pie del panel
```

La URL de la imagen de fondo se inyecta en el componente leyendo `environment.loginBgUrl` a través de la función `inject()` de `@env/environment`.

---

## 7. Diseño de Guards

### 7.1. `auth.guard.ts` — Protege rutas autenticadas

```typescript
// Lógica: lee selectIsAuthenticated del store NgRx
// Si false → redirige a /login?returnUrl=<ruta-actual>  (EC-005)
// Si true  → permite el acceso
```

### 7.2. `no-auth.guard.ts` — Protege rutas públicas

```typescript
// Lógica: lee selectIsAuthenticated del store NgRx
// Si true  → redirige a /  (HU-001 Esc.5)
// Si false → permite el acceso a /login o /forgot-password
```

**Regla CONSTITUTION §10:** Los guards operan sobre el estado NgRx, nunca leen `sessionStorage` directamente. El store es la única fuente de verdad para la UI.

---

## 8. Diseño de Modelos y Mock

### 8.1. Modelos en `shared/models/` (usados por guards, core y dominios futuros)

```typescript
// user-role.model.ts
export type UserRole = 'ADMIN' | 'SUPERVISOR' | 'OPERADOR' | 'AUDITOR';

// auth-user.model.ts  — modelo del frontend (camelCase)
export interface AuthUser {
  id:         string;
  email:      string;
  name:       string;
  role:       UserRole;
  tenantId:   string;    // ID único del operador/empresa al que pertenece el usuario
  tenantName: string;    // Nombre legible del operador (ej. "Ecopetrol S.A.") — usado en UI de otros módulos
}

// auth-user.dto.ts  — contrato de API futuro (snake_case) + request DTO
export interface AuthUserDTO {
  user_id:     string;
  email:       string;
  full_name:   string;
  role_code:   string;
  tenant_id:   string;   // Identificador del operador en el backend
  tenant_name: string;   // Nombre del operador
}
export interface LoginCredentials {
  email:    string;
  password: string;
}

// auth-user.mapper.ts
export function mapAuthUserDTOToModel(dto: AuthUserDTO): AuthUser { ... }
```

> **Nota de diseño:** `tenantId` y `tenantName` se incluyen en `AuthUser` porque múltiples dominios futuros los necesitan (ej. `wells/` filtra pozos por operador, `operations/` filtra formas por empresa). Al vivir en el `AuthState` de NgRx, cualquier dominio puede leerlos mediante selector sin hacer una llamada adicional.

### 8.2. Mock en `core/auth/auth.mock.ts` (privado del módulo)

Contiene el array `MOCK_USERS` con los 4 perfiles definidos en la spec §4, ampliados con los datos de tenant:

| email | tenantId | tenantName |
|---|---|---|
| `admin@gop360.com` | `tenant-anh` | `Agencia Nacional de Hidrocarburos` |
| `supervisor@gop360.com` | `tenant-ecopetrol` | `Ecopetrol S.A.` |
| `operador@gop360.com` | `tenant-ecopetrol` | `Ecopetrol S.A.` |
| `auditor@gop360.com` | `tenant-anh` | `Agencia Nacional de Hidrocarburos` |

La función `mockLogin(credentials)` aplica:
- Normalización a minúsculas del email (RN-001)
- Comparación case-sensitive de la contraseña (RN-002)
- Retorna `Observable<AuthUser>` (incluyendo `tenantId` y `tenantName`) o `throwError` para integrarse con los Effects sin romper el patrón de servicios

---

## 9. Modificaciones a Archivos Existentes

### 9.1. `app.routes.ts`

```typescript
// ANTES: redirectTo 'wells' por defecto, sin auth ni guards
// DESPUÉS:
{
  path: 'login',
  canActivate: [noAuthGuard],
  component: AuthLayoutComponent,
  children: [{ path: '', loadComponent: () => LoginComponent }]
},
{
  path: 'forgot-password',
  canActivate: [noAuthGuard],
  component: AuthLayoutComponent,
  children: [{ path: '', loadComponent: () => ForgotPasswordComponent }]
},
{ path: 'wells',       canActivate: [authGuard], loadChildren: () => wellsRoutes },
{ path: 'operations',  canActivate: [authGuard], loadChildren: () => operationsRoutes },
{ path: 'production',  canActivate: [authGuard], loadChildren: () => productionRoutes },
{ path: 'admin',       canActivate: [authGuard], loadChildren: () => adminRoutes },
{ path: '',            redirectTo: 'login', pathMatch: 'full' },
{ path: '**',          redirectTo: 'login' },
```

### 9.2. `app.config.ts`

Agregar `provideState(authFeature)` al array de providers para registrar el feature state NgRx del módulo de autenticación.

### 9.3. `auth.service.ts`

Ampliar con:
- `login(credentials: LoginCredentials): Observable<AuthUser>` → delega a `mockLogin()`, sin manejo de errores (los errores los captura el Effect y los despacha como `loginFailure`)
- `forgotPassword(email: string): Observable<void>` → mock que retorna `of(void)` tras 800ms simulados
- `clearSession(): void` → limpia `sessionStorage` y despacha `logoutSuccess()` al store
- Eliminar el `this.router.navigate` del `logout()` actual — la navegación es responsabilidad del Effect, no del servicio

### 9.4. `environment.interface.ts`

```typescript
// Agregar campo:
loginBgUrl: string;   // URL de la imagen de fondo del panel izquierdo del login
```

Agregar el valor en `environment.ts`, `environment.qa.ts` y `environment.prod.ts`. El valor por defecto para desarrollo apunta a `assets/images/login-bg.jpg` (imagen placeholder a agregar en assets).

---

## 10. Locale

### `core/auth/features/login/locale.ts`

```typescript
export const LOGIN_LOCALE = {
  title:        'Bienvenido de nuevo',
  subtitle:     'Ingresa a tu cuenta para continuar',
  fields: {
    email:       'CORREO ELECTRÓNICO',
    password:    'CONTRASEÑA',
    emailPlaceholder: 'usuario@dominio.com',
  },
  actions: {
    submit:      'Iniciar Sesión',
    submitting:  'Ingresando...',
    forgotPassword: '¿Olvidó su contraseña?',
    showPassword:   'Mostrar contraseña',
    hidePassword:   'Ocultar contraseña',
  },
  errors: {
    required:      'Este campo es requerido',
    emailInvalid:  'Ingrese un correo electrónico válido',
    credentials:   'Correo o contraseña incorrectos. Verifique sus datos.',
    noConnection:  'Sin conexión. Verifica tu red e intenta de nuevo.',
  },
} as const;
```

### `core/auth/features/forgot-password/locale.ts`

```typescript
export const FORGOT_PASSWORD_LOCALE = {
  title:            'Recuperar contraseña',
  subtitle:         'Ingresa tu correo registrado y te enviaremos las instrucciones.',
  fields: {
    email:           'CORREO ELECTRÓNICO',
  },
  actions: {
    submit:          'Enviar enlace de recuperación',
    submitting:      'Enviando...',
    backToLogin:     'Volver al inicio de sesión',
  },
  errors: {
    required:        'Este campo es requerido',
    emailInvalid:    'Ingrese un correo electrónico válido',
  },
  confirmation: {
    message:         'Si el correo está registrado, recibirás un enlace de recuperación en los próximos minutos.',
    action:          'Volver al inicio de sesión',
  },
} as const;
```

---

## 11. Dependencias entre Archivos (Orden de Implementación)

```
Fase 1 — Modelos y tipos base (sin dependencias entre sí)
  shared/models/user-role.model.ts
  shared/models/auth-user.dto.ts
  shared/models/auth-user.model.ts
  shared/models/auth-user.mapper.ts
  shared/models/index.ts
  environment.interface.ts (+ environment.ts, .qa.ts, .prod.ts)

Fase 2 — Estado NgRx y servicio (dependen de los modelos)
  core/auth/auth.mock.ts             ← depende de AuthUser, UserRole
  core/auth/store/auth.actions.ts    ← depende de LoginCredentials, AuthUser
  core/auth/store/auth.reducer.ts    ← depende de auth.actions
  core/auth/store/auth.selectors.ts  ← depende de auth.reducer
  core/auth/store/auth.effects.ts    ← depende de auth.actions + AuthService
  core/auth/store/index.ts
  core/auth/auth.service.ts (MODIFY) ← depende de auth.mock + AuthUser

Fase 3 — Guards (dependen del store)
  core/guards/auth.guard.ts          ← depende de auth.selectors
  core/guards/no-auth.guard.ts       ← depende de auth.selectors

Fase 4 — Layout y configuración (dependen de guards y store)
  core/layout/auth-layout/auth-layout.component.ts   ← depende de environment
  core/layout/auth-layout/auth-layout.component.html
  app.config.ts (MODIFY)             ← provideState(authFeature)
  app.routes.ts (MODIFY)             ← depende de guards + AuthLayoutComponent + features

Fase 5 — Pantallas (dependen de todo lo anterior)
  core/auth/features/login/locale.ts
  core/auth/features/login/login.component.ts        ← depende de store + locale + form
  core/auth/features/login/login.component.html
  core/auth/features/forgot-password/locale.ts
  core/auth/features/forgot-password/forgot-password.component.ts
  core/auth/features/forgot-password/forgot-password.component.html
```

---

## 12. Restricciones de Implementación (CONSTITUTION)

| Regla | Aplicación en este módulo |
|---|---|
| Standalone Components | `AuthLayoutComponent`, `LoginComponent`, `ForgotPasswordComponent` son standalone. Sin `NgModule`. |
| `ChangeDetectionStrategy.OnPush` | Obligatorio en los 3 componentes. |
| `inject()` en lugar de constructor | Todos los servicios y store se inyectan con `inject()`. |
| Control flow nativo | `@if` para renderizado condicional (show/hide password, formulario vs confirmación). |
| Signals para estado local | `showPassword`, `isLoading`, `isConfirmed` son Signals. |
| NgRx para estado global | `AuthState` (user, isAuthenticated, isLoading, error) vive en el store. |
| Sin manejo de errores en servicios | `AuthService.login()` no usa `catchError`. Los errores los despacha el Effect como `loginFailure`. |
| Locale obligatorio | Cero textos hardcodeados. Todos los strings en `LOGIN_LOCALE` o `FORGOT_PASSWORD_LOCALE`. |
| Path aliases | Todos los imports entre capas usan `@core/*`, `@shared/*`, `@env/*`. |
| Seguridad — token en sessionStorage | Token almacenado en `sessionStorage` (se limpia al cerrar el tab). Nunca en `localStorage`. |
| Auth module no importa de `domains/` | Verificado: modelos y guards en `core/` y `shared/` exclusivamente. |
