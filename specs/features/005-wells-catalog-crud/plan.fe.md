# Plan Frontend: Listado y Formulario de Pozos

**Feature ID:** 005-wells-catalog-crud
**Spec:** `specs/features/005-wells-catalog-crud/spec.md`
**Contrato:** `specs/features/005-wells-catalog-crud/contract.yml`
**Constitución:** `CONSTITUTION.md` (frontend)
**Depende de:** 001-auth, 002-layout, 004-auth-api (todos implementados)
**Estado:** Pendiente de revisión humana

---

## 1. Resumen del Cambio

Primera feature de negocio en el frontend. Agrega al dominio `wells/`:
1. **Página de listado** (`well-manage`) — tabla PrimeNG paginada con filtros y ordenamiento server-side
2. **Página de creación/edición** (`well-form`) — formulario reactivo con los campos del modelo
3. **Servicio API** (`wells-api.service.ts`) — consume los endpoints de wells y catálogos
4. **Modelos y DTOs** — reflejo del contrato
5. **Rutas** — `wells/manage`, `wells/create`, `wells/:id/edit`
6. **Sidebar** — el módulo Pozos ya existe en la navegación (002-layout), se conectan las rutas reales

Las cascadas reactivas (contrato→campo, departamento→municipio) se implementan como dropdowns simples en esta iteración. La reactividad automática se hará en una iteración posterior.

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
src/app/
├── domains/
│   └── wells/
│       ├── models/
│       │   ├── well.model.ts                    # Interfaces: Well, WellListItem, WellLocation
│       │   ├── well.dto.ts                      # DTOs: WellDetailDTO, WellListItemDTO, CreateWellRequestDTO, UpdateWellRequestDTO, WellLocationDTO
│       │   ├── well.mapper.ts                   # mapWellDetailDTOToModel(), mapWellListItemDTOToModel()
│       │   ├── catalog.model.ts                 # Interfaces: Contrato, Campo, Departamento, Municipio, Cluster
│       │   ├── catalog.dto.ts                   # DTOs: ContratoItemDTO, CampoItemDTO, DepartamentoItemDTO, MunicipioItemDTO, ClusterItemDTO
│       │   ├── well-enums.ts                    # String literal types: WellStatus, TipoTrayectoria, Clasificacion, etc.
│       │   └── index.ts                         # Barrel export
│       ├── services/
│       │   ├── wells-api.service.ts             # HttpClient: CRUD wells + catálogos
│       │   └── index.ts                         # Barrel export
│       └── features/
│           ├── well-manage/
│           │   ├── locale.ts                    # Textos del listado
│           │   ├── well-manage.component.ts     # Smart: tabla PrimeNG, filtros, paginación server-side
│           │   └── well-manage.component.html   # Template: toolbar + p-table + paginador
│           └── well-form/
│               ├── locale.ts                    # Textos del formulario
│               ├── well-form.component.ts       # Smart: formulario reactivo, carga catálogos, create/edit
│               └── well-form.component.html     # Template: campos, dropdowns, botones
```

### 2.2. Archivos a MODIFICAR

```
src/app/
├── domains/wells/wells.routes.ts                # + rutas: manage, create, :id/edit
├── core/http/api-endpoints.ts                   # + grupos wells y catalogs
└── core/layout/sidebar/nav-items.ts             # + submódulos de Pozos (si no existen)
```

---

## 3. Diseño Detallado

### 3.1. Modelos y DTOs

**`well-enums.ts`** — Tipos literales alineados con contract.yml:
```typescript
export type WellStatus = 'BORRADOR' | 'PENDING_UWI' | 'READY_FISCAL' | 'FISCALIZADO';
export type TipoTrayectoria = 'ST' | 'P' | 'PR' | 'ML' | 'G' | 'O';
export type Clasificacion = 'EXPLORATORIO' | 'DESARROLLO' | 'ESTRATIGRAFICO';
export type TipoUbicacion = 'CONTINENTAL' | 'COSTA_FUERA';
export type TipoAngulo = 'H' | 'V' | 'D';
export type TipoObjetivo = 'PH' | 'I' | 'M' | 'D';
export type TipoTerminacion = 'CD' | 'LC' | 'LR' | 'GP' | 'CC' | 'OH' | 'O';
```

**`well.model.ts`** — Modelos del dominio frontend:
```typescript
export interface WellListItem {
  id: string;
  nombrePozo: string;
  operadora: string;
  contrato: string;
  campo: string;
  clasificacion: Clasificacion;
  estado: WellStatus;
  createdAt: string;
}

export interface Well {
  id: string;
  operadora: string;
  contratoId: number;
  contrato: string;
  tipoContrato: string;
  cuenca: string;
  campoId: number;
  campo: string;
  tipoTrayectoria: TipoTrayectoria;
  clasificacion: Clasificacion;
  denominacion: string;
  consecutivo: string;
  nombrePozo: string;
  tipoUbicacion: TipoUbicacion;
  tipoAngulo: TipoAngulo;
  tipoObjetivo: TipoObjetivo;
  tipoTerminacion: TipoTerminacion;
  estado: WellStatus;
  ubicacion: WellLocation;
  createdAt: string;
  lastModifiedAt: string | null;
}
```

**DTOs:** Reflejan el contrato camelCase directamente. Como el backend ya serializa en camelCase, los DTOs son idénticos a los modelos. Los mappers son identity functions con cast de tipos — existen por el patrón DTO/Model de CONSTITUTION.md §6.3.

### 3.2. Wells API Service

```typescript
@Injectable({ providedIn: 'root' })
export class WellsApiService {
  private http = inject(HttpClient);

  // Wells CRUD
  getWells(params: WellsQueryParams): Observable<PagedResponse<WellListItemDTO>> { ... }
  getWell(id: string): Observable<WellDetailDTO> { ... }
  createWell(data: CreateWellRequestDTO): Observable<WellDetailDTO> { ... }
  updateWell(id: string, data: UpdateWellRequestDTO): Observable<WellDetailDTO> { ... }
  deleteWell(id: string): Observable<void> { ... }

  // Catálogos
  getContratos(): Observable<ContratoItemDTO[]> { ... }
  getCampos(contratoId: number): Observable<CampoItemDTO[]> { ... }
  getDepartamentos(): Observable<DepartamentoItemDTO[]> { ... }
  getMunicipios(departamentoId: number): Observable<MunicipioItemDTO[]> { ... }
  getClusters(campoId: number): Observable<ClusterItemDTO[]> { ... }
}
```

Según CONSTITUTION.md §6.4: el servicio solo hace HTTP + mapper. No maneja errores.

### 3.3. Well Manage Component (Listado)

**Tipo:** Smart component.

**Signals de UI:**
```typescript
wells = signal<WellListItem[]>([]);
totalRecords = signal(0);
isLoading = signal(true);
filters = signal<WellsQueryParams>({ page: 1, pageSize: 20 });
```

**Comportamiento:**
- Carga inicial con `page=1`, `pageSize=20`
- `p-table` de PrimeNG con `lazy="true"` — cada evento `onLazyLoad` actualiza los filtros y recarga
- Toolbar: campo de búsqueda (text input con debounce 400ms), dropdown de contrato, botón "Nuevo Pozo"
- Columnas: Nombre Pozo, Operadora, Contrato, Campo, Clasificación, Estado, Fecha Creación
- Acciones por fila: Ver detalle, Editar (solo borradores), Eliminar (solo borradores)
- Paginador integrado en `p-table`

### 3.4. Well Form Component (Crear/Editar)

**Tipo:** Smart component. Reutilizado para creación y edición.

**Signals de UI:**
```typescript
isLoading = signal(false);
isEditMode = signal(false);
contratos = signal<Contrato[]>([]);
campos = signal<Campo[]>([]);
departamentos = signal<Departamento[]>([]);
municipios = signal<Municipio[]>([]);
clusters = signal<Cluster[]>([]);
```

**Formulario reactivo:**
```typescript
wellForm = new FormGroup({
  contratoId: new FormControl<number | null>(null, Validators.required),
  campoId: new FormControl<number | null>(null, Validators.required),
  tipoTrayectoria: new FormControl('', Validators.required),
  clasificacion: new FormControl('', Validators.required),
  denominacion: new FormControl('', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-záéíóúÁÉÍÓÚñÑ ]+$/)]),
  consecutivo: new FormControl('', [Validators.required, Validators.pattern(/^\d{2}$/)]),
  tipoUbicacion: new FormControl('', Validators.required),
  tipoAngulo: new FormControl('', Validators.required),
  tipoObjetivo: new FormControl('', Validators.required),
  tipoTerminacion: new FormControl('', Validators.required),
  departamentoId: new FormControl<number | null>(null, Validators.required),
  municipioId: new FormControl<number | null>(null, Validators.required),
  clusterId: new FormControl<number | null>(null),
});
```

**Flujo de creación:**
1. Cargar catálogos root (contratos, departamentos) al init
2. Usuario selecciona contrato → carga campos (manual, no reactivo en esta iteración)
3. Usuario selecciona departamento → carga municipios (manual)
4. Usuario selecciona campo → carga clusters (manual)
5. Submit → `wellsApiService.createWell(dto)` → on success navigate a `/wells/manage`

**Flujo de edición:**
1. Leer `wellId` del route param
2. Cargar pozo + catálogos root + catálogos filtrados (campos del contrato, municipios del depto, clusters del campo)
3. Patch form con datos del pozo
4. Submit → `wellsApiService.updateWell(id, dto)` → navigate a `/wells/manage`

### 3.5. Rutas

```typescript
// wells.routes.ts
export const wellsRoutes: Routes = [
  { path: '', redirectTo: 'manage', pathMatch: 'full' },
  { path: 'manage', loadComponent: () => import('./features/well-manage/well-manage.component').then(m => m.WellManageComponent) },
  { path: 'create', loadComponent: () => import('./features/well-form/well-form.component').then(m => m.WellFormComponent) },
  { path: ':id/edit', loadComponent: () => import('./features/well-form/well-form.component').then(m => m.WellFormComponent) },
];
```

### 3.6. API Endpoints

```typescript
// api-endpoints.ts — agregar:
wells: {
  base:    `${hosts.gopApi}/api/v1/wells`,
  byId:    (id: string) => `${hosts.gopApi}/api/v1/wells/${id}`,
},
catalogs: {
  contratos:     `${hosts.gopApi}/api/v1/catalogs/contratos`,
  campos:        `${hosts.gopApi}/api/v1/catalogs/campos`,
  departamentos: `${hosts.gopApi}/api/v1/catalogs/departamentos`,
  municipios:    `${hosts.gopApi}/api/v1/catalogs/municipios`,
  clusters:      `${hosts.gopApi}/api/v1/catalogs/clusters`,
},
```

### 3.7. Locale

**`well-manage/locale.ts`:**
```typescript
export const WELL_MANAGE_LOCALE = {
  title: 'Gestión de Pozos',
  actions: { create: 'Nuevo Pozo', edit: 'Editar', delete: 'Eliminar', view: 'Ver Detalle' },
  columns: { nombrePozo: 'Nombre del Pozo', operadora: 'Operadora', contrato: 'Contrato', campo: 'Campo', clasificacion: 'Clasificación', estado: 'Estado', createdAt: 'Fecha Creación' },
  filters: { search: 'Buscar por nombre...', contrato: 'Filtrar por contrato', allContratos: 'Todos los contratos' },
  messages: { deleteConfirm: '¿Está seguro de eliminar este pozo?', deleteSuccess: 'Pozo eliminado exitosamente.', empty: 'No se encontraron pozos.' },
} as const;
```

**`well-form/locale.ts`:**
```typescript
export const WELL_FORM_LOCALE = {
  titleCreate: 'Crear Pozo',
  titleEdit: 'Editar Pozo',
  sections: { general: 'Información General', technical: 'Datos Técnicos', location: 'Ubicación' },
  fields: { contrato: 'Contrato', campo: 'Campo', tipoTrayectoria: 'Tipo de Trayectoria', clasificacion: 'Clasificación', denominacion: 'Denominación', consecutivo: 'Consecutivo', tipoUbicacion: 'Tipo de Ubicación', tipoAngulo: 'Tipo de Ángulo', tipoObjetivo: 'Tipo de Objetivo', tipoTerminacion: 'Tipo de Terminación', departamento: 'Departamento', municipio: 'Municipio', cluster: 'Cluster / Locación' },
  actions: { save: 'Guardar', cancel: 'Cancelar' },
  errors: { required: 'Este campo es requerido', denominacionPattern: 'Solo se permiten letras y espacios.', consecutivoPattern: 'Debe ser numérico de 2 dígitos (ej. 01).' },
  messages: { createSuccess: 'Pozo creado exitosamente.', updateSuccess: 'Pozo actualizado exitosamente.' },
} as const;
```

---

## 4. Restricciones (CONSTITUTION.md)

| Regla | Aplicación |
|---|---|
| §4 — Signals para estado local | `wells`, `isLoading`, `filters`, catálogos → Signals |
| §5 — Errores en interceptor | Sin `catchError` en el servicio |
| §6.1 — Solo wells usa wells models | Modelos en `domains/wells/models/`, no en `shared/` |
| §6.3 — Patrón DTO/Mapper | DTO → mapper → Model para every entity |
| §7 — Locale obligatorio | Cero textos hardcodeados |
| §11 — PrimeNG para tablas | `p-table` con lazy loading server-side |

---

## 5. Bloques Compilables

### Bloque 1 — Modelos y DTOs (ng build)
- `well-enums.ts`, `well.model.ts`, `well.dto.ts`, `well.mapper.ts`
- `catalog.model.ts`, `catalog.dto.ts`
- `index.ts` barrel

### Bloque 2 — Service y Endpoints (ng build)
- `api-endpoints.ts` (+ wells, catalogs)
- `wells-api.service.ts`, service barrel

### Bloque 3 — Listado (ng build)
- `well-manage/locale.ts`
- `well-manage.component.ts` + `.html`

### Bloque 4 — Formulario (ng build)
- `well-form/locale.ts`
- `well-form.component.ts` + `.html`

### Bloque 5 — Rutas e Integración (ng build)
- `wells.routes.ts` (actualizar con rutas reales)
- Verificar sidebar nav-items apuntan a `/wells/manage`

### Bloque 6 — Polish (ng build)
- Verificar imports, aliases, textos, build limpio

---

## 6. Orden de Dependencias

```
B1 (Modelos) → B2 (Service) → B3 (Listado) → B5 (Rutas) → B6 (Polish)
                             → B4 (Formulario) ↗
```

B3 y B4 pueden ejecutarse en paralelo después de B2. B5 requiere B3 y B4.
