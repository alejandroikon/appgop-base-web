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

**Propósito:** Crear la solución .NET, los 8 proyectos (.csproj), configuración compartida, references entre proyectos y paquetes NuGet. Al terminar, `dotnet build GOP.slnx` compila vacío sin errores.

**⚠️ BLOQUEANTE:** Ningún bloque posterior puede iniciar sin que este complete.

- [x] T001 [P] [HT-001] Crear `Directory.Build.props` — propiedades compartidas: `net10.0`, `Nullable`, `ImplicitUsings` — `backend/Directory.Build.props`
- [x] T002 [P] [HT-001] Crear `.editorconfig` — convenciones de estilo C# (indentación, naming, severity) — `backend/.editorconfig`
- [x] T003 [HT-001] Crear proyecto `GOP.Domain.csproj` — classlib sin paquetes NuGet, solo .NET BCL — `backend/src/GOP.Domain/GOP.Domain.csproj`
- [x] T004 [HT-001] Crear proyecto `GOP.Application.csproj` — classlib, ref a `GOP.Domain`, paquetes: MediatR 12.x, FluentValidation 11.x, FluentValidation.DI, AutoMapper 12.x, AutoMapper.DI, Microsoft.Extensions.Logging.Abstractions — `backend/src/GOP.Application/GOP.Application.csproj`
- [x] T005 [HT-001] Crear proyecto `GOP.Infrastructure.csproj` — classlib, ref a `GOP.Application`, paquetes: EF Core 10.x, EF Core SqlServer, EF Core Tools, Serilog.AspNetCore, Serilog.Sinks.File — `backend/src/GOP.Infrastructure/GOP.Infrastructure.csproj`
- [x] T006 [HT-001] Crear proyecto `GOP.API.csproj` — web, refs a `GOP.Application` + `GOP.Infrastructure`, paquetes: NSwag.AspNetCore 14.x, AspNetCore.HealthChecks.SqlServer, EF Core HealthChecks, Serilog.AspNetCore — `backend/src/GOP.API/GOP.API.csproj`
- [x] T007 [P] [HT-001] Crear proyecto `GOP.Domain.Tests.csproj` — xunit, ref a `GOP.Domain`, paquetes: xunit, FluentAssertions, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.Domain.Tests/GOP.Domain.Tests.csproj`
- [x] T008 [P] [HT-001] Crear proyecto `GOP.Application.Tests.csproj` — xunit, ref a `GOP.Application`, paquetes: xunit, FluentAssertions, NSubstitute, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.Application.Tests/GOP.Application.Tests.csproj`
- [x] T009 [P] [HT-001] Crear proyecto `GOP.Infrastructure.Tests.csproj` — xunit, ref a `GOP.Infrastructure`, paquetes: xunit, FluentAssertions, EF Core InMemory, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.Infrastructure.Tests/GOP.Infrastructure.Tests.csproj`
- [x] T010 [P] [HT-001] Crear proyecto `GOP.API.Tests.csproj` — xunit, ref a `GOP.API`, paquetes: xunit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, EF Core InMemory, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio — `backend/tests/GOP.API.Tests/GOP.API.Tests.csproj`
- [x] T011 [HT-001] Crear `GOP.slnx` — agregar los 8 proyectos a la solución con carpetas lógicas `src` y `tests` — `backend/GOP.slnx`

**Checkpoint:** `cd backend && dotnet build GOP.slnx` → exit code 0. ✅

---

## Bloque 1 — Domain

**Propósito:** Implementar las clases base del dominio: `Entity`, `AuditableEntity`, `Result<T>`, `Error`, interfaces `IUnitOfWork` e `ICurrentUserService`. Sin dependencias externas.

- [x] T012 [P] [HT-008] Crear clase abstracta `Entity` — `Id` Guid auto-generado con `protected init` — `backend/src/GOP.Domain/Common/Entity.cs`
- [x] T013 [P] [HT-008] Crear value object `Error` — record `Error(Code, Message)` + `Error.None` + `Error.NullValue` — `backend/src/GOP.Domain/Common/Error.cs`
- [x] T014 [HT-008] Crear clases `Result` y `Result<T>` — patrón sin excepciones según CONSTITUTION.backend.md §6.2: `Success()`, `Failure(error)`, `Success<T>(value)`, `Failure<T>(error)` — `backend/src/GOP.Domain/Common/Result.cs`
- [x] T015 [HT-008] Crear clase abstracta `AuditableEntity` — hereda `Entity`, agrega `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, `IsDeleted`, `DeletedAt` — `backend/src/GOP.Domain/Common/AuditableEntity.cs`
- [x] T016 [P] [HT-001] Crear interfaz `IUnitOfWork` — `SaveChangesAsync(CancellationToken)` según CONSTITUTION.backend.md §7.6 — `backend/src/GOP.Domain/Interfaces/IUnitOfWork.cs`
- [x] T017 [P] [HT-004] Crear interfaz `ICurrentUserService` — `UserId`, `Email`, `Name`, `Role`, `TenantId`, `IsAuthenticated`, `IsInRole(string)` según CONSTITUTION.backend.md §8.3 — `backend/src/GOP.Domain/Interfaces/Services/ICurrentUserService.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0. ✅

---

## Bloque 2 — Application

**Propósito:** Configurar el pipeline MediatR con behaviors cross-cutting, la interfaz del DbContext y el extension method de DI. Sin handlers de negocio.

- [x] T018 [P] [HT-004] Crear `AssemblyMarker` — clase vacía `internal` para assembly scanning de MediatR, FluentValidation y AutoMapper — `backend/src/GOP.Application/AssemblyMarker.cs`
- [x] T019 [P] [HT-001] Crear interfaz `IApplicationDbContext` — interfaz marcadora vacía (sin DbSets aún); se extiende en features de negocio — `backend/src/GOP.Application/Common/Interfaces/IApplicationDbContext.cs`
- [x] T020 [HT-004] Crear `LoggingBehavior<TRequest, TResponse>` — `IPipelineBehavior` que logguea inicio ("Handling {RequestName} by User {UserId} Tenant {TenantId}") y fin ("Handled {RequestName} in {ElapsedMs}ms") con Stopwatch y `ILogger` + `ICurrentUserService` — `backend/src/GOP.Application/Common/Behaviors/LoggingBehavior.cs`
- [x] T021 [HT-004] Crear `ValidationBehavior<TRequest, TResponse>` — `IPipelineBehavior` con constraint `where TResponse : Result`; ejecuta `IValidator<TRequest>` registrados, cortocircuita con `Result.Failure` si hay errores, pasa al next si no hay validators — `backend/src/GOP.Application/Common/Behaviors/ValidationBehavior.cs`
- [x] T022 [HT-004] Crear `DependencyInjection.cs` — extension method `AddApplication(this IServiceCollection)`: registra MediatR, FluentValidation, AutoMapper por assembly scanning + `LoggingBehavior` y `ValidationBehavior` como `IPipelineBehavior` — `backend/src/GOP.Application/DependencyInjection.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0. ✅

---

## Bloque 3 — Infrastructure

**Propósito:** Implementar `GopDbContext` vacío conectado a SQL Server, stub de `ICurrentUserService` y extension method de DI para infraestructura.

- [x] T023 [HT-002] Crear `GopDbContext` — hereda `DbContext`, implementa `IApplicationDbContext` + `IUnitOfWork`; sin DbSets; `OnModelCreating` con `ApplyConfigurationsFromAssembly` (vacío por ahora) — `backend/src/GOP.Infrastructure/Persistence/GopDbContext.cs`
- [x] T024 [HT-004] Crear `CurrentUserServiceStub` — implementa `ICurrentUserService` con valores dummy: `UserId = Guid.Empty`, `Email = "system"`, `Name = "System"`, `Role = "SYSTEM"`, `TenantId = 0`, `IsAuthenticated = false`, `IsInRole() = false` — `backend/src/GOP.Infrastructure/Services/CurrentUserServiceStub.cs`
- [x] T025 [HT-001] Crear `DependencyInjection.cs` — extension method `AddInfrastructure(this IServiceCollection, IConfiguration)`: registra `GopDbContext` con connection string `"DefaultConnection"`, `IApplicationDbContext` → `GopDbContext` (Scoped), `IUnitOfWork` → `GopDbContext` (Scoped), `ICurrentUserService` → `CurrentUserServiceStub` (Scoped) — `backend/src/GOP.Infrastructure/DependencyInjection.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0. ✅

---

## Bloque 4 — API

**Propósito:** Implementar el composition root (`Program.cs`), middleware de errores, health check controller, configuración de Serilog/Swagger/CORS y archivos de appsettings. Al terminar, la API puede arrancar y responder en `/api/v1/health`.

- [x] T026 [P] [HT-005] Crear `appsettings.json` — configuración base: connection string placeholder, Serilog (Console + File sinks, overrides para Microsoft/EF/System a Warning), PaginationDefaults (20/100) — `backend/src/GOP.API/appsettings.json`
- [x] T027 [P] [HT-007] Crear `appsettings.Development.json` — connection string al Docker Compose SQL Server: `Server=localhost,1433;Database=GOP360;User Id=sa;Password=Gop360_Dev!;TrustServerCertificate=true;` — `backend/src/GOP.API/appsettings.Development.json`
- [x] T028 [P] [HT-006] Crear `launchSettings.json` — perfiles `http` (port 5000) y `https` (port 5001), `ASPNETCORE_ENVIRONMENT=Development` — `backend/src/GOP.API/Properties/launchSettings.json`
- [x] T029 [HT-003] Crear `GlobalExceptionHandlerMiddleware` — try/catch global que captura `Exception`; logguea con Serilog (`LogError`); retorna ProblemDetails 500 con content-type `application/problem+json`; incluye stack trace solo en Development — `backend/src/GOP.API/Middleware/GlobalExceptionHandlerMiddleware.cs`
- [x] T030 [HT-003] Crear `ResultExtensions` — métodos `ToActionResult()` para `Result` y `Result<T>`, `ToCreatedResult<T>()` con route name; mapeo de error code patterns (`.NotFound` → 404, `.Duplicate` → 409, `.Unauthorized` → 403, default → 422) a ProblemDetails — `backend/src/GOP.API/Extensions/ResultExtensions.cs`
- [x] T031 [HT-002] Crear `HealthController` — `[ApiController]` en `[Route("api/v1/health")]`; action `GET` que invoca los health checks registrados y retorna `HealthResponse` según contract.yml; sin autenticación requerida — `backend/src/GOP.API/Controllers/HealthController.cs`
- [x] T032 [BLOQUEANTE] [HT-001] [HT-002] [HT-005] [HT-006] Crear `Program.cs` — composition root completo: Serilog bootstrap con try/catch, `AddApplication()`, `AddInfrastructure(config)`, `AddControllers()`, `AddOpenApiDocument()` (NSwag), `AddHealthChecks().AddSqlServer().AddDbContextCheck()`, `AddCors()` (DevPolicy: localhost:4200), middleware pipeline: `GlobalExceptionHandlerMiddleware` → `UseSerilogRequestLogging()` → `UseCors()` → `UseOpenApi()`/`UseSwaggerUi()` (solo Development) → `MapControllers()` → `Run()` — `backend/src/GOP.API/Program.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → exit code 0. ✅

---

## Bloque 5 — Docker Compose

**Propósito:** Configurar Docker Compose con SQL Server 2022 para desarrollo local. Puede ejecutarse en paralelo con los bloques 1-4.

- [x] T033 [P] [HT-007] Crear `docker-compose.yml` — servicio `sqlserver` con imagen `mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD=Gop360_Dev!`, puerto `1433:1433`, volume `sqlserver-data` para persistencia — `backend/docker-compose.yml`
- [x] T034 [P] [HT-007] Crear `docker-compose.override.yml` — override para desarrollo local (environment variables adicionales si se necesitan, restart policy) — `backend/docker-compose.override.yml`

**Checkpoint:** `cd backend && docker compose config` → sintaxis YAML válida. ✅

---

## Bloque 6 — Tests

**Propósito:** Implementar los tests unitarios y de integración mínimos. Verifica que las clases base y el pipeline funcionan correctamente.

**Depende de:** Bloques 1-4 completos.

- [x] T035 [P] [HT-008] Crear `ResultTests` — 4 tests: `Success_ReturnsIsSuccessTrue`, `Failure_ReturnsIsFailureTrueWithError`, `SuccessT_ReturnsValueCorrectly`, `FailureT_AccessingValueThrowsException` — `backend/tests/GOP.Domain.Tests/Common/ResultTests.cs`
- [x] T036 [P] [HT-008] Crear `EntityTests` — 2 tests: `NewEntity_GeneratesNonEmptyGuid`, `TwoEntities_HaveDifferentIds` (usa clase concreta de test derivada de Entity) — `backend/tests/GOP.Domain.Tests/Common/EntityTests.cs`
- [x] T037 [P] [HT-004] Crear `ValidationBehaviorTests` — 3 tests: `Handle_NoValidators_CallsNext`, `Handle_ValidRequest_CallsNext`, `Handle_InvalidRequest_ReturnsFailureWithoutCallingHandler` (usa NSubstitute para mockear `IValidator<T>` y `RequestHandlerDelegate`) — `backend/tests/GOP.Application.Tests/Common/ValidationBehaviorTests.cs`
- [x] T038 [P] [HT-001] Crear `GopDbContextTests` — 1 test canary: `CanInstantiateDbContext` (usa `DbContextOptionsBuilder` con `UseInMemoryDatabase` para evitar dependencia de Docker) — `backend/tests/GOP.Infrastructure.Tests/Persistence/GopDbContextTests.cs`
- [x] T039 [HT-002] Crear `HealthControllerTests` — 1 test de integración: `GetHealth_ReturnsOk` (usa `WebApplicationFactory<Program>` con SQL Server reemplazado por provider in-memory; valida status 200/503 y body JSON con campo `status`) — `backend/tests/GOP.API.Tests/Controllers/HealthControllerTests.cs`

**Checkpoint:** `cd backend && dotnet test GOP.slnx` → 11 tests, all pass, exit code 0. ✅

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

### Estado Final

| Bloque | Propósito | Tareas | Estado |
|---|---|---|---|
| B0 — Solución | Proyectos + NuGet + refs | T001–T011 (11) | ✅ COMPLETADO |
| B1 — Domain | Clases base + interfaces | T012–T017 (6) | ✅ COMPLETADO |
| B2 — Application | Pipeline MediatR + DI | T018–T022 (5) | ✅ COMPLETADO |
| B3 — Infrastructure | DbContext + stubs + DI | T023–T025 (3) | ✅ COMPLETADO |
| B4 — API | Program.cs + middleware + health | T026–T032 (7) | ✅ COMPLETADO |
| B5 — Docker | SQL Server dev local | T033–T034 (2) | ✅ COMPLETADO |
| B6 — Tests | Unitarios + integración | T035–T039 (5) | ✅ COMPLETADO |
| **Total** | | **39 tareas** | **✅ 11/11 tests pass** |
