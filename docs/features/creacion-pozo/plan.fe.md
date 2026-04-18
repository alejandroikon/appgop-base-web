# Plan Frontend — Creación de Pozo Nuevo V2.0

**Referencia:** `spec.md`, `contract.yml`, `uwi-algorithm.md`
**Constitución:** `CONSTITUTION.md` (frontend Angular)
**Stack:** Angular 21 + PrimeNG 21 + NgRx 21 + Signals

---

## 1. Resumen de cambios vs módulo existente

| Aspecto | Actual (iters 005-007) | V2.0 |
|---------|----------------------|------|
| Layout de creación | Wizard 4 pasos (Stepper) | Página única con scroll continuo |
| UWI | Formato simplificado `CO-...`, generado en backend solo | Formato PPDM, preview client-side + generación backend |
| Clasificación | Plana (3 opciones) | Jerárquica: Exploratorio → sub-clasificación (A3, A2a, A2b, A2c, A1) |
| TipoObjetivo | 4 valores | 7 valores (+C, +GT, +O) |
| Consecutivo | 2 dígitos (01-99) | 1-9999 (display sin padding, UWI con padding a 4) |
| Cluster | Sin abreviatura | Con abreviatura 2 chars + posibilidad de crear nuevo |
| WellStatus | 4 estados | 2 estados (BORRADOR, CREADO) + flag Forma101 |
| State machine | Transiciones ENVIAR/APROBAR/DEVOLVER | No hay transiciones; solo DRAFT / FINALIZE |

## 2. Árbol de archivos

```
src/app/domains/wells/
├── components/
│   └── well-status-badge/                    # MODIFICAR: actualizar WellStatus enum
│       ├── well-status-badge.component.html
│       └── well-status-badge.component.ts
├── features/
│   ├── well-create/                          # NUEVO: reemplaza well-form/
│   │   ├── components/
│   │   │   ├── section-contract-info/        # Sección 1
│   │   │   │   ├── section-contract-info.component.ts
│   │   │   │   └── section-contract-info.component.html
│   │   │   ├── section-technical-data/       # Sección 2
│   │   │   │   ├── section-technical-data.component.ts
│   │   │   │   └── section-technical-data.component.html
│   │   │   ├── section-location/             # Sección 3
│   │   │   │   ├── section-location.component.ts
│   │   │   │   └── section-location.component.html
│   │   │   └── uwi-preview-panel/            # Panel UWI
│   │   │       ├── uwi-preview-panel.component.ts
│   │   │       └── uwi-preview-panel.component.html
│   │   ├── well-create.component.ts          # Smart: formulario scroll
│   │   ├── well-create.component.html
│   │   └── locale.ts
│   ├── well-manage/                          # MODIFICAR: actualizar columnas tabla
│   │   ├── well-manage.component.ts
│   │   ├── well-manage.component.html
│   │   └── locale.ts
│   └── well-detail/                          # MODIFICAR: mostrar UWI PPDM, quitar transiciones
│       ├── well-detail.component.ts
│       ├── well-detail.component.html
│       └── locale.ts
├── domain/                                   # NUEVO: lógica pura de dominio
│   ├── uwi.generator.ts                     # Algoritmo PPDM client-side
│   ├── uwi.generator.spec.ts                # Tests del generador
│   ├── well-name.generator.ts               # Generación de nombre de pozo
│   ├── well-name.generator.spec.ts
│   ├── well-form.validators.ts              # Validadores puros
│   └── well-form.validators.spec.ts
├── data-access/                              # NUEVO: separación data-access
│   ├── wells-api.service.ts                  # MODIFICAR: nuevos endpoints (preview-uwi, cluster POST)
│   └── index.ts
├── models/
│   ├── well-enums.ts                         # MODIFICAR: nuevos enums
│   ├── well.model.ts                         # MODIFICAR: nuevo shape
│   ├── well.dto.ts                           # MODIFICAR: nuevo shape
│   ├── well.mapper.ts                        # MODIFICAR
│   ├── catalog.model.ts                      # MODIFICAR: agregar Cluster.abreviatura
│   ├── catalog.dto.ts                        # MODIFICAR
│   └── index.ts
├── store/                                    # MODIFICAR: simplificar state machine
│   ├── wells.actions.ts
│   ├── wells.reducer.ts
│   ├── wells.selectors.ts
│   └── wells.effects.ts
├── mocks/
│   ├── wells.mock-handlers.ts               # MODIFICAR: nuevos endpoints y data
│   └── index.ts
└── wells.routes.ts                           # MODIFICAR: ruta create
```

## 3. Estrategia de estado

### NgRx (estado global, sobrevive navegación)

| Slice | Contenido |
|-------|-----------|
| `wells.list` | Lista paginada de pozos, filtros activos |
| `wells.selectedWell` | Detalle del pozo activo |
| `wells.catalogs` | Contratos, campos, departamentos, municipios, clusters cacheados |

### Signals (estado local del formulario, se destruye con el componente)

| Signal | Tipo | Componente |
|--------|------|-----------|
| `formGroup` | `FormGroup` | `WellCreateComponent` |
| `uwiPreview` | `computed<string>` | `UwiPreviewPanelComponent` (a través del parent) |
| `wellNamePreview` | `computed<string>` | `WellCreateComponent` |
| `isSubmitting` | `signal<boolean>` | `WellCreateComponent` |
| `nameCheckResult` | `signal<{name, isUnique}>` | `WellCreateComponent` |
| `uwiCheckResult` | `signal<{uwi, isUnique}>` | `WellCreateComponent` |
| `showOhWarning` | `computed<boolean>` | `SectionTechnicalDataComponent` |

> Ver CONSTITUTION.md §4: Signals para estado local, NgRx para estado que cruza límites de feature.

## 4. Componentes: Smart vs Dumb

| Componente | Tipo | Responsabilidad |
|-----------|------|-----------------|
| `WellCreateComponent` | **Smart** | Orquesta formulario, despacha acciones NgRx, gestiona FormGroup, invoca validadores |
| `SectionContractInfoComponent` | **Dumb** | Recibe FormGroup slice, emite cambios de contrato |
| `SectionTechnicalDataComponent` | **Dumb** | Recibe FormGroup slice, emite cambios de clasificación, muestra warning OH |
| `SectionLocationComponent` | **Dumb** | Recibe FormGroup slice, cascada dpto→mpio |
| `UwiPreviewPanelComponent` | **Dumb** | Recibe string del UWI calculado, muestra formato monoespaciado |
| `WellManageComponent` | **Smart** | Lista, filtros, navegación |
| `WellDetailComponent` | **Smart** | Detalle completo del pozo |

## 5. Lógica pura en `domain/`

### 5.1. `uwi.generator.ts`

Función pura que implementa el algoritmo PPDM completo (ver `uwi-algorithm.md`):

```typescript
export function generateUwiPreview(params: UwiParams): UwiResult {
  // Implementa RN-28 a RN-36
}

export function computeSigla(denominacion: string, isAnh: boolean): string {
  // Implementa RN-30 con padding X y excepción ANH
}

export function computeClusterCode(clusterNombre: string | null): string {
  // Implementa RN-32
}
```

### 5.2. `well-name.generator.ts`

```typescript
export function generateWellName(
  campo: string | null,
  contratoNombre: string | null,
  denominacion: string,
  consecutivo: number
): string {
  // Implementa RN-16 a RN-22
}
```

### 5.3. `well-form.validators.ts`

```typescript
export function validateDenominacion(value: string): ValidationResult { /* RN-07 */ }
export function validateConsecutivo(value: number): ValidationResult { /* RN-08 */ }
export function isClasificacionValidForAnh(clasificacion: string, isAnh: boolean): boolean { /* RN-15 */ }
export function isCampoRequired(clasificacion: string): boolean { /* RN-11, RN-12, RN-13 */ }
```

> Ver principio transversal "Validators puros" en ADR-001: funciones puras en `domain/*.validators.ts`.

## 6. Rutas

```typescript
// wells.routes.ts
export const wellsRoutes: Routes = [
  { path: '', redirectTo: 'manage', pathMatch: 'full' },
  { path: 'manage', loadComponent: () => import('./features/well-manage/...') },
  { path: 'create', loadComponent: () => import('./features/well-create/...'),
    canActivate: [roleGuard(['OPERADOR', 'ADMIN'])] },
  { path: ':id', loadComponent: () => import('./features/well-detail/...') },
  { path: ':id/edit', loadComponent: () => import('./features/well-create/...'),
    canActivate: [roleGuard(['OPERADOR', 'SUPERVISOR', 'ADMIN'])] },
];
```

## 7. Flujo de datos del formulario

```
[Usuario llena campos]
       │
       ▼
  FormGroup (Reactive Forms) — Signals locales
       │
       ├──▶ computed: wellNamePreview (client-side, RN-16..22)
       │       └──▶ debounce 300ms ──▶ GET /wells/preview-name (unicidad)
       │
       ├──▶ computed: uwiPreview (client-side, algoritmo PPDM)
       │       └──▶ debounce 500ms ──▶ GET /wells/preview-uwi (unicidad)
       │
       └──▶ [Guardar Borrador] ──▶ dispatch(createWell({action:'DRAFT', ...}))
            [Finalizar] ──▶ dispatch(createWell({action:'FINALIZE', ...}))
                                     │
                                     ▼
                              Effect: POST /api/v1/wells
                                     │
                                     ├──▶ createWellSuccess ──▶ toast + navigate
                                     └──▶ createWellFailure ──▶ toast error
```

## 8. Mocks

El mock handler para `wells.mock-handlers.ts` se actualiza para soportar:

- `POST /wells` con `action: DRAFT | FINALIZE`
- `GET /wells/preview-uwi` con respuesta de UwiPreviewResponse
- `POST /catalogs/clusters` para crear clusters nuevos
- Datos de ejemplo con UWIs en formato PPDM

## 9. Reglas de la constitución validadas

| Sección CONSTITUTION.md | Cumplimiento |
|-------------------------|-------------|
| §4 Signals vs NgRx | ✅ Signals para formulario local, NgRx para catálogos |
| §4.1 Navegación en Effects | ✅ Navegación post-create en Effect |
| §6 Modelos DTO/Model/Mapper | ✅ DTOs espejo del contract.yml |
| §7 Locale | ✅ Textos en locale.ts por feature |
| §9 Dependencias entre capas | ✅ domain/ sin deps externas, data-access/ para HTTP |
| §11 PrimeNG + Tailwind | ✅ PrimeNG para dropdowns/inputs, Tailwind para layout |
| §13 Mocks | ✅ Mock handlers actualizados |
| §14 Design Tokens | ✅ Sin colores hardcodeados |
