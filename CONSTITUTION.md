# Principios Arquitectónicos para Angular Empresarial

## 1. Conceptos Core
* **Domain-Driven Design (DDD):** La aplicación se divide en "Dominios" de negocio (Pozos, Producción, Operaciones, Admin) en lugar de capas técnicas. Cada dominio es independiente y encapsula sus propios modelos, servicios, estado y componentes.
* **Standalone Components:** Eliminación de `NgModules` para reducir la complejidad y mejorar el tree-shaking (carga más rápida).
* **Smart vs. Dumb Components:** Separación estricta entre componentes de presentación (UI reutilizable, *Dumb*) y componentes contenedores (Páginas conectadas a servicios/estado, *Smart*).
* **Lazy Loading Extremo:** Cada dominio de negocio se carga bajo demanda a través del enrutador, reduciendo el tamaño del bundle inicial.
* **Gestión de Estado Centralizada/Local:** Uso de **NgRx** (para estado global complejo como catálogos o sesión) y **Signals** (para estado local reactivo en componentes como el Wizard de creación de pozos).

---

## 2. Ejemplo de Scaffolding (Estructura de Carpetas)

```text
src/
├── app/
│   ├── core/                       # 1. CORE: Singleton, configuraciones y seguridad
│   │   ├── auth/                   # Lógica de autenticación, interceptores de token
│   │   ├── guards/                 # Protecciones de rutas (RBAC)
│   │   ├── http/                   # Interceptores de errores globales, manejo de API
│   │   └── layout/                 # Componentes estructurales (Sidebar, TopHeader)
│   │
│   ├── shared/                     # 2. SHARED: UI genérica, modelos y servicios transversales
│   │   ├── models/                 # Interfaces/tipos usados por 2+ dominios (Well, Operator, UserRole)
│   │   │   ├── well.model.ts       #   → model + dto + mapper por entidad compartida
│   │   │   ├── well.dto.ts
│   │   │   ├── well.mapper.ts
│   │   │   └── index.ts            #   → barrel export del grupo
│   │   ├── services/               # Servicios transversales sin lógica de negocio propia
│   │   │   ├── catalog.service.ts  #   → catálogos read-only (tipos de pozo, operadoras)
│   │   │   └── index.ts
│   │   ├── locale/                 # Textos globales (Layout, Sidebar, Errores genéricos) -> locale.ts
│   │   ├── ui/                     # Componentes "Dumb" (Botones, Modales, Tooltips, Tablas)
│   │   ├── directives/             # Directivas estructurales o de atributos
│   │   ├── pipes/                  # Transformadores de datos (Fechas, Monedas, Formatos)
│   │   └── utils/                  # Funciones puras (ej. generador de UWI genérico)
│   │
│   ├── domains/                    # 3. DOMAINS: Lógica de negocio agrupada por contexto
│   │   │
│   │   ├── wells/                  # Dominio: Gestión de Pozos
│   │   │   ├── components/         # Componentes compartidos del dominio (ej. WellStatusBadge)
│   │   │   ├── features/           # Funcionalidades completas (Smart Components)
│   │   │   │   ├── well-create/    # Feature: Wizard de creación (F101)
│   │   │   │   │   ├── components/ # Componentes internos exclusivos de esta feature
│   │   │   │   │   ├── locale.ts   # Textos/Constantes locales de la feature
│   │   │   │   │   └── well-create.component.ts
│   │   │   │   ├── well-manage/    # Feature: Explorador jerárquico
│   │   │   │   └── well-info/      # Feature: Infografía del pozo
│   │   │   ├── models/             # Solo modelos PRIVADOS del dominio (WellHistory, Trajectory)
│   │   │   ├── services/           # Solo servicios PRIVADOS del dominio (wells-api.service.ts)
│   │   │   ├── store/              # Estado del dominio (NgRx Feature State o Signals)
│   │   │   └── wells.routes.ts     # Rutas lazy-loaded del dominio
│   │   │
│   │   ├── operations/             # Dominio: Operaciones y Formas 100
│   │   │   ├── features/
│   │   │   │   ├── idop/           # Informe Diario de Perforación
│   │   │   │   └── forms-100/      # Bandeja de Formas 100
│   │   │   ├── models/             # Solo modelos PRIVADOS: IDOP, Forma100
│   │   │   ├── services/           # Solo servicios PRIVADOS: operations-api.service.ts
│   │   │   └── operations.routes.ts
│   │   │
│   │   ├── production/             # Dominio: Producción y Formas 200
│   │   │   ├── features/
│   │   │   │   └── fiscalization/  # Fiscalización Volumétrica
│   │   │   ├── models/             # Solo modelos PRIVADOS: FiscalizationReport
│   │   │   ├── services/           # Solo servicios PRIVADOS: production-api.service.ts
│   │   │   └── production.routes.ts
│   │   │
│   │   └── admin/                  # Dominio: Administración y Seguridad
│   │       ├── features/
│   │       │   ├── users/          # Gestión de usuarios y roles
│   │       │   └── audit-logs/     # Trazabilidad
│   │       ├── models/             # Solo modelos PRIVADOS de admin
│   │       ├── services/           # Solo servicios PRIVADOS: admin-api.service.ts
│   │       └── admin.routes.ts
│   │
│   ├── app.component.ts            # Componente raíz
│   ├── app.routes.ts               # Enrutador principal (Carga los dominios por Lazy Loading)
│   └── app.config.ts               # Proveedores globales (HttpClient, Router, Store)
│
├── environments/                   # Variables de entorno (dev, qa, prod)
├── assets/                         # Imágenes, iconos
└── styles/                         # Estilos globales, variables CSS, configuración de Tailwind
```

> **Referencia:** Los dominios, features, modelos y servicios mostrados en esta estructura son ilustrativos. La estructura real de cada dominio se define y expande conforme a las especificaciones funcionales de cada elemento a desarrollar. No crear carpetas ni archivos anticipándose a funcionalidades no especificadas.

---

## 3. Descripción de las Capas (Por qué esta estructura)

### 1. `core/` (El Motor)
Aquí reside todo lo que la aplicación necesita instanciar **una sola vez** al arrancar.
* **Ejemplo en tu app:** El `AuthContext` actual pasaría a ser un `AuthService` inyectado en el `core/`. Los interceptores HTTP manejarían automáticamente la inyección del token JWT y la captura de errores globales (ej. mostrar un toast si la API de la ANH falla).

### 2. `shared/` (La Caja de Herramientas)
Contiene todo lo que es transversal a más de un dominio pero no pertenece al ciclo de vida del arranque de la app. Esto incluye UI reutilizable, **modelos e interfaces compartidas**, **servicios de catálogo o utilidad**, textos globales y funciones puras.
* **Regla de oro de `shared/`:** Un archivo en `shared/` no debe saber qué es un "IDOP" o una "Forma 101" — solo conoce abstracciones generales como `Well`, `Operator` o `UserRole` que múltiples dominios necesitan.
* **`shared/models/`:** Interfaces y DTOs que 2 o más dominios consumen. Incluyen su mapper y un `index.ts` de barrel export.
* **`shared/services/`:** Servicios sin lógica de negocio propia: catálogos read-only, utilidades HTTP genéricas. **No** incluye servicios que escriben datos de un dominio específico.
* **Ejemplo en tu app:** `Well` y `WellStatus` van en `shared/models/` porque los usan `wells/`, `operations/` y `production/`. Pero `WellHistory` va en `domains/wells/models/` porque solo lo usa el dominio de pozos.

### 3. `domains/` (El Corazón del Negocio)
Esta es la mejora más grande frente a tu arquitectura actual. En lugar de tener una carpeta `pages/` gigante con archivos de diferentes módulos mezclados, cada dominio es un ecosistema cerrado compuesto por **Features**.
* **Features (Funcionalidades):** Reemplazan el concepto tradicional de "Páginas". Una *feature* encapsula una funcionalidad completa (ej. `well-create`). Cada feature contiene sus propios componentes internos, su lógica específica y, crucialmente, **su propio archivo de constantes/textos locales**.
* **Ejemplo en tu app:** Si un desarrollador necesita arreglar un bug en la creación del UWI o en la validación de la Forma 101, no tiene que buscar en `src/utils`, luego en `src/pages`, y luego en `src/store`. Todo lo relacionado con pozos vive exclusivamente dentro de `src/app/domains/wells/features/well-create/`.
* **Escalabilidad:** Si el día de mañana el módulo de "Producción" crece demasiado, esta arquitectura permite extraer la carpeta `production/` a un micro-frontend o a una librería separada en un monorepo (usando herramientas como Nx).

---

## 4. Gestión de Estado: Cuándo usar Signals vs. NgRx

La regla es clara y no admite interpretación libre: **el scope del estado determina la herramienta**.

| Criterio | Usar |
|---|---|
| Estado vive en un solo componente o feature | Signals |
| Estado debe sobrevivir al destroy del componente | NgRx |
| Estado es compartido entre múltiples dominios | NgRx |
| Estado de sesión (usuario autenticado, permisos) | NgRx |
| Catálogos globales (tipos de pozo, operadoras) | NgRx |
| UI state (loading, modal abierto, paso del wizard) | Signals |

**Regla de oro:** Si un desarrollador tiene duda, usa Signals. Solo escala a NgRx cuando el estado cruce los límites de la feature. NgRx tiene un costo de boilerplate que no se justifica para estado local.

```typescript
// ✅ Signals: estado local de un wizard (vive y muere con el componente)
currentStep = signal(1);
isLoading = signal(false);
formData = signal<Partial<WellForm>>({});

// ✅ NgRx: catálogo de operadoras usado en múltiples dominios
// store/catalogs/catalogs.actions.ts → loadOperators
// store/catalogs/catalogs.selectors.ts → selectOperators
```

---

## 5. Manejo de Errores HTTP (`core/http/`)

Todo el manejo de errores de red es responsabilidad exclusiva de `core/http/`. Los servicios de dominio **nunca** manejan errores HTTP directamente — solo transforman la respuesta exitosa.

### Patrón del Interceptor

```typescript
// core/http/error.interceptor.ts

// Mensajes de fallback cuando el backend no retorna un mensaje legible
const HTTP_ERROR_MESSAGES: Record<number, string> = {
  403: 'No tienes permisos para realizar esta acción.',
  404: 'El recurso solicitado no fue encontrado.',
  422: 'Los datos enviados no son válidos.',
  500: 'Error interno del servidor.',
  503: 'Servicio no disponible. Intenta más tarde.',
};
const FALLBACK_MESSAGE = 'Ocurrió un error inesperado.';
const NO_CONNECTION_MESSAGE = 'Sin conexión. Verifica tu red e intenta de nuevo.';

// Extrae el mensaje del backend si existe; de lo contrario usa el fallback definido en front
function resolveErrorDetail(error: HttpErrorResponse, fallback: string): string {
  return error.error?.message ?? error.error?.detail ?? fallback;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const messageService = inject(MessageService); // PrimeNG toast

      switch (error.status) {
        case 401:
          inject(AuthService).logout(); // Token expirado → redirige a login
          break;

        case 0:
          // Sin conexión: el backend no responde, no hay mensaje posible
          messageService.add({ severity: 'error', summary: 'Sin conexión', detail: NO_CONNECTION_MESSAGE });
          break;

        default: {
          // Prioridad: mensaje del backend → fallback del front según código HTTP → fallback genérico
          const fallback = HTTP_ERROR_MESSAGES[error.status] ?? FALLBACK_MESSAGE;
          const detail = resolveErrorDetail(error, fallback);
          messageService.add({ severity: 'error', summary: 'Error', detail });
          break;
        }
      }

      return throwError(() => error);
    })
  );
};
```

### Patrón del Interceptor de Token (Auth)

```typescript
// core/http/auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).getToken();
  if (!token) return next(req);

  const authReq = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });
  return next(authReq);
};
```

### Regla de los Servicios de Dominio

```typescript
// ✅ Correcto: el servicio solo transforma la respuesta exitosa
getWell(id: string): Observable<Well> {
  return this.http.get<WellDTO>(`/api/wells/${id}`).pipe(
    map(dto => mapWellDTOToModel(dto)) // Transforma DTO → Modelo de dominio
  );
}

// ❌ Prohibido: el servicio NO maneja errores HTTP
getWell(id: string): Observable<Well> {
  return this.http.get<Well>(`/api/wells/${id}`).pipe(
    catchError(err => { console.error(err); return EMPTY; }) // ← NUNCA
  );
}
```

---

## 6. Modelos, Servicios y Barrel Exports: Reglas de Ubicación

### 6.1. La regla de decisión (una sola pregunta)

> **¿Lo necesita más de un dominio?**
> - **Sí** → va en `shared/models/` o `shared/services/`
> - **No** → va en `domains/[dominio]/models/` o `domains/[dominio]/services/`

No hay excepciones. Si un modelo que estaba en `domains/wells/models/` pasa a ser necesario en `operations/`, se mueve a `shared/models/` en ese momento — no antes.

### 6.2. Tabla de referencia rápida

| Elemento | ¿Dónde vive? | Condición |
|---|---|---|
| `Well`, `WellStatus`, `Operator` | `shared/models/` | Usado en 2+ dominios |
| `UserRole`, `User` | `shared/models/` | Usado en auth + admin |
| `WellHistory`, `Trajectory` | `domains/wells/models/` | Solo lo usa wells |
| `IDOP`, `Forma100` | `domains/operations/models/` | Solo lo usa operations |
| `FiscalizationReport` | `domains/production/models/` | Solo lo usa production |
| Catálogos read-only (tipos de pozo, operadoras) | `shared/services/` | Llamadas GET usadas en 2+ dominios |
| `WellsApiService` (CRUD de pozos) | `domains/wells/services/` | Solo lo llama wells |
| `OperationsApiService` | `domains/operations/services/` | Solo lo llama operations |
| `AuthService` | `core/auth/` | Singleton de sesión, arranca con la app |
| Interceptores HTTP | `core/http/` | Se registran una vez en `app.config.ts` |

> **Referencia:** Las entidades, modelos y servicios listados en esta tabla son ejemplos ilustrativos basados en los dominios conocidos. Los nombres reales, su alcance y su ubicación se determinan al especificar y desarrollar cada funcionalidad.

### 6.3. Estructura interna de un grupo de modelos

Cada entidad en `models/` (ya sea en `shared/` o en un dominio) sigue la misma estructura de 3 archivos + barrel:

```
models/
├── well.model.ts       # Interfaz del dominio → lo que usa el frontend
├── well.dto.ts         # Interfaz del contrato de la API → lo que devuelve el backend
├── well.mapper.ts      # Función pura: DTO → Model
└── index.ts            # Barrel export del grupo
```

```typescript
// well.dto.ts → refleja exactamente la respuesta de la API (snake_case del backend)
export interface WellDTO {
  well_id: string;
  well_name: string;
  operator_code: number;
  status_code: string;
}

// well.model.ts → modelo limpio en camelCase que usa el frontend
export interface Well {
  id: string;
  name: string;
  operatorId: number;
  status: WellStatus;
}

// well.mapper.ts → función pura, sin efectos secundarios, testeable de forma aislada
export function mapWellDTOToModel(dto: WellDTO): Well {
  return {
    id: dto.well_id,
    name: dto.well_name,
    operatorId: dto.operator_code,
    status: dto.status_code as WellStatus,
  };
}

// index.ts → barrel export: un solo punto de entrada para todo el grupo
export * from './well.model';
export * from './well.dto';
export * from './well.mapper';
```

**Por qué el mapper:** Si el backend cambia `well_id` por `id`, se actualiza solo el mapper — ningún template, componente ni selector se rompe.

> **Referencia:** El modelo `Well` mostrado es un ejemplo del patrón. Los campos, tipos y estructura real de cada entidad se definen según el contrato de API establecido en la especificación funcional correspondiente.

### 6.4. Servicios: tipos y responsabilidades

```typescript
// shared/services/catalog.service.ts → catálogos read-only, sin lógica de negocio
// Solo hace GET, no escribe datos, no conoce reglas de negocio
@Injectable({ providedIn: 'root' })
export class CatalogService {
  getWellTypes(): Observable<WellType[]> { ... }
  getOperators(): Observable<Operator[]> { ... }
}

// domains/wells/services/wells-api.service.ts → CRUD específico del dominio
// Conoce los endpoints de pozos, aplica mappers, devuelve modelos limpios
@Injectable({ providedIn: 'root' })
export class WellsApiService {
  getWell(id: string): Observable<Well> {
    return this.http.get<WellDTO>(`/api/wells/${id}`).pipe(
      map(mapWellDTOToModel)
    );
  }
  createWell(payload: CreateWellDTO): Observable<Well> { ... }
}
```

**Regla:** Los servicios de dominio solo hacen llamadas HTTP y aplican mappers. Nunca manejan errores HTTP — eso es responsabilidad exclusiva de `core/http/` (ver sección 5).

> **Referencia:** `CatalogService` y `WellsApiService` son ejemplos del patrón. Los servicios reales, sus métodos y los endpoints que consumen se definen al implementar cada feature según su especificación funcional.

### 6.5. Path Aliases: solución al problema de discoverability

Tener modelos y servicios en dos niveles (`shared/` y `domains/`) no implica rutas de importación largas. Se configuran aliases en `tsconfig.json` para que cada import sea predecible y corto:

```json
// tsconfig.json
{
  "compilerOptions": {
    "paths": {
      "@core/*":       ["src/app/core/*"],
      "@shared/*":     ["src/app/shared/*"],
      "@wells/*":      ["src/app/domains/wells/*"],
      "@operations/*": ["src/app/domains/operations/*"],
      "@production/*": ["src/app/domains/production/*"],
      "@admin/*":      ["src/app/domains/admin/*"],
      "@env/*":        ["src/environments/*"]
    }
  }
}
```

**Resultado:** imports siempre predecibles sin importar desde dónde se escriban:

```typescript
// ✅ Con aliases — claro, corto, no depende de la ubicación del archivo
import { Well }            from '@shared/models';
import { WellsApiService } from '@wells/services';
import { APP_LOCALE }      from '@shared/locale/locale';
import { environment }     from '@env/environment';

// ❌ Sin aliases — frágil, se rompe si se mueve el archivo
import { Well } from '../../../shared/models/well.model';
import { WellsApiService } from '../../domains/wells/services/wells-api.service';
```

**Regla:** Toda importación entre capas usa el alias correspondiente. Las importaciones dentro de la misma feature pueden usar rutas relativas cortas.

---

## 7. Patrón de Locale: Desacoplamiento de Textos

**Está estrictamente prohibido "quemar" (hardcode) textos, etiquetas o mensajes directamente en los archivos HTML o TS.** En lugar de librerías de i18n con pipes asíncronos y archivos JSON, se usa un enfoque ágil basado en objetos TypeScript constantes (`locale.ts`), sin dependencias externas.

Los textos se dividen en dos niveles:
- **Global** (`shared/locale/locale.ts`): navegación, errores genéricos, botones comunes.
- **Por Feature** (`domains/.../features/.../locale.ts`): textos exclusivos de esa funcionalidad. Si la feature se elimina, sus textos desaparecen con ella — sin textos huérfanos.

**Por qué este enfoque sobre i18n tradicional:**
- Sin pipes asíncronos ni archivos JSON que sincronizar
- Tipado fuerte: el IDE detecta si una clave de texto desaparece o cambia
- Si el cliente cambia "Taladro" por "Equipo de Perforación", se cambia en un solo `locale.ts` y se refleja en toda la feature
- Los templates quedan limpios y enfocados en estructura, no en texto

---

### Locale Global (`shared/locale/locale.ts`)

Contiene todos los textos del wrapper de la aplicación: navegación, errores genéricos y acciones comunes.

```typescript
// src/app/shared/locale/locale.ts
// REFERENCIA: las claves y textos se ajustan según el diseño final del layout y los flujos definidos
export const APP_LOCALE = {
  nav: {
    wells: 'Pozos',
    operations: 'Operaciones',
    production: 'Producción',
    admin: 'Administración',
  },
  actions: {
    save: 'Guardar',
    cancel: 'Cancelar',
    confirm: 'Confirmar',
    delete: 'Eliminar',
    back: 'Volver',
  },
  errors: {
    generic: 'Ocurrió un error inesperado.',
    noConnection: 'Sin conexión. Verifica tu red.',
    forbidden: 'No tienes permisos para realizar esta acción.',
  },
} as const; // "as const" garantiza tipos literales y evita mutación accidental
```

### Locale de Feature (`domains/wells/features/well-create/locale.ts`)

```typescript
// src/app/domains/wells/features/well-create/locale.ts
// REFERENCIA: las claves, secciones y textos se definen al especificar y desarrollar cada feature
export const WELL_CREATE_LOCALE = {
  title: 'Creación de Pozo',
  steps: {
    general: 'Información General',
    technical: 'Datos Técnicos',
    trajectory: 'Trayectoria',
  },
  fields: {
    wellName: 'Nombre del Pozo',
    uwi: 'UWI',
    operator: 'Operadora',
  },
  actions: {
    submit: 'Radicar Solicitud',
    saveDraft: 'Guardar Borrador',
  },
  messages: {
    success: 'Pozo creado exitosamente.',
    draftSaved: 'Borrador guardado.',
  },
} as const;
```

> **Referencia:** Las claves y textos de ambos locales son ilustrativos. Cada feature define su propio `locale.ts` con las claves que realmente necesita según su especificación funcional. El `APP_LOCALE` global crece únicamente con textos del layout y acciones verdaderamente comunes.

### Cómo se expone en el componente (patrón estándar)

```typescript
// well-create.component.ts
@Component({ ... })
export class WellCreateComponent {
  // "protected readonly" → accesible en template, no modificable, no parte de la API pública
  protected readonly locale = WELL_CREATE_LOCALE;
}
```

```html
<!-- well-create.component.html -->
<h1>{{ locale.title }}</h1>
<button>{{ locale.actions.submit }}</button>
```

**Regla:** Siempre `protected readonly locale = NOMBRE_LOCALE`. Nunca importar la constante directamente en el HTML ni exponer como `public`.

---

## 8. Convenciones de Nomenclatura

Reglas no negociables para garantizar consistencia en el equipo:

| Elemento | Convención | Ejemplo |
|---|---|---|
| Archivos | `kebab-case` | `well-create.component.ts` |
| Clases y Componentes | `PascalCase` | `WellCreateComponent` |
| Interfaces y Modelos | `PascalCase` | `Well`, `WellDTO` |
| Constantes de locale | `UPPER_SNAKE_CASE` | `WELL_CREATE_LOCALE` |
| Signals | `camelCase` | `currentStep`, `isLoading`, `wells` |
| Selectores CSS | prefijo `app-` | `app-well-status-badge` |
| Rutas de archivo | `kebab-case` | `well-status-badge.component.ts` |
| Variables de entorno | `camelCase` en objeto | `environment.apiUrl` |
| Funciones mapper | `mapXDTOToModel` | `mapWellDTOToModel` |
| Archivos de store NgRx | sufijo por tipo | `wells.actions.ts`, `wells.reducer.ts` |

### Convenciones de carpetas dentro de una feature

```
well-create/
├── components/         # Componentes "Dumb" internos de la feature (PascalCase en clase)
├── locale.ts           # Siempre "locale.ts" — nunca "strings.ts", "labels.ts", etc.
├── well-create.component.ts
├── well-create.component.html
└── well-create.component.spec.ts
```

---

## 9. Reglas de Dependencias entre Capas

Esta regla es la más crítica para evitar el acoplamiento que hace colapsar la arquitectura DDD con el tiempo.

```
┌─────────────────────────────────────────────┐
│                  domains/                    │  ← Puede importar de core/ y shared/
│  (wells, operations, production, admin)      │  ← NUNCA importa de otro dominio
└───────────────────┬─────────────────────────┘
                    │ puede importar de ↓
        ┌───────────┴────────────┐
        │        shared/         │  ← Puede importar de utils/ propios
        │  (ui, pipes, locale,   │  ← NUNCA importa de core/ ni domains/
        │   directives, utils)   │
        └───────────┬────────────┘
                    │
        ┌───────────┴────────────┐
        │         core/          │  ← No importa de shared/ui/ ni domains/
        │  (auth, guards, http,  │  ← Solo puede importar de shared/utils/
        │       layout)          │
        └────────────────────────┘
```

**Reglas explícitas:**

1. `domains/wells/` **NO puede** importar de `domains/operations/`. Si dos dominios comparten lógica, esa lógica sube a `shared/`.
2. `shared/` **NO puede** importar de ningún dominio — si lo hace, ya no es "shared", es parte del dominio.
3. `core/` **NO puede** importar componentes de `shared/ui/` — puede importar utilidades puras de `shared/utils/`.
4. Toda comunicación entre dominios pasa por el **Store (NgRx)** o por un **servicio en `core/`**, nunca por importación directa.

---

## 10. Seguridad (OWASP Top 10 — Aplicación Frontend)

La seguridad no es opcional. Estas reglas son de cumplimiento obligatorio desde el primer commit.

### A01 — Control de Acceso Roto
- **Route Guards obligatorios** en todas las rutas protegidas (nunca dejar una ruta sin guard si requiere autenticación).
- **RBAC en guards:** verificar rol + autenticación. No asumir que "si está logueado, puede todo".
- **Nunca** ocultar rutas solo en el sidebar — el guard en el router es la única defensa real.

```typescript
// core/guards/auth.guard.ts
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};

// core/guards/role.guard.ts
export const roleGuard = (roles: string[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  return roles.includes(auth.getUserRole()) ? true : inject(Router).createUrlTree(['/forbidden']);
};
```

### A03 — Inyección (XSS)
- **Prohibido** usar `[innerHTML]` sin sanitización. Si es absolutamente necesario, usar `DomSanitizer.sanitizeHtml()`.
- **Prohibido** usar `bypassSecurityTrustHtml()` salvo excepciones documentadas y revisadas.
- Angular escapa automáticamente la interpolación `{{ }}` — usar siempre interpolación, nunca concatenación en atributos.

```html
<!-- ✅ Seguro: Angular escapa el valor -->
<p>{{ userInput }}</p>

<!-- ❌ Peligroso: XSS directo -->
<p [innerHTML]="userInput"></p>

<!-- ✅ Si se necesita HTML dinámico (caso excepcional documentado) -->
<p [innerHTML]="sanitizer.sanitize(SecurityContext.HTML, userInput)"></p>
```

### A02 — Fallos Criptográficos / Exposición de Datos
- **Prohibido** almacenar tokens JWT en `localStorage`. Usar `sessionStorage` o cookies `HttpOnly` (coordinar con backend).
- **Prohibido** loggear datos sensibles en `console.log` — remover todos los logs antes de merge a `main`.
- **Prohibido** incluir credenciales, API keys o URLs de producción en el código fuente. Usar `environments/`.

```typescript
// ✅ Correcto: token en sessionStorage (se limpia al cerrar el tab)
sessionStorage.setItem('token', token);

// ❌ Prohibido: localStorage persiste indefinidamente
localStorage.setItem('token', token);
```

### A07 — Fallos de Autenticación
- El `AuthService` es el **único** punto de acceso al token — ningún componente ni servicio de dominio lo lee directamente.
- Implementar refresh automático del token antes de su expiración.
- El logout debe limpiar el store de NgRx, `sessionStorage`, y redirigir.

```typescript
// core/auth/auth.service.ts
logout(): void {
  this.store.dispatch(clearSession());    // Limpia NgRx store
  sessionStorage.clear();                 // Limpia storage
  this.router.navigate(['/login']);
}
```

### A05 — Configuración Insegura
- Los `environments/` **nunca** se suben al repositorio si contienen datos sensibles — usar `.gitignore` y variables de CI/CD.
- El archivo `environments/environment.ts` solo expone URLs y flags de feature — nunca secrets.
- Configurar **Content Security Policy (CSP)** en el servidor (coordinar con backend/infraestructura).

```typescript
// environments/environment.ts (solo datos públicos, nunca secrets)
export const environment = {
  production: false,
  apiUrl: 'https://api-dev.gop.internal',
  featureFlags: {
    enableBetaWizard: true,
  },
};
```

### A06 — Componentes Vulnerables
- Ejecutar `npm audit` antes de cada release y corregir vulnerabilidades `high` y `critical` obligatoriamente.
- Mantener las dependencias de Angular y PrimeNG actualizadas en cada sprint.
- No agregar librerías npm sin revisión previa del equipo (evitar supply chain attacks).

### A09 — Registro y Monitoreo
- El dominio `admin/audit-logs/` debe registrar todas las acciones sensibles: creación/eliminación de pozos, cambios de roles, aprobaciones de formas.
- Los errores 4xx y 5xx capturados en el interceptor deben enviarse a un sistema de monitoreo (ej. un endpoint de logging en el backend) — no solo mostrar toast al usuario.

---

## 11. Librería de Componentes y Estilos CSS

Esta arquitectura se basa en un enfoque de Diseño Atómico simplificado, optimizando PrimeNG para funcionalidad y Tailwind CSS para diseño visual.

### 11.1. Estrategia de Composición

**Principio rector: "PrimeNG para la lógica compleja, Tailwind para la estructura y estética".**

- **PrimeNG:** exclusivamente para componentes con lógica de estado compleja (tablas con filtros, modales con animaciones, selectores de fecha, drag & drop).
- **Tailwind CSS:** layouting (Grids/Flexbox), espaciado, tipografía y personalización de PrimeNG mediante Pass Through (PT).

### 11.2. Definición de Uso por Tipo de Componente

**A. Contenedores Básicos (Layout)**
- Regla: Prohibido usar PrimeNG para estructuras simples.
- Uso: Tailwind CSS puro (`div`, `section`, `main` con clases utilitarias).
- Por qué: Evita sobrecarga de selectores CSS y facilita cambios de diseño sin tocar el TS.

**B. Tablas y Grids de Datos**
- Regla: Usar `p-table` de PrimeNG.
- Personalización: Aplicar estilos de Tailwind mediante `styleClass` o Pass Through (`pt: { header: 'bg-slate-100 text-sm' }`).
- Por qué: Reinventar paginación, filtrado y ordenación es ineficiente y propenso a errores.

**C. Modales y Overlays**
- Regla: Usar `DialogService` de PrimeNG disparado desde un servicio centralizado.
- Personalización: Tailwind para el contenido interno del modal.
- Por qué: PrimeNG gestiona z-index, foco del teclado (accesibilidad) y bloqueo del scroll de forma nativa.

### 11.3. Cuándo Crear Componentes Personalizados (Wrappers)

**Por defecto: usar PrimeNG directamente.** Los wrappers no son la norma — son la excepción justificada. Crearlos sin criterio genera indirección innecesaria y duplica el trabajo de mantener la API del componente original.

El sistema de **Pass Through (PT) global** en `providePrimeNG({ pt: {...} })` ya resuelve la consistencia visual sin necesidad de wrappers. Úsalo siempre primero.

Un wrapper se crea **únicamente** cuando se cumple al menos uno de estos tres criterios:

#### Criterio 1 — Abstracción de Negocio (Smart Component repetido)
El mismo componente + lógica de API aparece en 2 o más features distintas.

```typescript
// ✅ Justificado: WellStatusDropdown siempre carga el mismo catálogo y aplica la misma lógica
// Se repite en well-create, well-manage y el filtro del listado → tiene sentido abstraerlo
@Component({ selector: 'app-well-status-dropdown', ... })
export class WellStatusDropdownComponent {
  options = toSignal(inject(CatalogService).getWellStatuses());
}

// ❌ No justificado: un p-dropdown que solo aparece en un formulario → usar p-dropdown directo
```

#### Criterio 2 — Comportamiento Forzado (lo que PT no puede hacer)
Se necesita añadir lógica de negocio transversal que el sistema PT de PrimeNG no puede manejar: verificación de permisos, eventos de auditoría, estados de carga estandarizados.

```typescript
// ✅ Justificado: AppButton agrega verificación de permiso y estado loading uniforme
// PT puede cambiar estilos, pero no puede inyectar lógica de autorización
@Component({ selector: 'app-button', ... })
export class AppButtonComponent {
  @Input() permission?: string;           // Oculta el botón si no tiene el permiso
  @Input() loading = false;               // Estado de carga estandarizado
  protected canRender = inject(AuthService).hasPermission(this.permission);
}

// ❌ No justificado: un p-button con solo un color diferente → usar styleClass o PT
```

#### Criterio 3 — UI Altamente Específica (PrimeNG como base, no como fin)
El componente de PrimeNG requiere tantos overrides de CSS o configuración que un componente propio con Tailwind resulta más simple y mantenible.

```
Señal de alerta: si el PT de un componente supera ~10 claves de configuración
para lograr el diseño requerido, considerar construirlo desde cero con Tailwind.
```

#### Tabla de decisión rápida

| Caso | Solución |
|---|---|
| Mismo estilo en todos los botones del proyecto | PT global en `providePrimeNG` |
| `p-dropdown` que siempre carga el mismo catálogo y aparece en 3+ features | Wrapper (Criterio 1) |
| `p-button` con validación de permiso RBAC | Wrapper (Criterio 2) |
| `p-tag` con un color distinto | `styleClass` directo, sin wrapper |
| `p-calendar` usado una sola vez | `p-calendar` directo, sin wrapper |
| Diseño de card totalmente personalizado sin lógica compleja | Tailwind puro, sin PrimeNG |

#### Dónde vive cada wrapper una vez creado

La ubicación depende de una sola pregunta: **¿el wrapper conoce conceptos del negocio (pozos, operaciones, producción)?**

| ¿Conoce el dominio? | ¿Cuántas features lo usan? | Ubicación |
|---|---|---|
| No — agnóstico al negocio | Cualquiera | `shared/ui/` |
| Sí — conoce el dominio | 2+ features del mismo dominio | `domains/[dominio]/components/` |
| Sí — conoce el dominio | Solo una feature | `domains/[dominio]/features/[feature]/components/` |

**Ejemplos:**

```
shared/ui/
├── app-button/             # p-button + RBAC + loading → no sabe qué es un "Pozo"
├── app-confirm-button/     # p-button + confirmación integrada → agnóstico
├── app-modal/              # DialogService + estructura base de contenido
└── app-data-table/         # p-table + paginación + skeleton → agnóstico

domains/wells/components/
├── well-status-badge/      # p-tag con colores de WellStatus → sabe del dominio wells
└── well-status-dropdown/   # p-dropdown que carga catálogo de estados → sabe del dominio wells

domains/wells/features/well-create/components/
└── uwi-preview/            # Solo lo usa el wizard de creación → vive dentro de la feature
```

> **Referencia:** Los componentes y wrappers listados son ejemplos del patrón. Los wrappers reales se crean únicamente cuando una feature los requiere y se cumplen los criterios definidos en este mismo punto. No crear wrappers anticipados sin una necesidad concreta.

> `shared/utils/` es exclusivamente para funciones puras (sin componentes, sin estado):
> `formatUWI()`, `parseCoordinates()`, `calculateDepth()`.
> Nunca colocar componentes o servicios ahí.

### 11.4. Mejores Prácticas

- **Signals & Standalone:** Usar Angular Signals para estado de UI de componentes (abrir/cerrar modales, loading) para detección de cambios ultra rápida.
- **Configuración Global:** Definir un preset de Tailwind alineado con los colores de PrimeNG para evitar inconsistencias visuales.
- **Pass Through Global:** Configurar estilos por defecto en `providePrimeNG({ pt: {...} })` para que todos los componentes compartan la misma estética sin necesidad de repetir clases.

---

## 12. Ambientes y Variables de Entorno (DEV / QA / PROD)

### 12.1. Principio de funcionamiento

Angular utiliza **file replacement** en tiempo de compilación: todo el código importa siempre `environment.ts`, y el compilador lo reemplaza físicamente por el archivo del perfil indicado en el flag `--configuration`. No hay lógica condicional en runtime.

```
src/environments/
├── environment.interface.ts   ← Contrato TypeScript compartido por los 3 perfiles
├── environment.ts             ← DEV  (default — el que importa todo el código)
├── environment.qa.ts          ← QA   (reemplaza environment.ts en build QA)
└── environment.prod.ts        ← PROD (reemplaza environment.ts en build PROD)
```

**Regla de seguridad:** Los archivos `environment.qa.ts` y `environment.prod.ts` **nunca se suben al repositorio**. Se generan en el pipeline de CI/CD mediante variables de entorno del servidor. Solo `environment.ts` (DEV) se versiona como referencia de estructura.

```
# .gitignore
src/environments/environment.qa.ts
src/environments/environment.prod.ts
```

### 12.2. Interfaz tipada (`environment.interface.ts`)

La interfaz garantiza que los 3 archivos de ambiente tengan siempre la misma forma. TypeScript obliga a declarar todos los hosts en cada perfil — no es posible olvidar uno.

```typescript
// src/environments/environment.interface.ts
export interface AppEnvironment {
  name: 'development' | 'qa' | 'production';
  production: boolean;
  hosts: {
    gopApi: string; // API principal del sistema GOP
    // Al integrar un nuevo servicio externo, agregar su clave aquí
    // y declarar su URL en los 3 archivos de ambiente
  };
  featureFlags: {
    enableBetaFeatures: boolean;
  };
}
```

### 12.3. Archivos por perfil

```typescript
// src/environments/environment.ts — DEV
import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'development',
  production: false,
  hosts: {
    gopApi: 'http://localhost:3000',
  },
  featureFlags: { enableBetaFeatures: true },
};
```

```typescript
// src/environments/environment.qa.ts — QA
import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'qa',
  production: false,
  hosts: {
    gopApi: 'https://api-qa.gop.internal',
  },
  featureFlags: { enableBetaFeatures: true },
};
```

```typescript
// src/environments/environment.prod.ts — PROD
import { AppEnvironment } from './environment.interface';

export const environment: AppEnvironment = {
  name: 'production',
  production: true,
  hosts: {
    gopApi: 'https://api.gop.internal',
  },
  featureFlags: { enableBetaFeatures: false },
};
```

> Los hosts y URLs mostrados son de referencia. Las URLs reales de cada ambiente se definen según la infraestructura del proyecto. Al integrar un nuevo servicio externo, se agrega su clave en `AppEnvironment.hosts` y su URL en los 3 archivos.

### 12.4. Centralización de endpoints (`api-endpoints.ts`)

Los paths de la API **no cambian entre ambientes** — solo cambia el host. Por eso viven en un archivo de constantes separado en `core/http/`. Los servicios importan `API`, nunca `environment` directamente.

```typescript
// src/app/core/http/api-endpoints.ts
// REFERENCIA: los grupos y paths se definen según las especificaciones funcionales de cada dominio
import { environment } from '@env/environment';

const { hosts } = environment;

export const API = {
  wells: {
    base:    `${hosts.gopApi}/api/v1/wells`,
    byId:    (id: string) => `${hosts.gopApi}/api/v1/wells/${id}`,
  },
  operations: {
    base: `${hosts.gopApi}/api/v1/operations`,
  },
  production: {
    base: `${hosts.gopApi}/api/v1/production`,
  },
  admin: {
    base: `${hosts.gopApi}/api/v1/admin`,
  },
  catalogs: {
    base: `${hosts.gopApi}/api/v1/catalogs`,
  },
} as const;
```

> Los grupos de endpoints y sus paths son de referencia. Cada endpoint específico se agrega al momento de implementar la feature que lo requiere, según el contrato definido en la especificación funcional.

**Uso en servicios:**
```typescript
// ✅ El servicio importa API, no environment
import { API } from '@core/http/api-endpoints';

getWell(id: string): Observable<Well> {
  return this.http.get<WellDTO>(API.wells.byId(id)).pipe(map(mapWellDTOToModel));
}

// ❌ Prohibido: el servicio no construye URLs directamente
getWell(id: string): Observable<Well> {
  return this.http.get<WellDTO>(`${environment.hosts.gopApi}/api/v1/wells/${id}`);
}
```

### 12.5. Configuración en `angular.json`

```json
"configurations": {
  "production": {
    "fileReplacements": [
      { "replace": "src/environments/environment.ts",
        "with":    "src/environments/environment.prod.ts" }
    ],
    "optimization": true,
    "sourceMap": false,
    "outputHashing": "all",
    "budgets": [
      { "type": "initial", "maximumWarning": "500kB", "maximumError": "1MB" },
      { "type": "anyComponentStyle", "maximumWarning": "4kB", "maximumError": "8kB" }
    ]
  },
  "qa": {
    "fileReplacements": [
      { "replace": "src/environments/environment.ts",
        "with":    "src/environments/environment.qa.ts" }
    ],
    "optimization": true,
    "sourceMap": true,
    "outputHashing": "all",
    "budgets": [
      { "type": "initial", "maximumWarning": "500kB", "maximumError": "1MB" },
      { "type": "anyComponentStyle", "maximumWarning": "4kB", "maximumError": "8kB" }
    ]
  },
  "development": {
    "optimization": false,
    "sourceMap": true,
    "extractLicenses": false,
    "namedChunks": true
  }
}
```

### 12.6. Scripts de compilación (`package.json`)

| Script npm | Comando Angular | Perfil | Optimizado | Source map |
|---|---|---|---|---|
| `npm start` | `ng serve` | DEV | No | Sí |
| `npm run start:qa` | `ng serve --configuration=qa` | QA | Sí | Sí |
| `npm run build:dev` | `ng build --configuration=development` | DEV | No | Sí |
| `npm run build:qa` | `ng build --configuration=qa` | QA | Sí | Sí |
| `npm run build:prod` | `ng build --configuration=production` | PROD | Sí | No |

### 12.7. Por qué `hosts` en lugar de un solo `baseUrl`

| Escenario | `baseUrl` único | `hosts` por servicio |
|---|---|---|
| Un solo backend en todos los ambientes | Funciona | Funciona |
| Servicios externos con URL diferente por ambiente | No soportado | Funciona |
| Agregar un nuevo host sin tocar servicios existentes | Requiere refactor | Solo agregar clave en `hosts` e interfaz |
| TypeScript detecta host faltante en un perfil | No | Sí — error en compilación |
