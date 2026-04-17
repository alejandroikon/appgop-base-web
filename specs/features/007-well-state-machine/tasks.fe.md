# Tareas Frontend: Máquina de Estados del Pozo

**Feature:** 007-well-state-machine
**Plan:** `specs/features/007-well-state-machine/plan.fe.md`
**Contrato:** `specs/features/007-well-state-machine/contract.yml`

---

## Bloque 1 — Modelos y Tipos (`ng build`)

Modelos, DTOs, mappers y barrel exports. Sin dependencias de componentes.

- [x] **T001** `[P]` Crear `src/app/domains/wells/models/well-transition.model.ts`
  - Tipo: `TransitionAction = 'ENVIAR' | 'APROBAR_UWI' | 'DEVOLVER' | 'FISCALIZAR'`
  - Interface: `TransitionResult` con campos: id, estado, estadoAnterior, uwi, action, comment, transitionedAt, transitionedBy
  - Interface: `TransitionHistoryItem` con campos: id, fromState, toState, action, comment, performedBy, performedByName, performedByRole, createdAt

- [x] **T002** `[P]` Crear `src/app/domains/wells/models/well-transition.dto.ts`
  - Interface: `TransitionRequestDTO` con campos: action (string), comment (string | null)
  - Interface: `TransitionResultDTO` — espejo del schema TransitionResult del contract.yml
  - Interface: `TransitionHistoryItemDTO` — espejo del schema TransitionHistoryItem del contract.yml

- [x] **T003** `[P]` Crear `src/app/domains/wells/models/well-transition.mapper.ts`
  - Función: `mapTransitionResultDTOToModel(dto: TransitionResultDTO): TransitionResult`
  - Función: `mapTransitionHistoryItemDTOToModel(dto: TransitionHistoryItemDTO): TransitionHistoryItem`
  - Mapeo directo (camelCase→camelCase), cast de action a TransitionAction type

- [x] **T004** Modificar `src/app/domains/wells/models/well.model.ts`
  - Agregar campo: `uwi: string | null` a `WellListItem` y `Well`

- [x] **T005** Modificar `src/app/domains/wells/models/well.dto.ts`
  - Agregar campo: `uwi: string | null` a `WellListItemDTO` y `WellDetailDTO`

- [x] **T006** Modificar `src/app/domains/wells/models/well.mapper.ts`
  - Agregar mapeo: `uwi: dto.uwi ?? null` en ambos mappers

- [x] **T007** Modificar `src/app/domains/wells/models/index.ts`
  - Agregar re-exports de `well-transition.model.ts`, `well-transition.dto.ts`, `well-transition.mapper.ts`
  - Usar `export type` para interfaces/tipos, `export` para funciones

- [x] **T005b** `[EMERGENTE]` Modificar `src/app/domains/wells/mocks/wells.mock-handlers.ts`
  - Agregar campo `uwi` a todos los registros de `MOCK_WELLS_LIST` y `MOCK_WELLS_DETAIL`
  - Agregar campo `uwi: null` a los objetos creados por handlers POST y PUT
  - Necesario porque los tipos `WellListItemDTO` y `WellDetailDTO` ahora requieren el campo `uwi`

**Verificación:** `ng build` ✅

---

## Bloque 2 — Endpoints y Servicio API (`ng build`)

Endpoints centralizados y métodos de servicio. Depende de Bloque 1.

- [x] **T008** Modificar `src/app/core/http/api-endpoints.ts`
  - Agregar al grupo `wells`:
    - `transition: (id: string) => \`${hosts.gopApi}/api/v1/wells/${id}/transition\``
    - `history: (id: string) => \`${hosts.gopApi}/api/v1/wells/${id}/history\``

- [x] **T009** Modificar `src/app/domains/wells/services/wells-api.service.ts`
  - Agregar método: `transitionWell(wellId: string, action: TransitionAction, comment?: string): Observable<TransitionResult>`
    - `this.http.patch<TransitionResultDTO>(API.wells.transition(wellId), { action, comment }).pipe(map(mapTransitionResultDTOToModel))`
  - Agregar método: `getWellHistory(wellId: string): Observable<TransitionHistoryItem[]>`
    - `this.http.get<TransitionHistoryItemDTO[]>(API.wells.history(wellId)).pipe(map(items => items.map(mapTransitionHistoryItemDTOToModel)))`
  - Sin manejo de errores (CONSTITUTION.md §5)

**Verificación:** `ng build` ✅

---

## Bloque 3 — Mocks HTTP (`ng build`)

Handlers mock para desarrollo desacoplado. Depende de Bloque 2.

- [x] **T010** Modificar `src/app/domains/wells/mocks/wells.mock-handlers.ts`
  - Agregar handler: `PATCH /api/v1/wells/{id}/transition`
    - Parsea body `{ action, comment }`
    - Retorna `HttpResponse<TransitionResultDTO>` con:
      - estado calculado según la acción (ENVIAR→PENDING_UWI, APROBAR_UWI→READY_FISCAL, etc.)
      - UWI mock: `CO-50-50568-ALPHA-01-ST` (para ENVIAR)
      - transitionedAt: new Date().toISOString()
      - transitionedBy: "Usuario Mock"
  - Agregar handler: `GET /api/v1/wells/{id}/history`
    - Retorna `HttpResponse<TransitionHistoryItemDTO[]>` con 2 entradas de ejemplo
    - Incluir una entrada ENVIAR y una DEVOLVER (con comment)

**Verificación:** `ng build` ✅

---

## Bloque 4 — Componente de Dominio Compartido (`ng build`)

Badge de estado reutilizable en listado + detalle.

- [x] **T011** Crear `src/app/domains/wells/components/well-status-badge/well-status-badge.component.ts`
  - Selector: `app-well-status-badge`
  - Input: `estado = input.required<string>()`
  - Computed: mapea estado → `{ label, severity, icon }` para PrimeNG `p-tag`
  - Standalone, OnPush
  - Imports: PrimeNG `Tag`
  - Config map inline como const:
    - BORRADOR → severity 'secondary', label 'Borrador', icon 'pi pi-pencil'
    - PENDING_UWI → severity 'warn', label 'Pendiente UWI', icon 'pi pi-clock'
    - READY_FISCAL → severity 'info', label 'Listo Fiscal', icon 'pi pi-check-circle'
    - FISCALIZADO → severity 'success', label 'Fiscalizado', icon 'pi pi-verified'

- [x] **T012** Crear `src/app/domains/wells/components/well-status-badge/well-status-badge.component.html`
  - Template: `<p-tag [value]="config().label" [severity]="config().severity" [icon]="config().icon" />`

**Verificación:** `ng build` ✅

---

## Bloque 5 — Componentes Internos de well-detail (`ng build`)

Componentes dumb usados exclusivamente por WellDetailComponent. Depende de Bloque 4.

- [x] **T013** Crear `src/app/domains/wells/features/well-detail/locale.ts`
  - Constante `WELL_DETAIL_LOCALE` con secciones: title, sections, status, actions, dialog, history, uwi, messages, labels
  - Per plan.fe.md §8.1

- [x] **T014** `[P]` Crear `src/app/domains/wells/features/well-detail/components/well-transition-actions/well-transition-actions.component.ts`
  - Selector: `app-well-transition-actions`
  - Inputs: `estado = input.required<string>()`, `userRole = input.required<string>()`, `isLoading = input(false)`
  - Output: `transition = output<{ action: TransitionAction }>()`
  - Computed `availableActions`: filtra TRANSITION_MATRIX por estado + rol actual
  - Constante TRANSITION_MATRIX interna: array de `{ from, action, roles[], severity }` espejando spec §2.2
  - Standalone, OnPush
  - Imports: PrimeNG `Button`

- [x] **T015** `[P]` Crear `src/app/domains/wells/features/well-detail/components/well-transition-actions/well-transition-actions.component.html`
  - Template: `@for` sobre `availableActions()` → `<p-button>` con label del locale, emit transition on click
  - Todos los botones disabled cuando `isLoading()` es true

- [x] **T016** `[P]` Crear `src/app/domains/wells/features/well-detail/components/well-history-timeline/well-history-timeline.component.ts`
  - Selector: `app-well-history-timeline`
  - Inputs: `history = input.required<TransitionHistoryItem[]>()`, `isLoading = input(false)`
  - Standalone, OnPush
  - Imports: PrimeNG `Timeline`, `Tag`, `Skeleton`
  - Si `history().length === 0` y `!isLoading()` → muestra mensaje vacío del locale

- [x] **T017** `[P]` Crear `src/app/domains/wells/features/well-detail/components/well-history-timeline/well-history-timeline.component.html`
  - Template: PrimeNG `<p-timeline>` con cada item mostrando:
    - Badge de acción (ENVIAR, APROBAR_UWI, etc.)
    - De→A (fromState → toState) con labels del locale
    - Nombre del usuario + rol
    - Fecha formateada
    - Si comment existe → cuadro informativo con motivo de devolución

- [x] **T018** `[P]` Crear `src/app/domains/wells/features/well-detail/components/transition-confirm-dialog/transition-confirm-dialog.component.ts`
  - Selector: `app-transition-confirm-dialog`
  - Inputs: `visible = input(false)`, `action = input<TransitionAction | null>(null)`, `isLoading = input(false)`
  - Outputs: `confirm = output<{ action: TransitionAction; comment?: string }>()`, `cancel = output()`
  - Signal local: `comment = signal('')`
  - Computed: `isDevolver = computed(() => this.action() === 'DEVOLVER')`
  - Computed: `canConfirm = computed(() => !this.isDevolver() || this.comment().trim().length >= 10)`
  - Standalone, OnPush
  - Imports: PrimeNG `Dialog`, `Button`, `Textarea`

- [x] **T019** `[P]` Crear `src/app/domains/wells/features/well-detail/components/transition-confirm-dialog/transition-confirm-dialog.component.html`
  - Template:
    - `<p-dialog>` con header dinámico (DEVOLVER → titulo devolver, otros → titulo confirmar)
    - Si `isDevolver()` → `<textarea>` con placeholder y contador de caracteres
    - Si no → mensaje de confirmación genérico del locale
    - Botones: Cancelar (siempre), Confirmar (disabled si !canConfirm() o isLoading)

**Verificación:** `ng build` ✅

---

## Bloque 6 — Smart Component + Ruta (`ng build`)

Componente contenedor y registro de ruta. Depende de Bloque 5.

- [x] **T020** Crear `src/app/domains/wells/features/well-detail/well-detail.component.ts`
  - Selector: `app-well-detail`
  - Standalone, OnPush
  - Inyecta: `WellsApiService`, `ActivatedRoute`, `Router`, `MessageService` (PrimeNG toast)
  - Lee `userRole` del store NgRx de auth: `inject(Store).select(selectCurrentUser)` → signal con `toSignal()`
  - Signals: well, history, isLoadingWell, isLoadingHistory, isTransitioning, dialogVisible, dialogAction
  - OnInit:
    - Lee `wellId` de `ActivatedRoute.snapshot.paramMap`
    - Llama `wellsApi.getWell(wellId)` → setea well signal, isLoadingWell=false
    - Llama `wellsApi.getWellHistory(wellId)` → setea history signal, isLoadingHistory=false
    - Si error en getWell → navega a `/wells/manage`
  - Método `onTransitionRequested(event: { action })`: abre dialog
  - Método `onTransitionConfirmed(event: { action, comment? })`: ejecuta transición, actualiza estado, refetch history, toast
  - Imports: componentes hijos, `WellStatusBadgeComponent`, PrimeNG `Card`, `Button`, `ProgressSpinner`

- [x] **T021** Crear `src/app/domains/wells/features/well-detail/well-detail.component.html`
  - Layout:
    - Header: nombre del pozo, `<app-well-status-badge>`, UWI (o "Sin UWI" del locale)
    - Sección acciones: `<app-well-transition-actions>` con inputs de estado, rol, loading
    - Botón "Editar" visible solo si estado === 'BORRADOR' y rol no es AUDITOR
    - Secciones de datos: contrato, técnicos, ubicación (read-only, agrupados en `p-card`)
    - Sección historial: `<app-well-history-timeline>` con inputs de history, loading
    - `<app-transition-confirm-dialog>` con bindings bidireccionales
  - Loading state: `p-progressSpinner` mientras isLoadingWell()

- [x] **T022** Modificar `src/app/domains/wells/wells.routes.ts`
  - Agregar ruta `:id` ANTES de `:id/edit` para resolución correcta de Angular

**Verificación:** `ng build` ✅

---

## Bloque 7 — Integración con Listado (`ng build`)

Link desde el listado al detalle. Depende de Bloque 6.

- [x] **T023** Modificar template del listado (`src/app/domains/wells/features/well-manage/well-manage.component.html`)
  - Nombre del pozo envuelto en `<a [routerLink]="['/wells', well.id]">` para navegar al detalle
  - Columna de estado reemplazada por `<app-well-status-badge [estado]="well.estado" />`
  - Columna UWI agregada: muestra `well.uwi ?? '-'`
  - Botón "ver" actualizado para usar `[routerLink]` en lugar de `(onClick)="onEdit(well.id)"`
  - Colspan de emptymessage actualizado a 9

- [x] **T024** Modificar `src/app/domains/wells/features/well-manage/well-manage.component.ts`
  - Agregar `RouterLink` a imports del componente
  - Agregar `WellStatusBadgeComponent` a imports del componente

**Verificación:** `ng build` ✅

---

## Resumen de Tareas

| Bloque | Tareas    | Archivos nuevos | Archivos modificados | Verificación |
|--------|-----------|-----------------|----------------------|--------------|
| 1      | T001–T007 + T005b | 3       | 5                    | `ng build` ✅ |
| 2      | T008–T009 | 0               | 2                    | `ng build` ✅ |
| 3      | T010      | 0               | 1                    | `ng build` ✅ |
| 4      | T011–T012 | 2               | 0                    | `ng build` ✅ |
| 5      | T013–T019 | 7               | 0                    | `ng build` ✅ |
| 6      | T020–T022 | 2               | 1                    | `ng build` ✅ |
| 7      | T023–T024 | 0               | 2                    | `ng build` ✅ |
| **Total** | **25** | **14**          | **11**               |              |

---

## Trazabilidad Contrato → Tareas

| Endpoint del contrato                  | Tareas del servicio | Tareas de componente |
|---------------------------------------|---------------------|----------------------|
| `PATCH /wells/{id}/transition`        | T009 (service)      | T014-T015 (actions), T018-T019 (dialog), T020-T021 (smart) |
| `GET /wells/{id}/history`             | T009 (service)      | T016-T017 (timeline), T020-T021 (smart) |
| Extensión `uwi` en WellDetail/ListItem | T004-T006 (models)  | T023-T024 (listado), T021 (detalle) |
