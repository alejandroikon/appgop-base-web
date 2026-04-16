# Tasks Backend: Catálogos y CRUD de Pozos (005-wells-catalog-crud)

**Input:** `specs/features/005-wells-catalog-crud/spec.md` + `specs/features/005-wells-catalog-crud/plan.be.md`
**Contrato:** `specs/features/005-wells-catalog-crud/contract.yml`
**Constitución:** `CONSTITUTION.backend.md`

**Formato:** `[ID] [P?] [HU?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — sin dependencias de tareas incompletas en el mismo bloque
- **[HU-N]**: Historia de usuario de referencia en spec.md
- **[BLOQUEANTE]**: Tareas posteriores no pueden iniciar sin esta completada

---

## Bloque 1 — Domain (dotnet build GOP.Domain)

**Propósito:** Crear las 7 entidades de dominio, 7 enums y los errores de negocio de Wells. Sin dependencias externas ni NuGet adicionales.

### Enums

- [x] T001 [P] Crear enum `WellStatus` — valores: `Borrador`, `PendingUwi`, `ReadyFiscal`, `Fiscalizado` — `backend/src/GOP.Domain/Enums/WellStatus.cs`
- [x] T002 [P] Crear enum `TipoTrayectoria` — valores: `ST`, `P`, `PR`, `ML`, `G`, `O` — `backend/src/GOP.Domain/Enums/TipoTrayectoria.cs`
- [x] T003 [P] Crear enum `Clasificacion` — valores: `Exploratorio`, `Desarrollo`, `Estratigrafico` — `backend/src/GOP.Domain/Enums/Clasificacion.cs`
- [x] T004 [P] Crear enum `TipoUbicacion` — valores: `Continental`, `CostaFuera` — `backend/src/GOP.Domain/Enums/TipoUbicacion.cs`
- [x] T005 [P] Crear enum `TipoAngulo` — valores: `H`, `V`, `D` — `backend/src/GOP.Domain/Enums/TipoAngulo.cs`
- [x] T006 [P] Crear enum `TipoObjetivo` — valores: `PH`, `I`, `M`, `D` — `backend/src/GOP.Domain/Enums/TipoObjetivo.cs`
- [x] T007 [P] Crear enum `TipoTerminacion` — valores: `CD`, `LC`, `LR`, `GP`, `CC`, `OH`, `O` — `backend/src/GOP.Domain/Enums/TipoTerminacion.cs`

### Entidades de catálogo

- [x] T008 [P] Crear clase base `CatalogEntity` — `int Id`, `string Nombre`; no hereda de `Entity` (que usa Guid) — `backend/src/GOP.Domain/Entities/CatalogEntity.cs`
- [x] T009 [P] Crear entidad `Contrato` — hereda `CatalogEntity`, agrega: `Tipo` (string), `Cuenca` (string); nav prop `ICollection<Campo>` — `backend/src/GOP.Domain/Entities/Contrato.cs`
- [x] T010 [P] Crear entidad `Departamento` — hereda `CatalogEntity`, agrega: `CodigoDane` (string); nav prop `ICollection<Municipio>` — `backend/src/GOP.Domain/Entities/Departamento.cs`
- [x] T011 Crear entidad `Campo` — hereda `CatalogEntity`, agrega: `ContratoId` (int FK); nav props `Contrato`, `ICollection<Cluster>` — `backend/src/GOP.Domain/Entities/Campo.cs`
- [x] T012 Crear entidad `Municipio` — hereda `CatalogEntity`, agrega: `DepartamentoId` (int FK), `CodigoDane` (string); nav prop `Departamento` — `backend/src/GOP.Domain/Entities/Municipio.cs`
- [x] T013 Crear entidad `Cluster` — hereda `CatalogEntity`, agrega: `CampoId` (int FK); nav prop `Campo` — `backend/src/GOP.Domain/Entities/Cluster.cs`

### Entidades de negocio

- [x] T014 Crear clase `WellLocation` — owned entity (no hereda Entity): `DepartamentoId` (int), `MunicipioId` (int), `ClusterId` (int?), `CodigoDaneDpto` (string), `CodigoDaneMpio` (string) — `backend/src/GOP.Domain/Entities/WellLocation.cs`
- [x] T015 [BLOQUEANTE] [HU-021] Crear entidad `Well` — hereda `AuditableEntity`; propiedades: `Operadora`, `ContratoId`, `CampoId`, `TipoContrato`, `Cuenca` (strings derivados), los 6 enums, `Denominacion`, `Consecutivo`, `NombrePozo` (calculado), `Estado` (WellStatus, default Borrador), `TenantId` (int); owned `Location` (WellLocation); factory method estático `Create(...)` que calcula `NombrePozo = "{Cuenca}-{Denominacion}-{Consecutivo}"` y setea `Estado = Borrador`; método `Update(...)` que recalcula NombrePozo — `backend/src/GOP.Domain/Entities/Well.cs`

### Errores de dominio

- [x] T016 [HU-021] Modificar `DomainErrors.cs` — agregar clase parcial `DomainErrors.Well` con errores: `NotFound` ("Well.NotFound"), `NotFoundById(Guid)`, `InvalidStatus` ("Well.InvalidStatus", "Solo se pueden editar pozos en estado borrador."), `DeleteInvalidStatus` ("Well.DeleteInvalidStatus", "Solo se pueden eliminar pozos en estado borrador."), `CampoNotBelongsToContrato` ("Well.CampoNotBelongsToContrato"), `MunicipioNotBelongsToDepartamento` ("Well.MunicipioNotBelongsToDepartamento") — `backend/src/GOP.Domain/Errors/DomainErrors.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → ✅ exit code 0.

---

## Bloque 2 — Application (dotnet build GOP.Application)

**Propósito:** Implementar los 10 casos de uso (5 wells + 5 catálogos) con handlers, validators, DTOs y mappings. Extender `IApplicationDbContext` con los DbSets necesarios.

### Interfaz DbContext

- [x] T017 [BLOQUEANTE] Modificar `IApplicationDbContext.cs` — agregar `DbSet<Well> Wells`, `DbSet<Contrato> Contratos`, `DbSet<Campo> Campos`, `DbSet<Departamento> Departamentos`, `DbSet<Municipio> Municipios`, `DbSet<Cluster> Clusters` — `backend/src/GOP.Application/Common/Interfaces/IApplicationDbContext.cs`

### DTOs de Wells

- [x] T018 [P] [HU-020] Crear `WellListItemDto` — sealed record: `Id` (Guid), `NombrePozo`, `Operadora`, `Contrato`, `Campo`, `Clasificacion`, `Estado` (strings), `CreatedAt` (DateTime) — `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/WellListItemDto.cs`
- [x] T019 [P] [HU-022] Crear `WellDetailDto` — sealed record: todos los campos del pozo + ubicación aplanada (departamento, municipio, cluster con nombres y códigos DANE) + `CreatedAt`, `LastModifiedAt` — `backend/src/GOP.Application/Features/Wells/Queries/GetWellById/WellDetailDto.cs`

### DTOs de catálogos

- [x] T020 [P] [HU-025] Crear `ContratoItemDto` — sealed record: `Id` (int), `Nombre`, `Tipo`, `Cuenca` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetContratos/ContratoItemDto.cs`
- [x] T021 [P] [HU-025] Crear `CampoItemDto` — sealed record: `Id` (int), `Nombre`, `ContratoId` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetCampos/CampoItemDto.cs`
- [x] T022 [P] [HU-025] Crear `DepartamentoItemDto` — sealed record: `Id` (int), `Nombre`, `CodigoDane` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetDepartamentos/DepartamentoItemDto.cs`
- [x] T023 [P] [HU-025] Crear `MunicipioItemDto` — sealed record: `Id` (int), `Nombre`, `DepartamentoId`, `CodigoDane` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetMunicipios/MunicipioItemDto.cs`
- [x] T024 [P] [HU-025] Crear `ClusterItemDto` — sealed record: `Id` (int), `Nombre`, `CampoId` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetClusters/ClusterItemDto.cs`

### Mapping

- [x] T025 [HU-020] Crear `WellMappingProfile` — AutoMapper profile: `Well` → `WellListItemDto` (flatten Contrato.Nombre, Campo.Nombre, enum→string), `Well` → `WellDetailDto` (flatten ubicación + catálogos) — `backend/src/GOP.Application/Features/Wells/Mappings/WellMappingProfile.cs`

### Commands de Wells

- [x] T026 [HU-021] Crear `CreateWellCommand` — record: `ContratoId`, `CampoId`, `TipoTrayectoria`, `Clasificacion`, `Denominacion`, `Consecutivo`, `TipoUbicacion`, `TipoAngulo`, `TipoObjetivo`, `TipoTerminacion`, `DepartamentoId`, `MunicipioId`, `ClusterId?`; implementa `IRequest<Result<WellDetailDto>>` — `backend/src/GOP.Application/Features/Wells/Commands/CreateWell/CreateWellCommand.cs`
- [x] T027 [HU-021] Crear `CreateWellCommandValidator` — reglas: todos los campos required NotEmpty, `Denominacion` MaxLength(50) + Matches solo letras, `Consecutivo` Matches `^\d{2}$`, enums válidos — `backend/src/GOP.Application/Features/Wells/Commands/CreateWell/CreateWellCommandValidator.cs`
- [x] T028 [BLOQUEANTE] [HU-021] Crear `CreateWellCommandHandler` — flujo: buscar Contrato → validar Campo pertenece al Contrato → buscar Departamento → validar Municipio pertenece al Departamento → buscar Cluster (opcional) → calcular NombrePozo → setear Operadora y TenantId de ICurrentUserService → `Well.Create(...)` → persistir → map a WellDetailDto — `backend/src/GOP.Application/Features/Wells/Commands/CreateWell/CreateWellCommandHandler.cs`
- [x] T029 [HU-023] Crear `UpdateWellCommand` — record: `WellId` (Guid, del path), mismos campos editables que Create; implementa `IRequest<Result<WellDetailDto>>` — `backend/src/GOP.Application/Features/Wells/Commands/UpdateWell/UpdateWellCommand.cs`
- [x] T030 [HU-023] Crear `UpdateWellCommandValidator` — mismas reglas que CreateWellCommandValidator — `backend/src/GOP.Application/Features/Wells/Commands/UpdateWell/UpdateWellCommandValidator.cs`
- [x] T031 [HU-023] Crear `UpdateWellCommandHandler` — flujo: buscar Well por Id → verificar `Estado == Borrador` (si no → InvalidStatus) → mismas validaciones de FK que Create → `well.Update(...)` → persistir → map a WellDetailDto — `backend/src/GOP.Application/Features/Wells/Commands/UpdateWell/UpdateWellCommandHandler.cs`
- [x] T032 [HU-024] Crear `DeleteWellCommand` — record: `WellId` (Guid); implementa `IRequest<Result>` — `backend/src/GOP.Application/Features/Wells/Commands/DeleteWell/DeleteWellCommand.cs`
- [x] T033 [HU-024] Crear `DeleteWellCommandHandler` — flujo: buscar Well por Id → verificar `Estado == Borrador` (si no → DeleteInvalidStatus) → setear `IsDeleted = true` → persistir — `backend/src/GOP.Application/Features/Wells/Commands/DeleteWell/DeleteWellCommandHandler.cs`

### Queries de Wells

- [x] T034 [HU-022] Crear `GetWellByIdQuery` — record: `WellId` (Guid); implementa `IRequest<Result<WellDetailDto>>` — `backend/src/GOP.Application/Features/Wells/Queries/GetWellById/GetWellByIdQuery.cs`
- [x] T035 [HU-022] Crear `GetWellByIdQueryHandler` — `AsNoTracking`, Include Contrato+Campo+Location relations, map con AutoMapper `ProjectTo<WellDetailDto>()` o manual; multi-tenant (ADMIN/AUDITOR → IgnoreQueryFilters) — `backend/src/GOP.Application/Features/Wells/Queries/GetWellById/GetWellByIdQueryHandler.cs`
- [x] T036 [HU-020] Crear `GetWellsListQuery` — record: `Page`, `PageSize`, `Search?`, `SortBy?`, `SortDir?`, `ContratoId?`, `Estado?`; implementa `IRequest<Result<PagedList<WellListItemDto>>>` (usar PagedList del scaffold o crear) — `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQuery.cs`
- [x] T037 [HU-020] Crear `GetWellsListQueryHandler` — `AsNoTracking`, multi-tenant filter, filtros opcionales (contratoId, estado, search por NombrePozo/Denominacion), sort con whitelist (`nombrePozo`, `operadora`, `contrato`, `createdAt`), paginar Skip/Take, map a `PagedList<WellListItemDto>` — `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQueryHandler.cs`

### Queries de catálogos

- [x] T038 [P] [HU-025] Crear `GetContratosQuery` + `GetContratosQueryHandler` — record vacío; handler: `AsNoTracking`, retorna lista de `ContratoItemDto` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetContratos/GetContratosQuery.cs` + `GetContratosQueryHandler.cs`
- [x] T039 [P] [HU-025] Crear `GetCamposQuery` + `GetCamposQueryHandler` — record: `ContratoId` (int); handler: filtra por ContratoId, retorna `CampoItemDto[]` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetCampos/GetCamposQuery.cs` + `GetCamposQueryHandler.cs`
- [x] T040 [P] [HU-025] Crear `GetDepartamentosQuery` + `GetDepartamentosQueryHandler` — record vacío; handler: retorna todos — `backend/src/GOP.Application/Features/Catalogs/Queries/GetDepartamentos/GetDepartamentosQuery.cs` + `GetDepartamentosQueryHandler.cs`
- [x] T041 [P] [HU-025] Crear `GetMunicipiosQuery` + `GetMunicipiosQueryHandler` — record: `DepartamentoId` (int); handler: filtra — `backend/src/GOP.Application/Features/Catalogs/Queries/GetMunicipios/GetMunicipiosQuery.cs` + `GetMunicipiosQueryHandler.cs`
- [x] T042 [P] [HU-025] Crear `GetClustersQuery` + `GetClustersQueryHandler` — record: `CampoId` (int); handler: filtra — `backend/src/GOP.Application/Features/Catalogs/Queries/GetClusters/GetClustersQuery.cs` + `GetClustersQueryHandler.cs`

> **Nota:** T038–T042 son 2 archivos por tarea (query + handler). Son lo suficientemente simples para mantenerse como una tarea atómica (query record + handler trivial read-only).

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → ✅ exit code 0.

---

## Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)

**Propósito:** Configurar EF Core con las 7 entidades, seed data de catálogos, interceptor de auditoría, actualizar DbContext y DI, generar primera migración real.

### Configuraciones EF Core

- [x] T043 [P] Crear `ContratoConfiguration` — tabla `"Contratos"`, PK int, `Nombre` required maxlen 200, `Tipo` required maxlen 50, `Cuenca` required maxlen 200; seed data: 3 contratos de spec.md §5 — `backend/src/GOP.Infrastructure/Persistence/Configurations/ContratoConfiguration.cs`
- [x] T044 [P] Crear `DepartamentoConfiguration` — tabla `"Departamentos"`, PK int, `Nombre` required maxlen 100, `CodigoDane` required maxlen 10; seed data: 3 departamentos — `backend/src/GOP.Infrastructure/Persistence/Configurations/DepartamentoConfiguration.cs`
- [x] T045 Crear `CampoConfiguration` — tabla `"Campos"`, PK int, FK a Contrato (Restrict), `Nombre` required maxlen 200; seed data: 6 campos — `backend/src/GOP.Infrastructure/Persistence/Configurations/CampoConfiguration.cs`
- [x] T046 Crear `MunicipioConfiguration` — tabla `"Municipios"`, PK int, FK a Departamento (Restrict), `Nombre` required maxlen 200, `CodigoDane` required maxlen 10; seed data: 6 municipios — `backend/src/GOP.Infrastructure/Persistence/Configurations/MunicipioConfiguration.cs`
- [x] T047 Crear `ClusterConfiguration` — tabla `"Clusters"`, PK int, FK a Campo (Restrict), `Nombre` required maxlen 200; seed data: 4 clusters — `backend/src/GOP.Infrastructure/Persistence/Configurations/ClusterConfiguration.cs`
- [x] T048 [BLOQUEANTE] Crear `WellConfiguration` — tabla `"Wells"`, PK Guid, `Operadora` maxlen 200, `NombrePozo` maxlen 300, `Denominacion` maxlen 50, `Consecutivo` maxlen 2, enums como string con `HasConversion<string>()`, FKs a Contrato/Campo con Restrict, `OwnsOne(w => w.Location)` con FK a Departamento/Municipio/Cluster, `HasQueryFilter(w => !w.IsDeleted)`, Index en `TenantId` — `backend/src/GOP.Infrastructure/Persistence/Configurations/WellConfiguration.cs`

### Interceptor de auditoría

- [x] T049 Crear `AuditableEntityInterceptor` — `SaveChangesInterceptor`; en `SavingChangesAsync`: iterar `ChangeTracker.Entries<AuditableEntity>()`; si `Added` → setear `CreatedAt = DateTime.UtcNow`, `CreatedBy = currentUser.Email`; si `Modified` → setear `LastModifiedAt`, `LastModifiedBy`; si prop `IsDeleted` cambió a `true` → setear `DeletedAt` — `backend/src/GOP.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs`

### DbContext y DI

- [x] T050 Modificar `GopDbContext.cs` — agregar DbSets: `DbSet<Well> Wells`, `DbSet<Contrato> Contratos`, `DbSet<Campo> Campos`, `DbSet<Departamento> Departamentos`, `DbSet<Municipio> Municipios`, `DbSet<Cluster> Clusters` — `backend/src/GOP.Infrastructure/Persistence/GopDbContext.cs`
- [x] T051 Modificar `DependencyInjection.cs` — registrar `AuditableEntityInterceptor` como Scoped; agregar el interceptor al `AddDbContext<GopDbContext>` via `options.AddInterceptors(...)` — `backend/src/GOP.Infrastructure/DependencyInjection.cs`

### Migración

- [x] T052 Generar migración `InitialWellsAndCatalogs` — ejecutar `dotnet ef migrations add InitialWellsAndCatalogs -p src/GOP.Infrastructure -s src/GOP.API`; verificar que la migración incluye: tablas Wells, Contratos, Campos, Departamentos, Municipios, Clusters con seed data — `backend/src/GOP.Infrastructure/Persistence/Migrations/` (generado)

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → ✅ exit code 0.

---

## Bloque 4 — API (dotnet build GOP.API)

**Propósito:** Crear los 2 controllers que implementan los 10 endpoints del contrato.

- [x] T053 [HU-020] [HU-021] [HU-022] [HU-023] [HU-024] Crear `WellsController` — `[ApiController]`, `[Route("api/v1/wells")]`, `[Authorize]`; inyecta `ISender`; 5 actions — `backend/src/GOP.API/Controllers/WellsController.cs`
- [x] T054 [HU-025] Crear `CatalogsController` — `[ApiController]`, `[Route("api/v1/catalogs")]`, `[Authorize]`; inyecta `ISender`; 5 actions — `backend/src/GOP.API/Controllers/CatalogsController.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → ✅ exit code 0.

---

## Bloque 5 — Tests (dotnet test GOP.sln)

**Propósito:** Tests unitarios de handlers y validators, tests de integración de los endpoints.

- [x] T055 [P] [HU-021] Crear `CreateWellCommandValidatorTests` — 4 tests — `backend/tests/GOP.Application.Tests/Features/Wells/CreateWellCommandValidatorTests.cs`
- [x] T056 [HU-021] Crear `CreateWellCommandHandlerTests` — 4 tests — `backend/tests/GOP.Application.Tests/Features/Wells/CreateWellCommandHandlerTests.cs`
- [x] T057 [P] [HU-020] Crear `GetWellsListQueryHandlerTests` — 3 tests — `backend/tests/GOP.Application.Tests/Features/Wells/GetWellsListQueryHandlerTests.cs`
- [x] T058 [P] [HU-025] Crear `GetContratosQueryHandlerTests` — 2 tests — `backend/tests/GOP.Application.Tests/Features/Catalogs/GetContratosQueryHandlerTests.cs`
- [x] T059 [HU-020] [HU-021] [HU-023] [HU-024] Crear `WellsControllerTests` — 5 tests de integración — `backend/tests/GOP.API.Tests/Controllers/WellsControllerTests.cs`
- [x] T060 [HU-025] Crear `CatalogsControllerTests` — 3 tests de integración — `backend/tests/GOP.API.Tests/Controllers/CatalogsControllerTests.cs`

**Checkpoint:** `dotnet test` → ✅ 54 tests pasan (9 Domain + 28 Application + 1 Infrastructure + 16 API), exit code 0.

---

## Dependencies & Execution Order

### Diagrama

```
B1 (Domain) → B2 (Application) → B3 (Infrastructure) → B4 (API) → B5 (Tests)
```

✅ **TODOS LOS BLOQUES COMPLETADOS** — Iteración 3 Wells Backend implementada.
