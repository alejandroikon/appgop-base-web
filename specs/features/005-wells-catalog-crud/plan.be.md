# Plan Backend: Catálogos y CRUD de Pozos

**Feature ID:** 005-wells-catalog-crud
**Spec:** `specs/features/005-wells-catalog-crud/spec.md`
**Contrato:** `specs/features/005-wells-catalog-crud/contract.yml`
**Constitución:** `CONSTITUTION.backend.md`
**Depende de:** 003-backend-scaffold, 004-auth-api (ambos implementados)
**Estado:** Pendiente de revisión humana

---

## 1. Resumen Arquitectónico

Primera feature de negocio real. Introduce entidades de dominio en EF Core, la primera migración de base de datos, seed data de catálogos y CRUD completo con multi-tenant. Afecta las 4 capas:

- **Domain:** 7 entidades (Well, WellLocation, Contrato, Campo, Departamento, Municipio, Cluster), 7 enums, errores `DomainErrors.Well`
- **Application:** 5 Commands/Queries para Wells, 5 Queries para catálogos, DTOs, validators, mappings
- **Infrastructure:** 7 EF Core Configurations, DbContext con DbSets, `AuditableEntityInterceptor`, migración `InitialWellsAndCatalogs`, seed data
- **API:** `WellsController` (5 endpoints), `CatalogsController` (5 endpoints)

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
backend/src/
├── GOP.Domain/
│   ├── Entities/
│   │   ├── Well.cs                          # Entidad principal: AuditableEntity + todos los campos + TenantId
│   │   ├── WellLocation.cs                  # Value object owned: departamento, municipio, cluster
│   │   ├── Contrato.cs                      # Catálogo: Id(int), Nombre, Tipo, Cuenca
│   │   ├── Campo.cs                         # Catálogo: Id(int), Nombre, ContratoId(FK)
│   │   ├── Departamento.cs                  # Catálogo: Id(int), Nombre, CodigoDane
│   │   ├── Municipio.cs                     # Catálogo: Id(int), Nombre, DepartamentoId(FK), CodigoDane
│   │   └── Cluster.cs                       # Catálogo: Id(int), Nombre, CampoId(FK)
│   └── Enums/
│       ├── WellStatus.cs                    # Borrador, PendingUwi, ReadyFiscal, Fiscalizado
│       ├── TipoTrayectoria.cs               # ST, P, PR, ML, G, O
│       ├── Clasificacion.cs                 # Exploratorio, Desarrollo, Estratigrafico
│       ├── TipoUbicacion.cs                 # Continental, CostaFuera
│       ├── TipoAngulo.cs                    # H, V, D
│       ├── TipoObjetivo.cs                  # PH, I, M, D
│       └── TipoTerminacion.cs               # CD, LC, LR, GP, CC, OH, O
│
├── GOP.Application/
│   └── Features/
│       ├── Wells/
│       │   ├── Commands/
│       │   │   ├── CreateWell/
│       │   │   │   ├── CreateWellCommand.cs
│       │   │   │   ├── CreateWellCommandHandler.cs
│       │   │   │   └── CreateWellCommandValidator.cs
│       │   │   ├── UpdateWell/
│       │   │   │   ├── UpdateWellCommand.cs
│       │   │   │   ├── UpdateWellCommandHandler.cs
│       │   │   │   └── UpdateWellCommandValidator.cs
│       │   │   └── DeleteWell/
│       │   │       ├── DeleteWellCommand.cs
│       │   │       └── DeleteWellCommandHandler.cs
│       │   ├── Queries/
│       │   │   ├── GetWellById/
│       │   │   │   ├── GetWellByIdQuery.cs
│       │   │   │   ├── GetWellByIdQueryHandler.cs
│       │   │   │   └── WellDetailDto.cs
│       │   │   └── GetWellsList/
│       │   │       ├── GetWellsListQuery.cs
│       │   │       ├── GetWellsListQueryHandler.cs
│       │   │       └── WellListItemDto.cs
│       │   └── Mappings/
│       │       └── WellMappingProfile.cs
│       └── Catalogs/
│           └── Queries/
│               ├── GetContratos/
│               │   ├── GetContratosQuery.cs
│               │   ├── GetContratosQueryHandler.cs
│               │   └── ContratoItemDto.cs
│               ├── GetCampos/
│               │   ├── GetCamposQuery.cs
│               │   ├── GetCamposQueryHandler.cs
│               │   └── CampoItemDto.cs
│               ├── GetDepartamentos/
│               │   ├── GetDepartamentosQuery.cs
│               │   ├── GetDepartamentosQueryHandler.cs
│               │   └── DepartamentoItemDto.cs
│               ├── GetMunicipios/
│               │   ├── GetMunicipiosQuery.cs
│               │   ├── GetMunicipiosQueryHandler.cs
│               │   └── MunicipioItemDto.cs
│               └── GetClusters/
│                   ├── GetClustersQuery.cs
│                   ├── GetClustersQueryHandler.cs
│                   └── ClusterItemDto.cs
│
├── GOP.Infrastructure/
│   └── Persistence/
│       ├── Configurations/
│       │   ├── WellConfiguration.cs
│       │   ├── ContratoConfiguration.cs
│       │   ├── CampoConfiguration.cs
│       │   ├── DepartamentoConfiguration.cs
│       │   ├── MunicipioConfiguration.cs
│       │   └── ClusterConfiguration.cs
│       ├── Interceptors/
│       │   └── AuditableEntityInterceptor.cs
│       ├── Seed/
│       │   └── CatalogSeedData.cs
│       └── Migrations/
│           └── (generada por EF Core)
│
└── GOP.API/
    └── Controllers/
        ├── WellsController.cs
        └── CatalogsController.cs

backend/tests/
├── GOP.Application.Tests/Features/
│   ├── Wells/
│   │   ├── CreateWellCommandHandlerTests.cs
│   │   ├── CreateWellCommandValidatorTests.cs
│   │   └── GetWellsListQueryHandlerTests.cs
│   └── Catalogs/
│       └── GetContratosQueryHandlerTests.cs
└── GOP.API.Tests/Controllers/
    ├── WellsControllerTests.cs
    └── CatalogsControllerTests.cs
```

### 2.2. Archivos a MODIFICAR

```
backend/src/
├── GOP.Domain/Errors/DomainErrors.cs            # + clase parcial DomainErrors.Well
├── GOP.Application/Common/Interfaces/IApplicationDbContext.cs  # + DbSets de las 6 entidades
├── GOP.Infrastructure/Persistence/GopDbContext.cs              # + DbSets, + Interceptor
└── GOP.Infrastructure/DependencyInjection.cs    # + AuditableEntityInterceptor
```

---

## 3. Detalle de Diseño

### 3.1. Entidades de Dominio

**`Well.cs`** — AuditableEntity con:
- `Operadora` (string, auto-filled), `ContratoId` (int FK), `CampoId` (int FK)
- `TipoContrato`, `Cuenca` (strings derivados del Contrato, almacenados para consulta rápida)
- `TipoTrayectoria`, `Clasificacion`, `TipoUbicacion`, `TipoAngulo`, `TipoObjetivo`, `TipoTerminacion` (enums)
- `Denominacion` (string), `Consecutivo` (string), `NombrePozo` (string calculado)
- `Estado` (WellStatus, default Borrador)
- `TenantId` (int, multi-tenant)
- `Location` (WellLocation owned entity)
- Factory method `Create(...)` que calcula `NombrePozo` y setea defaults

**`WellLocation.cs`** — Owned entity (no tabla separada):
- `DepartamentoId`, `MunicipioId`, `ClusterId?` (FKs)
- `CodigoDaneDpto`, `CodigoDaneMpio` (strings derivados, almacenados)

**Catálogos** — Entidades simples con `int Id`, no heredan de `Entity` (que usa Guid):
- Cada catálogo tiene una base class `CatalogEntity` con `int Id` y `string Nombre`

### 3.2. EF Core Configurations

**`WellConfiguration.cs`:**
- Table `"Wells"`, PK `Id` (Guid)
- Enums como string con `HasConversion<string>()`
- `HasIndex(w => w.TenantId)` para filtros multi-tenant
- `HasQueryFilter(w => !w.IsDeleted)` para soft delete
- `OwnsOne(w => w.Location)` para WellLocation como columnas en la misma tabla
- FK a Contrato, Campo con `HasForeignKey` + `OnDelete(Restrict)`

**Seed data:** `CatalogSeedData.cs` aplica `HasData()` en las configuraciones de cada catálogo.

### 3.3. AuditableEntityInterceptor

Interceptor `SaveChangesInterceptor` que en `SavingChangesAsync`:
- Entidades `Added`: setea `CreatedAt = DateTime.UtcNow`, `CreatedBy = currentUser.Email`
- Entidades `Modified`: setea `LastModifiedAt = DateTime.UtcNow`, `LastModifiedBy = currentUser.Email`
- Entidades soft-deleted: setea `DeletedAt = DateTime.UtcNow`

### 3.4. Handlers Clave

**`CreateWellCommandHandler`:**
1. Validar que `ContratoId` existe → buscar Contrato para extraer `Tipo` y `Cuenca`
2. Validar que `CampoId` pertenece al `ContratoId`
3. Validar que `MunicipioId` pertenece al `DepartamentoId`
4. Calcular `NombrePozo = "{Cuenca}-{Denominacion}-{Consecutivo}"`
5. Setear `Operadora = currentUser.TenantName`, `TenantId = currentUser.TenantId`
6. Crear entidad via factory method, persistir, retornar detail DTO

**`GetWellsListQueryHandler`:**
- Multi-tenant: si `ADMIN` o `AUDITOR` → `IgnoreQueryFilters` para tenant. Si `OPERADOR`/`SUPERVISOR` → query filter por `TenantId`
- Aplicar filtros opcionales: `contratoId`, `estado`, `search`
- Aplicar sort con whitelist: `nombrePozo`, `operadora`, `contrato`, `createdAt`
- Paginar con `Skip/Take`, retornar `PagedList<WellListItemDto>`

---

## 4. Bloques Compilables

### Bloque 1 — Domain (dotnet build GOP.Domain)
- 7 enums, 7 entidades, `DomainErrors.Well` (parcial)
- ~15 archivos

### Bloque 2 — Application (dotnet build GOP.Application)
- `IApplicationDbContext` + DbSets
- 5 Well Commands/Queries (3 commands + 2 queries) con handlers, validators, DTOs
- 5 Catalog Queries con handlers y DTOs
- `WellMappingProfile`
- ~26 archivos

### Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)
- 6 EF Configurations + `AuditableEntityInterceptor`
- `CatalogSeedData`
- `GopDbContext` + DbSets
- `DependencyInjection` + interceptor
- Migración `InitialWellsAndCatalogs`
- ~10 archivos + migración

### Bloque 4 — API (dotnet build GOP.API)
- `WellsController` (5 endpoints)
- `CatalogsController` (5 endpoints)
- ~2 archivos

### Bloque 5 — Tests (dotnet test)
- Handler tests: CreateWell (4), Validator (4), GetWellsList (3), GetContratos (2)
- Integration tests: Wells controller (5), Catalogs controller (3)
- ~6 archivos

---

## 5. Orden de Dependencias

```
B1 (Domain) → B2 (Application) → B3 (Infrastructure) → B4 (API) → B5 (Tests)
```

Secuencial estricto. La migración EF Core en B3 depende de que Domain y Application tengan las entidades y DbSets.
