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

- [ ] T001 [P] Crear enum `WellStatus` — valores: `Borrador`, `PendingUwi`, `ReadyFiscal`, `Fiscalizado` — `backend/src/GOP.Domain/Enums/WellStatus.cs`
- [ ] T002 [P] Crear enum `TipoTrayectoria` — valores: `ST`, `P`, `PR`, `ML`, `G`, `O` — `backend/src/GOP.Domain/Enums/TipoTrayectoria.cs`
- [ ] T003 [P] Crear enum `Clasificacion` — valores: `Exploratorio`, `Desarrollo`, `Estratigrafico` — `backend/src/GOP.Domain/Enums/Clasificacion.cs`
- [ ] T004 [P] Crear enum `TipoUbicacion` — valores: `Continental`, `CostaFuera` — `backend/src/GOP.Domain/Enums/TipoUbicacion.cs`
- [ ] T005 [P] Crear enum `TipoAngulo` — valores: `H`, `V`, `D` — `backend/src/GOP.Domain/Enums/TipoAngulo.cs`
- [ ] T006 [P] Crear enum `TipoObjetivo` — valores: `PH`, `I`, `M`, `D` — `backend/src/GOP.Domain/Enums/TipoObjetivo.cs`
- [ ] T007 [P] Crear enum `TipoTerminacion` — valores: `CD`, `LC`, `LR`, `GP`, `CC`, `OH`, `O` — `backend/src/GOP.Domain/Enums/TipoTerminacion.cs`

### Entidades de catálogo

- [ ] T008 [P] Crear clase base `CatalogEntity` — `int Id`, `string Nombre`; no hereda de `Entity` (que usa Guid) — `backend/src/GOP.Domain/Entities/CatalogEntity.cs`
- [ ] T009 [P] Crear entidad `Contrato` — hereda `CatalogEntity`, agrega: `Tipo` (string), `Cuenca` (string); nav prop `ICollection<Campo>` — `backend/src/GOP.Domain/Entities/Contrato.cs`
- [ ] T010 [P] Crear entidad `Departamento` — hereda `CatalogEntity`, agrega: `CodigoDane` (string); nav prop `ICollection<Municipio>` — `backend/src/GOP.Domain/Entities/Departamento.cs`
- [ ] T011 Crear entidad `Campo` — hereda `CatalogEntity`, agrega: `ContratoId` (int FK); nav props `Contrato`, `ICollection<Cluster>` — `backend/src/GOP.Domain/Entities/Campo.cs`
- [ ] T012 Crear entidad `Municipio` — hereda `CatalogEntity`, agrega: `DepartamentoId` (int FK), `CodigoDane` (string); nav prop `Departamento` — `backend/src/GOP.Domain/Entities/Municipio.cs`
- [ ] T013 Crear entidad `Cluster` — hereda `CatalogEntity`, agrega: `CampoId` (int FK); nav prop `Campo` — `backend/src/GOP.Domain/Entities/Cluster.cs`

### Entidades de negocio

- [ ] T014 Crear clase `WellLocation` — owned entity (no hereda Entity): `DepartamentoId` (int), `MunicipioId` (int), `ClusterId` (int?), `CodigoDaneDpto` (string), `CodigoDaneMpio` (string) — `backend/src/GOP.Domain/Entities/WellLocation.cs`
- [ ] T015 [BLOQUEANTE] [HU-021] Crear entidad `Well` — hereda `AuditableEntity`; propiedades: `Operadora`, `ContratoId`, `CampoId`, `TipoContrato`, `Cuenca` (strings derivados), los 6 enums, `Denominacion`, `Consecutivo`, `NombrePozo` (calculado), `Estado` (WellStatus, default Borrador), `TenantId` (int); owned `Location` (WellLocation); factory method estático `Create(...)` que calcula `NombrePozo = "{Cuenca}-{Denominacion}-{Consecutivo}"` y setea `Estado = Borrador`; método `Update(...)` que recalcula NombrePozo — `backend/src/GOP.Domain/Entities/Well.cs`

### Errores de dominio

- [ ] T016 [HU-021] Modificar `DomainErrors.cs` — agregar clase parcial `DomainErrors.Well` con errores: `NotFound` ("Well.NotFound"), `NotFoundById(Guid)`, `InvalidStatus` ("Well.InvalidStatus", "Solo se pueden editar pozos en estado borrador."), `DeleteInvalidStatus` ("Well.DeleteInvalidStatus", "Solo se pueden eliminar pozos en estado borrador."), `CampoNotBelongsToContrato` ("Well.CampoNotBelongsToContrato"), `MunicipioNotBelongsToDepartamento` ("Well.MunicipioNotBelongsToDepartamento") — `backend/src/GOP.Domain/Errors/DomainErrors.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0.

---

## Bloque 2 — Application (dotnet build GOP.Application)

**Propósito:** Implementar los 10 casos de uso (5 wells + 5 catálogos) con handlers, validators, DTOs y mappings. Extender `IApplicationDbContext` con los DbSets necesarios.

### Interfaz DbContext

- [ ] T017 [BLOQUEANTE] Modificar `IApplicationDbContext.cs` — agregar `DbSet<Well> Wells`, `DbSet<Contrato> Contratos`, `DbSet<Campo> Campos`, `DbSet<Departamento> Departamentos`, `DbSet<Municipio> Municipios`, `DbSet<Cluster> Clusters` — `backend/src/GOP.Application/Common/Interfaces/IApplicationDbContext.cs`

### DTOs de Wells

- [ ] T018 [P] [HU-020] Crear `WellListItemDto` — sealed record: `Id` (Guid), `NombrePozo`, `Operadora`, `Contrato`, `Campo`, `Clasificacion`, `Estado` (strings), `CreatedAt` (DateTime) — `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/WellListItemDto.cs`
- [ ] T019 [P] [HU-022] Crear `WellDetailDto` — sealed record: todos los campos del pozo + ubicación aplanada (departamento, municipio, cluster con nombres y códigos DANE) + `CreatedAt`, `LastModifiedAt` — `backend/src/GOP.Application/Features/Wells/Queries/GetWellById/WellDetailDto.cs`

### DTOs de catálogos

- [ ] T020 [P] [HU-025] Crear `ContratoItemDto` — sealed record: `Id` (int), `Nombre`, `Tipo`, `Cuenca` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetContratos/ContratoItemDto.cs`
- [ ] T021 [P] [HU-025] Crear `CampoItemDto` — sealed record: `Id` (int), `Nombre`, `ContratoId` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetCampos/CampoItemDto.cs`
- [ ] T022 [P] [HU-025] Crear `DepartamentoItemDto` — sealed record: `Id` (int), `Nombre`, `CodigoDane` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetDepartamentos/DepartamentoItemDto.cs`
- [ ] T023 [P] [HU-025] Crear `MunicipioItemDto` — sealed record: `Id` (int), `Nombre`, `DepartamentoId`, `CodigoDane` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetMunicipios/MunicipioItemDto.cs`
- [ ] T024 [P] [HU-025] Crear `ClusterItemDto` — sealed record: `Id` (int), `Nombre`, `CampoId` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetClusters/ClusterItemDto.cs`

### Mapping

- [ ] T025 [HU-020] Crear `WellMappingProfile` — AutoMapper profile: `Well` → `WellListItemDto` (flatten Contrato.Nombre, Campo.Nombre, enum→string), `Well` → `WellDetailDto` (flatten ubicación + catálogos) — `backend/src/GOP.Application/Features/Wells/Mappings/WellMappingProfile.cs`

### Commands de Wells

- [ ] T026 [HU-021] Crear `CreateWellCommand` — record: `ContratoId`, `CampoId`, `TipoTrayectoria`, `Clasificacion`, `Denominacion`, `Consecutivo`, `TipoUbicacion`, `TipoAngulo`, `TipoObjetivo`, `TipoTerminacion`, `DepartamentoId`, `MunicipioId`, `ClusterId?`; implementa `IRequest<Result<WellDetailDto>>` — `backend/src/GOP.Application/Features/Wells/Commands/CreateWell/CreateWellCommand.cs`
- [ ] T027 [HU-021] Crear `CreateWellCommandValidator` — reglas: todos los campos required NotEmpty, `Denominacion` MaxLength(50) + Matches solo letras, `Consecutivo` Matches `^\d{2}$`, enums válidos — `backend/src/GOP.Application/Features/Wells/Commands/CreateWell/CreateWellCommandValidator.cs`
- [ ] T028 [BLOQUEANTE] [HU-021] Crear `CreateWellCommandHandler` — flujo: buscar Contrato → validar Campo pertenece al Contrato → buscar Departamento → validar Municipio pertenece al Departamento → buscar Cluster (opcional) → calcular NombrePozo → setear Operadora y TenantId de ICurrentUserService → `Well.Create(...)` → persistir → map a WellDetailDto — `backend/src/GOP.Application/Features/Wells/Commands/CreateWell/CreateWellCommandHandler.cs`
- [ ] T029 [HU-023] Crear `UpdateWellCommand` — record: `WellId` (Guid, del path), mismos campos editables que Create; implementa `IRequest<Result<WellDetailDto>>` — `backend/src/GOP.Application/Features/Wells/Commands/UpdateWell/UpdateWellCommand.cs`
- [ ] T030 [HU-023] Crear `UpdateWellCommandValidator` — mismas reglas que CreateWellCommandValidator — `backend/src/GOP.Application/Features/Wells/Commands/UpdateWell/UpdateWellCommandValidator.cs`
- [ ] T031 [HU-023] Crear `UpdateWellCommandHandler` — flujo: buscar Well por Id → verificar `Estado == Borrador` (si no → InvalidStatus) → mismas validaciones de FK que Create → `well.Update(...)` → persistir → map a WellDetailDto — `backend/src/GOP.Application/Features/Wells/Commands/UpdateWell/UpdateWellCommandHandler.cs`
- [ ] T032 [HU-024] Crear `DeleteWellCommand` — record: `WellId` (Guid); implementa `IRequest<Result>` — `backend/src/GOP.Application/Features/Wells/Commands/DeleteWell/DeleteWellCommand.cs`
- [ ] T033 [HU-024] Crear `DeleteWellCommandHandler` — flujo: buscar Well por Id → verificar `Estado == Borrador` (si no → DeleteInvalidStatus) → setear `IsDeleted = true` → persistir — `backend/src/GOP.Application/Features/Wells/Commands/DeleteWell/DeleteWellCommandHandler.cs`

### Queries de Wells

- [ ] T034 [HU-022] Crear `GetWellByIdQuery` — record: `WellId` (Guid); implementa `IRequest<Result<WellDetailDto>>` — `backend/src/GOP.Application/Features/Wells/Queries/GetWellById/GetWellByIdQuery.cs`
- [ ] T035 [HU-022] Crear `GetWellByIdQueryHandler` — `AsNoTracking`, Include Contrato+Campo+Location relations, map con AutoMapper `ProjectTo<WellDetailDto>()` o manual; multi-tenant (ADMIN/AUDITOR → IgnoreQueryFilters) — `backend/src/GOP.Application/Features/Wells/Queries/GetWellById/GetWellByIdQueryHandler.cs`
- [ ] T036 [HU-020] Crear `GetWellsListQuery` — record: `Page`, `PageSize`, `Search?`, `SortBy?`, `SortDir?`, `ContratoId?`, `Estado?`; implementa `IRequest<Result<PagedList<WellListItemDto>>>` (usar PagedList del scaffold o crear) — `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQuery.cs`
- [ ] T037 [HU-020] Crear `GetWellsListQueryHandler` — `AsNoTracking`, multi-tenant filter, filtros opcionales (contratoId, estado, search por NombrePozo/Denominacion), sort con whitelist (`nombrePozo`, `operadora`, `contrato`, `createdAt`), paginar Skip/Take, map a `PagedList<WellListItemDto>` — `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQueryHandler.cs`

### Queries de catálogos

- [ ] T038 [P] [HU-025] Crear `GetContratosQuery` + `GetContratosQueryHandler` — record vacío; handler: `AsNoTracking`, retorna lista de `ContratoItemDto` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetContratos/GetContratosQuery.cs` + `GetContratosQueryHandler.cs`
- [ ] T039 [P] [HU-025] Crear `GetCamposQuery` + `GetCamposQueryHandler` — record: `ContratoId` (int); handler: filtra por ContratoId, retorna `CampoItemDto[]` — `backend/src/GOP.Application/Features/Catalogs/Queries/GetCampos/GetCamposQuery.cs` + `GetCamposQueryHandler.cs`
- [ ] T040 [P] [HU-025] Crear `GetDepartamentosQuery` + `GetDepartamentosQueryHandler` — record vacío; handler: retorna todos — `backend/src/GOP.Application/Features/Catalogs/Queries/GetDepartamentos/GetDepartamentosQuery.cs` + `GetDepartamentosQueryHandler.cs`
- [ ] T041 [P] [HU-025] Crear `GetMunicipiosQuery` + `GetMunicipiosQueryHandler` — record: `DepartamentoId` (int); handler: filtra — `backend/src/GOP.Application/Features/Catalogs/Queries/GetMunicipios/GetMunicipiosQuery.cs` + `GetMunicipiosQueryHandler.cs`
- [ ] T042 [P] [HU-025] Crear `GetClustersQuery` + `GetClustersQueryHandler` — record: `CampoId` (int); handler: filtra — `backend/src/GOP.Application/Features/Catalogs/Queries/GetClusters/GetClustersQuery.cs` + `GetClustersQueryHandler.cs`

> **Nota:** T038–T042 son 2 archivos por tarea (query + handler). Son lo suficientemente simples para mantenerse como una tarea atómica (query record + handler trivial read-only).

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0.

---

## Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)

**Propósito:** Configurar EF Core con las 7 entidades, seed data de catálogos, interceptor de auditoría, actualizar DbContext y DI, generar primera migración real.

### Configuraciones EF Core

- [ ] T043 [P] Crear `ContratoConfiguration` — tabla `"Contratos"`, PK int, `Nombre` required maxlen 200, `Tipo` required maxlen 50, `Cuenca` required maxlen 200; seed data: 3 contratos de spec.md §5 — `backend/src/GOP.Infrastructure/Persistence/Configurations/ContratoConfiguration.cs`
- [ ] T044 [P] Crear `DepartamentoConfiguration` — tabla `"Departamentos"`, PK int, `Nombre` required maxlen 100, `CodigoDane` required maxlen 10; seed data: 3 departamentos — `backend/src/GOP.Infrastructure/Persistence/Configurations/DepartamentoConfiguration.cs`
- [ ] T045 Crear `CampoConfiguration` — tabla `"Campos"`, PK int, FK a Contrato (Restrict), `Nombre` required maxlen 200; seed data: 6 campos — `backend/src/GOP.Infrastructure/Persistence/Configurations/CampoConfiguration.cs`
- [ ] T046 Crear `MunicipioConfiguration` — tabla `"Municipios"`, PK int, FK a Departamento (Restrict), `Nombre` required maxlen 200, `CodigoDane` required maxlen 10; seed data: 6 municipios — `backend/src/GOP.Infrastructure/Persistence/Configurations/MunicipioConfiguration.cs`
- [ ] T047 Crear `ClusterConfiguration` — tabla `"Clusters"`, PK int, FK a Campo (Restrict), `Nombre` required maxlen 200; seed data: 4 clusters — `backend/src/GOP.Infrastructure/Persistence/Configurations/ClusterConfiguration.cs`
- [ ] T048 [BLOQUEANTE] Crear `WellConfiguration` — tabla `"Wells"`, PK Guid, `Operadora` maxlen 200, `NombrePozo` maxlen 300, `Denominacion` maxlen 50, `Consecutivo` maxlen 2, enums como string con `HasConversion<string>()`, FKs a Contrato/Campo con Restrict, `OwnsOne(w => w.Location)` con FK a Departamento/Municipio/Cluster, `HasQueryFilter(w => !w.IsDeleted)`, Index en `TenantId` — `backend/src/GOP.Infrastructure/Persistence/Configurations/WellConfiguration.cs`

### Interceptor de auditoría

- [ ] T049 Crear `AuditableEntityInterceptor` — `SaveChangesInterceptor`; en `SavingChangesAsync`: iterar `ChangeTracker.Entries<AuditableEntity>()`; si `Added` → setear `CreatedAt = DateTime.UtcNow`, `CreatedBy = currentUser.Email`; si `Modified` → setear `LastModifiedAt`, `LastModifiedBy`; si prop `IsDeleted` cambió a `true` → setear `DeletedAt` — `backend/src/GOP.Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs`

### DbContext y DI

- [ ] T050 Modificar `GopDbContext.cs` — agregar DbSets: `DbSet<Well> Wells`, `DbSet<Contrato> Contratos`, `DbSet<Campo> Campos`, `DbSet<Departamento> Departamentos`, `DbSet<Municipio> Municipios`, `DbSet<Cluster> Clusters` — `backend/src/GOP.Infrastructure/Persistence/GopDbContext.cs`
- [ ] T051 Modificar `DependencyInjection.cs` — registrar `AuditableEntityInterceptor` como Scoped; agregar el interceptor al `AddDbContext<GopDbContext>` via `options.AddInterceptors(...)` — `backend/src/GOP.Infrastructure/DependencyInjection.cs`

### Migración

- [ ] T052 Generar migración `InitialWellsAndCatalogs` — ejecutar `dotnet ef migrations add InitialWellsAndCatalogs -p src/GOP.Infrastructure -s src/GOP.API`; verificar que la migración incluye: tablas Wells, Contratos, Campos, Departamentos, Municipios, Clusters con seed data — `backend/src/GOP.Infrastructure/Persistence/Migrations/` (generado)

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0. `dotnet ef migrations list -p src/GOP.Infrastructure -s src/GOP.API` muestra la migración.

---

## Bloque 4 — API (dotnet build GOP.API)

**Propósito:** Crear los 2 controllers que implementan los 10 endpoints del contrato.

- [ ] T053 [HU-020] [HU-021] [HU-022] [HU-023] [HU-024] Crear `WellsController` — `[ApiController]`, `[Route("api/v1/wells")]`, `[Authorize]`; inyecta `ISender`; 5 actions: `[HttpGet]` listWells → Send(GetWellsListQuery), `[HttpGet("{id:guid}")]` getWell → Send(GetWellByIdQuery), `[HttpPost]` `[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]` createWell → Send(CreateWellCommand) → ToCreatedResult, `[HttpPut("{id:guid}")]` `[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]` updateWell → Send(UpdateWellCommand), `[HttpDelete("{id:guid}")]` `[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]` deleteWell → Send(DeleteWellCommand); cada action max 3 líneas; `[ProducesResponseType]` para Swagger — `backend/src/GOP.API/Controllers/WellsController.cs`
- [ ] T054 [HU-025] Crear `CatalogsController` — `[ApiController]`, `[Route("api/v1/catalogs")]`, `[Authorize]`; inyecta `ISender`; 5 actions: `[HttpGet("contratos")]` → Send(GetContratosQuery), `[HttpGet("campos")]` con `[FromQuery] int contratoId` → Send(GetCamposQuery), `[HttpGet("departamentos")]` → Send(GetDepartamentosQuery), `[HttpGet("municipios")]` con `[FromQuery] int departamentoId` → Send(GetMunicipiosQuery), `[HttpGet("clusters")]` con `[FromQuery] int campoId` → Send(GetClustersQuery) — `backend/src/GOP.API/Controllers/CatalogsController.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → exit code 0. Con Docker SQL Server: `dotnet ef database update -p src/GOP.Infrastructure -s src/GOP.API` aplica migración. `GET /api/v1/catalogs/contratos` retorna 3 contratos seed.

---

## Bloque 5 — Tests (dotnet test GOP.sln)

**Propósito:** Tests unitarios de handlers y validators, tests de integración de los endpoints.

- [ ] T055 [P] [HU-021] Crear `CreateWellCommandValidatorTests` — 4 tests: `Validate_EmptyDenominacion_ReturnsError`, `Validate_DenominacionWithNumbers_ReturnsError` (pattern), `Validate_InvalidConsecutivo_ReturnsError` (no 2 dígitos), `Validate_ValidInput_PassesValidation` — `backend/tests/GOP.Application.Tests/Features/Wells/CreateWellCommandValidatorTests.cs`
- [ ] T056 [HU-021] Crear `CreateWellCommandHandlerTests` — 4 tests: `Handle_ValidCommand_ReturnsWellDetail` (mock IApplicationDbContext con catálogos, ICurrentUserService con tenant), `Handle_CampoNotBelongsToContrato_ReturnsFailure`, `Handle_MunicipioNotBelongsToDepartamento_ReturnsFailure`, `Handle_ValidCommand_CalculatesNombrePozoCorrectly` (verifica "{Cuenca}-{Denom}-{Consec}") — `backend/tests/GOP.Application.Tests/Features/Wells/CreateWellCommandHandlerTests.cs`
- [ ] T057 [P] [HU-020] Crear `GetWellsListQueryHandlerTests` — 3 tests: `Handle_ReturnsPagedList`, `Handle_FilterByContratoId_ReturnsFiltered`, `Handle_SearchByNombrePozo_ReturnsMatching` — `backend/tests/GOP.Application.Tests/Features/Wells/GetWellsListQueryHandlerTests.cs`
- [ ] T058 [P] [HU-025] Crear `GetContratosQueryHandlerTests` — 2 tests: `Handle_ReturnsAllContratos`, `Handle_ReturnsCorrectDtoShape` — `backend/tests/GOP.Application.Tests/Features/Catalogs/GetContratosQueryHandlerTests.cs`
- [ ] T059 [HU-020] [HU-021] [HU-023] [HU-024] Crear `WellsControllerTests` — 5 tests de integración con `WebApplicationFactory`: `ListWells_ReturnsPagedResponse`, `CreateWell_ValidRequest_Returns201`, `CreateWell_Auditor_Returns403`, `UpdateWell_NotBorrador_Returns422`, `DeleteWell_ValidBorrador_Returns204` — `backend/tests/GOP.API.Tests/Controllers/WellsControllerTests.cs`
- [ ] T060 [HU-025] Crear `CatalogsControllerTests` — 3 tests de integración: `ListContratos_ReturnsSeededData`, `ListCampos_FilterByContratoId_ReturnsFiltered`, `ListMunicipios_FilterByDepartamentoId_ReturnsFiltered` — `backend/tests/GOP.API.Tests/Controllers/CatalogsControllerTests.cs`

**Checkpoint:** `cd backend && dotnet test GOP.sln` → todos los tests pasan (32 existentes + 21 nuevos = 53 tests), exit code 0.

---

## Dependencies & Execution Order

### Diagrama

```
B1 (Domain) → B2 (Application) → B3 (Infrastructure) → B4 (API) → B5 (Tests)
```

### Dependencias Dentro de Bloques

```
Bloque 1:
  T001–T007 paralelos (enums independientes)
  T008 paralelo con enums (CatalogEntity base)
  T009, T010 dependen de T008 (Contrato, Departamento heredan CatalogEntity)
  T011 depende de T009 (Campo FK a Contrato)
  T012 depende de T010 (Municipio FK a Departamento)
  T013 depende de T011 (Cluster FK a Campo)
  T014 independiente (WellLocation no hereda nada)
  T015 depende de T001–T007 + T014 (Well usa todos los enums + WellLocation)
  T016 independiente (errores no dependen de entidades en compilación, solo de Error.cs)

Bloque 2:
  T017 BLOQUEANTE (DbSets necesarios para handlers)
  T018–T024 paralelos (DTOs independientes)
  T025 depende de T018, T019 (mapping profile mapea a los DTOs)
  T026 depende de T019 (command retorna WellDetailDto)
  T027 depende de T026 (validator valida el command)
  T028 depende de T017, T025, T026, T027 (handler usa DbContext, mapper, command)
  T029 depende de T019 (command retorna WellDetailDto)
  T030 depende de T029
  T031 depende de T017, T025, T029, T030
  T032 independiente (command solo tiene WellId)
  T033 depende de T017, T032
  T034, T036 dependen de T018/T019 (queries retornan DTOs)
  T035 depende de T017, T019, T025, T034
  T037 depende de T017, T018, T025, T036
  T038–T042 dependen de T017 y sus DTOs respectivos (T020–T024)

Bloque 3:
  T043, T044 paralelos (configs de catálogos raíz)
  T045 depende de T043 (Campo FK a Contrato)
  T046 depende de T044 (Municipio FK a Departamento)
  T047 depende de T045 (Cluster FK a Campo)
  T048 depende de T043–T047 (WellConfig FK a catálogos)
  T049 independiente (interceptor no depende de configs)
  T050 depende de T048 (DbContext referencia entidades configuradas)
  T051 depende de T049, T050 (DI registra interceptor + DbContext)
  T052 depende de T050, T051 (migración necesita DbContext completo)

Bloque 4:
  T053, T054 paralelos (controllers independientes)

Bloque 5:
  T055, T057, T058 paralelos (tests unitarios independientes)
  T056 depende de T055 (handler tests después de validator tests por orden lógico)
  T059 depende de T056 (integration tests después de unit tests)
  T060 paralelo con T059
```

---

## Implementation Blocks (Secuencia de Ejecución)

### Bloque 1 — Domain

```
T001–T007             # paralelas: 7 enums
T008                  # CatalogEntity base
T009, T010            # paralelas: Contrato, Departamento
T011, T012            # paralelas: Campo, Municipio (dependen de padres)
T013, T014            # paralelas: Cluster, WellLocation
T015                  # Well (depende de enums + WellLocation)
T016                  # DomainErrors.Well (parcial)
```
**Validación:** `cd backend && dotnet build src/GOP.Domain` ✅

### Bloque 2 — Application

```
T017                  # IApplicationDbContext + DbSets (BLOQUEANTE)
T018–T024             # paralelas: 7 DTOs (2 wells + 5 catálogos)
T025                  # WellMappingProfile
T026, T029, T032, T034, T036  # paralelas: 5 Command/Query records
T027, T030            # paralelas: 2 validators
T038–T042             # paralelas: 5 catalog query+handler pairs
T028, T031, T033, T035, T037  # paralelas: 5 command/query handlers
```
**Validación:** `cd backend && dotnet build src/GOP.Application` ✅

### Bloque 3 — Infrastructure

```
T043, T044            # paralelas: ContratoConfig, DepartamentoConfig (con seed)
T045, T046            # paralelas: CampoConfig, MunicipioConfig (con seed)
T047                  # ClusterConfig (con seed)
T048                  # WellConfiguration
T049                  # AuditableEntityInterceptor
T050                  # GopDbContext + DbSets
T051                  # DependencyInjection + interceptor
T052                  # Generar migración InitialWellsAndCatalogs
```
**Validación:** `cd backend && dotnet build src/GOP.Infrastructure` ✅

### Bloque 4 — API

```
T053, T054            # paralelas: WellsController, CatalogsController
```
**Validación:** `cd backend && dotnet build src/GOP.API` ✅

### Bloque 5 — Tests

```
T055, T057, T058      # paralelas: validator tests, list handler tests, catalog handler tests
T056                  # CreateWellCommandHandlerTests
T059, T060            # paralelas: WellsControllerTests, CatalogsControllerTests
```
**Validación:** `cd backend && dotnet test GOP.sln` ✅ — 53 tests pass.

---

## Resumen

| Bloque | Propósito | Tareas | Archivos nuevos | Archivos modif. | Verificación |
|---|---|---|---|---|---|
| B1 — Domain | Entidades + enums + errores | T001–T016 (16) | 15 | 1 | `dotnet build src/GOP.Domain` |
| B2 — Application | Handlers + DTOs + mappings | T017–T042 (26) | 25 | 1 | `dotnet build src/GOP.Application` |
| B3 — Infrastructure | EF configs + seed + migración | T043–T052 (10) | 8 + migración | 2 | `dotnet build src/GOP.Infrastructure` |
| B4 — API | 2 Controllers (10 endpoints) | T053–T054 (2) | 2 | 0 | `dotnet build src/GOP.API` |
| B5 — Tests | Unit + integration | T055–T060 (6) | 6 | 0 | `dotnet test GOP.sln` (53 tests) |
| **Total** | | **60 tareas** | **56 nuevos + migración** | **4 modif.** | |
