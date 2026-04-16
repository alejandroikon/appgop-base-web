# Tasks Frontend: Listado y Formulario de Pozos (005-wells-catalog-crud)

**Input:** `specs/features/005-wells-catalog-crud/spec.md` + `specs/features/005-wells-catalog-crud/plan.fe.md`
**Contrato:** `specs/features/005-wells-catalog-crud/contract.yml`
**Constitución:** `CONSTITUTION.md` (frontend)

**Formato:** `[ID] [P?] [HU?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — sin dependencias de tareas incompletas en el mismo bloque
- **[HU-N]**: Historia de usuario de referencia en spec.md
- **[BLOQUEANTE]**: Tareas posteriores no pueden iniciar sin esta completada

---

## Bloque 1 — Modelos y DTOs (ng build)

**Propósito:** Crear los tipos, interfaces DTO, modelos de dominio y mappers para Wells y catálogos. Todos los archivos viven en `domains/wells/models/` (propiedad privada del dominio, ver CONSTITUTION.md §6.1).

- [x] T001 [P] Crear `well-enums.ts` — tipos literales: `WellStatus` (BORRADOR, PENDING_UWI, READY_FISCAL, FISCALIZADO), `TipoTrayectoria` (ST, P, PR, ML, G, O), `Clasificacion` (EXPLORATORIO, DESARROLLO, ESTRATIGRAFICO), `TipoUbicacion` (CONTINENTAL, COSTA_FUERA), `TipoAngulo` (H, V, D), `TipoObjetivo` (PH, I, M, D), `TipoTerminacion` (CD, LC, LR, GP, CC, OH, O); constantes de opciones para dropdowns: `TIPO_TRAYECTORIA_OPTIONS`, `CLASIFICACION_OPTIONS`, etc. — `src/app/domains/wells/models/well-enums.ts`
- [x] T002 [P] Crear `well.dto.ts` — interfaces reflejando contract.yml: `WellListItemDTO` (id, nombrePozo, operadora, contrato, campo, clasificacion, estado, createdAt), `WellDetailDTO` (todos los campos del WellDetail schema), `WellLocationDTO` (departamentoId, departamento, codigoDaneDpto, municipioId, municipio, codigoDaneMpio, clusterId?, cluster?), `CreateWellRequestDTO`, `UpdateWellRequestDTO` (ambos con los campos del form), `PagedResponseDTO<T>` (items, total, page, pageSize) — `src/app/domains/wells/models/well.dto.ts`
- [x] T003 [P] Crear `catalog.dto.ts` — interfaces: `ContratoItemDTO` (id, nombre, tipo, cuenca), `CampoItemDTO` (id, nombre, contratoId), `DepartamentoItemDTO` (id, nombre, codigoDane), `MunicipioItemDTO` (id, nombre, departamentoId, codigoDane), `ClusterItemDTO` (id, nombre, campoId) — `src/app/domains/wells/models/catalog.dto.ts`
- [x] T004 Crear `well.model.ts` — interfaces de dominio frontend: `WellListItem`, `Well` (con todos los campos tipados usando los enums de T001), `WellLocation` — `src/app/domains/wells/models/well.model.ts`
- [x] T005 Crear `catalog.model.ts` — interfaces: `Contrato` (id, nombre, tipo, cuenca), `Campo` (id, nombre, contratoId), `Departamento` (id, nombre, codigoDane), `Municipio` (id, nombre, departamentoId, codigoDane), `Cluster` (id, nombre, campoId) — `src/app/domains/wells/models/catalog.model.ts`
- [x] T006 Crear `well.mapper.ts` — funciones puras: `mapWellListItemDTOToModel(dto: WellListItemDTO): WellListItem`, `mapWellDetailDTOToModel(dto: WellDetailDTO): Well`; mapeo directo camelCase→camelCase con cast de enums — `src/app/domains/wells/models/well.mapper.ts`
- [x] T007 [BLOQUEANTE] Crear `index.ts` — barrel export de todos los modelos, DTOs, enums y mappers del dominio wells — `src/app/domains/wells/models/index.ts`

**[EMERGENTE] T007b** — Crear mock handlers de wells (CRUD + catálogos) y registrarlos en `mock.registry.ts` — necesario para que el sistema funcione con `useMocks=true`. Incluye 5 pozos semilla, 3 contratos, 5 campos, 4 departamentos, 6 municipios, 4 clusters. CRUD en memoria completo. — `src/app/domains/wells/mocks/wells.mock-handlers.ts` + `src/app/domains/wells/mocks/index.ts` + actualización de `core/http/mock.registry.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 2 — Service y Endpoints (ng build)

**Propósito:** Agregar endpoints de wells y catálogos a la configuración centralizada y crear el servicio API del dominio.

- [x] T008 [BLOQUEANTE] Modificar `api-endpoints.ts` — agregar grupo `wells`: `base` (`/api/v1/wells`), `byId` (función con id); agregar grupo `catalogs`: `contratos`, `campos`, `departamentos`, `municipios`, `clusters` (todas las URLs del contrato) — `src/app/core/http/api-endpoints.ts`
- [x] T009 [HU-020] [HU-021] [HU-022] [HU-023] [HU-024] [HU-025] Crear `wells-api.service.ts` — `@Injectable({ providedIn: 'root' })`, inyecta `HttpClient`; 10 métodos: `getWells(params)` → GET con HttpParams, `getWell(id)` → GET byId, `createWell(data)` → POST, `updateWell(id, data)` → PUT, `deleteWell(id)` → DELETE retorna `Observable<void>`; 5 catálogos: `getContratos()` → GET, `getCampos(contratoId)` → GET con query param, `getDepartamentos()` → GET, `getMunicipios(departamentoId)` → GET con query param, `getClusters(campoId)` → GET con query param; sin `catchError` (CONSTITUTION.md §5) — `src/app/domains/wells/services/wells-api.service.ts`
- [x] T010 Crear barrel export de servicios — `src/app/domains/wells/services/index.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 3 — Listado de Pozos (ng build)

**Propósito:** Crear la página de listado con tabla PrimeNG paginada server-side, filtros y acciones de fila.

- [x] T011 [P] [HU-020] Crear `well-manage/locale.ts` — constante `WELL_MANAGE_LOCALE` con: `title`, `actions` (create, edit, delete, view), `columns` (nombrePozo, operadora, contrato, campo, clasificacion, estado, createdAt, acciones), `filters` (search, contrato, allContratos), `messages` (deleteConfirm, deleteSuccess, empty, loading, pageReport); todo `as const` — `src/app/domains/wells/features/well-manage/locale.ts`
- [x] T012 [HU-020] Crear `well-manage.component.ts` — Smart component standalone, `ChangeDetectionStrategy.OnPush`; inyecta `WellsApiService`, `Router`, `MessageService`, `ConfirmationService`; Signals: `wells`, `totalRecords`, `isLoading`, `filters` (page, pageSize, search, contratoId), `contratos` (para dropdown de filtro); `ngOnInit` → carga contratos + wells; método `loadWells()` llama `wellsApiService.getWells(filters())`; método `onLazyLoad(event)` → actualiza filtros desde evento PrimeNG; método `onSearch(term)` con debounce 400ms; método `onCreate()` → navigate `/wells/create`; método `onEdit(id)` → navigate `/wells/${id}/edit`; método `onDelete(well)` → confirm dialog → `wellsApiService.deleteWell(id)` → reload — `src/app/domains/wells/features/well-manage/well-manage.component.ts`
- [x] T013 [HU-020] Crear `well-manage.component.html` — template: heading con `locale.title`, toolbar con input de búsqueda + `p-select` de contratos + botón "Nuevo Pozo"; `p-table` con `[lazy]="true"` server-side; columnas con sort; badges de estado con severidad; acciones de editar/eliminar solo para BORRADOR con `@if`; empty message desde locale — `src/app/domains/wells/features/well-manage/well-manage.component.html`

**Checkpoint:** `ng build` ✅

---

## Bloque 4 — Formulario de Pozos (ng build)

**Propósito:** Crear la página de creación/edición con formulario reactivo y carga de catálogos.

- [x] T014 [P] [HU-021] Crear `well-form/locale.ts` — constante `WELL_FORM_LOCALE` con: `titleCreate`, `titleEdit`, `sections` (general, technical, location), `fields` (13 campos), `placeholders` (11 mensajes), `actions` (save, cancel), `errors` (required, denominacionPattern, consecutivoPattern, denominacionMaxlength), `messages` (createSuccess, updateSuccess); todo `as const` — `src/app/domains/wells/features/well-form/locale.ts`
- [x] T015 [HU-021] [HU-023] Crear `well-form.component.ts` — Smart component standalone, `ChangeDetectionStrategy.OnPush`; inyecta `WellsApiService`, `ActivatedRoute`, `Router`, `MessageService`; Signals: `isLoading`, `isSaving`, `isEditMode`, `contratos`, `campos`, `departamentos`, `municipios`, `clusters`; `wellForm` ReactiveFormsModule FormGroup con todos los campos + validators; `ngOnInit` → detecta modo edit vs create; métodos `onContratoChange`, `onDepartamentoChange`, `onCampoChange`; método `onSubmit()` → create o update según modo — `src/app/domains/wells/features/well-form/well-form.component.ts`
- [x] T016 [HU-021] [HU-023] Crear `well-form.component.html` — template con 3 secciones (p-card): Información General, Datos Técnicos, Ubicación; dropdowns `p-select` con cascada; inputs con validación inline vía `p-message`; botones Guardar (disabled si form invalid o isSaving) y Cancelar — `src/app/domains/wells/features/well-form/well-form.component.html`

**Checkpoint:** `ng build` ✅

---

## Bloque 5 — Rutas e Integración (ng build)

**Propósito:** Conectar los componentes a las rutas del dominio wells y verificar que el sidebar navega correctamente.

- [x] T017 [BLOQUEANTE] Modificar `wells.routes.ts` — rutas: `{ path: '', redirectTo: 'manage' }`, `{ path: 'manage', loadComponent: WellManageComponent }`, `{ path: 'create', loadComponent: WellFormComponent }`, `{ path: ':id/edit', loadComponent: WellFormComponent }` — `src/app/domains/wells/wells.routes.ts`
- [x] T018 Verificar `nav-items.ts` — agregado item `wells` con icono `pi pi-map-marker`, ruta `/wells`, roles `[ADMIN, SUPERVISOR, OPERADOR, AUDITOR]`; agregado `sidebar.wells: 'Gestión de Pozos'` en `shared/locale/locale.ts` — `src/app/core/layout/sidebar/nav-items.ts`

**Checkpoint:** `ng build` ✅

---

## Bloque 6 — Polish y Validación (ng build)

**Propósito:** Verificación transversal y build limpio.

- [x] T019 [P] Verificar que todos los imports usan path aliases (`@wells/*`, `@core/*`, `@shared/*`) — OK, cero rutas relativas entre capas
- [x] T020 [P] Verificar que ningún texto está hardcodeado en templates — corregido `currentPageReportTemplate` y error de maxlength
- [x] T021 [P] Eliminar archivos `.gitkeep` de las carpetas `models/`, `services/`, `features/` del dominio wells — eliminados
- [x] T022 Ejecutar `ng build` final y corregir errores de compilación — `ng build` ✅ exitoso

**Checkpoint final:** `ng build` ✅ — Feature lista para PR.

---

## Dependencies & Execution Order

### Dependencias entre Bloques

```
Bloque 1 (Modelos)     → Sin dependencias internas. Primer paso.
Bloque 2 (Service)     → Depende de Bloque 1 (DTOs usados por el servicio).
Bloque 3 (Listado)     → Depende de Bloque 2 (WellsApiService).
Bloque 4 (Formulario)  → Depende de Bloque 2 (WellsApiService).
Bloque 5 (Rutas)       → Depende de Bloque 3 + Bloque 4 (componentes deben existir antes de las rutas).
Bloque 6 (Polish)      → Depende de Bloques 1–5 completos.
```

### Diagrama

```
B1 ──→ B2 ──→ B3 ──→ B5 ──→ B6
             └──→ B4 ──↗
```

B3 y B4 pueden ejecutarse en paralelo después de B2.

### Dependencias Dentro de Bloques

```
Bloque 1:
  T001, T002, T003 paralelos (enums, DTOs well, DTOs catalog independientes)
  T004 depende de T001 (well.model usa enums)
  T005 independiente (catalog.model no depende de well types)
  T006 depende de T002, T004 (mapper usa DTO + model)
  T007 depende de T001–T006 (barrel re-exporta todo)

Bloque 2:
  T008 BLOQUEANTE (endpoints deben existir para el servicio)
  T009 depende de T008 (servicio usa API endpoints)
  T010 depende de T009 (barrel exporta servicio)

Bloque 3:
  T011 independiente (locale)
  T012 depende de T011 (component usa locale)
  T013 depende de T012 (template del componente)

Bloque 4:
  T014 independiente (locale)
  T015 depende de T014 (component usa locale)
  T016 depende de T015 (template del componente)

Bloque 5:
  T017 depende de B3 + B4 (rutas referencian componentes)
  T018 independiente (verificación de sidebar)

Bloque 6:
  T019, T020, T021 paralelos (verificaciones)
  T022 depende de T019–T021 (build final)
```

---

## Implementation Blocks (Secuencia de Ejecución)

### Bloque 1 — Modelos y DTOs

```
T001, T002, T003      # paralelas: enums, well DTOs, catalog DTOs
T004, T005            # paralelas: well model, catalog model
T006                  # well mapper
T007                  # barrel index.ts
[T007b EMERGENTE]     # mock handlers wells
```
**Validación:** `ng build` ✅

### Bloque 2 — Service y Endpoints

```
T008                  # api-endpoints.ts (+ wells, catalogs)
T009                  # wells-api.service.ts
T010                  # service barrel
```
**Validación:** `ng build` ✅

### Bloque 3 — Listado (puede ir en paralelo con B4)

```
T011                  # well-manage locale
T012                  # well-manage.component.ts
T013                  # well-manage.component.html
```
**Validación:** `ng build` ✅

### Bloque 4 — Formulario (puede ir en paralelo con B3)

```
T014                  # well-form locale
T015                  # well-form.component.ts
T016                  # well-form.component.html
```
**Validación:** `ng build` ✅

### Bloque 5 — Rutas

```
T017                  # wells.routes.ts
T018                  # verificar nav-items.ts + locale wells
```
**Validación:** `ng build` ✅

### Bloque 6 — Polish

```
T019, T020, T021      # paralelas: verificaciones
T022                  # ng build final
```
**Validación:** `ng build` ✅ — Feature lista para PR.

---

## Resumen

| Bloque | Propósito | Tareas | Archivos nuevos | Archivos modif. | Verificación |
|---|---|---|---|---|---|
| B1 — Modelos | Enums + DTOs + models + mappers + mocks | T001–T007 + T007b (8) | 9 | 1 | `ng build` ✅ |
| B2 — Service | API endpoints + WellsApiService | T008–T010 (3) | 2 | 1 | `ng build` ✅ |
| B3 — Listado | Página well-manage con p-table | T011–T013 (3) | 3 | 0 | `ng build` ✅ |
| B4 — Formulario | Página well-form reactivo | T014–T016 (3) | 3 | 0 | `ng build` ✅ |
| B5 — Rutas | Conectar componentes a rutas | T017–T018 (2) | 0 | 2 | `ng build` ✅ |
| B6 — Polish | Verificación y build limpio | T019–T022 (4) | 0 | varios | `ng build` ✅ |
| **Total** | | **23 tareas** | **17 nuevos** | **4 modif.** | |
