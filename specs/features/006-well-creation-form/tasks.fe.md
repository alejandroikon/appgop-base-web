# Tareas Frontend: 006 — Well Creation Form (Wizard Multi-Paso)

**Feature ID:** 006-well-creation-form
**Plan:** `specs/features/006-well-creation-form/plan.fe.md`
**Contrato:** `specs/features/006-well-creation-form/contract.yml`

---

## Bloque 1 — Modelos, DTO y Endpoint (`ng build`)

- [ ] **T001** [P]: Crear `src/app/domains/wells/models/well-name-preview.dto.ts`
  - Interface `WellNamePreviewDTO` con campos `nombrePozo: string`, `available: boolean`
  - Refleja exactamente el schema `WellNamePreview` del contract.yml

- [ ] **T002** [P]: Modificar `src/app/core/http/api-endpoints.ts`
  - Agregar `previewName: \`\${hosts.gopApi}/api/v1/wells/preview-name\`` dentro de `API.wells`

- [ ] **T003**: Modificar `src/app/domains/wells/models/index.ts`
  - Re-exportar `WellNamePreviewDTO` desde `./well-name-preview.dto`
  - Usar `export type` para la interface (CONSTITUTION.md §6.3)

**Verificación:** `ng build`

---

## Bloque 2 — Servicio y Mocks (`ng build`)

- [ ] **T004**: Modificar `src/app/domains/wells/services/wells-api.service.ts`
  - Agregar método `previewWellName(contratoId, denominacion, consecutivo, excludeWellId?): Observable<WellNamePreviewDTO>`
  - Usar `API.wells.previewName` como URL
  - Pasar parámetros como `HttpParams`
  - El servicio no maneja errores (CONSTITUTION.md §5)

- [ ] **T005**: Modificar `src/app/domains/wells/mocks/wells.mock-handlers.ts`
  - Agregar handler `GET /api/v1/wells/preview-name` al array `wellsMockHandlers`
  - Calcular nombre a partir de contratoId → cuenca del mock + denominacion + consecutivo
  - Verificar contra `mockWellsDb` si ya existe
  - Retornar `{ nombrePozo, available }` con status 200

**Verificación:** `ng build`

---

## Bloque 3 — Locale y Componentes Dumb (`ng build`)

- [ ] **T006**: Modificar `src/app/domains/wells/features/well-form/locale.ts`
  - Agregar claves: `steps` (4 nombres de paso), `preview` (6 textos), `actions.next`, `actions.previous`, `actions.saveDraft`, `summary` (4 textos de sección)
  - Mantener textos existentes de `fields`, `errors`, `placeholders`, `messages`
  - Todo texto en español (CONSTITUTION.md §7)

- [ ] **T007** [P]: Crear `src/app/domains/wells/features/well-form/components/well-name-preview/well-name-preview.component.ts`
  - Componente Dumb standalone con `ChangeDetectionStrategy.OnPush`
  - Inputs: `nombrePozo: string | null`, `available: boolean | null`, `loading: boolean`
  - Sin outputs — solo presentación
  - Muestra badge verde (disponible), rojo (no disponible), o spinner (verificando)
  - Textos desde locale pasado como input

- [ ] **T008** [P]: Crear `src/app/domains/wells/features/well-form/components/well-name-preview/well-name-preview.component.html`
  - Template del componente well-name-preview
  - Usa `@if` para renderizado condicional (CONSTITUTION.md §Templates)
  - PrimeNG `Tag` para badges de estado

- [ ] **T009** [P]: Crear `src/app/domains/wells/features/well-form/components/step-contract-info/step-contract-info.component.ts`
  - Componente Dumb standalone con `OnPush`
  - Inputs: `formGroup`, `contratos`, `campos`, `clusters`, `clasificacionOpts`, `cuenca`, `tipoContrato`, `locale`
  - Outputs: `contratoChanged`, `campoChanged`
  - Usa `input()` y `output()` signal-based (CONSTITUTION.md §Componentes)

- [ ] **T010** [P]: Crear `src/app/domains/wells/features/well-form/components/step-contract-info/step-contract-info.component.html`
  - Grid de campos: contrato (p-select), campo (p-select), clasificación (p-select), denominación (input), consecutivo (input), cluster (p-select opcional)
  - Campos read-only: cuenca, tipo_contrato (mostrar como texto tras seleccionar contrato)
  - Incluye `<app-well-name-preview>` al final
  - Mensajes de error por campo con `@if` y `p-message`

- [ ] **T011** [P]: Crear `src/app/domains/wells/features/well-form/components/step-technical-data/step-technical-data.component.ts`
  - Componente Dumb standalone con `OnPush`
  - Inputs: `formGroup`, opciones de los 5 dropdowns técnicos, `locale`
  - Sin outputs

- [ ] **T012** [P]: Crear `src/app/domains/wells/features/well-form/components/step-technical-data/step-technical-data.component.html`
  - Grid de 5 dropdowns: tipoTrayectoria, tipoUbicación, tipoÁngulo, tipoObjetivo, tipoTerminación
  - Mensajes de error por campo

- [ ] **T013** [P]: Crear `src/app/domains/wells/features/well-form/components/step-location/step-location.component.ts`
  - Componente Dumb standalone con `OnPush`
  - Inputs: `formGroup`, `departamentos`, `municipios`, `locale`
  - Outputs: `departamentoChanged`

- [ ] **T014** [P]: Crear `src/app/domains/wells/features/well-form/components/step-location/step-location.component.html`
  - Grid: departamento (p-select), municipio (p-select)
  - Mensajes de error por campo

- [ ] **T015** [P]: Crear `src/app/domains/wells/features/well-form/components/step-summary/step-summary.component.ts`
  - Componente Dumb standalone con `OnPush`
  - Inputs: `formData` (object con todos los valores raw), `cuenca`, `tipoContrato`, `nombrePozo`, `contratoNombre`, `campoNombre`, `departamentoNombre`, `municipioNombre`, `clusterNombre`, `locale`
  - Sin outputs

- [ ] **T016** [P]: Crear `src/app/domains/wells/features/well-form/components/step-summary/step-summary.component.html`
  - 3 secciones read-only (Contrato, Técnicos, Ubicación) con datos en texto plano
  - Nombre del pozo destacado en banner/card
  - Badge "Borrador" como estado

**Verificación:** `ng build`

---

## Bloque 4 — Smart Component: Refactorizar Wizard (`ng build`)

- [ ] **T017**: Modificar `src/app/domains/wells/features/well-form/well-form.component.ts`
  - Importar `StepperModule` (o `Stepper` standalone) de PrimeNG
  - Importar los 5 step components creados en Bloque 3
  - Agregar signals: `activeStep`, `selectedContrato`, `namePreview`, `namePreviewLoading`
  - Agregar computed signals: `cuencaDisplay`, `tipoContratoDisplay`, `isStep1Valid`, `isStep2Valid`, `isStep3Valid`
  - Agregar métodos: `nextStep()`, `prevStep()`, `onContratoChanged()`, `onCampoChanged()`, `onDepartamentoChanged()`
  - Implementar lógica de debounce para preview de nombre (rxjs `debounceTime` + `switchMap`)
  - Mantener la lógica de submit y modo edición existente
  - Refactorizar `ngOnInit` para resolver cascadas correctamente en modo edición

- [ ] **T018**: Modificar `src/app/domains/wells/features/well-form/well-form.component.html`
  - Reemplazar layout actual (3 `p-card`) por `p-stepper` con 4 step-panels
  - Paso 1: `<app-step-contract-info>` con inputs/outputs conectados
  - Paso 2: `<app-step-technical-data>` con inputs conectados
  - Paso 3: `<app-step-location>` con inputs/outputs conectados
  - Paso 4: `<app-step-summary>` con inputs conectados + botón "Guardar Borrador"
  - Botones Anterior/Siguiente en cada paso
  - Mantener `p-toast` y `p-progressSpinner` para loading

**Verificación:** `ng build`

---

## Resumen de Tareas

| ID | Archivo | Tipo | Bloque | Paralelo |
|---|---|---|---|---|
| T001 | `well-name-preview.dto.ts` | Crear | 1 | [P] con T002 |
| T002 | `api-endpoints.ts` | Modificar | 1 | [P] con T001 |
| T003 | `models/index.ts` | Modificar | 1 | Después de T001 |
| T004 | `wells-api.service.ts` | Modificar | 2 | — |
| T005 | `wells.mock-handlers.ts` | Modificar | 2 | Después de T004 |
| T006 | `locale.ts` | Modificar | 3 | Antes de T007-T016 |
| T007 | `well-name-preview.component.ts` | Crear | 3 | [P] con T009-T016 |
| T008 | `well-name-preview.component.html` | Crear | 3 | [P] con T007 |
| T009 | `step-contract-info.component.ts` | Crear | 3 | [P] |
| T010 | `step-contract-info.component.html` | Crear | 3 | [P] |
| T011 | `step-technical-data.component.ts` | Crear | 3 | [P] |
| T012 | `step-technical-data.component.html` | Crear | 3 | [P] |
| T013 | `step-location.component.ts` | Crear | 3 | [P] |
| T014 | `step-location.component.html` | Crear | 3 | [P] |
| T015 | `step-summary.component.ts` | Crear | 3 | [P] |
| T016 | `step-summary.component.html` | Crear | 3 | [P] |
| T017 | `well-form.component.ts` | Modificar | 4 | — |
| T018 | `well-form.component.html` | Modificar | 4 | Después de T017 |

**Total: 18 tareas atómicas en 4 bloques compilables.**
