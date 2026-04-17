# Plan Frontend: Máquina de Estados del Pozo

**Feature:** 007-well-state-machine
**Contrato:** `specs/features/007-well-state-machine/contract.yml`
**Constitución:** CONSTITUTION.md (frontend Angular)

---

## 1. Estrategia General

### Estado: Signals (no NgRx)

La vista de detalle y sus acciones de transición son **feature-local**: la carga del pozo, el historial y el estado de loading viven y mueren con el componente `WellDetailComponent`. No necesitan sobrevivir a la destrucción del componente ni se comparten entre dominios.

Per CONSTITUTION.md §4: "Si un desarrollador tiene duda, usa Signals. Solo escala a NgRx cuando el estado cruce los límites de la feature."

El listado de pozos (`well-manage`) refetcha al navegar (comportamiento estándar), por lo que no necesita sincronización bidireccional con el detalle.

### Componentes: Smart vs Dumb

| Componente                  | Tipo  | Ubicación                                                      |
|-----------------------------|-------|----------------------------------------------------------------|
| `WellDetailComponent`       | Smart | `domains/wells/features/well-detail/`                          |
| `WellStatusBadgeComponent`  | Dumb  | `domains/wells/components/well-status-badge/`                  |
| `WellTransitionActionsComponent` | Dumb  | `domains/wells/features/well-detail/components/well-transition-actions/` |
| `WellHistoryTimelineComponent`   | Dumb  | `domains/wells/features/well-detail/components/well-history-timeline/`   |
| `TransitionConfirmDialogComponent` | Dumb | `domains/wells/features/well-detail/components/transition-confirm-dialog/` |

**Decisiones de ubicación (ver CONSTITUTION.md §11.3):**
- `WellStatusBadge` va en `domains/wells/components/` porque se usa en 2+ features: el listado (`well-manage`) y el detalle (`well-detail`).
- Los demás componentes solo se usan en `well-detail`, así que viven dentro de la feature.

---

## 2. Árbol de Archivos

```
src/app/
├── domains/wells/
│   ├── components/
│   │   └── well-status-badge/
│   │       ├── well-status-badge.component.ts       # NUEVO — Dumb, p-tag con color por estado
│   │       └── well-status-badge.component.html     # NUEVO
│   │
│   ├── features/
│   │   └── well-detail/                              # NUEVA FEATURE
│   │       ├── well-detail.component.ts              # Smart — carga well + history, orquesta
│   │       ├── well-detail.component.html
│   │       ├── locale.ts                             # Textos de la feature
│   │       └── components/
│   │           ├── well-transition-actions/
│   │           │   ├── well-transition-actions.component.ts   # Dumb — botones contextuales
│   │           │   └── well-transition-actions.component.html
│   │           ├── well-history-timeline/
│   │           │   ├── well-history-timeline.component.ts     # Dumb — timeline de transiciones
│   │           │   └── well-history-timeline.component.html
│   │           └── transition-confirm-dialog/
│   │               ├── transition-confirm-dialog.component.ts  # Dumb — diálogo confirm + devolver
│   │               └── transition-confirm-dialog.component.html
│   │
│   ├── models/
│   │   ├── well-transition.model.ts     # NUEVO — TransitionAction, TransitionResult, TransitionHistoryItem
│   │   ├── well-transition.dto.ts       # NUEVO — DTOs espejo del contract.yml
│   │   ├── well-transition.mapper.ts    # NUEVO — DTO → Model
│   │   └── index.ts                     # MODIFICAR — agregar re-exports
│   │
│   ├── services/
│   │   └── wells-api.service.ts         # MODIFICAR — agregar transitionWell() y getWellHistory()
│   │
│   ├── mocks/
│   │   └── wells.mock.ts               # MODIFICAR — agregar handlers para transition y history
│   │
│   └── wells.routes.ts                  # MODIFICAR — agregar ruta /wells/:id
│
├── shared/models/
│   ├── well.model.ts                    # MODIFICAR — agregar campo uwi: string | null
│   ├── well.dto.ts                      # MODIFICAR — agregar campo uwi
│   └── well.mapper.ts                   # MODIFICAR — mapear uwi
│
└── core/http/
    └── api-endpoints.ts                 # MODIFICAR — agregar endpoints de transition y history
```

---

## 3. Modelos y DTOs

### 3.1. Extensión: Well model/dto (shared/models/)

Adición no-breaking del campo `uwi`:

```typescript
// well.dto.ts — agregar
uwi: string | null;

// well.model.ts — agregar
uwi: string | null;

// well.mapper.ts — agregar al mapeo
uwi: dto.uwi,
```

### 3.2. Nuevos modelos de transición (domains/wells/models/)

**well-transition.model.ts:**
```typescript
export type TransitionAction = 'ENVIAR' | 'APROBAR_UWI' | 'DEVOLVER' | 'FISCALIZAR';

export interface TransitionResult {
  id: string;
  estado: string;
  estadoAnterior: string;
  uwi: string | null;
  action: TransitionAction;
  comment: string | null;
  transitionedAt: string;
  transitionedBy: string;
}

export interface TransitionHistoryItem {
  id: string;
  fromState: string;
  toState: string;
  action: TransitionAction;
  comment: string | null;
  performedBy: string;
  performedByName: string;
  performedByRole: string;
  createdAt: string;
}
```

**well-transition.dto.ts:**
```typescript
export interface TransitionRequestDTO {
  action: string;
  comment?: string | null;
}

export interface TransitionResultDTO {
  id: string;
  estado: string;
  estadoAnterior: string;
  uwi: string | null;
  action: string;
  comment: string | null;
  transitionedAt: string;
  transitionedBy: string;
}

export interface TransitionHistoryItemDTO {
  id: string;
  fromState: string;
  toState: string;
  action: string;
  comment: string | null;
  performedBy: string;
  performedByName: string;
  performedByRole: string;
  createdAt: string;
}
```

**well-transition.mapper.ts:**
```typescript
export function mapTransitionResultDTOToModel(dto: TransitionResultDTO): TransitionResult { ... }
export function mapTransitionHistoryItemDTOToModel(dto: TransitionHistoryItemDTO): TransitionHistoryItem { ... }
```

### 3.3. Barrel export (domains/wells/models/index.ts)

Agregar re-exports de los nuevos modelos de transición.

---

## 4. Servicio API

### 4.1. Extensión: `WellsApiService` (domains/wells/services/wells-api.service.ts)

Nuevos métodos:

```typescript
transitionWell(wellId: string, action: TransitionAction, comment?: string): Observable<TransitionResult> {
  return this.http.patch<TransitionResultDTO>(API.wells.transition(wellId), { action, comment }).pipe(
    map(mapTransitionResultDTOToModel)
  );
}

getWellHistory(wellId: string): Observable<TransitionHistoryItem[]> {
  return this.http.get<TransitionHistoryItemDTO[]>(API.wells.history(wellId)).pipe(
    map(items => items.map(mapTransitionHistoryItemDTOToModel))
  );
}
```

Per CONSTITUTION.md §5: el servicio no maneja errores HTTP. Solo transforma la respuesta exitosa.

### 4.2. Extensión: API Endpoints (core/http/api-endpoints.ts)

```typescript
wells: {
  // ... existentes ...
  transition: (id: string) => `${hosts.gopApi}/api/v1/wells/${id}/transition`,
  history: (id: string) => `${hosts.gopApi}/api/v1/wells/${id}/history`,
},
```

---

## 5. Mocks HTTP

### 5.1. Extensión: wells.mock.ts

Nuevos handlers:

**PATCH /wells/{id}/transition:**
- Parsea el body `{ action, comment }`
- Valida la acción contra el estado actual del mock well
- Retorna `TransitionResultDTO` con estado actualizado y UWI generado (mock)

**GET /wells/{id}/history:**
- Retorna array de `TransitionHistoryItemDTO[]` con datos de ejemplo

---

## 6. Componentes

### 6.1. WellStatusBadgeComponent (Dumb)

**Ubicación:** `domains/wells/components/well-status-badge/`
**Inputs:** `estado: string` (WellStatus)
**Lógica:** Mapea estado a `{ label, severity, icon }` para `p-tag` de PrimeNG.

| Estado         | Severity  | Label           | Icono              |
|----------------|-----------|-----------------|--------------------|
| BORRADOR       | secondary | Borrador        | pi pi-pencil       |
| PENDING_UWI    | warn      | Pendiente UWI   | pi pi-clock        |
| READY_FISCAL   | info      | Listo Fiscal    | pi pi-check-circle |
| FISCALIZADO    | success   | Fiscalizado     | pi pi-verified     |

```typescript
@Component({
  selector: 'app-well-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Tag],
})
export class WellStatusBadgeComponent {
  estado = input.required<string>();
  protected readonly config = computed(() => STATUS_CONFIG[this.estado()] ?? STATUS_CONFIG['BORRADOR']);
}
```

### 6.2. WellTransitionActionsComponent (Dumb)

**Ubicación:** `domains/wells/features/well-detail/components/well-transition-actions/`
**Inputs:** `estado`, `userRole`, `isLoading`
**Outputs:** `transition` (emite `{ action: TransitionAction }`)

Lógica de visibilidad (computed):

```typescript
protected readonly availableActions = computed(() => {
  const estado = this.estado();
  const role = this.userRole();
  return TRANSITION_MATRIX
    .filter(t => t.from === estado && t.roles.includes(role))
    .map(t => t.action);
});
```

Constante `TRANSITION_MATRIX` define las transiciones válidas por estado/rol, espejando la tabla del spec §2.2.

### 6.3. WellHistoryTimelineComponent (Dumb)

**Ubicación:** `domains/wells/features/well-detail/components/well-history-timeline/`
**Inputs:** `history: TransitionHistoryItem[]`, `isLoading: boolean`
**Template:** PrimeNG `p-timeline` con cada evento mostrando: acción, de→a, usuario (nombre + rol), fecha, y comentario (destacado para DEVOLVER).

### 6.4. TransitionConfirmDialogComponent (Dumb)

**Ubicación:** `domains/wells/features/well-detail/components/transition-confirm-dialog/`
**Inputs:** `visible: boolean`, `action: TransitionAction | null`, `isLoading: boolean`
**Outputs:** `confirm` (emite `{ action, comment? }`), `cancel`

Lógica:
- Si `action === 'DEVOLVER'`: muestra textarea para comentario (min 10 chars), botón disabled hasta válido
- Otros: muestra mensaje de confirmación simple
- Botón de confirmar con loading spinner

### 6.5. WellDetailComponent (Smart)

**Ubicación:** `domains/wells/features/well-detail/`
**Ruta:** `/wells/:id`

**Signals de estado local:**
```typescript
protected readonly well = signal<Well | null>(null);
protected readonly history = signal<TransitionHistoryItem[]>([]);
protected readonly isLoadingWell = signal(true);
protected readonly isLoadingHistory = signal(true);
protected readonly isTransitioning = signal(false);
protected readonly dialogVisible = signal(false);
protected readonly dialogAction = signal<TransitionAction | null>(null);
```

**Flujo:**
1. `OnInit`: lee `wellId` de `ActivatedRoute.params`, llama `wellsApi.getWell(id)` y `wellsApi.getWellHistory(id)`
2. Pasa datos a hijos via inputs
3. Recibe evento `transition` de `WellTransitionActionsComponent` → abre dialog
4. Recibe evento `confirm` de dialog → llama `wellsApi.transitionWell(id, action, comment)`
5. On success: actualiza `well` signal (nuevo estado, uwi), refetch history, muestra toast
6. On error: handled by `errorInterceptor` (CONSTITUTION.md §5)

**Rol del usuario:** se obtiene del store NgRx de auth (`selectCurrentUser`) o del `AuthService`. Se pasa como input a `WellTransitionActionsComponent`.

---

## 7. Rutas

### 7.1. Extensión: wells.routes.ts

```typescript
{
  path: ':id',
  loadComponent: () => import('./features/well-detail/well-detail.component')
    .then(m => m.WellDetailComponent),
},
```

Se inserta **antes** de la ruta `:id/edit` para que Angular resuelva la ruta correcta. No requiere guard especial más allá del `authGuard` ya existente en el dominio (todos los roles autenticados pueden ver detalle).

### 7.2. Navegación desde listado

El componente `well-manage` (listado) necesita un enlace al detalle. Esto se resuelve haciendo el nombre del pozo clickeable en la tabla, navegando a `/wells/{id}`. Si el componente de listado ya existe, se modifica el template para agregar un `routerLink`.

---

## 8. Locale

### 8.1. Feature locale: `well-detail/locale.ts`

```typescript
export const WELL_DETAIL_LOCALE = {
  title: 'Detalle del Pozo',
  sections: {
    contract: 'Información del Contrato',
    technical: 'Datos Técnicos',
    location: 'Ubicación Geográfica',
    history: 'Historial de Transiciones',
  },
  status: {
    BORRADOR: 'Borrador',
    PENDING_UWI: 'Pendiente UWI',
    READY_FISCAL: 'Listo para Fiscalización',
    FISCALIZADO: 'Fiscalizado',
  },
  actions: {
    ENVIAR: 'Enviar para UWI',
    APROBAR_UWI: 'Aprobar UWI',
    DEVOLVER: 'Devolver a Borrador',
    FISCALIZAR: 'Fiscalizar',
    edit: 'Editar Pozo',
  },
  dialog: {
    confirmTitle: 'Confirmar Transición',
    confirmMessage: '¿Está seguro de ejecutar esta acción?',
    devolverTitle: 'Devolver a Borrador',
    devolverPlaceholder: 'Ingrese el motivo de la devolución (mínimo 10 caracteres)...',
    confirm: 'Confirmar',
    cancel: 'Cancelar',
  },
  history: {
    empty: 'No hay transiciones registradas.',
    by: 'por',
    reason: 'Motivo:',
  },
  uwi: {
    label: 'UWI',
    pending: 'Sin UWI asignado',
  },
  messages: {
    transitionSuccess: 'Transición ejecutada exitosamente.',
    wellNotFound: 'Pozo no encontrado.',
  },
} as const;
```

### 8.2. Extensión: APP_LOCALE (shared/locale/locale.ts)

No se requieren cambios al locale global. Los textos de estado y transición son exclusivos de esta feature.

---

## 9. Dependencias con Features Anteriores

| Dependencia | Feature | Impacto en 007 |
|-------------|---------|-----------------|
| `Well` model/dto/mapper | 005 | Se extiende con campo `uwi` |
| `WellsApiService` | 005 | Se extienden con 2 métodos nuevos |
| `wells.mock.ts` | 005 | Se extienden con 2 handlers nuevos |
| `wells.routes.ts` | 005/006 | Se agrega ruta `:id` |
| `api-endpoints.ts` | 005 | Se agregan 2 endpoints |
| `WellDetail` schema | 005 contract | Campo `uwi` añadido (non-breaking) |
| `well-manage` template | 005 | Se agrega link al detalle (routerLink en nombre) |
