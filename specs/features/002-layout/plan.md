# Plan: Layout Base — Sidebar + TopHeader

**Feature ID:** 002-layout
**Spec de referencia:** `specs/features/002-layout/spec.md`
**Referencia visual:** `specs/features/002-layout/ref-layout.png`
**Estado:** Completado — implementación finalizada (Blocks 1-5 + tareas emergentes T017b-d)

---

## 1. Resumen Arquitectónico

Este módulo implementa el App Shell de GOP 360° según el patrón definido en CONSTITUTION §16-18. El `MainLayoutComponent` (Smart) envuelve todas las rutas autenticadas y orquesta la comunicación entre sus hijos Dumb: `SidebarComponent`, `TopHeaderComponent` y el `<router-outlet>` de contenido.

**Decisiones clave de implementación:**

| Decisión | Elección | Justificación |
|---|---|---|
| Estado del sidebar (collapsed) | **Signal** local en MainLayout | Es UI local que no cruza features (CONSTITUTION §16.4) |
| Menú expandido del sidebar | **PanelMenu** de PrimeNG | Provee acordeón, keyboard nav, routerLink, a11y nativo. Override de CSS vía tokens |
| Menú colapsado del sidebar | **Lista custom** de iconos con Tooltip | PanelMenu no soporta modo icon-only. Lista custom con `pTooltip` de PrimeNG |
| Drawer mobile | **Drawer** de PrimeNG (`p-drawer`) | Overlay modal nativo con animación, backdrop, dismiss y a11y integrados |
| TopHeader | **Toolbar** de PrimeNG (`p-toolbar`) | Tres slots (start/center/end) listos para usar |
| Avatar | **Avatar** de PrimeNG (`p-avatar`) | Genera iniciales, forma circular, tamaños estándar |
| Badge de rol | **Badge** de PrimeNG (`p-badge`) | Consistente con el sistema de componentes |
| Datos del usuario | **NgRx selectors** (store de auth) | El usuario ya vive en AuthState (Feature 001-auth) |
| RBAC en sidebar | Filtro `computed()` sobre el array `NavItem[]` | Se filtra antes de pasar a los componentes Dumb |
| AppNavLink reutilizable | **Condicional** — se crea solo si PanelMenu no cubre el caso | Si PanelMenu + override CSS funciona bien, no se crea. Si hay que construir el menú a mano (§11.3 Criterio 3), se crea en `shared/ui/` |

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
src/
├── app/
│   ├── core/
│   │   ├── layout/
│   │   │   ├── main-layout/
│   │   │   │   ├── main-layout.component.ts          # Smart container: inyecta Store, lee user/rol, gestiona sidebar state
│   │   │   │   └── main-layout.component.html        # Template: aside + topheader + router-outlet + drawer mobile
│   │   │   ├── sidebar/
│   │   │   │   ├── nav-items.ts                      # Interface NavItem + array NAV_ITEMS con módulos y submódulos
│   │   │   │   ├── sidebar.component.ts              # Dumb: recibe navItems, collapsed, user. Renderiza PanelMenu o icon-list
│   │   │   │   ├── sidebar.component.html            # Template: branding + menú + perfil usuario
│   │   │   │   └── sidebar.component.css             # Override CSS de PanelMenu para fondo azul/acento amarillo
│   │   │   └── top-header/
│   │   │       ├── top-header.component.ts           # Dumb: recibe user, emite logout/toggleSidebar
│   │   │       └── top-header.component.html         # Template: toolbar con welcome, badge, avatar, tenant
│   │   └── guards/
│   │       └── role.guard.ts                         # Guard paramétrico por roles (defensa en profundidad para RBAC)
│   ├── shared/
│   │   └── ui/
│   │       └── app-nav-link/                         # [CONDICIONAL] Se crea solo si PanelMenu no cubre el caso
│   │           └── app-nav-link.component.ts         #   → Si PanelMenu funciona con overrides CSS, este componente NO se crea
│   │           #                                     #   → Si se construye menú a mano (§11.3 Criterio 3), SÍ se crea
│   └── domains/
│       └── admin/
│           ├── admin.routes.ts                       # Rutas del dominio admin (users, audit-logs) con roleGuard
│           └── features/
│               ├── users-placeholder/
│               │   └── users-placeholder.component.ts    # Placeholder para /admin/users
│               └── audit-logs-placeholder/
│                   └── audit-logs-placeholder.component.ts # Placeholder para /admin/audit-logs
├── assets/
│   └── icons/
│       └── logo-top-header.png                       # [YA EXISTE] Logo ANH para cabecera del sidebar
```

### 2.2. Archivos a MODIFICAR

```
src/
├── app/
│   ├── app.routes.ts                                 # Envolver rutas autenticadas en MainLayoutComponent como shell padre
│   ├── core/
│   │   └── layout/
│   │       └── home/
│   │           └── home.component.ts                 # Adaptar como DashboardPlaceholder dentro del MainLayout
│   └── shared/
│       └── locale/
│           └── locale.ts                             # + secciones: sidebar, topHeader, placeholders
├── styles/
│   └── _tokens.css                                   # + primitivos sidebar (#083075, #0D66EA, #FFCB05) + semánticos
```

---

## 3. Árbol de Componentes

```
MainLayoutComponent  [core/layout/main-layout/]                    ← Smart
├── SidebarComponent         [core/layout/sidebar/]                ← Dumb
│   ├── [Branding] Logo + título "GOP 360°"
│   ├── [Menú expandido] PanelMenu de PrimeNG (visible si !collapsed)
│   │   └── Items renderizados por PanelMenu vía MenuItem[] model
│   │       (Si PanelMenu no cubre el caso → AppNavLink en shared/ui/)
│   ├── [Menú colapsado] Lista custom de iconos + Tooltip (visible si collapsed)
│   │   └── Cada icono usa pTooltip de PrimeNG
│   └── [Perfil] Avatar + nombre + rol + botón logout
├── TopHeaderComponent       [core/layout/top-header/]             ← Dumb
│   ├── [start] Botón hamburguesa (mobile) + "Bienvenido, {nombre}"
│   ├── [center] Badge de rol
│   └── [end] Avatar + nombre tenant + botón logout (futuro)
├── Drawer (PrimeNG)         [solo mobile, visible via signal]
│   └── SidebarComponent (misma instancia lógica, reutiliza template expandido)
└── <router-outlet />        ← Contenido del dominio activo
    ├── HomeComponent             [/dashboard o /]
    ├── UsersPlaceholderComponent  [/admin/users]
    ├── AuditLogsPlaceholderComponent [/admin/audit-logs]
    └── (futuros dominios)
```

**Smart vs Dumb:**

| Componente | Tipo | Inyecta | Responsabilidad |
|---|---|---|---|
| `MainLayoutComponent` | **Smart** | Store, Router | Lee usuario/rol del store, filtra navItems por RBAC, gestiona signal collapsed, despacha logout |
| `SidebarComponent` | **Dumb** | — | Recibe `navItems`, `collapsed`, `user` como inputs. Renderiza menú y perfil |
| `TopHeaderComponent` | **Dumb** | — | Recibe `user` como input. Emite `logout` y `toggleSidebar` |
| `AppNavLinkComponent` | **Dumb** (condicional) | — | Solo se crea si PanelMenu no cubre el caso. Recibe `NavItem` + `userRole`, aplica RBAC + estilo activo |

---

## 4. Modelo de Datos: NavItem

### 4.1. Interface extendida con soporte para submódulos

La interface base de CONSTITUTION §17.1 se extiende para soportar la jerarquía de 2 niveles requerida:

```typescript
// core/layout/sidebar/nav-items.ts
import { UserRole } from '@shared/models';

export interface NavItem {
  key:       string;           // Clave para locale (APP_LOCALE.sidebar[key])
  icon?:     string;           // Clase PrimeIcons (ej. 'pi pi-objects-column'). Omitido en nivel 2
  route?:    string;           // Ruta de navegación. Omitido si el item solo agrupa (tiene children)
  roles:     UserRole[];       // Roles que pueden ver este item
  children?: NavItem[];        // Submódulos (máximo 1 nivel de profundidad)
}
```

### 4.2. Definición de items de navegación

```typescript
// core/layout/sidebar/nav-items.ts
export const NAV_ITEMS: NavItem[] = [
  {
    key: 'dashboard',
    icon: 'pi pi-objects-column',
    route: '/dashboard',
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
```

### 4.3. Filtrado RBAC en el Smart container

```typescript
// main-layout.component.ts — computed signal que filtra por rol
private readonly currentUser = toSignal(this.store.select(selectCurrentUser));

readonly filteredNavItems = computed(() => {
  const user = this.currentUser();
  if (!user) return [];
  return this.filterByRole(NAV_ITEMS, user.role);
});

private filterByRole(items: NavItem[], role: UserRole): NavItem[] {
  return items
    .filter(item => item.roles.includes(role))
    .map(item => ({
      ...item,
      children: item.children
        ? item.children.filter(child => child.roles.includes(role))
        : undefined,
    }))
    .filter(item => !item.children || item.children.length > 0);
}
```

> El filtrado se hace en el Smart parent. Los componentes Dumb reciben el array ya filtrado y lo renderizan sin lógica RBAC propia.

### 4.4. Flujo de Logout desde el Layout (CONSTITUTION §4.1)

El `MainLayoutComponent` es el único punto de despacho de la acción de logout. Sidebar y TopHeader solo emiten un evento; **nunca** inyectan Store ni Router:

```
SidebarComponent / TopHeaderComponent
  └── output logout.emit()
          ↓
MainLayoutComponent.onLogout()
  └── this.store.dispatch(AuthActions.logout())
          ↓
auth.effects.ts → logout$  [Feature 001-auth, ya existente]
  └── authService.clearSession()
  └── router.navigate(['/login'])
  └── dispatch(AuthActions.logoutSuccess())
```

> **Regla §4.1:** La navegación a `/login` ocurre exclusivamente en el Effect `logout$`. El `MainLayoutComponent` solo despacha la acción — nunca invoca `router.navigate()` directamente.

---

## 5. Diseño de Componentes PrimeNG

### 5.1. PanelMenu — Menú acordeón en sidebar expandido

**Import:** `import { PanelMenu } from 'primeng/panelmenu'`

El `SidebarComponent` transforma el array `NavItem[]` a `MenuItem[]` (interface de PrimeNG) para alimentar el PanelMenu:

```typescript
// sidebar.component.ts
private readonly navItems = input.required<NavItem[]>();
private readonly locale = APP_LOCALE.sidebar;

readonly menuModel = computed<MenuItem[]>(() =>
  this.navItems().map(item => ({
    label: this.locale[item.key as keyof typeof this.locale] as string,
    icon: item.icon,
    routerLink: item.route,
    expanded: this.isItemActive(item),
    items: item.children?.map(child => ({
      label: this.locale[child.key as keyof typeof this.locale] as string,
      routerLink: child.route,
    })),
  }))
);
```

**Override CSS:** El PanelMenu viene con fondo y colores propios del tema Aura. Se sobreescriben a nivel del componente sidebar usando CSS scoped:

```css
/* sidebar.component.css — overrides de PanelMenu sobre fondo sidebar */
:host ::ng-deep .p-panelmenu {
  background: transparent;
}
:host ::ng-deep .p-panelmenu-panel {
  background: transparent;
  border: none;
}
:host ::ng-deep .p-panelmenu-header-link {
  color: var(--sidebar-text);
  background: transparent;
}
:host ::ng-deep .p-panelmenu-header-link:hover {
  background: var(--sidebar-item-hover);
}
:host ::ng-deep .p-panelmenu-header-link.p-highlight {
  color: var(--sidebar-accent);
}
:host ::ng-deep .p-panelmenu-content {
  background: transparent;
  border: none;
}
:host ::ng-deep .p-panelmenu-content .p-menuitem-link {
  color: var(--sidebar-text);
}
:host ::ng-deep .p-panelmenu-content .p-menuitem-link.p-highlight,
:host ::ng-deep .p-panelmenu-content .p-menuitem-link.router-link-active {
  background: var(--sidebar-item-active-bg);
  color: var(--sidebar-accent);
}
```

> **Nota:** Si los overrides resultan demasiado complejos en la implementación real (> 10 reglas de override), se evaluará construir el acordeón a mano con Tailwind (según criterio CONSTITUTION §11.3 Criterio 3). Esta decisión se toma durante la implementación, no en el plan.

### 5.2. Drawer — Sidebar mobile

**Import:** `import { Drawer } from 'primeng/drawer'`

```html
<!-- main-layout.component.html — solo visible en mobile -->
<p-drawer [(visible)]="mobileDrawerOpen" position="left" [modal]="true"
          [dismissible]="true" [closeOnEscape]="true" [blockScroll]="true"
          styleClass="sidebar-drawer">
  <app-sidebar [navItems]="filteredNavItems()" [collapsed]="false"
               [user]="currentUser()" (logout)="onLogout()"
               (navigated)="onMobileNavigated()" />
</p-drawer>
```

El Drawer cierra automáticamente (`mobileDrawerOpen.set(false)`) cuando el sidebar emite `navigated` (tras seleccionar un submódulo).

### 5.3. Toolbar — TopHeader

**Import:** `import { Toolbar } from 'primeng/toolbar'`

```html
<!-- top-header.component.html -->
<p-toolbar styleClass="border-none shadow-sm bg-surface">
  <ng-template #start>
    <!-- Hamburguesa mobile -->
    <button class="lg:hidden ..." (click)="toggleSidebar.emit()">
      <i class="pi pi-bars"></i>
    </button>
    <!-- Bienvenida -->
    <span class="hidden sm:inline ...">{{ locale.welcome }}, {{ user().name }}</span>
  </ng-template>

  <ng-template #end>
    <!-- Badge de rol -->
    <p-badge [value]="user().role" severity="info" />
    <!-- Avatar -->
    <p-avatar [label]="initials()" shape="circle" />
    <!-- Tenant -->
    <span class="hidden md:inline ...">{{ user().tenantName }}</span>
  </ng-template>
</p-toolbar>
```

### 5.4. Avatar — Perfil de usuario

**Import:** `import { Avatar } from 'primeng/avatar'`

Generación de iniciales:

```typescript
// Función utilitaria (usada en sidebar y topheader)
readonly initials = computed(() => {
  const name = this.user()?.name ?? '';
  return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2);
});
```

### 5.5. Tooltip — Sidebar colapsado

**Import:** `import { Tooltip } from 'primeng/tooltip'`

```html
<!-- sidebar.component.html — modo colapsado -->
@if (collapsed()) {
  <nav class="flex flex-col gap-1 px-2">
    @for (item of navItems(); track item.key) {
      <a [routerLink]="item.route ?? item.children?.[0]?.route"
         [pTooltip]="locale[item.key]" tooltipPosition="right"
         class="..." routerLinkActive="sidebar-icon-active">
        <i [class]="item.icon"></i>
      </a>
    }
  </nav>
}
```

---

## 6. Design Tokens — Nuevos tokens para el Sidebar

### 6.1. Primitivos a agregar en `_tokens.css`

```css
/* Paleta sidebar — según referencia visual */
--primitive-navy-900:   #083075;    /* Fondo principal sidebar */
--primitive-navy-800:   #0a3d8f;    /* Hover items */
--primitive-blue-600:   #0D66EA;    /* Fondo submódulo activo / highlight box */
--primitive-amber-400:  #FFCB05;    /* Acento: texto activo, toggle, bordes */
```

### 6.2. Semánticos a agregar en `_tokens.css`

```css
/* Sidebar */
--sidebar-bg:              var(--primitive-navy-900);
--sidebar-text:            rgba(255, 255, 255, 0.75);
--sidebar-text-active:     var(--primitive-amber-400);
--sidebar-item-hover:      var(--primitive-navy-800);
--sidebar-item-active-bg:  var(--primitive-blue-600);
--sidebar-accent:          var(--primitive-amber-400);
--sidebar-width-expanded:  18rem;    /* w-72 = 288px */
--sidebar-width-collapsed: 5rem;     /* w-20 = 80px */
```

### 6.3. Tailwind config — agregar tokens del sidebar

```javascript
// tailwind.config.js — extend colors
'sidebar-bg':          'var(--sidebar-bg)',
'sidebar-text':        'var(--sidebar-text)',
'sidebar-text-active': 'var(--sidebar-text-active)',
'sidebar-item-hover':  'var(--sidebar-item-hover)',
'sidebar-item-active': 'var(--sidebar-item-active-bg)',
'sidebar-accent':      'var(--sidebar-accent)',
```

> Esto permite usar clases como `bg-sidebar-bg`, `text-sidebar-accent`, `border-sidebar-accent` en los templates.

---

## 7. Estructura HTML del Main Layout

```html
<!-- main-layout.component.html -->
<div class="flex h-screen overflow-hidden">

  <!-- Sidebar estático — solo desktop -->
  <aside class="hidden lg:flex flex-col transition-all duration-300 relative"
         [class]="sidebarCollapsed() ? 'w-20' : 'w-72'"
         [style.background]="'var(--sidebar-bg)'">
    <app-sidebar
      [navItems]="filteredNavItems()"
      [collapsed]="sidebarCollapsed()"
      [user]="currentUser()"
      (logout)="onLogout()" />

    <!-- Toggle button — borde derecho -->
    <button class="absolute top-1/2 -right-3 z-10 w-6 h-6 rounded-full
                   bg-sidebar-accent flex items-center justify-center shadow-md
                   transition-transform"
            (click)="onToggleSidebar()"
            [attr.aria-label]="sidebarCollapsed() ? locale.expandMenu : locale.collapseMenu">
      <i class="pi text-xs"
         [class]="sidebarCollapsed() ? 'pi-chevron-right' : 'pi-chevron-left'"></i>
    </button>
  </aside>

  <!-- Drawer mobile -->
  <p-drawer [(visible)]="mobileDrawerOpen" position="left" [modal]="true"
            [dismissible]="true" [closeOnEscape]="true" [blockScroll]="true"
            [showCloseIcon]="false" styleClass="w-72"
            [style]="{ background: 'var(--sidebar-bg)' }">
    <app-sidebar
      [navItems]="filteredNavItems()"
      [collapsed]="false"
      [user]="currentUser()"
      (logout)="onLogout()"
      (navigated)="onMobileNavigated()" />
  </p-drawer>

  <!-- Área principal -->
  <div class="flex flex-col flex-1 overflow-hidden">
    <app-top-header
      [user]="currentUser()"
      (logout)="onLogout()"
      (toggleSidebar)="onToggleMobile()" />

    <main class="flex-1 overflow-y-auto p-6 bg-surface-alt">
      <router-outlet />
    </main>
  </div>
</div>
```

---

## 8. Modificaciones a la Configuración de Rutas

### 8.1. `app.routes.ts` — Shell padre para rutas autenticadas

Actualmente las rutas autenticadas son planas con `canActivate: [authGuard]` individual. Se refactorizan bajo `MainLayoutComponent` como shell padre:

```typescript
// app.routes.ts — DESPUÉS
export const routes: Routes = [
  // --- Rutas públicas (AuthLayout) ---
  {
    path: 'login',
    component: AuthLayoutComponent,
    canActivate: [noAuthGuard],
    children: [{ path: '', loadComponent: () => import('...LoginComponent') }],
  },
  {
    path: 'forgot-password',
    component: AuthLayoutComponent,
    canActivate: [noAuthGuard],
    children: [{ path: '', loadComponent: () => import('...ForgotPasswordComponent') }],
  },

  // --- Rutas autenticadas (MainLayout como shell) ---
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      // Dashboard (ruta por defecto)
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () => import('@core/layout/home/home.component')
          .then(m => m.HomeComponent),
      },
      // Admin — con roleGuard adicional
      {
        path: 'admin',
        canActivate: [roleGuard(['ADMIN', 'AUDITOR'])],
        loadChildren: () => import('./domains/admin/admin.routes')
          .then(m => m.adminRoutes),
      },
      // Dominios futuros (wells, operations, production)
      // Se agregan cuando se implementen sus features
    ],
  },

  // --- Wildcard ---
  { path: '**', redirectTo: '' },
];
```

**Cambio clave:** La ruta `''` ahora usa `MainLayoutComponent` como `component` (no `loadComponent`), y todos los dominios son `children`. Esto garantiza que Sidebar y TopHeader persistan sin re-renderizarse al navegar entre módulos (RN-017).

### 8.2. `admin.routes.ts` — Rutas del dominio Admin

```typescript
// domains/admin/admin.routes.ts
export const adminRoutes: Routes = [
  { path: '', redirectTo: 'users', pathMatch: 'full' },
  {
    path: 'users',
    canActivate: [roleGuard(['ADMIN'])],
    loadComponent: () => import('./features/users-placeholder/users-placeholder.component')
      .then(m => m.UsersPlaceholderComponent),
  },
  {
    path: 'audit-logs',
    canActivate: [roleGuard(['ADMIN', 'AUDITOR'])],
    loadComponent: () => import('./features/audit-logs-placeholder/audit-logs-placeholder.component')
      .then(m => m.AuditLogsPlaceholderComponent),
  },
];
```

### 8.3. `role.guard.ts` — Guard paramétrico RBAC

```typescript
// core/guards/role.guard.ts
export const roleGuard = (allowedRoles: UserRole[]): CanActivateFn => () => {
  const store = inject(Store);
  const router = inject(Router);
  return store.select(selectCurrentUser).pipe(
    take(1),
    map(user =>
      user && allowedRoles.includes(user.role)
        ? true
        : router.createUrlTree(['/dashboard'])
    ),
  );
};
```

---

## 9. Locale — Nuevas secciones

### 9.1. `APP_LOCALE` — Secciones a agregar

```typescript
// shared/locale/locale.ts — secciones nuevas
sidebar: {
  dashboard:      'Panel de Control',
  admin:          'Seguridad & Administración',
  adminUsers:     'Gestión de Usuarios',
  adminAuditLogs: 'Registros de Auditoría',
},
topHeader: {
  welcome:       'Bienvenido',
  logout:        'Cerrar Sesión',
  expandMenu:    'Expandir menú',
  collapseMenu:  'Colapsar menú',
  openMenu:      'Abrir menú de navegación',
},
placeholders: {
  title:    'Módulo en construcción',
  subtitle: 'Esta funcionalidad estará disponible próximamente.',
},
```

---

## 10. Componentes Placeholder

Los placeholders son componentes mínimos que se cargan en las rutas de destino del sidebar que aún no tienen implementación real. Todos comparten el mismo patrón:

```typescript
// Patrón genérico de placeholder
@Component({
  selector: 'app-xxx-placeholder',
  template: `
    <div class="flex items-center justify-center h-full">
      <div class="text-center">
        <i class="pi pi-wrench text-4xl text-text-secondary mb-4"></i>
        <h2 class="text-xl font-semibold text-text-primary mb-2">{{ locale.title }}</h2>
        <p class="text-text-secondary">{{ locale.subtitle }}</p>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
```

**`HomeComponent` existente** se adapta para funcionar como placeholder del Dashboard dentro del MainLayout (eliminar `h-screen` ya que el MainLayout controla el alto).

---

## 11. Dependencias entre Archivos (Orden de Implementación)

```
Fase 1 — Tokens, Locale y Modelos (sin dependencias entre sí)
  styles/_tokens.css (MODIFY)            ← + primitivos sidebar + semánticos
  tailwind.config.js (MODIFY)            ← + colores sidebar
  shared/locale/locale.ts (MODIFY)       ← + secciones sidebar, topHeader, placeholders
  core/layout/sidebar/nav-items.ts       ← interface NavItem + array NAV_ITEMS
  core/guards/role.guard.ts              ← guard paramétrico RBAC

Fase 2 — Componentes Dumb (dependen de Fase 1)
  core/layout/sidebar/sidebar.component.ts            ← depende de NavItem, PanelMenu, Avatar, Tooltip
  core/layout/sidebar/sidebar.component.html
  core/layout/sidebar/sidebar.component.css           ← overrides PanelMenu
  core/layout/top-header/top-header.component.ts      ← depende de AuthUser, Toolbar, Avatar, Badge
  core/layout/top-header/top-header.component.html
  [CONDICIONAL] shared/ui/app-nav-link/              ← solo si PanelMenu no cubre el caso (decisión en implementación)

Fase 3 — Smart Container y Placeholders (dependen de Fase 2)
  core/layout/main-layout/main-layout.component.ts    ← depende de Sidebar, TopHeader, Drawer, Store
  core/layout/main-layout/main-layout.component.html
  core/layout/home/home.component.ts (MODIFY)         ← adaptar para funcionar dentro del MainLayout
  domains/admin/features/users-placeholder/users-placeholder.component.ts
  domains/admin/features/audit-logs-placeholder/audit-logs-placeholder.component.ts
  domains/admin/admin.routes.ts                        ← depende de placeholders + roleGuard

Fase 4 — Rutas y Wiring (dependen de todo lo anterior)
  app.routes.ts (MODIFY)                               ← envolver en MainLayoutComponent + admin children
```

---

## 12. Restricciones de Implementación (CONSTITUTION)

| Regla | Aplicación en este módulo |
|---|---|
| Standalone Components | Todos los componentes son standalone. Sin `NgModule`. Sin `standalone: true` explícito |
| `ChangeDetectionStrategy.OnPush` | Obligatorio en MainLayout, Sidebar, TopHeader, AppNavLink y placeholders |
| `inject()` en lugar de constructor | Store y Router se inyectan con `inject()` en MainLayout |
| Control flow nativo | `@if` para collapsed/expanded, mobile/desktop. `@for` con `track item.key` para navItems |
| Signals para estado local | `sidebarCollapsed`, `mobileDrawerOpen` son Signals en MainLayout |
| NgRx solo para lectura | MainLayout lee `selectCurrentUser` del store existente. No agrega nuevo estado NgRx |
| Locale obligatorio | Cero textos hardcodeados. Módulos, roles, acciones desde `APP_LOCALE.sidebar` y `APP_LOCALE.topHeader` |
| Path aliases | `@core/*`, `@shared/*`, `@env/*` para imports entre capas |
| No importar de domains/ desde core/ | MainLayout y Sidebar no conocen conceptos de negocio. NavItems define rutas, no lógica de dominio |
| Design Tokens | Colores del sidebar vía CSS custom properties. Prohibido `bg-[#083075]` en templates |
| Smart vs Dumb estricto | Solo MainLayout inyecta Store. Sidebar/TopHeader/AppNavLink son Dumb puros con inputs/outputs |
| Wrapper justificado (§11.3) | AppNavLink es condicional. Se crea solo si PanelMenu requiere > 10 overrides y se construye el menú a mano (Criterio 3) |
| Defensa en profundidad | Sidebar filtra visualmente (UX) + roleGuard bloquea URL directa (seguridad) |