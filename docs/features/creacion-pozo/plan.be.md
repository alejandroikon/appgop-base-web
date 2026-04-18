# Plan Backend — Creación de Pozo Nuevo V2.0

**Referencia:** `spec.md`, `contract.yml`, `data-model.md`, `uwi-algorithm.md`
**Constitución:** `CONSTITUTION.backend.md`
**Stack:** .NET 10 / C# 13 + Clean Architecture + MediatR 12 + EF Core 10 + FluentValidation 11

---

## 1. Proyectos afectados

| Proyecto | Cambios |
|----------|---------|
| `GOP.Domain` | Entidad Well refactorizada, nuevos enums, Value Object Uwi, errors actualizados |
| `GOP.Application` | Commands/Queries nuevos (CreateWell, UpdateWell, DeleteWell, PreviewUwi, PreviewName), validators |
| `GOP.Infrastructure` | WellConfiguration actualizada, migración, repositorio extendido |
| `GOP.API` | WellsController actualizado, endpoint preview-uwi nuevo |

## 2. Domain Layer

### 2.1. Entidades modificadas

**`Well.cs`** — Refactorizar según `data-model.md` §1:
- Agregar `SubClasificacion` (nullable `SubClasificacionExploratoria`)
- Cambiar `Consecutivo` de string a int (1-9999)
- Agregar `Forma101Radicada` (bool)
- Simplificar `WellStatus` a 2 valores
- Aplanar ubicación (quitar `WellLocation` nested, poner Dpto/Mpio/Cluster directo)
- Factory method `Well.CreateDraft(...)` y `Well.CreateFinalized(...)`

**`Uwi.cs`** (NUEVO) — Value Object según `uwi-algorithm.md`:
- Factory method `Uwi.Generate(...)` con Result Pattern
- Componentes desglosados para auditoría
- Igualdad por `Value`

### 2.2. Enums nuevos/actualizados

| Enum | Cambio |
|------|--------|
| `WellStatus` | Reducir a `Borrador`, `Creado` |
| `SubClasificacionExploratoria` | NUEVO: A3, A2a, A2b, A2c, A1 |
| `TipoObjetivo` | Agregar C, GT, O |

### 2.3. Domain Errors

```csharp
public static partial class DomainErrors
{
    public static class Well
    {
        public static readonly Error DuplicateUwi;      // RN-37
        public static readonly Error DuplicateName;     // RN-19
        public static readonly Error Forma101Locked;    // RN-40
        public static readonly Error InvalidClasificacionForAnh;  // RN-15
        public static readonly Error CampoRequiredForDesarrollo;  // RN-12
        public static readonly Error NotEditable;       // RN-40
        public static readonly Error NotDeletable;      // RN-40
    }
}
```

### 2.4. Interfaces de repositorio

```csharp
public interface IWellRepository
{
    void Add(Well well);
    void Update(Well well);
    Task<Well?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByUwiAsync(string uwi, Guid? excludeWellId, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string nombrePozo, int tenantId, Guid? excludeWellId, CancellationToken ct);
}
```

## 3. Application Layer

### 3.1. Commands

#### `CreateWell/`

| Archivo | Responsabilidad |
|---------|----------------|
| `CreateWellCommand.cs` | Record con action (DRAFT\|FINALIZE) + todos los campos nullable |
| `CreateWellCommandHandler.cs` | Si DRAFT: validación mínima, persistir sin UWI. Si FINALIZE: validación completa, generar UWI via `Uwi.Generate()`, verificar unicidad. |
| `CreateWellCommandValidator.cs` | Si FINALIZE: todos los campos required. Si DRAFT: al menos 1 campo. RN-07 (denominación regex), RN-08 (consecutivo 1-9999). |

> Ver CONSTITUTION.backend.md §4.2 y §5: estructura de 3 archivos por Command.

#### `UpdateWell/`

| Archivo | Responsabilidad |
|---------|----------------|
| `UpdateWellCommand.cs` | Record con wellId + action (SAVE\|FINALIZE) + campos |
| `UpdateWellCommandHandler.cs` | Verificar estado editable (BORRADOR o CREADO sin Forma101). Si FINALIZE: generar UWI. |
| `UpdateWellCommandValidator.cs` | Mismas reglas que CreateWell según action. |

#### `DeleteWell/`

| Archivo | Responsabilidad |
|---------|----------------|
| `DeleteWellCommand.cs` | Record con wellId |
| `DeleteWellCommandHandler.cs` | Verificar: estado BORRADOR, o CREADO sin Forma101. Soft delete. |

#### `CreateCluster/`

| Archivo | Responsabilidad |
|---------|----------------|
| `CreateClusterCommand.cs` | Record con nombre + campoId |
| `CreateClusterCommandHandler.cs` | Verificar unicidad en el campo. Generar abreviatura (2 chars). |
| `CreateClusterCommandValidator.cs` | nombre required, maxLength 100. |

### 3.2. Queries

#### `GetWellById/`

| Archivo | Responsabilidad |
|---------|----------------|
| `GetWellByIdQuery.cs` | Record con wellId |
| `GetWellByIdQueryHandler.cs` | Query con AsNoTracking. Multi-tenant filter. |
| `WellDetailDto.cs` | DTO espejo del schema WellDetail en contract.yml |

#### `GetWellsList/`

| Archivo | Responsabilidad |
|---------|----------------|
| `GetWellsListQuery.cs` | Record con page, pageSize, search, sortBy, sortDir, estado, contratoId |
| `GetWellsListQueryHandler.cs` | Paginación + filtros. AsNoTracking. |
| `WellListItemDto.cs` | DTO espejo del schema WellListItem |

#### `PreviewUwi/`

| Archivo | Responsabilidad |
|---------|----------------|
| `PreviewUwiQuery.cs` | Record con todos los parámetros del UWI |
| `PreviewUwiQueryHandler.cs` | Usa `Uwi.Generate()` del Domain. Verifica unicidad en DB. |
| `UwiPreviewDto.cs` | DTO con uwi, isUnique, components |

#### `PreviewWellName/`

| Archivo | Responsabilidad |
|---------|----------------|
| `PreviewWellNameQuery.cs` | Record con contratoId, campoId?, denominacion, consecutivo |
| `PreviewWellNameQueryHandler.cs` | Genera nombre y verifica unicidad por tenant. |
| `WellNamePreviewDto.cs` | DTO con nombrePozo, isUnique |

### 3.3. Mappings

```
Features/Wells/Mappings/
└── WellMappingProfile.cs   # Well → WellDetailDto, Well → WellListItemDto
```

> Ver CONSTITUTION.backend.md §12: solo Entity → DTO, nunca al revés.

## 4. Infrastructure Layer

### 4.1. Configurations

**`WellConfiguration.cs`** — Actualizar:
- `SubClasificacion` como string nullable
- `Consecutivo` como int
- `Uwi` como string unique (global, no per-tenant)
- `NombrePozo` como string unique per-tenant (composite index: TenantId + NombrePozo)
- `Forma101Radicada` como bool default false
- Aplanar campos de ubicación (sin owned entity)

**`ClusterConfiguration.cs`** — Agregar `Abreviatura` (maxLength 2).

### 4.2. Migración

```
Nombre: UpdateWellsForV2_PpdmUwi
Cambios:
- ALTER Wells: agregar SubClasificacion (nvarchar 5, nullable)
- ALTER Wells: agregar Forma101Radicada (bit, default 0)
- ALTER Wells: cambiar Consecutivo de nvarchar a int
- ALTER Wells: aplanar ubicación (mover DptoId, MpioId, ClusterId a columnas directas si eran nested)
- DROP INDEX IX_Wells_Uwi (si existe con old format)
- CREATE UNIQUE INDEX IX_Wells_Uwi_Global ON Wells(Uwi) WHERE Uwi IS NOT NULL AND IsDeleted = 0
- CREATE UNIQUE INDEX IX_Wells_TenantId_NombrePozo ON Wells(TenantId, NombrePozo) WHERE IsDeleted = 0
- ALTER Clusters: agregar Abreviatura (nvarchar 2)
- UPDATE WellStatus values: PENDING_UWI → (migrar a BORRADOR), READY_FISCAL → CREADO, FISCALIZADO → CREADO
```

> Ver CONSTITUTION.backend.md §7.4: una migración por cambio lógico.

### 4.3. Repository

**`WellRepository.cs`** — Extender con:
- `ExistsByUwiAsync(uwi, excludeWellId?, ct)`
- `ExistsByNameAsync(nombrePozo, tenantId, excludeWellId?, ct)`

## 5. API Layer

### 5.1. Controller Actions

```csharp
[ApiController]
[Route("api/v1/wells")]
[Authorize]
public sealed class WellsController(ISender sender) : ControllerBase
{
    [HttpGet]                              // listWells
    [HttpPost]                             // createWell
    [HttpGet("{wellId:guid}")]             // getWell
    [HttpPut("{wellId:guid}")]             // updateWell
    [HttpDelete("{wellId:guid}")]          // deleteWell
    [HttpGet("preview-uwi")]              // previewUwi
    [HttpGet("preview-name")]             // previewWellName
}
```

> Ver CONSTITUTION.backend.md §13.3: controllers max 3 líneas por action, solo `sender.Send()` + `.ToActionResult()`.

### 5.2. CatalogsController

Agregar action `POST /catalogs/clusters` → `CreateClusterCommand`.

## 6. Tests

### 6.1. Domain Tests

| Test | Escenarios |
|------|-----------|
| `UwiTests.cs` | Generate: happy path, ANH sigla, padding, trayectoria vacía (Original), longitud válida |
| `WellTests.cs` | CreateDraft: datos mínimos. CreateFinalized: datos completos. Estado correcto. |

### 6.2. Application Tests

| Test | Escenarios mínimos |
|------|-------------------|
| `CreateWellCommandHandlerTests.cs` | Draft OK, Finalize OK, UWI duplicado, nombre duplicado, ANH solo estratigráfico |
| `UpdateWellCommandHandlerTests.cs` | Update borrador OK, Forma101 bloqueado, Finalize borrador OK |
| `DeleteWellCommandHandlerTests.cs` | Delete borrador OK, Forma101 bloqueado, not found |
| `PreviewUwiQueryHandlerTests.cs` | UWI correcto, unicidad true, unicidad false |
| `CreateClusterCommandHandlerTests.cs` | OK, duplicado |

> Ver CONSTITUTION.backend.md §11.6: mínimo 3 escenarios por handler.

## 7. Trazabilidad contract → plan

| Endpoint (contract.yml) | Controller Action | Command/Query | Validator |
|-------------------------|------------------|---------------|-----------|
| `POST /wells` | `CreateWell` | `CreateWellCommand` | `CreateWellCommandValidator` |
| `GET /wells` | `GetWells` | `GetWellsListQuery` | — |
| `GET /wells/{id}` | `GetWell` | `GetWellByIdQuery` | — |
| `PUT /wells/{id}` | `UpdateWell` | `UpdateWellCommand` | `UpdateWellCommandValidator` |
| `DELETE /wells/{id}` | `DeleteWell` | `DeleteWellCommand` | — |
| `GET /wells/preview-uwi` | `PreviewUwi` | `PreviewUwiQuery` | `PreviewUwiQueryValidator` |
| `GET /wells/preview-name` | `PreviewWellName` | `PreviewWellNameQuery` | — |
| `GET /catalogs/contratos` | `ListContratos` | `GetContratosQuery` | — |
| `GET /catalogs/campos` | `ListCampos` | `GetCamposQuery` | — |
| `GET /catalogs/departamentos` | `ListDepartamentos` | `GetDepartamentosQuery` | — |
| `GET /catalogs/municipios` | `ListMunicipios` | `GetMunicipiosQuery` | — |
| `GET /catalogs/clusters` | `ListClusters` | `GetClustersQuery` | — |
| `POST /catalogs/clusters` | `CreateCluster` | `CreateClusterCommand` | `CreateClusterCommandValidator` |
