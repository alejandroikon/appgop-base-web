# Plan Frontend: Formulario de Creación de Pozo — Wizard Multi-Paso

**Feature ID:** 006-well-creation-form
**Contrato:** `specs/features/006-well-creation-form/contract.yml` + `005-wells-catalog-crud/contract.yml`
**Dependencias frontend:** Iteración 005 implementada (modelos, DTOs, WellsApiService, mocks, well-form)

---

## 1. Alcance Frontend

Refactorizar el componente `well-form` existente (formulario plano con `p-card` por sección) a un **wizard multi-paso** usando PrimeNG Stepper. Se extraen componentes Dumb por paso, se agrega preview de nombre en tiempo real, y se incorpora el nuevo endpoint `preview-name`.

---

## 2. Árbol de Archivos

### Archivos NUEVOS

```
src/app/domains/wells/
├── features/
│   └── well-form/
│       ├── components/                              # Componentes Dumb internos del wizard
│       │   ├── step-contract-info/
│       │   │   ├── step-contract-info.component.ts  # Paso 1: contrato, campo, denominación, consecutivo
│       │   │   └── step-contract-info.component.html
│       │   ├── step-technical-data/
│       │   │   ├── step-technical-data.component.ts # Paso 2: datos técnicos
│       │   │   └── step-technical-data.component.html
│       │   ├── step-location/
│       │   │   ├── step-location.component.ts       # Paso 3: departamento, municipio
│       │   │   └── step-location.component.html
│       │   ├── step-summary/
│       │   │   ├── step-summary.component.ts        # Paso 4: resumen read-only
│       │   │   └── step-summary.component.html
│       │   └── well-name-preview/
│       │       ├── well-name-preview.component.ts   # Preview de nombre con verificación
│       │       └── well-name-preview.component.html
│       ├── well-form.component.ts                   # MODIFICAR — Smart Component con stepper
│       ├── well-form.component.html                 # MODIFICAR — Template con p-stepper
│       └── locale.ts                                # MODIFICAR — Agregar textos de pasos y preview
├── models/
│   ├── well-name-preview.dto.ts                     # NUEVO — DTO para preview-name
│   └── index.ts                                     # MODIFICAR — re-exportar nuevo DTO
├── services/
│   └── wells-api.service.ts                         # MODIFICAR — agregar método previewWellName
└── mocks/
    └── wells.mock-handlers.ts                       # MODIFICAR — agregar handler de preview-name
```

### Archivos EXISTENTES que se modifican

| Archivo | Cambio |
|---|---|
| `well-form.component.ts` | Refactorizar: agregar Stepper, mover campos a step components, agregar Signals de paso y cascadas |
| `well-form.component.html` | Reemplazar layout de p-card por p-stepper con 4 step-panels |
| `locale.ts` (well-form) | Agregar textos de stepper (nombres de pasos, preview, botones Anterior/Siguiente) |
| `wells-api.service.ts` | Agregar método `previewWellName()` |
| `wells.mock-handlers.ts` | Agregar handler para `GET /api/v1/wells/preview-name` |
| `models/index.ts` | Re-exportar `WellNamePreviewDTO` |
| `api-endpoints.ts` | Agregar `wells.previewName` |

---

## 3. Componentes: Smart vs. Dumb

| Componente | Tipo | Responsabilidad |
|---|---|---|
| `WellFormComponent` | **Smart** | Orquesta el wizard: mantiene el FormGroup completo, gestiona señales de paso activo, dispara cascadas, invoca API al guardar |
| `StepContractInfoComponent` | **Dumb** | Recibe FormGroup parcial y opciones de dropdowns. Emite eventos de cambio en cascadas |
| `StepTechnicalDataComponent` | **Dumb** | Recibe FormGroup parcial y opciones de dropdowns estáticos |
| `StepLocationComponent` | **Dumb** | Recibe FormGroup parcial, opciones de dept/mpio/cluster. Emite eventos de cascada |
| `StepSummaryComponent` | **Dumb** | Recibe todos los datos como input read-only. Emite evento de confirmación |
| `WellNamePreviewComponent` | **Dumb** | Recibe nombrePozo, available, loading. Solo presenta el badge/chip |

### Patrón de inputs/outputs para step components

Cada step component sigue este patrón (referencia CONSTITUTION.md §6 — Smart vs Dumb):

```typescript
// Ejemplo: StepContractInfoComponent
formGroup = input.required<FormGroup>();           // El sub-FormGroup del paso
contratos = input.required<Contrato[]>();           // Opciones del dropdown
campos = input.required<Campo[]>();                 // Opciones filtradas
clusters = input.required<Cluster[]>();             // Opciones filtradas
contratoChanged = output<number | null>();          // Emite al cambiar contrato
campoChanged = output<number | null>();             // Emite al cambiar campo
```

---

## 4. Estado: Signals (Local)

Todo el estado del wizard es local al componente `WellFormComponent`. No cruza límites de feature → se usa **Signals**, no NgRx (CONSTITUTION.md §4).

| Signal | Tipo | Propósito |
|---|---|---|
| `activeStep` | `signal(0)` | Índice del paso activo (0-based para PrimeNG) |
| `isLoading` | `signal(false)` | Carga inicial (modo edición) |
| `isSaving` | `signal(false)` | Estado de guardado en curso |
| `isEditMode` | `signal(false)` | true si navega con `:id` |
| `contratos` | `signal<Contrato[]>([])` | Catálogo raíz de contratos |
| `campos` | `signal<Campo[]>([])` | Campos filtrados por contrato |
| `clusters` | `signal<Cluster[]>([])` | Clusters filtrados por campo |
| `departamentos` | `signal<Departamento[]>([])` | Catálogo raíz de departamentos |
| `municipios` | `signal<Municipio[]>([])` | Municipios filtrados por departamento |
| `selectedContrato` | `signal<Contrato \| null>(null)` | Contrato seleccionado (para mostrar cuenca/tipo read-only) |
| `namePreview` | `signal<WellNamePreviewDTO \| null>(null)` | Resultado del preview de nombre |
| `namePreviewLoading` | `signal(false)` | Loading del debounce de preview |

### computed Signals

| Computed | Derivado de | Valor |
|---|---|---|
| `cuencaDisplay` | `selectedContrato` | `selectedContrato()?.cuenca ?? ''` |
| `tipoContratoDisplay` | `selectedContrato` | `selectedContrato()?.tipo ?? ''` |
| `isStep1Valid` | `wellForm` controles del paso 1 | Valida campos requeridos del paso |
| `isStep2Valid` | `wellForm` controles del paso 2 | Valida campos requeridos del paso |
| `isStep3Valid` | `wellForm` controles del paso 3 | Valida campos requeridos del paso |
| `canAdvance` | `activeStep`, validez del paso actual | `true` si el paso actual es válido |

---

## 5. FormGroup: Estructura Unificada

El FormGroup se mantiene **unificado** en el Smart component (no se divide en sub-FormGroups). Los step components reciben el FormGroup completo y acceden a sus controles. Esto simplifica la recolección de datos al hacer submit.

```typescript
// Misma estructura que el WellFormComponent actual de la iteración 005
// No cambia la definición de controles — solo la UI que los presenta
wellForm = new FormGroup<WellFormControls>({
  contratoId, campoId, clasificacion, denominacion, consecutivo,
  tipoTrayectoria, tipoUbicacion, tipoAngulo, tipoObjetivo, tipoTerminacion,
  departamentoId, municipioId, clusterId
});
```

---

## 6. Cascadas Reactivas

Las cascadas se mantienen en el Smart component (`WellFormComponent`). Los step components Dumb solo emiten eventos; el Smart reacciona invocando el servicio.

| Trigger | Acción | Limpieza |
|---|---|---|
| `contratoChanged(id)` | `wellsApi.getCampos(id)` → actualizar `campos` signal + resolver `selectedContrato` | Limpiar `campoId`, `clusterId` si no válidos |
| `campoChanged(id)` | `wellsApi.getClusters(id)` → actualizar `clusters` signal | Limpiar `clusterId` si no válido |
| `departamentoChanged(id)` | `wellsApi.getMunicipios(id)` → actualizar `municipios` signal | Limpiar `municipioId` si no válido |

### Preview de Nombre — Debounce

El preview se activa cuando los 3 valores están completos (contratoId + denominación + consecutivo). Se aplica un debounce de 500ms para evitar llamadas excesivas al backend:

1. Se observan cambios en `contratoId`, `denominacion`, `consecutivo` del FormGroup
2. Se filtran combinaciones donde los 3 valores existan
3. Se aplica `debounceTime(500)` + `distinctUntilChanged` (comparando los 3 valores)
4. Se invoca `wellsApi.previewWellName()`
5. Se actualiza `namePreview` signal con el resultado

---

## 7. PrimeNG Stepper — Uso

Se usa el componente `Stepper` de PrimeNG (importar desde `primeng/stepper`). El stepper se configura en modo lineal para forzar validación por paso.

```html
<!-- Estructura conceptual — no código final -->
<p-stepper [activeStep]="activeStep()" linear>
  <p-step-panel header="Información del Contrato">
    <app-step-contract-info ... />
    <button (click)="nextStep()" [disabled]="!isStep1Valid()">Siguiente</button>
  </p-step-panel>
  <p-step-panel header="Datos Técnicos">
    <app-step-technical-data ... />
  </p-step-panel>
  ...
</p-stepper>
```

**Referencia:** PrimeNG se usa directamente sin wrapper (CONSTITUTION.md §11.3 — Criterio: el Stepper se usa una sola vez en esta feature).

---

## 8. Nuevo DTO y Servicio

### well-name-preview.dto.ts

```typescript
export interface WellNamePreviewDTO {
  nombrePozo: string;
  available: boolean;
}
```

**Nota:** El DTO ya llega en camelCase del wire format. No requiere mapper (las propiedades coinciden 1:1 con el schema del contrato). Ver CONSTITUTION.contracts.md §7.3.

### wells-api.service.ts — Método nuevo

```typescript
previewWellName(contratoId: number, denominacion: string, consecutivo: string, excludeWellId?: string): Observable<WellNamePreviewDTO> {
  let params = new HttpParams()
    .set('contratoId', contratoId.toString())
    .set('denominacion', denominacion)
    .set('consecutivo', consecutivo);
  if (excludeWellId) params = params.set('excludeWellId', excludeWellId);
  return this.http.get<WellNamePreviewDTO>(API.wells.previewName, { params });
}
```

---

## 9. Mock Handler Nuevo

Agregar en `wells.mock-handlers.ts`:

```typescript
// GET /api/v1/wells/preview-name?contratoId=N&denominacion=X&consecutivo=YY
{
  urlPattern: /\/api\/v1\/wells\/preview-name/,
  method: 'GET',
  handle: (req) => {
    const url = new URL(req.url, 'http://localhost');
    const contratoId = parseInt(url.searchParams.get('contratoId') ?? '0', 10);
    const denominacion = url.searchParams.get('denominacion') ?? '';
    const consecutivo = url.searchParams.get('consecutivo') ?? '';
    const contrato = MOCK_CONTRATOS.find(c => c.id === contratoId);
    const nombrePozo = contrato
      ? `${contrato.cuenca}-${denominacion.trim()}-${consecutivo}`
      : '';
    // Check against existing mock wells
    const exists = mockWellsDb.some(w => w.nombrePozo === nombrePozo);
    return new HttpResponse({
      status: 200,
      body: { nombrePozo, available: !exists }
    });
  }
}
```

---

## 10. Locale — Textos Nuevos

Agregar a `WELL_FORM_LOCALE`:

```typescript
steps: {
  contractInfo: 'Información del Contrato',
  technicalData: 'Datos Técnicos',
  location: 'Ubicación Geográfica',
  summary: 'Resumen y Confirmación',
},
preview: {
  title: 'Nombre del Pozo',
  placeholder: 'Complete contrato, denominación y consecutivo para ver el nombre',
  available: 'Disponible',
  unavailable: 'Nombre ya en uso',
  checking: 'Verificando...',
  errorChecking: 'No se pudo verificar',
},
actions: {
  next: 'Siguiente',
  previous: 'Anterior',
  saveDraft: 'Guardar Borrador',
  cancel: 'Cancelar',
},
summary: {
  sectionContract: 'Contrato y Campo',
  sectionTechnical: 'Datos Técnicos',
  sectionLocation: 'Ubicación',
  status: 'Estado',
  statusDraft: 'Borrador',
},
```

---

## 11. API Endpoints — Agregar

En `core/http/api-endpoints.ts`, agregar:

```typescript
wells: {
  base:        `${hosts.gopApi}/api/v1/wells`,
  byId:        (id: string) => `${hosts.gopApi}/api/v1/wells/${id}`,
  previewName: `${hosts.gopApi}/api/v1/wells/preview-name`,  // NUEVO
},
```

---

## 12. Rutas

No hay cambios en rutas. Las rutas existentes en `wells.routes.ts` ya cubren:

| Ruta | Componente | Modo |
|---|---|---|
| `/wells/create` | `WellFormComponent` | Creación (sin `:id`) |
| `/wells/:id/edit` | `WellFormComponent` | Edición (con `:id`) |

---

## 13. Validaciones por Paso

| Paso | Controles validados | Reglas |
|---|---|---|
| 1 | `contratoId`, `campoId`, `clasificacion`, `denominacion`, `consecutivo` | required + pattern de denominación y consecutivo |
| 2 | `tipoTrayectoria`, `tipoUbicacion`, `tipoAngulo`, `tipoObjetivo`, `tipoTerminacion` | required |
| 3 | `departamentoId`, `municipioId` | required |
| 4 | — (read-only) | Botón habilitado solo si los 3 pasos anteriores son válidos |

**Patrón de validación por paso:**

```typescript
// Computed signal que verifica validez de controles del paso 1
isStep1Valid = computed(() => {
  const step1Controls = ['contratoId', 'campoId', 'clasificacion', 'denominacion', 'consecutivo'];
  return step1Controls.every(name => this.wellForm.get(name)?.valid);
});
```

---

## 14. Restricciones de Constitución

| Regla CONSTITUTION.md | Cumplimiento |
|---|---|
| §4 — Signals para estado local de wizard | ✅ Todos los signals son locales a `WellFormComponent` |
| §6 — Modelos en dominio (solo wells lo usa) | ✅ `WellNamePreviewDTO` en `domains/wells/models/` |
| §7 — Textos en locale, nunca hardcodeados | ✅ Todo texto en `locale.ts` de la feature |
| §11.3 — PrimeNG directo sin wrapper para uso único | ✅ Stepper se usa directamente |
| §13 — Mocks junto al dominio | ✅ Mock handler en `domains/wells/mocks/` |
| §5 — Servicios no manejan errores HTTP | ✅ Solo `map()` en el servicio |
