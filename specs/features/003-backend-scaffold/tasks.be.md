# Tasks Backend: Scaffolding Base .NET 10 (003-backend-scaffold)

**Input:** `specs/features/003-backend-scaffold/spec.md` + `specs/features/003-backend-scaffold/plan.be.md`
**Contrato:** `specs/features/003-backend-scaffold/contract.yml`
**Constitución:** `CONSTITUTION.backend.md`

**Formato:** `[ID] [P?] [HT?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — sin dependencias de tareas incompletas en el mismo bloque
- **[HT-N]**: Historia técnica de referencia en spec.md
- **[BLOQUEANTE]**: Tareas posteriores no pueden iniciar sin esta completada

---

## Bloque 0 — Solución y Proyectos

**Propósito:** Crear la solución .NET, los 8 proyectos (.csproj), configuración compartida, references entre proyectos y paquetes NuGet. Al terminar, `dotnet build GOP.sln` compila vacío sin errores.

**⚠️ BLOQUEANTE:** Ningún bloque posterior puede iniciar sin que este complete.

- [ ] T001 [P] [HT-001] Crear `Directory.Build.props` — propiedades compartidas: `net10.0`, `Nullable`, `ImplicitUsings` — `backend/Directory.Build.props`
- [ ] T002 [P] [HT-001] Crear `.editorconfig` — convenciones de estilo C# (indentación, naming, severity) — `backend/.editorconfig`
- [ ] T003 [HT-001] Crear proyecto `GOP.Domain.csproj` — classlib sin paquetes NuGet, solo .NET BCL — `backend/src/GOP.Domain/GOP.Domain.csproj`
- [ ] T004 [HT-001] Crear proyecto `GOP.Application.csproj` — classlib, ref a `GOP.Domain`, paquetes: MediatR 12.x, FluentValidation 11.x, FluentValidation.DI, AutoMapper 13.x, AutoMapper.DI, Microsoft.Extensions.Logging.Abstractions — `backend/src/GOP.Application/GOP.Application.csproj`
- [ ] T005 [HT-001] Crear proyecto `GOP.Infrastructure.csproj` — classlib, ref a `GOP.Application`, paquetes: EF Core 10.x, EF Core SqlServer, EF Core Tools, Serilog.AspNetCore, Serilog.Sinks.File — `backend/src/GOP.Infrastructure/GOP.Infrastructure.csproj`
- [ ] T006 [HT-001] Crear proyecto `GOP.API.csproj` — web, refs a `GOP.Application` + `GOP.Infrastructure`, paquetes: NSwag.AspNetCore 14.x, AspNetCore.HealthChecks.SqlServer, Serilog.AspNetCore — `backend/src/GOP.API/GOP.API.csproj`
- [ ] T007 [P] [HT-001] Crear proyecto `GOP.Domain.Tests.csproj` — xunit, ref a `GOP.Domain`, paquetes: xunit, FluentAssertions, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.Domain.Tests/GOP.Domain.Tests.csproj`
- [ ] T008 [P] [HT-001] Crear proyecto `GOP.Application.Tests.csproj` — xunit, ref a `GOP.Application`, paquetes: xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.Application.Tests/GOP.Application.Tests.csproj`
- [ ] T009 [P] [HT-001] Crear proyecto `GOP.Infrastructure.Tests.csproj` — xunit, ref a `GOP.Infrastructure`, paquetes: xunit, FluentAssertions, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.Infrastructure.Tests/GOP.Infrastructure.Tests.csproj`
- [ ] T010 [P] [HT-001] Crear proyecto `GOP.API.Tests.csproj` — xunit, ref a `GOP.API`, paquetes: xunit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.API.Tests/GOP.API.Tests.csproj`
- [ ] T011 [HT-001] Crear `GOP.sln` — agregar los 8 proyectos a la solución con carpetas lógicas `src` y `tests` — `backend/GOP.sln`

**Checkpoint:** `cd backend && dotnet build GOP.sln` → exit code 0. Los 8 proyectos reportan "Build succeeded" (vacíos).

---

## Bloque 1 — Domain

**Propósito:** Implementar las clases base del dominio: `Entity`, `AuditableEntity`, `Result<T>`, `Error`, interfaces `IUnitOfWork` e `ICurrentUserService`. Sin dependencias externas.

- [ ] T012 [P] [HT-008] Crear clase abstracta `Entity` — `Id` Guid auto-generado con `protected init` — `backend/src/GOP.Domain/Common/Entity.cs`
- [ ] T013 [P] [HT-008] Crear value object `Error` — record `Error(Code, Message)` + `Error.None` + `Error.NullValue` — `backend/src/GOP.Domain/Common/Error.cs`
- [ ] T014 [HT-008] Crear clases `Result` y `Result<T>` — patrón sin excepciones según CONSTITUTION.backend.md §6.2: `Success()`, `Failure(error)`, `Success<T>(value)`, `Failure<T>(error)` — `backend/src/GOP.Domain/Common/Result.cs`
- [ ] T015 [HT-008] Crear clase abstracta `AuditableEntity` — hereda `Entity`, agrega `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, `IsDeleted`, `DeletedAt` — `backend/src/GOP.Domain/Common/AuditableEntity.cs`
- [ ] T016 [P] [HT-001] Crear interfaz `IUnitOfWork` — `SaveChangesAsync(CancellationToken)` según CONSTITUTION.backend.md §7.6 — `backend/src/GOP.Domain/Interfaces/IUnitOfWork.cs`
- [ ] T017 [P] [HT-004] Crear interfaz `ICurrentUserService` — `UserId`, `Email`, `Name`, `Role`, `TenantId`, `IsAuthenticated`, `IsInRole(string)` según CONSTITUTION.backend.md §8.3 — `backend/src/GOP.Domain/Interfaces/Services/ICurrentUserService.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0.

---

## Bloque 2 — Application

**Propósito:** Configurar el pipeline MediatR con behaviors cross-cutting, la interfaz del DbContext y el extension method de DI. Sin handlers de negocio.

- [ ] T018 [P] [HT-004] Crear `AssemblyMarker` — clase vacía `internal` para assembly scanning de MediatR, FluentValidation y AutoMapper — `backend/src/GOP.Application/AssemblyMarker.cs`
- [ ] T019 [P] [HT-001] Crear interfaz `IApplicationDbContext` — interfaz marcadora vacía (sin DbSets aún); se extiende en features de negocio — `backend/src/GOP.Application/Common/Interfaces/IApplicationDbContext.cs`
- [ ] T020 [HT-004] Crear `LoggingBehavior<TRequest, TResponse>` — `IPipelineBehavior` que logguea inicio ("Handling {RequestName} by User {UserId} Tenant {TenantId}") y fin ("Handled {RequestName} in {ElapsedMs}ms") con Stopwatch y `ILogger` + `ICurrentUserService` — `backend/src/GOP.Application/Common/Behaviors/LoggingBehavior.cs`
- [ ] T021 [HT-004] Crear `ValidationBehavior<TRequest, TResponse>` — `IPipelineBehavior` con constraint `where TResponse : Result`; ejecuta `IValidator<TRequest>` registrados, cortocircuita con `Result.Failure` si hay errores, pasa al next si no hay validators — `backend/src/GOP.Application/Common/Behaviors/ValidationBehavior.cs`
- [ ] T022 [HT-004] Crear `DependencyInjection.cs` — extension method `AddApplication(this IServiceCollection)`: registra MediatR, FluentValidation, AutoMapper por assembly scanning + `LoggingBehavior` y `ValidationBehavior` como `IPipelineBehavior` — `backend/src/GOP.Application/DependencyInjection.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0.

---

## Bloque 3 — Infrastructure

**Propósito:** Implementar `GopDbContext` vacío conectado a SQL Server, stub de `ICurrentUserService` y extension method de DI para infraestructura.

- [ ] T023 [HT-002] Crear `GopDbContext` — hereda `DbContext`, implementa `IApplicationDbContext` + `IUnitOfWork`; sin DbSets; `OnModelCreating` con `ApplyConfigurationsFromAssembly` (vacío por ahora) — `backend/src/GOP.Infrastructure/Persistence/GopDbContext.cs`
- [ ] T024 [HT-004] Crear `CurrentUserServiceStub` — implementa `ICurrentUserService` con valores dummy: `UserId = Guid.Empty`, `Email = "system"`, `Name = "System"`, `Role = "SYSTEM"`, `TenantId = 0`, `IsAuthenticated = false`, `IsInRole() = false` — `backend/src/GOP.Infrastructure/Services/CurrentUserServiceStub.cs`
- [ ] T025 [HT-001] Crear `DependencyInjection.cs` — extension method `AddInfrastructure(this IServiceCollection, IConfiguration)`: registra `GopDbContext` con connection string `"DefaultConnection"`, `IApplicationDbContext` → `GopDbContext` (Scoped), `IUnitOfWork` → `GopDbContext` (Scoped), `ICurrentUserService` → `CurrentUserServiceStub` (Scoped) — `backend/src/GOP.Infrastructure/DependencyInjection.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0.

---

## Bloque 4 — API

**Propósito:** Implementar el composition root (`Program.cs`), middleware de errores, health check controller, configuración de Serilog/Swagger/CORS y archivos de appsettings. Al terminar, la API puede arrancar y responder en `/api/v1/health`.

- [ ] T026 [P] [HT-005] Crear `appsettings.json` — configuración base: connection string placeholder, Serilog (Console + File sinks, overrides para Microsoft/EF/System a Warning), PaginationDefaults (20/100) — `backend/src/GOP.API/appsettings.json`
- [ ] T027 [P] [HT-007] Crear `appsettings.Development.json` — connection string al Docker Compose SQL Server: `Server=localhost,1433;Database=GOP360;User Id=sa;Password=Gop360_Dev!;TrustServerCertificate=true;` — `backend/src/GOP.API/appsettings.Development.json`
- [ ] T028 [P] [HT-006] Crear `launchSettings.json` — perfiles `http` (port 5000) y `https` (port 5001), `ASPNETCORE_ENVIRONMENT=Development` — `backend/src/GOP.API/Properties/launchSettings.json`
- [ ] T029 [HT-003] Crear `GlobalExceptionHandlerMiddleware` — try/catch global que captura `Exception`; logguea con Serilog (`LogError`); retorna ProblemDetails 500 con content-type `application/problem+json`; incluye stack trace solo en Development — `backend/src/GOP.API/Middleware/GlobalExceptionHandlerMiddleware.cs`
- [ ] T030 [HT-003] Crear `ResultExtensions` — métodos `ToActionResult()` para `Result` y `Result<T>`, `ToCreatedResult<T>()` con route name; mapeo de error code patterns (`.NotFound` → 404, `.Duplicate` → 409, `.Unauthorized` → 403, default → 422) a ProblemDetails — `backend/src/GOP.API/Extensions/ResultExtensions.cs`
- [ ] T031 [HT-002] Crear `HealthController` — `[ApiController]` en `[Route("api/v1/health")]`; action `GET` que invoca los health checks registrados y retorna `HealthResponse` según contract.yml; sin autenticación requerida — `backend/src/GOP.API/Controllers/HealthController.cs`
- [ ] T032 [BLOQUEANTE] [HT-001] [HT-002] [HT-005] [HT-006] Crear `Program.cs` — composition root completo: Serilog bootstrap con try/catch, `AddApplication()`, `AddInfrastructure(config)`, `AddControllers()`, `AddOpenApiDocument()` (NSwag), `AddHealthChecks().AddSqlServer().AddDbContextCheck()`, `AddCors()` (DevPolicy: localhost:4200), middleware pipeline: `GlobalExceptionHandlerMiddleware` → `UseSerilogRequestLogging()` → `UseCors()` → `UseOpenApi()`/`UseSwaggerUi()` (solo Development) → `MapControllers()` → `Run()` — `backend/src/GOP.API/Program.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → exit code 0. Si Docker SQL Server está corriendo, `dotnet run --project src/GOP.API` levanta y `GET http://localhost:5000/api/v1/health` retorna 200 o 503.

---

## Bloque 5 — Docker Compose

**Propósito:** Configurar Docker Compose con SQL Server 2022 para desarrollo local. Puede ejecutarse en paralelo con los bloques 1-4.

- [ ] T033 [P] [HT-007] Crear `docker-compose.yml` — servicio `sqlserver` con imagen `mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD=Gop360_Dev!`, puerto `1433:1433`, volume `sqlserver-data` para persistencia — `backend/docker-compose.yml`
- [ ] T034 [P] [HT-007] Crear `docker-compose.override.yml` — override para desarrollo local (environment variables adicionales si se necesitan, restart policy) — `backend/docker-compose.override.yml`

**Checkpoint:** `cd backend && docker compose config` → sintaxis YAML válida. `docker compose up -d` → contenedor SQL Server accesible en `localhost:1433`.

---

## Bloque 6 — Tests

**Propósito:** Implementar los tests unitarios y de integración mínimos. Verifica que las clases base y el pipeline funcionan correctamente.

**Depende de:** Bloques 1-4 completos.

- [ ] T035 [P] [HT-008] Crear `ResultTests` — 4 tests: `Success_ReturnsIsSuccessTrue`, `Failure_ReturnsIsFailureTrueWithError`, `SuccessT_ReturnsValueCorrectly`, `FailureT_AccessingValueThrowsException` — `backend/tests/GOP.Domain.Tests/Common/ResultTests.cs`
- [ ] T036 [P] [HT-008] Crear `EntityTests` — 2 tests: `NewEntity_GeneratesNonEmptyGuid`, `TwoEntities_HaveDifferentIds` (usa clase concreta de test derivada de Entity) — `backend/tests/GOP.Domain.Tests/Common/EntityTests.cs`
- [ ] T037 [P] [HT-004] Crear `ValidationBehaviorTests` — 3 tests: `Handle_NoValidators_CallsNext`, `Handle_ValidRequest_CallsNext`, `Handle_InvalidRequest_ReturnsFailureWithoutCallingHandler` (usa NSubstitute para mockear `IValidator<T>` y `RequestHandlerDelegate`) — `backend/tests/GOP.Application.Tests/Common/ValidationBehaviorTests.cs`
- [ ] T038 [P] [HT-001] Crear `GopDbContextTests` — 1 test canary: `CanInstantiateDbContext` (usa `DbContextOptionsBuilder` con `UseInMemoryDatabase` para evitar dependencia de Docker) — `backend/tests/GOP.Infrastructure.Tests/Persistence/GopDbContextTests.cs`
- [ ] T039 [HT-002] Crear `HealthControllerTests` — 1 test de integración: `GetHealth_ReturnsOk` (usa `WebApplicationFactory<Program>` con SQL Server reemplazado por provider in-memory; valida status 200 y body JSON con campo `status`) — `backend/tests/GOP.API.Tests/Controllers/HealthControllerTests.cs`

**Checkpoint:** `cd backend && dotnet test GOP.sln` → 11 tests, all pass, exit code 0.

---

## Dependencies & Execution Order

### Dependencias entre Bloques

```
Bloque 0 (Solución)       → Sin dependencias. Primer paso obligatorio.
Bloque 1 (Domain)         → Depende de Bloque 0 (proyectos deben existir).
Bloque 2 (Application)    → Depende de Bloque 1 (Result.cs, interfaces Domain).
Bloque 3 (Infrastructure) → Depende de Bloque 2 (IApplicationDbContext, ICurrentUserService).
Bloque 4 (API)            → Depende de Bloque 2 + Bloque 3 (DI extensions).
Bloque 5 (Docker)         → Sin dependencias de código. Puede ejecutarse en paralelo con 1-4.
Bloque 6 (Tests)          → Depende de Bloques 1-4 completos (clases bajo test deben existir).
```

### Diagrama

```
B0 ──→ B1 ──→ B2 ──→ B3 ──→ B4 ──→ B6
                                ↑
B5 (paralelo) ─────────────────┘
```

### Dependencias Dentro de Bloques

```
Bloque 0:
  T001, T002 paralelos (sin deps)
  T003 → T004 (Application ref Domain) → T005 (Infrastructure ref Application) → T006 (API ref App+Infra)
  T007, T008, T009, T010 paralelos (dependen de su proyecto de producción correspondiente)
  T011 depende de T003–T010 (todos los proyectos deben existir para agregarse al .sln)

Bloque 1:
  T012, T013 paralelos (Entity y Error son independientes)
  T014 depende de T013 (Result usa Error)
  T015 depende de T012 (AuditableEntity hereda Entity)
  T016, T017 paralelos (interfaces independientes)

Bloque 2:
  T018, T019 paralelos (AssemblyMarker e interfaz son independientes)
  T020 depende de T017 (LoggingBehavior usa ICurrentUserService)
  T021 depende de T014 (ValidationBehavior tiene constraint on Result)
  T022 depende de T018, T020, T021 (DI registra todo lo anterior)

Bloque 3:
  T023 depende de T019 (GopDbContext implementa IApplicationDbContext)
  T024 depende de T017 (stub implementa ICurrentUserService)
  T025 depende de T023, T024 (DI registra ambos)

Bloque 4:
  T026, T027, T028 paralelos (archivos de configuración)
  T029 independiente (middleware no depende de otros archivos del bloque)
  T030 depende de T014 (ResultExtensions usa Result)
  T031 independiente (controller usa health checks de ASP.NET Core)
  T032 depende de T022, T025, T026, T029, T030, T031 (Program.cs compone todo)

Bloque 5:
  T033, T034 paralelos

Bloque 6:
  T035 depende de T014 (tests de Result)
  T036 depende de T012 (tests de Entity)
  T037 depende de T021 (tests de ValidationBehavior)
  T038 depende de T023 (tests de GopDbContext)
  T039 depende de T032 (tests de API necesitan Program.cs)
  T035, T036, T037, T038 paralelos entre sí
```

### Oportunidades de Paralelismo

**Bloque 0:** T001 y T002 en paralelo → T003 → T004 → T005 → T006 → T007/T008/T009/T010 en paralelo → T011.

**Bloque 1:** T012 y T013 en paralelo → T014 y T015 en paralelo → T016 y T017 en paralelo.

**Bloque 2:** T018 y T019 en paralelo → T020 y T021 en paralelo → T022.

**Bloque 3:** T023 y T024 en paralelo → T025.

**Bloque 4:** T026/T027/T028/T029 en paralelo → T030 y T031 en paralelo → T032.

**Bloque 5:** T033 y T034 en paralelo (ejecutable en cualquier momento).

**Bloque 6:** T035/T036/T037/T038 en paralelo → T039.

---

## Implementation Blocks (Secuencia de Ejecución)

### Bloque 0 — Solución y Proyectos

```
T001, T002           # paralelas: Directory.Build.props + .editorconfig
T003                  # GOP.Domain.csproj
T004                  # GOP.Application.csproj (ref Domain)
T005                  # GOP.Infrastructure.csproj (ref Application)
T006                  # GOP.API.csproj (ref Application + Infrastructure)
T007, T008, T009, T010  # paralelas: 4 proyectos de test
T011                  # GOP.sln (agrega los 8 proyectos)
```
**Validación:** `cd backend && dotnet build GOP.sln` ✅

### Bloque 1 — Domain

```
T012, T013            # paralelas: Entity.cs + Error.cs
T014, T015            # paralelas: Result.cs (dep Error) + AuditableEntity.cs (dep Entity)
T016, T017            # paralelas: IUnitOfWork.cs + ICurrentUserService.cs
```
**Validación:** `cd backend && dotnet build src/GOP.Domain` ✅

### Bloque 2 — Application

```
T018, T019            # paralelas: AssemblyMarker + IApplicationDbContext
T020, T021            # paralelas: LoggingBehavior + ValidationBehavior
T022                  # DependencyInjection.cs (registra todo)
```
**Validación:** `cd backend && dotnet build src/GOP.Application` ✅

### Bloque 3 — Infrastructure

```
T023, T024            # paralelas: GopDbContext + CurrentUserServiceStub
T025                  # DependencyInjection.cs (registra todo)
```
**Validación:** `cd backend && dotnet build src/GOP.Infrastructure` ✅

### Bloque 4 — API

```
T026, T027, T028, T029  # paralelas: appsettings.json, appsettings.Dev.json, launchSettings, Middleware
T030, T031              # paralelas: ResultExtensions + HealthController
T032                    # Program.cs (composition root — depende de todo lo anterior)
```
**Validación:** `cd backend && dotnet build src/GOP.API` ✅

### Bloque 5 — Docker

```
T033, T034            # paralelas: docker-compose.yml + docker-compose.override.yml
```
**Validación:** `cd backend && docker compose config` ✅

### Bloque 6 — Tests

```
T035, T036, T037, T038  # paralelas: ResultTests, EntityTests, ValidationBehaviorTests, GopDbContextTests
T039                    # HealthControllerTests (depende de Program.cs via WebApplicationFactory)
```
**Validación:** `cd backend && dotnet test GOP.sln` ✅ — 11 tests pass.

---

## Resumen

| Bloque | Propósito | Tareas | Archivos | Comando de verificación |
|---|---|---|---|---|
| B0 — Solución | Proyectos + NuGet + refs | T001–T011 (11) | 11 nuevos | `dotnet build GOP.sln` |
| B1 — Domain | Clases base + interfaces | T012–T017 (6) | 6 nuevos | `dotnet build src/GOP.Domain` |
| B2 — Application | Pipeline MediatR + DI | T018–T022 (5) | 5 nuevos | `dotnet build src/GOP.Application` |
| B3 — Infrastructure | DbContext + stubs + DI | T023–T025 (3) | 3 nuevos | `dotnet build src/GOP.Infrastructure` |
| B4 — API | Program.cs + middleware + health | T026–T032 (7) | 7 nuevos | `dotnet build src/GOP.API` |
| B5 — Docker | SQL Server dev local | T033–T034 (2) | 2 nuevos | `docker compose config` |
| B6 — Tests | Unitarios + integración | T035–T039 (5) | 5 nuevos | `dotnet test GOP.sln` |
| **Total** | | **39 tareas** | **39 archivos nuevos** | |
