# Tareas Frontend — Creación de Pozo Nuevo V2.0

**Referencia:** `plan.fe.md`, `contract.yml`
**Verificación por bloque:** `ng build` al final de cada bloque

---

## Bloque 1 — Domain: Lógica Pura + Enums (ng build)

- [x] **T-FE-001**: Crear `domains/wells/domain/uwi.generator.ts` — función pura `generateUwiPreview()`, `computeSigla()`, `computeClusterCode()`. Implementa RN-28 a RN-36 según `uwi-algorithm.md`.
- [x] **T-FE-002**: Crear `domains/wells/domain/uwi.generator.spec.ts` — tests unitarios: happy path (Ejemplo 1, 2, 3 del uwi-algorithm.md), padding de sigla (1-4 letras), excepción ANH, trayectoria vacía para Original.
- [x] **T-FE-003**: Crear `domains/wells/domain/well-name.generator.ts` — función pura `generateWellName()`. Implementa RN-16 a RN-22.
- [x] **T-FE-004**: Crear `domains/wells/domain/well-name.generator.spec.ts` — tests: con campo, sin campo (fallback contrato), uppercase, consecutivo sin padding.
- [x] **T-FE-005**: Crear `domains/wells/domain/well-form.validators.ts` — funciones puras: `validateDenominacion()`, `validateConsecutivo()`, `isClasificacionValidForAnh()`, `isCampoRequired()`.
- [x] **T-FE-006**: Crear `domains/wells/domain/well-form.validators.spec.ts` — tests: regex denominación válida/inválida, consecutivo rango, ANH solo estratigráfico, campo requerido por clasificación.

**Verificación:** `ng build` ✅ (archivos .ts puros sin deps Angular)

---

## Bloque 2 — Models: DTOs, Enums, Mappers (ng build)

- [x] **T-FE-007**: Modificar `domains/wells/models/well-enums.ts` — actualizar `WellStatus` a `BORRADOR | CREADO`. Agregar `SubClasificacionExploratoria`. Actualizar `TipoObjetivo` (+C, +GT, +O). Actualizar dropdown options arrays.
- [x] **T-FE-008**: Modificar `domains/wells/models/well.model.ts` — actualizar `Well` interface: aplanar ubicación, agregar `subClasificacion`, `forma101Radicada`, cambiar `consecutivo` a number. Actualizar `WellListItem`.
- [x] **T-FE-009**: Modificar `domains/wells/models/well.dto.ts` — actualizar `WellDetailDTO`, `WellListItemDTO`, `CreateWellRequestDTO` (agregar `action`, `subClasificacion`), `UpdateWellRequestDTO` (agregar `action`). Agregar `UwiPreviewResponseDTO`, `WellNamePreviewResponseDTO`.
- [x] **T-FE-010**: Modificar `domains/wells/models/well.mapper.ts` — actualizar mappers para nuevo shape. Agregar `mapUwiPreviewDTOToModel()`.
- [x] **T-FE-011**: Modificar `domains/wells/models/catalog.model.ts` — agregar `abreviatura` a `Cluster`. Agregar `ubicacionDefault` a `Contrato`.
- [x] **T-FE-012**: Modificar `domains/wells/models/catalog.dto.ts` — agregar `abreviatura` a `ClusterItemDTO`, `ubicacionDefault` a `ContratoItemDTO`. Agregar `CreateClusterRequestDTO`.
- [x] **T-FE-013**: Modificar `domains/wells/models/index.ts` — re-exportar nuevos tipos y mappers.

**Verificación:** `ng build` ✅

---

## Bloque 3 — Data Access: API Service + Endpoints (ng build)

- [x] **T-FE-014**: Modificar `core/http/api-endpoints.ts` — agregar `previewUwi` endpoint, `createCluster` endpoint al grupo `catalogs`.
- [x] **T-FE-015**: Modificar `domains/wells/data-access/wells-api.service.ts` (actualmente `services/wells-api.service.ts`) — agregar métodos: `previewUwi()`, `createCluster()`. Actualizar `createWell()` y `updateWell()` para incluir `action`. Eliminar `transitionWell()` y `getWellHistory()` (ya no aplican en V2.0).
- [x] **T-FE-016**: Modificar `domains/wells/data-access/index.ts` (o `services/index.ts`) — actualizar barrel exports.

**Verificación:** `ng build` ✅

---

## Bloque 4 — Store: NgRx Actions, Reducer, Effects, Selectors (ng build)

- [x] **T-FE-017**: Modificar `domains/wells/store/wells.actions.ts` — eliminar acciones de transición. Agregar `createWellDraft`, `createWellFinalize`, `previewUwi*`, `createCluster*`.
- [x] **T-FE-018**: Modificar `domains/wells/store/wells.reducer.ts` — simplificar state (sin transitionHistory). Agregar slices para uwiPreview, namePreview.
- [x] **T-FE-019**: Modificar `domains/wells/store/wells.selectors.ts` — nuevos selectores: `selectUwiPreview`, `selectNamePreview`, `selectCatalogs`.
- [x] **T-FE-020**: Modificar `domains/wells/store/wells.effects.ts` — eliminar effects de transición. Agregar effects para createWell (con action dispatch), previewUwi, previewName, createCluster. Navegación post-create en effect (CONSTITUTION.md §4.1).

**Verificación:** `ng build` ✅

---

## Bloque 5 — Mocks (ng build)

- [x] **T-FE-021**: Modificar `domains/wells/mocks/wells.mock-handlers.ts` — actualizar handlers para: `POST /wells` con action DRAFT/FINALIZE, `GET /wells/preview-uwi`, `POST /catalogs/clusters`. Datos con UWIs PPDM.

**Verificación:** `ng build` ✅

---

## Bloque 6 — Componentes Dumb de Sección (ng build)

- [x] **T-FE-022**: Crear `domains/wells/features/well-create/components/section-contract-info/section-contract-info.component.ts` — inputs: FormGroup slice, contratos, campos. Outputs: contratoChange, campoChange. Usa p-select para dropdowns, campos read-only para Tipo Contrato y Cuenca.
- [x] **T-FE-023**: Crear `domains/wells/features/well-create/components/section-contract-info/section-contract-info.component.html` — template con grid Tailwind 2 columnas, 4 campos.
- [x] **T-FE-024**: Crear `domains/wells/features/well-create/components/section-technical-data/section-technical-data.component.ts` — inputs: FormGroup slice, isAnh signal. Outputs: clasificacionChange, terminacionChange. Clasificación jerárquica (dropdown principal + subdropdown si Exploratorio). Warning OH (p-message).
- [x] **T-FE-025**: Crear `domains/wells/features/well-create/components/section-technical-data/section-technical-data.component.html` — template con grid Tailwind, 10 campos. Warning OH condicional.
- [x] **T-FE-026**: Crear `domains/wells/features/well-create/components/section-location/section-location.component.ts` — inputs: FormGroup slice, departamentos, municipios, clusters. Outputs: departamentoChange. Cascada dpto→mpio.
- [x] **T-FE-027**: Crear `domains/wells/features/well-create/components/section-location/section-location.component.html` — template con grid, 5 campos. Códigos DANE read-only.
- [x] **T-FE-028**: Crear `domains/wells/features/well-create/components/uwi-preview-panel/uwi-preview-panel.component.ts` — input: uwiPreview string, uwiComponents object, isUnique boolean. Monoespaciado, color-coded por segmento.
- [x] **T-FE-029**: Crear `domains/wells/features/well-create/components/uwi-preview-panel/uwi-preview-panel.component.html` — template con panel destacado, tipografía `font-mono`.

**Verificación:** `ng build` ✅

---

## Bloque 7 — Smart Component + Locale + Rutas (ng build)

- [x] **T-FE-030**: Crear `domains/wells/features/well-create/locale.ts` — textos de la feature: títulos de sección, labels de campos, mensajes de error, botones (Guardar Borrador, Finalizar Registro), warning OH.
- [x] **T-FE-031**: Crear `domains/wells/features/well-create/well-create.component.ts` — Smart component: FormGroup reactivo, inject Store, dispatch actions, computed signals para UWI preview y nombre, debounce para validación de unicidad.
- [x] **T-FE-032**: Crear `domains/wells/features/well-create/well-create.component.html` — template scroll continuo: 3 secciones + panel UWI + barra de acciones (Guardar Borrador | Finalizar Registro).
- [x] **T-FE-033**: Modificar `domains/wells/wells.routes.ts` — ruta `create` apunta a `well-create`, agregar roleGuard. Ruta `edit` apunta a `well-create` con param `id`.

**Verificación:** `ng build` ✅

---

## Bloque 8 — Componentes existentes actualizados (ng build)

- [x] **T-FE-034**: Modificar `domains/wells/components/well-status-badge/well-status-badge.component.ts` — actualizar para WellStatus `BORRADOR | CREADO` (eliminar PENDING_UWI, READY_FISCAL, FISCALIZADO).
- [x] **T-FE-035**: Modificar `domains/wells/features/well-manage/well-manage.component.ts` — actualizar columnas de tabla: agregar UWI, subClasificación. Actualizar filtros de estado.
- [x] **T-FE-036**: Modificar `domains/wells/features/well-manage/well-manage.component.html` — template con nuevas columnas.
- [x] **T-FE-037**: Modificar `domains/wells/features/well-manage/locale.ts` — textos actualizados.
- [x] **T-FE-038**: Modificar `domains/wells/features/well-detail/well-detail.component.ts` — quitar componentes de transición. Mostrar UWI PPDM. Mostrar flag Forma101.
- [x] **T-FE-039**: Modificar `domains/wells/features/well-detail/well-detail.component.html` — template actualizado sin transiciones.
- [x] **T-FE-040**: Modificar `domains/wells/features/well-detail/locale.ts` — textos actualizados.

**Verificación:** `ng build` ✅

---

## Bloque 9 — Limpieza Legacy (ng build)

- [x] **T-FE-041**: Eliminar `domains/wells/features/well-form/` (directorio completo) — reemplazado por `well-create/`.
- [x] **T-FE-042**: Eliminar `domains/wells/features/well-detail/components/transition-confirm-dialog/` — ya no aplica en V2.0.
- [x] **T-FE-043**: Eliminar `domains/wells/features/well-detail/components/well-transition-actions/` — ya no aplica.
- [x] **T-FE-044**: Eliminar `domains/wells/models/well-transition.dto.ts` — ya no aplica.
- [x] **T-FE-045**: Eliminar `domains/wells/models/well-transition.mapper.ts` — ya no aplica.
- [x] **T-FE-046**: Eliminar `domains/wells/models/well-transition.model.ts` — ya no aplica.
- [x] **T-FE-047**: Eliminar `domains/wells/models/well-name-preview.dto.ts` — reemplazado por nuevo DTO en well.dto.ts.

**Verificación:** `ng build` ✅

---

## Resumen

| Bloque | Tareas | Tipo |
|--------|--------|------|
| 1 — Domain | T-FE-001 a T-FE-006 | Crear (lógica pura + tests) |
| 2 — Models | T-FE-007 a T-FE-013 | Modificar |
| 3 — Data Access | T-FE-014 a T-FE-016 | Modificar |
| 4 — Store | T-FE-017 a T-FE-020 | Modificar |
| 5 — Mocks | T-FE-021 | Modificar |
| 6 — Componentes Dumb | T-FE-022 a T-FE-029 | Crear |
| 7 — Smart + Rutas | T-FE-030 a T-FE-033 | Crear + Modificar |
| 8 — Actualizar existentes | T-FE-034 a T-FE-040 | Modificar |
| 9 — Limpieza | T-FE-041 a T-FE-047 | Eliminar |
| **Total** | **47 tareas** | |
