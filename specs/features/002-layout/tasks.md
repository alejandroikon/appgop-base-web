# Tasks: Layout Base — Sidebar + TopHeader (002-layout)

**Input**: `specs/features/002-layout/spec.md` + `specs/features/002-layout/plan.md`
**Referencia visual**: `specs/features/002-layout/ref-layout.png`
**Referencia arquitectónica**: `CONSTITUTION.md` §16-18

**Formato**: `[ID] [P?] [Story?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — no tiene dependencias de tareas incompletas en la misma fase
- **[HU4]**: Navegación por Módulos | **[HU5]**: Colapso/Expansión | **[HU6]**: Responsivo Mobile | **[HU7]**: TopHeader | **[HU8]**: Perfil en Sidebar
- **[BLOQUEANTE]**: Fases posteriores no pueden iniciar sin esta tarea

---

## Block 1: Setup — Design Tokens, Locale y Modelos de Navegación

**Propósito**: Definir los tokens visuales del sidebar, textos del locale y la interface `NavItem` con los items de navegación. Sin dependencias entre sí — todas paralelas.

- [x] T001 [P] Agregar primitivos CSS del sidebar (`navy-900`, `navy-800`, `blue-600`, `amber-400`) y semánticos (`--sidebar-*`) — `src/styles/_tokens.css`
- [x] T002 [P] Agregar colores del sidebar al config de Tailwind (`sidebar-bg`, `sidebar-text`, `sidebar-accent`, etc.) — `tailwind.config.js`
- [x] T003 [P] Agregar secciones `sidebar`, `topHeader` y `placeholders` al locale global — `src/app/shared/locale/locale.ts`
- [x] T004 [P] Crear interface `NavItem` y array `NAV_ITEMS` con módulos y submódulos — `src/app/core/layout/sidebar/nav-items.ts`
- [x] T005 [P] Crear `roleGuard` paramétrico (recibe array de roles, lee store NgRx) — `src/app/core/guards/role.guard.ts`

**Checkpoint**: `ng build` compila sin errores. Los tokens CSS y clases Tailwind del sidebar están disponibles. El locale tiene las nuevas secciones. `NavItem` y `roleGuard` listos para consumo.

---

## Block 2: Componentes Dumb — Sidebar + TopHeader

**Propósito**: Crear los componentes de presentación que reciben datos via inputs y emiten eventos via outputs. No inyectan Store ni Router.

**⚠️ Depende de**: Block 1 completo (tokens, locale, NavItem).

> **Referencia visual obligatoria:** Antes de implementar cualquier tarea de este bloque, consultar `specs/features/002-layout/ref-layout.png` para respetar estructura, colores y disposición.

- [x] T006 [HU4] [HU5] [HU8] Crear `SidebarComponent` (TS): inputs (`navItems`, `collapsed`, `user`), outputs (`logout`, `navigated`), computed para `MenuItem[]` del PanelMenu e iniciales del avatar — `src/app/core/layout/sidebar/sidebar.component.ts`
- [x] T007 [HU4] [HU5] [HU8] Crear template del sidebar: branding (logo + título), PanelMenu en modo expandido, lista de iconos con Tooltip en modo colapsado, perfil de usuario con Avatar en zona inferior — `src/app/core/layout/sidebar/sidebar.component.html`
- [x] T008 [HU4] Crear CSS overrides del PanelMenu sobre fondo sidebar (transparencia, colores de texto/acento, hover, active) — `src/app/core/layout/sidebar/sidebar.component.css`
- [x] T009 [HU7] Crear `TopHeaderComponent` (TS): inputs (`user`), outputs (`logout`, `toggleSidebar`), computed para iniciales del avatar — `src/app/core/layout/top-header/top-header.component.ts`
- [x] T010 [HU7] Crear template del TopHeader: Toolbar de PrimeNG con hamburguesa mobile, bienvenida, badge de rol, Avatar, nombre de tenant — `src/app/core/layout/top-header/top-header.component.html`

**Checkpoint**: `ng build` compila sin errores. Los componentes Sidebar y TopHeader existen como unidades aisladas (aún no montados en un layout). Verificar en el build que los imports de PrimeNG (PanelMenu, Toolbar, Avatar, Badge, Tooltip, Drawer) resuelven correctamente.

---

## Block 3: Smart Container + Placeholders

**Propósito**: Crear el `MainLayoutComponent` que orquesta Sidebar + TopHeader + Drawer mobile, y los componentes placeholder necesarios para que las rutas tengan destino.

**⚠️ Depende de**: Block 2 completo (Sidebar y TopHeader creados).

- [x] T011 [P] Crear `UsersPlaceholderComponent` — `src/app/domains/admin/features/users-placeholder/users-placeholder.component.ts`
- [x] T012 [P] Crear `AuditLogsPlaceholderComponent` — `src/app/domains/admin/features/audit-logs-placeholder/audit-logs-placeholder.component.ts`
- [x] T013 [HU4] [HU5] [HU6] [HU7] Crear `MainLayoutComponent` (TS): inyecta Store, lee `selectCurrentUser`, crea signals `sidebarCollapsed` y `mobileDrawerOpen`, computed `filteredNavItems` con filtrado RBAC, métodos `onLogout`, `onToggleSidebar`, `onToggleMobile`, `onMobileNavigated` — `src/app/core/layout/main-layout/main-layout.component.ts`
- [x] T014 [HU4] [HU5] [HU6] Crear template del MainLayout: `<aside>` desktop con Sidebar + botón toggle circular, `<p-drawer>` mobile con Sidebar, zona principal con TopHeader + `<main>` + `<router-outlet>` — `src/app/core/layout/main-layout/main-layout.component.html`
- [x] T015 Adaptar `HomeComponent` existente: eliminar `h-screen` (el MainLayout controla el alto), usar locale de `placeholders` — `src/app/core/layout/home/home.component.ts`

**Checkpoint**: `ng build` compila sin errores. MainLayout, placeholders y HomeComponent adaptado existen como unidades. Aún no están conectados al router.

---

## Block 4: Rutas — Wiring del App Shell

**Propósito**: Conectar el MainLayoutComponent como shell padre de todas las rutas autenticadas. Crear rutas del dominio admin con roleGuard.

**⚠️ Depende de**: Block 3 completo (MainLayout + placeholders creados antes que las rutas que los referencian).

> **Orden crítico**: Los componentes referenciados por rutas lazy (`loadComponent`) deben existir ANTES de que la ruta los importe (compilación AOT).

- [x] T016 [BLOQUEANTE] Crear rutas del dominio admin: `/admin/users` y `/admin/audit-logs` con `roleGuard` y lazy loading de placeholders — `src/app/domains/admin/admin.routes.ts`
- [x] T017 [BLOQUEANTE] Refactorizar `app.routes.ts`: envolver rutas autenticadas bajo `MainLayoutComponent` como shell padre, redirigir `/` a `/dashboard`, agregar ruta `/admin` con `loadChildren`, mantener rutas auth públicas intactas — `src/app/app.routes.ts`
- [x] T017b [EMERGENTE] Persistir `AuthUser` completo en `sessionStorage` y agregar `getStoredUser()` — `src/app/core/auth/auth.service.ts`
- [x] T017c [EMERGENTE] Agregar acción `RestoreSession` y efecto `restoreSession$` que rehidrata el store desde `sessionStorage` al arrancar — `src/app/core/auth/store/auth.actions.ts`, `auth.effects.ts`
- [x] T017d [EMERGENTE] Despachar `AuthActions.restoreSession()` en `ngOnInit` del componente raíz `App` — `src/app/app.ts`

**Checkpoint**: `ng build` compila sin errores. **Validación funcional completa:**
1. Login con `admin@gop360.com` → redirige a `/dashboard` → Sidebar visible con "Panel de Control" + "Seguridad & Administración" expandible → TopHeader muestra "Bienvenido, Administrador ANH"
2. Login con `operador@gop360.com` → Sidebar muestra solo "Panel de Control" (sin "Administración")
3. Login con `auditor@gop360.com` → Sidebar muestra "Panel de Control" + "Administración" pero solo submódulo "Registros de Auditoría" (sin "Gestión de Usuarios")
4. Navegar a `/admin/users` como AUDITOR via URL directa → `roleGuard` redirige a `/dashboard`
5. Click en botón toggle → Sidebar colapsa a solo iconos con tooltips
6. Reducir ventana a < 1024px → Sidebar desaparece, hamburguesa visible en TopHeader → Click en hamburguesa → Drawer abre con menú expandido
7. Click en "Cerrar Sesión" (sidebar o TopHeader) → redirige a `/login`

---

## Block 5: Polish & Validación Final

**Propósito**: Verificación transversal de calidad, compilación limpia y adherencia a CONSTITUTION.

**⚠️ Depende de**: Block 4 completo y validado.

- [x] T018 [P] Verificar que todos los imports entre capas usan path aliases (`@core/*`, `@shared/*`, `@env/*`) — revisar todos los archivos creados/modificados
- [x] T019 [P] Verificar que ningún texto está hardcodeado en templates o componentes (todo debe venir de `locale.ts`) — archivos `*.html` y `*.ts` de la feature
- [x] T020 [P] Verificar que ningún color está hardcodeado en templates (todo via tokens semánticos o clases Tailwind semánticas, prohibido `bg-[#083075]`) — archivos `*.html` y `*.css` de la feature
- [x] T021 Ejecutar `ng build` y corregir todos los errores/warnings de compilación — todos los archivos
- [x] T022 [P] Verificar accesibilidad: `aria-label` en toggle, hamburguesa y logout; `routerLinkActive` en items activos; area mínima de toque 44x44px en controles — archivos `*.html`

**Checkpoint final**: `ng build` exitoso sin warnings. Los 4 usuarios mock validan RBAC diferenciado en sidebar. Los 7 escenarios de validación del Block 4 pasan correctamente. Sidebar expandido, colapsado y drawer mobile funcionan. TopHeader muestra datos del usuario autenticado.

---

## Dependencies & Execution Order

### Dependencias entre Bloques

```
Block 1 (Setup)       → Sin dependencias. Iniciar inmediatamente.
Block 2 (Dumb)        → Depende de Block 1 (tokens, locale, NavItem).
Block 3 (Smart+Place) → Depende de Block 2 (Sidebar, TopHeader creados).
Block 4 (Rutas)       → Depende de Block 3 (componentes antes que rutas — AOT).
Block 5 (Polish)      → Depende de Block 4 completo y validado.
```

### Dependencias Críticas Dentro de Bloques

```
Block 1:
  T001–T005 son todas paralelas (sin dependencias entre sí)

Block 2:
  T007 depende de T006 (template necesita el TS del componente)
  T008 depende de T007 (CSS aplica sobre el PanelMenu del template)
  T010 depende de T009 (template necesita el TS del componente)

Block 3:
  T011, T012 son paralelas entre sí
  T013 depende de T006, T009 (referencia Sidebar y TopHeader en imports)
  T014 depende de T013 (template necesita el TS del MainLayout)
  T015 es paralela con T013–T014

Block 4:
  T016 depende de T011, T012, T005 (placeholders + roleGuard)
  T017 depende de T013, T016 (MainLayout + admin routes)
```

### Resumen de Archivos por Bloque

| Bloque | Crear | Modificar | Total |
|---|---|---|---|
| Block 1 | 2 | 3 | 5 |
| Block 2 | 5 | 0 | 5 |
| Block 3 | 4 | 1 | 5 |
| Block 4 | 1 | 1 | 2 |
| Block 5 | 0 | 0 (revisión) | 0 |
| **Total** | **12** | **5** | **17** |