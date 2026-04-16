# Plan Backend: Scaffolding Base .NET 10

**Feature ID:** 003-backend-scaffold
**Spec de referencia:** `specs/features/003-backend-scaffold/spec.md`
**Contrato de referencia:** `specs/features/003-backend-scaffold/contract.yml`
**Constitución de referencia:** `CONSTITUTION.backend.md`
**Estado:** Pendiente de revisión humana

---

## 1. Resumen Arquitectónico

Esta feature construye la carcasa compilable del backend GOP 360° desde cero. No contiene lógica de negocio. Al terminar, el resultado es una solución .NET 10 con:

- 4 proyectos de producción (`Domain`, `Application`, `Infrastructure`, `API`) con dependencias correctas
- 4 proyectos de test con un canary test cada uno
- Pipeline MediatR con behaviors cross-cutting
- DbContext vacío conectado a SQL Server vía Docker Compose
- Health check público en `/api/v1/health`
- Serilog configurado con sinks de consola y archivo
- Swagger UI habilitado solo en Development
- Middleware global de ProblemDetails RFC 7807

El flujo de dependencias sigue estrictamente CONSTITUTION.backend.md §3:

```
Domain (sin deps) ← Application ← Infrastructure ← API
```

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
backend/
├── GOP.sln                                              # Solución raíz con los 8 proyectos
├── docker-compose.yml                                   # SQL Server 2022 + volume persistente
├── docker-compose.override.yml                          # Override para desarrollo local (puertos, passwords)
├── .editorconfig                                        # Convenciones de estilo C# del equipo
├── Directory.Build.props                                # Propiedades compartidas: TargetFramework, Nullable, ImplicitUsings
│
├── src/
│   ├── GOP.Domain/
│   │   ├── GOP.Domain.csproj                            # Sin paquetes NuGet — solo .NET BCL
│   │   ├── Common/
│   │   │   ├── Entity.cs                                # Clase base: Id (Guid auto-generado)
│   │   │   ├── AuditableEntity.cs                       # Hereda Entity + CreatedAt/By, Modified, IsDeleted, DeletedAt
│   │   │   ├── Result.cs                                # Result y Result<T> — patrón sin excepciones
│   │   │   └── Error.cs                                 # Value object Error(Code, Message) + Error.None
│   │   └── Interfaces/
│   │       ├── IUnitOfWork.cs                           # SaveChangesAsync(CancellationToken)
│   │       └── Services/
│   │           └── ICurrentUserService.cs               # UserId, Email, Role, TenantId, IsAuthenticated
│   │
│   ├── GOP.Application/
│   │   ├── GOP.Application.csproj                       # Refs: GOP.Domain + MediatR, FluentValidation, AutoMapper
│   │   ├── AssemblyMarker.cs                            # Clase vacía para assembly scanning
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── LoggingBehavior.cs                   # Logguea inicio/fin de cada request con duración
│   │   │   │   └── ValidationBehavior.cs                # Ejecuta validators, cortocircuita con Result.Failure si falla
│   │   │   └── Interfaces/
│   │   │       └── IApplicationDbContext.cs              # Interfaz del DbContext (vacía por ahora, sin DbSets)
│   │   └── DependencyInjection.cs                       # Extension method: AddApplication()
│   │
│   ├── GOP.Infrastructure/
│   │   ├── GOP.Infrastructure.csproj                    # Refs: GOP.Application + EF Core, SQL Server, Serilog
│   │   ├── Persistence/
│   │   │   └── GopDbContext.cs                          # DbContext vacío, implementa IApplicationDbContext + IUnitOfWork
│   │   ├── Services/
│   │   │   └── CurrentUserServiceStub.cs                # Stub: retorna valores dummy (sin JWT aún)
│   │   └── DependencyInjection.cs                       # Extension method: AddInfrastructure(IConfiguration)
│   │
│   └── GOP.API/
│       ├── GOP.API.csproj                               # Refs: GOP.Application + GOP.Infrastructure + NSwag, HealthChecks
│       ├── Program.cs                                   # Composition root: DI, Serilog, CORS, Swagger, pipeline
│       ├── Controllers/
│       │   └── HealthController.cs                      # GET /api/v1/health — endpoint público
│       ├── Middleware/
│       │   └── GlobalExceptionHandlerMiddleware.cs      # Try/catch global → ProblemDetails 500
│       ├── Extensions/
│       │   └── ResultExtensions.cs                      # Result → IActionResult conversion
│       ├── Properties/
│       │   └── launchSettings.json                      # Perfiles de ejecución local (http: 5000, https: 5001)
│       ├── appsettings.json                             # Configuración base (Serilog, pagination defaults)
│       └── appsettings.Development.json                 # Connection string al Docker Compose SQL Server
│
└── tests/
    ├── GOP.Domain.Tests/
    │   ├── GOP.Domain.Tests.csproj                      # Refs: GOP.Domain + xUnit, FluentAssertions
    │   └── Common/
    │       ├── ResultTests.cs                           # Tests del patrón Result<T>
    │       └── EntityTests.cs                           # Tests de Entity (Id generado)
    │
    ├── GOP.Application.Tests/
    │   ├── GOP.Application.Tests.csproj                 # Refs: GOP.Application + xUnit, FluentAssertions, NSubstitute
    │   └── Common/
    │       └── ValidationBehaviorTests.cs               # Tests del ValidationBehavior con validator mockeado
    │
    ├── GOP.Infrastructure.Tests/
    │   ├── GOP.Infrastructure.Tests.csproj              # Refs: GOP.Infrastructure + xUnit, FluentAssertions
    │   └── Persistence/
    │       └── GopDbContextTests.cs                     # Test canary: DbContext se instancia correctamente
    │
    └── GOP.API.Tests/
        ├── GOP.API.Tests.csproj                         # Refs: GOP.API + xUnit, FluentAssertions, Mvc.Testing
        └── Controllers/
            └── HealthControllerTests.cs                 # Test de integración: health endpoint retorna 200
```

---

## 3. Detalle de Archivos Clave

### 3.1. `Directory.Build.props` — Configuración compartida

Vive en `backend/` y aplica a todos los `.csproj` hijos. Evita repetir propiedades en cada proyecto.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### 3.2. Domain — Clases Base

**`Entity.cs`** — Ver CONSTITUTION.backend.md §7.5.
- `Id`: `Guid`, `protected init`, default `Guid.NewGuid()`

**`AuditableEntity.cs`** — Hereda `Entity`.
- Propiedades: `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, `IsDeleted`, `DeletedAt`
- Todas con setters públicos (el interceptor de EF Core las setea)

**`Result.cs`** y **`Error.cs`** — Implementación completa según CONSTITUTION.backend.md §6.2–6.3.
- `Result.Success()`, `Result.Failure(error)`, `Result<T>.Success(value)`, `Result<T>.Failure(error)`
- `Error.None` como valor centinela para success

**`IUnitOfWork.cs`** — Según CONSTITUTION.backend.md §7.6.
- Un solo método: `Task<int> SaveChangesAsync(CancellationToken)`

**`ICurrentUserService.cs`** — Según CONSTITUTION.backend.md §8.3.
- Propiedades: `UserId`, `Email`, `Name`, `Role`, `TenantId`, `IsAuthenticated`
- Método: `bool IsInRole(string role)`

### 3.3. Application — Pipeline

**`LoggingBehavior.cs`** — Según CONSTITUTION.backend.md §10.3.
- Logguea `"Handling {RequestName} by User {UserId} Tenant {TenantId}"` al inicio
- Logguea `"Handled {RequestName} in {ElapsedMs}ms"` al final
- Usa `ICurrentUserService` para contexto del usuario

**`ValidationBehavior.cs`** — Según CONSTITUTION.backend.md §4.5.
- Constraint: `where TResponse : Result` (solo funciona con requests que retornan Result)
- Ejecuta todos los `IValidator<TRequest>` registrados
- Si hay failures, retorna `Result.Failure` con errores de validación sin ejecutar el handler
- Si no hay validators, pasa al siguiente behavior

**`IApplicationDbContext.cs`** — Interfaz vacía por ahora.
- Solo declara la herencia (marcador). Los `DbSet<T>` se agregan en features de negocio.

**`DependencyInjection.cs`** — `AddApplication()` extension method.
- Registra MediatR assembly scanning
- Registra FluentValidation assembly scanning
- Registra AutoMapper assembly scanning
- Registra `LoggingBehavior` y `ValidationBehavior` como `IPipelineBehavior`

### 3.4. Infrastructure — Persistencia y Stubs

**`GopDbContext.cs`**
- Hereda `DbContext`, implementa `IApplicationDbContext` y `IUnitOfWork`
- Sin DbSets de entidades (se agregan en features de negocio)
- `OnModelCreating`: `ApplyConfigurationsFromAssembly` (no hay configs aún, pero el hook está listo)

**`CurrentUserServiceStub.cs`**
- Implementa `ICurrentUserService`
- Retorna valores dummy: `UserId = Guid.Empty`, `Role = "SYSTEM"`, `TenantId = 0`, `IsAuthenticated = false`
- Se reemplazará por la implementación real en la feature de JWT

**`DependencyInjection.cs`** — `AddInfrastructure(IConfiguration)` extension method.
- Registra `GopDbContext` con connection string de configuración
- Registra `IApplicationDbContext` → `GopDbContext` (Scoped)
- Registra `IUnitOfWork` → `GopDbContext` (Scoped)
- Registra `ICurrentUserService` → `CurrentUserServiceStub` (Scoped)

### 3.5. API — Composition Root

**`Program.cs`** — Según CONSTITUTION.backend.md §15.1.
```
1. Serilog bootstrap (try/catch para logging de startup errors)
2. builder.Services.AddApplication()
3. builder.Services.AddInfrastructure(configuration)
4. builder.Services.AddControllers()
5. builder.Services.AddEndpointsApiExplorer()
6. builder.Services.AddOpenApiDocument() (NSwag)
7. builder.Services.AddHealthChecks().AddSqlServer().AddDbContextCheck()
8. builder.Services.AddCors() (DevPolicy: localhost:4200)
9. app.UseMiddleware<GlobalExceptionHandlerMiddleware>()
10. app.UseSerilogRequestLogging()
11. app.UseCors()
12. app.UseOpenApi() + app.UseSwaggerUi() (solo en Development)
13. app.MapControllers()
14. app.MapHealthChecks() (si se usa minimal API para health, alternativa al controller)
15. app.Run()
```

> **Nota:** La autenticación (`app.UseAuthentication()` / `app.UseAuthorization()`) NO se configura en esta feature. Se agrega cuando se implemente JWT.

**`HealthController.cs`** — Según contract.yml.
- Ruta: `api/v1/health`
- Atributo: `[AllowAnonymous]` (aunque auth no está configurada aún, se prepara)
- Invoca los health checks registrados y retorna `HealthResponse`

**`GlobalExceptionHandlerMiddleware.cs`** — Según CONSTITUTION.backend.md §9.4.
- `try { await next(context); } catch (Exception ex) { ... }`
- En Development: incluye stack trace en ProblemDetails
- En Production: ProblemDetails genérico sin stack trace
- Logguea la excepción completa con Serilog antes de responder

**`ResultExtensions.cs`** — Según CONSTITUTION.backend.md §9.5.
- `ToActionResult()` para `Result` y `Result<T>`
- `ToCreatedResult()` con route name y values
- Mapeo de error code patterns a HTTP status codes

### 3.6. Docker Compose

**`docker-compose.yml`**
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Gop360_Dev!"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

volumes:
  sqlserver-data:
```

**Connection string Development:**
```
Server=localhost,1433;Database=GOP360;User Id=sa;Password=Gop360_Dev!;TrustServerCertificate=true;
```

### 3.7. appsettings

**`appsettings.json`** — Configuración base:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "CONFIGURE_IN_ENVIRONMENT_SPECIFIC_FILE"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/gop-.log", "rollingInterval": "Day", "retainedFileCountLimit": 30 } }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  },
  "PaginationDefaults": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100
  }
}
```

**`appsettings.Development.json`** — Solo connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GOP360;User Id=sa;Password=Gop360_Dev!;TrustServerCertificate=true;"
  }
}
```

---

## 4. Tests Mínimos

### 4.1. `ResultTests.cs` (Domain.Tests) — HT-008

| Test | Escenario |
|---|---|
| `Success_ReturnsIsSuccessTrue` | `Result.Success()` → `IsSuccess == true`, `Error == Error.None` |
| `Failure_ReturnsIsFailureTrueWithError` | `Result.Failure(error)` → `IsFailure == true`, `Error == error` |
| `SuccessT_ReturnsValueCorrectly` | `Result.Success(42)` → `Value == 42` |
| `FailureT_AccessingValueThrowsException` | `Result.Failure<int>(error).Value` → `InvalidOperationException` |

### 4.2. `EntityTests.cs` (Domain.Tests) — HT-008

| Test | Escenario |
|---|---|
| `NewEntity_GeneratesNonEmptyGuid` | Instanciar entidad concreta → `Id != Guid.Empty` |
| `TwoEntities_HaveDifferentIds` | Instanciar dos entidades → `a.Id != b.Id` |

### 4.3. `ValidationBehaviorTests.cs` (Application.Tests) — HT-004

| Test | Escenario |
|---|---|
| `Handle_NoValidators_CallsNext` | Sin validators registrados → el handler se ejecuta normalmente |
| `Handle_ValidRequest_CallsNext` | Validator pasa → el handler se ejecuta |
| `Handle_InvalidRequest_ReturnsFailureWithoutCallingHandler` | Validator falla → retorna `Result.Failure`, handler no se invoca |

### 4.4. `GopDbContextTests.cs` (Infrastructure.Tests)

| Test | Escenario |
|---|---|
| `CanInstantiateDbContext` | `GopDbContext` se crea con `DbContextOptions` in-memory sin errores |

### 4.5. `HealthControllerTests.cs` (API.Tests) — HT-002

| Test | Escenario |
|---|---|
| `GetHealth_ReturnsOk` | `GET /api/v1/health` → status `200 OK` con body JSON válido |

> **Nota:** El test de API usa `WebApplicationFactory<Program>` con SQL Server mockeado/reemplazado para evitar depender de Docker en CI.

---

## 5. Configuración de CORS

Según CONSTITUTION.backend.md §16:

```csharp
// Solo en Development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});
```

El frontend Angular corre en `http://localhost:4200`. El CORS se restringe en producción.

---

## 6. Restricciones de Implementación (CONSTITUTION)

| Regla de CONSTITUTION.backend.md | Aplicación en esta feature |
|---|---|
| §3 — Domain sin dependencias | `GOP.Domain.csproj` no referencia ningún paquete NuGet ni proyecto |
| §3 — Application solo ref Domain | `GOP.Application.csproj` solo tiene `<ProjectReference>` a `GOP.Domain` |
| §4.5 — Pipeline behaviors | `LoggingBehavior` y `ValidationBehavior` registrados en orden |
| §6 — Result Pattern | `Result`, `Result<T>` y `Error` implementados en Domain/Common |
| §7.1 — DbContext | `GopDbContext` implementa `IApplicationDbContext` y `IUnitOfWork` |
| §7.4 — Sin migraciones vacías | No se generan migraciones sin entidades. La primera migración viene con la primera feature de negocio |
| §8.3 — ICurrentUserService | Interfaz en Application, stub en Infrastructure |
| §9 — ProblemDetails | Middleware global que captura excepciones y retorna RFC 7807 |
| §10 — Serilog | Configurado en `appsettings.json`, sinks Console + File |
| §11 — Tests | xUnit + FluentAssertions + NSubstitute en cada proyecto de test |
| §14.3 — Un archivo = una clase | Verificado en todo el scaffolding |

---

## 7. Tareas Atómicas (tasks.be.md preview)

> El `tasks.be.md` completo se genera tras aprobación de este plan. Aquí el resumen por bloques compilables:

### Bloque 0 — Solución y Proyectos (dotnet build GOP.sln)
- Crear `GOP.sln`, 4 `.csproj` de producción, 4 `.csproj` de test
- Configurar `Directory.Build.props`, `.editorconfig`
- Agregar references entre proyectos
- Agregar paquetes NuGet a cada proyecto
- **Verificación:** `dotnet build GOP.sln` → exit code 0

### Bloque 1 — Domain (dotnet build GOP.Domain)
- `Entity.cs`, `AuditableEntity.cs`, `Result.cs`, `Error.cs`
- `IUnitOfWork.cs`, `ICurrentUserService.cs`
- **Verificación:** `dotnet build src/GOP.Domain`

### Bloque 2 — Application (dotnet build GOP.Application)
- `IApplicationDbContext.cs`, `AssemblyMarker.cs`
- `LoggingBehavior.cs`, `ValidationBehavior.cs`
- `DependencyInjection.cs` (AddApplication)
- **Verificación:** `dotnet build src/GOP.Application`

### Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)
- `GopDbContext.cs`, `CurrentUserServiceStub.cs`
- `DependencyInjection.cs` (AddInfrastructure)
- **Verificación:** `dotnet build src/GOP.Infrastructure`

### Bloque 4 — API (dotnet build GOP.API)
- `Program.cs`, `GlobalExceptionHandlerMiddleware.cs`, `ResultExtensions.cs`
- `HealthController.cs`
- `launchSettings.json`, `appsettings.json`, `appsettings.Development.json`
- **Verificación:** `dotnet build src/GOP.API`

### Bloque 5 — Docker + Docs
- `docker-compose.yml`, `docker-compose.override.yml`
- **Verificación:** `docker compose config` (valida sintaxis)

### Bloque 6 — Tests (dotnet test GOP.sln)
- `ResultTests.cs`, `EntityTests.cs`
- `ValidationBehaviorTests.cs`
- `GopDbContextTests.cs`
- `HealthControllerTests.cs`
- **Verificación:** `dotnet test GOP.sln` → all pass

---

## 8. Orden de Dependencias entre Bloques

```
Bloque 0 (Solución)       → Sin dependencias. Primer paso obligatorio.
Bloque 1 (Domain)         → Depende de Bloque 0.
Bloque 2 (Application)    → Depende de Bloque 1.
Bloque 3 (Infrastructure) → Depende de Bloque 2.
Bloque 4 (API)            → Depende de Bloque 2 + Bloque 3.
Bloque 5 (Docker)         → Sin dependencias de código (puede ir en paralelo con 1-4).
Bloque 6 (Tests)          → Depende de Bloques 1-4 completos.
```

```
B0 ──→ B1 ──→ B2 ──→ B3 ──→ B4 ──→ B6
                                ↑
B5 (paralelo) ─────────────────┘
```
