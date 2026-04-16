# Principios Arquitectónicos para Backend .NET — GOP 360°

## 1. Conceptos Core

* **Clean Architecture:** El backend se estructura en cuatro proyectos con dependencias estrictamente unidireccionales. El dominio es el centro — no depende de nada externo.
* **CQRS (Command Query Responsibility Segregation):** Toda operación es un Command (escritura) o una Query (lectura). Nunca ambos. MediatR 12.x orquesta el dispatch.
* **Result Pattern:** El flujo de negocio **nunca** usa excepciones. Toda operación retorna `Result<T>` o `Result`. Las excepciones se reservan para fallos de infraestructura irrecuperables.
* **Multi-Tenancy:** Cada request opera en el contexto de un tenant (operadora). El filtrado por tenant es automático e invisible para la lógica de negocio.
* **Contratos OpenAPI:** Los endpoints implementan los contratos definidos en `specs/features/NNN-nombre/contract.yml`. El contrato es la verdad compartida con el frontend.

---

## 2. Estructura de Proyectos

```text
backend/
├── GOP.sln                              # Solución raíz
│
├── src/
│   ├── GOP.Domain/                      # 1. DOMINIO: Entidades, Value Objects, Enums, Interfaces de repositorio
│   │   ├── Entities/                    #    Entidades de negocio (Well, Operator, User, Form100...)
│   │   ├── ValueObjects/                #    Objetos de valor inmutables (UWI, Coordinate, DateRange...)
│   │   ├── Enums/                       #    Enumeraciones de dominio (WellStatus, UserRole, FormState...)
│   │   ├── Interfaces/                  #    Contratos de repositorios e infraestructura
│   │   │   ├── Repositories/            #    IWellRepository, IUserRepository...
│   │   │   └── Services/               #    IDateTimeProvider, ICurrentUserService...
│   │   ├── Events/                      #    Domain Events (WellCreatedEvent, FormApprovedEvent...)
│   │   ├── Errors/                      #    Catálogo de errores de dominio (DomainErrors static class)
│   │   ├── Common/                      #    Base classes: Entity, AggregateRoot, ValueObject, Result<T>
│   │   └── GOP.Domain.csproj            #    Sin dependencias externas (solo .NET BCL)
│   │
│   ├── GOP.Application/                 # 2. APLICACIÓN: Casos de uso (Commands, Queries, Validators)
│   │   ├── Common/                      #    Behaviors, Interfaces compartidas, Mappings base
│   │   │   ├── Behaviors/               #    ValidationBehavior, LoggingBehavior, AuthorizationBehavior
│   │   │   ├── Interfaces/              #    IApplicationDbContext, IIdentityService
│   │   │   └── Mappings/                #    Perfiles de AutoMapper compartidos
│   │   ├── Features/                    #    Un directorio por feature/aggregate
│   │   │   ├── Wells/                   #    Feature de Pozos
│   │   │   │   ├── Commands/            #    CreateWell/, UpdateWell/, DeleteWell/
│   │   │   │   │   └── CreateWell/
│   │   │   │   │       ├── CreateWellCommand.cs
│   │   │   │   │       ├── CreateWellCommandHandler.cs
│   │   │   │   │       └── CreateWellCommandValidator.cs
│   │   │   │   ├── Queries/             #    GetWellById/, GetWellsList/
│   │   │   │   │   └── GetWellById/
│   │   │   │   │       ├── GetWellByIdQuery.cs
│   │   │   │   │       ├── GetWellByIdQueryHandler.cs
│   │   │   │   │       └── WellDetailDto.cs
│   │   │   │   └── Mappings/            #    WellMappingProfile.cs (AutoMapper)
│   │   │   ├── Operations/
│   │   │   ├── Production/
│   │   │   └── Admin/
│   │   └── GOP.Application.csproj       #    Refs: GOP.Domain, MediatR, FluentValidation, AutoMapper
│   │
│   ├── GOP.Infrastructure/              # 3. INFRAESTRUCTURA: EF Core, repositorios, servicios externos
│   │   ├── Persistence/                 #    DbContext, Configurations, Migrations, Repositories
│   │   │   ├── GopDbContext.cs
│   │   │   ├── Configurations/          #    Fluent API por entidad (WellConfiguration.cs...)
│   │   │   ├── Migrations/              #    Migraciones generadas por EF Core
│   │   │   ├── Repositories/            #    Implementaciones de IXxxRepository
│   │   │   └── Interceptors/            #    AuditableEntityInterceptor, SoftDeleteInterceptor
│   │   ├── Identity/                    #    JWT generation, token validation, claims mapping
│   │   ├── Services/                    #    Implementaciones de IDateTimeProvider, IEmailService...
│   │   └── GOP.Infrastructure.csproj    #    Refs: GOP.Application, EF Core, SQL Server, Serilog sinks
│   │
│   └── GOP.API/                         # 4. API: Controllers, Middleware, DI composition root
│       ├── Controllers/                 #    Un controller por aggregate/feature
│       │   ├── WellsController.cs
│       │   ├── OperationsController.cs
│       │   ├── ProductionController.cs
│       │   └── AdminController.cs
│       ├── Middleware/                   #    ExceptionHandling, RequestLogging, TenantResolution
│       ├── Filters/                     #    ResultToActionResultFilter (convierte Result<T> → HTTP)
│       ├── Extensions/                  #    DI registration: AddApplication(), AddInfrastructure()...
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs                   #    Composition root — todo el DI se registra aquí
│       └── GOP.API.csproj               #    Refs: GOP.Application, GOP.Infrastructure
│
└── tests/
    ├── GOP.Domain.Tests/                #    Tests unitarios de entidades y value objects
    ├── GOP.Application.Tests/           #    Tests unitarios de handlers (mocked repos)
    ├── GOP.Infrastructure.Tests/        #    Tests de integración con Testcontainers (SQL Server)
    └── GOP.API.Tests/                   #    Tests de integración de endpoints (WebApplicationFactory)
```

> **Referencia:** Los features, entidades y controllers mostrados son ilustrativos. La estructura real se expande conforme a las especificaciones funcionales. No crear carpetas ni archivos anticipándose a funcionalidades no especificadas.

---

## 3. Flujo de Dependencias

```
┌──────────────────────────────────────────────────────┐
│                     GOP.API                          │  ← Composition Root. Registra todo el DI.
│              (Controllers, Middleware)                │  ← Referencia: Application + Infrastructure
└───────────────────┬──────────────────────────────────┘
                    │ depende de ↓
┌───────────────────┴──────────────────────────────────┐
│                GOP.Infrastructure                    │  ← Implementa interfaces de Application y Domain
│     (EF Core, Repositories, Identity, Services)      │  ← Referencia: Application (transitiva → Domain)
└───────────────────┬──────────────────────────────────┘
                    │ depende de ↓
┌───────────────────┴──────────────────────────────────┐
│                 GOP.Application                      │  ← Casos de uso. Define interfaces.
│       (Commands, Queries, Validators, DTOs)          │  ← Referencia: Domain (solo)
└───────────────────┬──────────────────────────────────┘
                    │ depende de ↓
┌───────────────────┴──────────────────────────────────┐
│                   GOP.Domain                         │  ← Centro del sistema. Sin dependencias.
│      (Entities, ValueObjects, Enums, Interfaces)     │  ← Referencia: ninguna (solo .NET BCL)
└──────────────────────────────────────────────────────┘
```

### Reglas explícitas

1. **Domain no referencia ningún otro proyecto** — solo usa tipos de .NET BCL. Prohibido agregar paquetes NuGet al Domain salvo anotaciones puras.
2. **Application referencia solo Domain** — define interfaces (`IApplicationDbContext`, `IWellRepository`) que Infrastructure implementa. Nunca referencia Infrastructure.
3. **Infrastructure referencia Application** (y transitivamente Domain) — implementa las interfaces. Nunca referencia API.
4. **API referencia Application e Infrastructure** — es el composition root donde se registra todo el DI. No contiene lógica de negocio.
5. **Nunca crear dependencias circulares.** Si Application necesita algo de Infrastructure, se define una interfaz en Application y se implementa en Infrastructure (Dependency Inversion).

---

## 4. CQRS con MediatR 12.x

### 4.1. Principio fundamental

Toda interacción con el sistema es un **Command** (modifica estado) o una **Query** (lee estado). Nunca ambos.

| Tipo | Responsabilidad | Retorno | Ejemplo |
|---|---|---|---|
| **Command** | Escritura: crea, actualiza, elimina | `Result` o `Result<TId>` | `CreateWellCommand` → `Result<Guid>` |
| **Query** | Lectura: consulta datos | `Result<TDto>` o `Result<PagedList<TDto>>` | `GetWellByIdQuery` → `Result<WellDetailDto>` |

### 4.2. Estructura de un Command

Cada command vive en su propio directorio con exactamente 3 archivos:

```
Features/Wells/Commands/CreateWell/
├── CreateWellCommand.cs           # Record inmutable con los datos de entrada
├── CreateWellCommandHandler.cs    # Lógica del caso de uso
└── CreateWellCommandValidator.cs  # Reglas de validación (FluentValidation)
```

```csharp
// CreateWellCommand.cs
public sealed record CreateWellCommand(
    string Name,
    string Uwi,
    int OperatorId,
    string WellType
) : IRequest<Result<Guid>>;
```

```csharp
// CreateWellCommandHandler.cs
internal sealed class CreateWellCommandHandler(
    IWellRepository wellRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateWellCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateWellCommand request, CancellationToken cancellationToken)
    {
        // 1. Verificar reglas de negocio
        if (await wellRepository.ExistsByUwiAsync(request.Uwi, cancellationToken))
            return Result.Failure<Guid>(DomainErrors.Well.DuplicateUwi);

        // 2. Crear entidad de dominio
        var well = Well.Create(request.Name, request.Uwi, request.OperatorId, request.WellType);

        // 3. Persistir
        wellRepository.Add(well);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(well.Id);
    }
}
```

### 4.3. Estructura de una Query

Cada query vive en su propio directorio con 2-3 archivos:

```
Features/Wells/Queries/GetWellById/
├── GetWellByIdQuery.cs            # Record inmutable con parámetros de búsqueda
├── GetWellByIdQueryHandler.cs     # Lógica de lectura
└── WellDetailDto.cs               # DTO de respuesta (puede ser compartido)
```

```csharp
// GetWellByIdQuery.cs
public sealed record GetWellByIdQuery(Guid WellId) : IRequest<Result<WellDetailDto>>;
```

```csharp
// GetWellByIdQueryHandler.cs
internal sealed class GetWellByIdQueryHandler(
    IApplicationDbContext dbContext,
    IMapper mapper
) : IRequestHandler<GetWellByIdQuery, Result<WellDetailDto>>
{
    public async Task<Result<WellDetailDto>> Handle(
        GetWellByIdQuery request, CancellationToken cancellationToken)
    {
        var well = await dbContext.Wells
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure<WellDetailDto>(DomainErrors.Well.NotFound(request.WellId));

        return Result.Success(mapper.Map<WellDetailDto>(well));
    }
}
```

### 4.4. Reglas de handlers

| Regla | Descripción |
|---|---|
| Un handler por request | Cada `IRequest<T>` tiene exactamente un handler. Nunca handlers compartidos. |
| Handlers son `internal sealed` | No se exponen fuera del assembly. MediatR los descubre por convención. |
| Primary constructors para DI | Usar constructor primario de C# 12 para inyección (como en los ejemplos). |
| Queries usan `AsNoTracking()` | Toda lectura via EF Core desactiva tracking para optimizar rendimiento. |
| Queries pueden leer directo del DbContext | No es obligatorio pasar por repositorio para lecturas simples. El repositorio es obligatorio para escrituras. |
| Commands pasan por repositorio + UoW | Toda escritura usa el repositorio para agregar/modificar y `IUnitOfWork.SaveChangesAsync()` para confirmar. |
| Sin lógica en el Command/Query record | El record es solo un contenedor de datos. Toda lógica va en el handler. |

### 4.5. Pipeline Behaviors (orden de ejecución)

MediatR procesa cada request a través de un pipeline de behaviors en este orden:

```
Request → [1. LoggingBehavior] → [2. ValidationBehavior] → [3. AuthorizationBehavior] → Handler → Response
```

| Behavior | Responsabilidad | Aplica a |
|---|---|---|
| `LoggingBehavior` | Registra inicio/fin y duración de cada request en Serilog | Commands + Queries |
| `ValidationBehavior` | Ejecuta todos los `IValidator<T>` registrados. Si falla, retorna `Result.Failure` sin llegar al handler | Commands (principalmente) |
| `AuthorizationBehavior` | Verifica permisos del usuario actual contra atributos `[Authorize(Roles)]` del request | Commands + Queries con restricción |

```csharp
// Common/Behaviors/ValidationBehavior.cs
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length != 0)
            return CreateValidationResult<TResponse>(failures);

        return await next();
    }
}
```

---

## 5. FluentValidation 11.x

### 5.1. Reglas de ubicación

Cada Command tiene su validator en el mismo directorio. Las Queries solo tienen validator si reciben parámetros que requieren validación (ej. paginación).

```
Features/Wells/Commands/CreateWell/
├── CreateWellCommand.cs
├── CreateWellCommandHandler.cs
└── CreateWellCommandValidator.cs    ← Siempre junto al Command
```

### 5.2. Convenciones

```csharp
// CreateWellCommandValidator.cs
internal sealed class CreateWellCommandValidator : AbstractValidator<CreateWellCommand>
{
    public CreateWellCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del pozo es requerido.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Uwi)
            .NotEmpty().WithMessage("El UWI es requerido.")
            .Matches(@"^[A-Z0-9\-]{10,20}$").WithMessage("El UWI debe contener entre 10 y 20 caracteres alfanuméricos.");

        RuleFor(x => x.OperatorId)
            .GreaterThan(0).WithMessage("Debe seleccionar una operadora válida.");

        RuleFor(x => x.WellType)
            .NotEmpty().WithMessage("El tipo de pozo es requerido.");
    }
}
```

### 5.3. Reglas obligatorias

| Regla | Descripción |
|---|---|
| Validators son `internal sealed` | No se exponen fuera del assembly. |
| Mensajes en español | Todos los mensajes de validación son en español, consistentes con el locale del frontend. |
| Sin lógica de negocio en validators | El validator solo valida formato y presencia. Reglas de negocio (ej. "UWI ya existe") van en el handler. |
| Registrado automáticamente | `AddValidatorsFromAssembly()` en el DI los descubre por convención. No registrar manualmente. |
| Un validator por Command/Query | Si un Command no requiere validación, no crear un validator vacío. |

---

## 6. Patrón Result\<T\>

### 6.1. Principio

**Las excepciones son para situaciones excepcionales** (fallo de red, base de datos inaccesible). El flujo de negocio (entidad no encontrada, validación fallida, operación no permitida) usa `Result<T>`.

### 6.2. Implementación base

```csharp
// Domain/Common/Result.cs
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result.");
}
```

### 6.3. Error como Value Object

```csharp
// Domain/Common/Error.cs
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "El valor proporcionado es nulo.");
}
```

### 6.4. Catálogo de errores de dominio

Cada aggregate define sus errores como constantes estáticas en una clase parcial:

```csharp
// Domain/Errors/DomainErrors.cs
public static partial class DomainErrors
{
    public static class Well
    {
        public static readonly Error NotFound = new(
            "Well.NotFound", "El pozo no fue encontrado.");

        public static Error NotFoundById(Guid id) => new(
            "Well.NotFound", $"No se encontró un pozo con Id '{id}'.");

        public static readonly Error DuplicateUwi = new(
            "Well.DuplicateUwi", "Ya existe un pozo con ese UWI.");

        public static readonly Error InvalidStatus = new(
            "Well.InvalidStatus", "El estado del pozo no permite esta operación.");
    }

    public static class User
    {
        public static readonly Error NotFound = new(
            "User.NotFound", "El usuario no fue encontrado.");

        public static readonly Error InvalidCredentials = new(
            "User.InvalidCredentials", "Correo o contraseña incorrectos.");

        public static readonly Error Unauthorized = new(
            "User.Unauthorized", "No tiene permisos para realizar esta acción.");
    }
}
```

### 6.5. Reglas del patrón Result

| Regla | Descripción |
|---|---|
| Nunca `throw` en handlers | Los handlers retornan `Result.Failure(error)` para errores de negocio. |
| Nunca `try/catch` en handlers | Si se necesita catch, es infraestructura — va en un behavior o middleware. |
| Nunca acceder `Value` sin verificar `IsSuccess` | El controller o el filter verifica antes de extraer el valor. |
| Códigos de error son `Entidad.Accion` | Formato: `"Well.NotFound"`, `"User.InvalidCredentials"`. Permite al frontend mapear. |
| Mensajes en español | Consistentes con el idioma del sistema. |

---

## 7. EF Core + SQL Server

### 7.1. DbContext

```csharp
// Infrastructure/Persistence/GopDbContext.cs
public sealed class GopDbContext(
    DbContextOptions<GopDbContext> options
) : DbContext(options), IApplicationDbContext
{
    public DbSet<Well> Wells => Set<Well>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Operator> Operators => Set<Operator>();
    // DbSets adicionales conforme se agregan entidades

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GopDbContext).Assembly);
    }
}
```

### 7.2. Configuraciones Fluent API

Cada entidad tiene su archivo de configuración. Prohibido usar Data Annotations en el Domain.

```csharp
// Infrastructure/Persistence/Configurations/WellConfiguration.cs
internal sealed class WellConfiguration : IEntityTypeConfiguration<Well>
{
    public void Configure(EntityTypeBuilder<Well> builder)
    {
        builder.ToTable("Wells");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Uwi)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(w => w.Uwi)
            .IsUnique();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        // Multi-tenant: filtro global automático
        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.Property(w => w.TenantId)
            .IsRequired();
    }
}
```

### 7.3. Convenciones de base de datos

| Convención | Regla |
|---|---|
| Nombres de tablas | PascalCase plural: `Wells`, `Users`, `Operators`, `AuditLogs` |
| Nombres de columnas | PascalCase: `WellName`, `OperatorId`, `CreatedAt` |
| Primary keys | `Id` tipo `Guid` (generado por el dominio, no por la DB) |
| Foreign keys | `{Entidad}Id`: `OperatorId`, `CreatedByUserId` |
| Índices | Nombrados explícitamente: `IX_Wells_Uwi`, `IX_Users_Email` |
| Soft delete | Columna `IsDeleted` (bool) + `DeletedAt` (DateTime?) + query filter global |
| Auditoría | `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy` en toda entidad |
| Enums | Almacenados como `string` con conversión explícita, nunca como `int` |
| Strings | Siempre con `HasMaxLength()`. Prohibido `nvarchar(MAX)` sin justificación. |
| Decimales | Siempre con precision: `HasPrecision(18, 6)` para coordenadas, `HasPrecision(18, 2)` para volúmenes |

### 7.4. Migraciones

```bash
# Generar migración (desde raíz del backend)
dotnet ef migrations add NombreDescriptivo -p src/GOP.Infrastructure -s src/GOP.API

# Aplicar migración
dotnet ef database update -p src/GOP.Infrastructure -s src/GOP.API
```

| Regla | Descripción |
|---|---|
| Nombres descriptivos | `AddWellsTable`, `AddUwiUniqueIndex`, `AddAuditColumnsToForms` |
| Una migración por cambio lógico | No agrupar cambios no relacionados en una migración. |
| Sin datos semilla en migraciones | Los datos seed van en un `DbInitializer` separado, no en las migraciones. |
| Migraciones versionadas en Git | Toda migración se commitea. Nunca eliminar migraciones ya aplicadas en ambientes compartidos. |
| Sin `Down()` destructivo | El `Down()` debe poder revertir sin pérdida de datos en producción cuando sea posible. |

### 7.5. Entidades base (Auditoría + Soft Delete)

```csharp
// Domain/Common/Entity.cs
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
}

// Domain/Common/AuditableEntity.cs
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

```csharp
// Infrastructure/Persistence/Interceptors/AuditableEntityInterceptor.cs
// Interceptor de EF Core que setea automáticamente CreatedAt/By, LastModifiedAt/By
// y DeletedAt en SaveChanges. Lee el usuario actual de ICurrentUserService.
```

### 7.6. IUnitOfWork

```csharp
// Domain/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

La implementación es el `GopDbContext`. Se registra como interfaz para que los handlers no dependan del tipo concreto.

---

## 8. JWT Multi-Tenant + SLAP Auth

### 8.1. Modelo de autenticación

| Concepto | Descripción |
|---|---|
| **JWT Bearer** | Toda request autenticada incluye header `Authorization: Bearer {token}`. |
| **Tenant** | Cada usuario pertenece a un operador (tenant). El `TenantId` se incluye como claim en el JWT. |
| **SLAP (Service Layer Authentication Pattern)** | La identidad del usuario y su tenant se resuelven en middleware y se inyectan como `ICurrentUserService` disponible en toda la capa Application. |
| **Roles** | `ADMIN`, `SUPERVISOR`, `OPERADOR`, `AUDITOR` — definidos como enum en Domain. |

### 8.2. Claims del JWT

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "admin@gop360.com",
  "name": "Juan Pérez",
  "role": "ADMIN",
  "tenant_id": "1",
  "tenant_name": "Agencia Nacional de Hidrocarburos",
  "iat": 1700000000,
  "exp": 1700028800
}
```

| Claim | Tipo | Descripción |
|---|---|---|
| `sub` | `Guid` (string) | ID del usuario |
| `email` | `string` | Correo del usuario |
| `name` | `string` | Nombre completo |
| `role` | `string` | Rol del sistema: `ADMIN`, `SUPERVISOR`, `OPERADOR`, `AUDITOR` |
| `tenant_id` | `string` | ID del operador/tenant |
| `tenant_name` | `string` | Nombre del operador (informativo, para UI) |

### 8.3. ICurrentUserService

```csharp
// Application/Common/Interfaces/ICurrentUserService.cs
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    string Role { get; }
    int TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
```

```csharp
// Infrastructure/Identity/CurrentUserService.cs
// Lee los claims del HttpContext.User y los expone tipados.
// Se registra como Scoped en DI.
```

### 8.4. Filtrado multi-tenant automático

El filtrado por tenant opera en dos niveles:

1. **Query Filter global en EF Core:** Toda entidad con `TenantId` tiene un `HasQueryFilter` que filtra automáticamente por el tenant del usuario actual.
2. **Interceptor de escritura:** Al crear una entidad, el `TenantId` se asigna automáticamente desde `ICurrentUserService`.

```csharp
// El Admin (ANH) puede ver datos de todos los tenants.
// Se desactiva el query filter con .IgnoreQueryFilters() solo para queries de Admin.
```

### 8.5. Autorización en endpoints

```csharp
// Controller
[Authorize]                              // Requiere autenticación (cualquier rol)
[Authorize(Roles = "ADMIN")]             // Solo ADMIN
[Authorize(Roles = "ADMIN,SUPERVISOR")]  // ADMIN o SUPERVISOR
[AllowAnonymous]                         // Endpoint público (solo login, health)
```

### 8.6. Reglas de seguridad

| Regla | Descripción |
|---|---|
| Todo endpoint requiere `[Authorize]` por defecto | Solo `/api/v1/auth/login` y `/health` son `[AllowAnonymous]`. |
| El token tiene expiración configurable | Default: 8 horas (configurable en `appsettings.json`). |
| El signing key nunca va en código fuente | Se lee de variable de entorno o Azure Key Vault. |
| Refresh tokens | Se implementan como feature separada. El MVP usa token de vida larga. |
| Rate limiting en login | Máximo 5 intentos por IP en 15 minutos (configurable). |

---

## 9. ProblemDetails — RFC 7807

### 9.1. Principio

**Toda respuesta de error (4xx/5xx) usa `application/problem+json`**. Sin excepciones. El frontend espera este formato en todos los errores.

### 9.2. Estructura estándar

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 422,
  "detail": "Uno o más campos contienen errores de validación.",
  "instance": "/api/v1/wells",
  "errors": {
    "Name": ["El nombre del pozo es requerido."],
    "Uwi": ["El UWI debe contener entre 10 y 20 caracteres alfanuméricos."]
  },
  "traceId": "00-abc123-def456-01"
}
```

### 9.3. Mapeo Result → ProblemDetails

El `ResultToActionResultFilter` o el controller convierte cada tipo de `Error` en el HTTP status correspondiente:

| Error Code Pattern | HTTP Status | Title |
|---|---|---|
| `*.NotFound` | `404 Not Found` | "Resource Not Found" |
| `*.Duplicate*` | `409 Conflict` | "Conflict" |
| `*.Invalid*` | `422 Unprocessable Entity` | "Validation Failed" |
| `*.Unauthorized` | `403 Forbidden` | "Forbidden" |
| Validation failures (FluentValidation) | `422 Unprocessable Entity` | "Validation Failed" |
| Exception no controlada | `500 Internal Server Error` | "Internal Server Error" |

### 9.4. Middleware de excepciones globales

```csharp
// API/Middleware/GlobalExceptionHandlerMiddleware.cs
// Atrapa excepciones no controladas (fallos de infraestructura).
// Retorna ProblemDetails 500 sin exponer detalles internos en Producción.
// En Development incluye stack trace para debugging.
// Logguea la excepción completa en Serilog antes de retornar.
```

### 9.5. Método de extensión para controllers

```csharp
// API/Extensions/ResultExtensions.cs
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return result.Error.Code switch
        {
            var c when c.EndsWith(".NotFound") => new NotFoundObjectResult(
                CreateProblemDetails(404, "Resource Not Found", result.Error.Message)),
            var c when c.Contains(".Duplicate") => new ConflictObjectResult(
                CreateProblemDetails(409, "Conflict", result.Error.Message)),
            var c when c.EndsWith(".Unauthorized") => new ObjectResult(
                CreateProblemDetails(403, "Forbidden", result.Error.Message)) { StatusCode = 403 },
            _ => new UnprocessableEntityObjectResult(
                CreateProblemDetails(422, "Validation Failed", result.Error.Message)),
        };
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return ((Result)result).ToActionResult();
    }

    public static IActionResult ToCreatedResult<T>(this Result<T> result, string routeName, object routeValues)
    {
        if (result.IsSuccess)
            return new CreatedAtRouteResult(routeName, routeValues, result.Value);

        return ((Result)result).ToActionResult();
    }

    private static ProblemDetails CreateProblemDetails(
        int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = "https://tools.ietf.org/html/rfc7807",
    };
}
```

---

## 10. Serilog Structured Logging

### 10.1. Configuración

```csharp
// Program.cs
builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));
```

```json
// appsettings.json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
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
      {
        "Name": "File",
        "Args": {
          "path": "logs/gop-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### 10.2. Convenciones de logging

| Regla | Descripción |
|---|---|
| Structured logging siempre | `Log.Information("Well {WellId} created by {UserId}", id, userId)` — nunca concatenar strings. |
| Propiedades en PascalCase | `{WellId}`, `{UserId}`, `{TenantId}` — consistente con C#. |
| Niveles semánticos | `Information` = operaciones exitosas. `Warning` = situaciones recuperables. `Error` = fallos. `Debug` = desarrollo. |
| Request logging via middleware | Serilog.AspNetCore registra automáticamente cada request HTTP con duración y status code. |
| Sin datos sensibles en logs | Prohibido loggear passwords, tokens completos, datos personales. Enmascarar si es necesario. |
| Correlation ID | Cada request tiene un `CorrelationId` (header `X-Correlation-Id` o generado) propagado en todos los logs. |

### 10.3. LoggingBehavior para MediatR

```csharp
// Application/Common/Behaviors/LoggingBehavior.cs
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName} by User {UserId} Tenant {TenantId}",
            requestName, currentUser.UserId, currentUser.TenantId);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
```

---

## 11. Testing

### 11.1. Stack de testing

| Herramienta | Versión | Propósito |
|---|---|---|
| **xUnit** | 2.x | Framework de tests |
| **FluentAssertions** | 7.x | Assertions legibles y expresivas |
| **NSubstitute** | 5.x | Mocking de interfaces |
| **Testcontainers** | 3.x | SQL Server real en contenedor Docker para tests de integración |
| **Microsoft.AspNetCore.Mvc.Testing** | — | `WebApplicationFactory` para tests de API end-to-end |

### 11.2. Estructura de proyectos de test

```
tests/
├── GOP.Domain.Tests/           # Unitarios: entidades, value objects, reglas de dominio
├── GOP.Application.Tests/      # Unitarios: handlers con repositorios mockeados (NSubstitute)
├── GOP.Infrastructure.Tests/   # Integración: EF Core + Testcontainers SQL Server
└── GOP.API.Tests/              # Integración: endpoints con WebApplicationFactory
```

### 11.3. Convenciones de naming para tests

```
Método: {MetodoOEscenario}_{Condicion}_{ResultadoEsperado}
Clase:  {ClaseBajoTest}Tests
```

```csharp
// Application.Tests/Features/Wells/Commands/CreateWellCommandHandlerTests.cs
public sealed class CreateWellCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithWellId() { ... }

    [Fact]
    public async Task Handle_DuplicateUwi_ReturnsFailureWithDuplicateError() { ... }

    [Fact]
    public async Task Handle_InvalidOperatorId_ReturnsFailureWithNotFoundError() { ... }
}
```

### 11.4. Test unitario de handler (patrón)

```csharp
public sealed class CreateWellCommandHandlerTests
{
    private readonly IWellRepository _wellRepository = Substitute.For<IWellRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateWellCommandHandler CreateSut() => new(_wellRepository, _unitOfWork);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithWellId()
    {
        // Arrange
        var command = new CreateWellCommand("Pozo Alpha", "COL-001-ABCD", 1, "EXPLORATORY");
        _wellRepository.ExistsByUwiAsync(command.Uwi, Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _wellRepository.Received(1).Add(Arg.Is<Well>(w => w.Name == "Pozo Alpha"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateUwi_ReturnsFailure()
    {
        // Arrange
        var command = new CreateWellCommand("Pozo Beta", "COL-001-ABCD", 1, "EXPLORATORY");
        _wellRepository.ExistsByUwiAsync(command.Uwi, Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Well.DuplicateUwi);
        _wellRepository.DidNotReceive().Add(Arg.Any<Well>());
    }
}
```

### 11.5. Test de integración con Testcontainers (patrón)

```csharp
// Infrastructure.Tests/Persistence/WellRepositoryTests.cs
public sealed class WellRepositoryTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        // Aplicar migraciones al contenedor
    }

    public async Task DisposeAsync() => await _sqlContainer.DisposeAsync();

    [Fact]
    public async Task Add_ValidWell_PersistsToDatabase() { ... }
}
```

### 11.6. Reglas de testing

| Regla | Descripción |
|---|---|
| Arrange / Act / Assert | Toda prueba sigue el patrón AAA con comentarios de sección. |
| Un assert lógico por test | Cada `[Fact]` valida un solo comportamiento. Múltiples `.Should()` sobre el mismo resultado están permitidos. |
| Mocks solo de interfaces | NSubstitute solo mockea interfaces, nunca clases concretas. |
| Sin dependencia entre tests | Cada test es independiente. Sin estado compartido entre `[Fact]`. |
| Tests de handler cubren 3 escenarios mínimo | Happy path, error de validación/negocio, entidad no encontrada. |
| `dotnet test` verde para merge | Ningún PR se aprueba con tests fallando. |

---

## 12. AutoMapper 13.x

### 12.1. Ubicación de perfiles

```
Application/Features/Wells/Mappings/
└── WellMappingProfile.cs      ← Un profile por feature/aggregate
```

### 12.2. Convenciones

```csharp
// Application/Features/Wells/Mappings/WellMappingProfile.cs
internal sealed class WellMappingProfile : Profile
{
    public WellMappingProfile()
    {
        CreateMap<Well, WellDetailDto>();
        CreateMap<Well, WellListItemDto>();
        // Solo Entity → DTO. Nunca DTO → Entity.
    }
}
```

### 12.3. Reglas de mapeo

| Regla | Descripción |
|---|---|
| Solo Entity → DTO | AutoMapper solo mapea de entidades de dominio a DTOs de respuesta. La creación de entidades usa constructores/factory methods del dominio, nunca AutoMapper. |
| Un Profile por feature | Cada aggregate tiene su `XxxMappingProfile.cs` en su directorio `Mappings/`. |
| Profiles son `internal sealed` | No se exponen fuera del assembly. |
| Registro automático | `AddAutoMapper(typeof(ApplicationAssemblyMarker).Assembly)` en DI. No registrar perfiles manualmente. |
| Sin lógica en mappings | Los profiles solo declaran mapeos. Si se necesita transformación, usar `.ForMember()` con expresiones simples. Lógica compleja va en el handler. |
| Prohibido mapear colecciones anidadas complejas | Si el DTO tiene relaciones profundas, usar `ProjectTo<T>()` para que EF Core genere el SQL optimizado. |

### 12.4. DTOs de respuesta

Los DTOs viven junto a la Query que los retorna:

```
Features/Wells/Queries/GetWellById/
├── GetWellByIdQuery.cs
├── GetWellByIdQueryHandler.cs
└── WellDetailDto.cs              ← DTO de respuesta específico de esta query

Features/Wells/Queries/GetWellsList/
├── GetWellsListQuery.cs
├── GetWellsListQueryHandler.cs
└── WellListItemDto.cs            ← DTO simplificado para listados
```

```csharp
// WellDetailDto.cs
public sealed record WellDetailDto(
    Guid Id,
    string Name,
    string Uwi,
    string Status,
    string OperatorName,
    DateTime CreatedAt
);
```

**Regla:** Los DTOs son `sealed record` inmutables. Nunca clases mutables con setters.

---

## 13. Reglas de API

### 13.1. Versionado

| Regla | Descripción |
|---|---|
| Prefijo de URL | Todos los endpoints bajo `/api/v1/`. |
| Estrategia | URL path versioning. No headers ni query params. |
| Inmutabilidad | Un endpoint publicado en `v1` no cambia su contrato. Cambios breaking crean `v2`. |
| Un controller por aggregate | `WellsController` → `/api/v1/wells`, `AdminController` → `/api/v1/admin/users`. |

### 13.2. Estructura de controllers

```csharp
[ApiController]
[Route("api/v1/wells")]
[Authorize]
[Produces("application/json")]
public sealed class WellsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<WellListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWells([FromQuery] GetWellsListQuery query)
        => (await sender.Send(query)).ToActionResult();

    [HttpGet("{id:guid}", Name = "GetWellById")]
    [ProducesResponseType(typeof(WellDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWell(Guid id)
        => (await sender.Send(new GetWellByIdQuery(id))).ToActionResult();

    [HttpPost]
    [Authorize(Roles = "ADMIN,SUPERVISOR")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateWell(CreateWellCommand command)
        => (await sender.Send(command)).ToCreatedResult("GetWellById", new { id = "placeholder" });
}
```

### 13.3. Reglas de controllers

| Regla | Descripción |
|---|---|
| Controllers son `sealed` | Sin herencia entre controllers. |
| Sin lógica en controllers | El controller despacha el Command/Query via MediatR y convierte el Result a HTTP. Máximo 3 líneas por action. |
| Primary constructor con `ISender` | Solo `ISender` (de MediatR). Nunca inyectar repositorios o servicios directamente. |
| `[ApiController]` siempre | Habilita model binding automático y respuestas ProblemDetails para errores de binding. |
| `[ProducesResponseType]` en todo action | Documenta los posibles status codes para OpenAPI/Swagger. |

### 13.4. Paginación estándar

Toda lista paginada sigue el mismo contrato:

**Request (query params):**

| Parámetro | Tipo | Default | Restricción |
|---|---|---|---|
| `page` | `int` | `1` | Mínimo 1 (1-indexed) |
| `pageSize` | `int` | `20` | Mínimo 1, máximo 100 |

**Response:**

```json
{
  "items": [...],
  "total": 150,
  "page": 1,
  "pageSize": 20
}
```

```csharp
// Application/Common/PagedList.cs
public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize
)
{
    public bool HasNextPage => Page * PageSize < Total;
    public bool HasPreviousPage => Page > 1;
}
```

```csharp
// Implementación en Query Handler
var totalCount = await query.CountAsync(cancellationToken);
var items = await query
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);

return Result.Success(new PagedList<WellListItemDto>(
    mapper.Map<List<WellListItemDto>>(items), totalCount, request.Page, request.PageSize));
```

### 13.5. Filtros y ordenamiento

Los endpoints de listado aceptan filtros opcionales como query params:

```
GET /api/v1/wells?page=1&pageSize=20&status=ACTIVE&operatorId=5&search=alpha&sortBy=name&sortDir=asc
```

| Parámetro | Tipo | Descripción |
|---|---|---|
| `search` | `string?` | Búsqueda por texto libre (nombre, UWI, etc.) |
| `sortBy` | `string?` | Campo de ordenamiento (validar contra whitelist) |
| `sortDir` | `string?` | Dirección: `asc` o `desc`. Default: `asc` |
| Filtros específicos | Varios | Dependen del recurso: `status`, `operatorId`, `dateFrom`, `dateTo` |

**Regla:** Los filtros se definen en el Query record. El handler aplica `Where()` condicionalmente. Los valores de `sortBy` se validan contra una whitelist para evitar inyección de nombres de columna.

```csharp
// Patrón de filtrado condicional en el handler
var query = dbContext.Wells.AsNoTracking();

if (!string.IsNullOrWhiteSpace(request.Search))
    query = query.Where(w => w.Name.Contains(request.Search) || w.Uwi.Contains(request.Search));

if (!string.IsNullOrWhiteSpace(request.Status))
    query = query.Where(w => w.Status == request.Status);

if (request.OperatorId.HasValue)
    query = query.Where(w => w.OperatorId == request.OperatorId.Value);
```

### 13.6. Respuestas HTTP estándar

| Operación | Éxito | Errores posibles |
|---|---|---|
| `GET /resource` (lista) | `200 OK` con `PagedList<T>` | `401`, `403` |
| `GET /resource/{id}` | `200 OK` con `T` | `401`, `403`, `404` |
| `POST /resource` | `201 Created` con location header | `401`, `403`, `409`, `422` |
| `PUT /resource/{id}` | `200 OK` con `T` actualizado | `401`, `403`, `404`, `409`, `422` |
| `PATCH /resource/{id}` | `200 OK` con `T` actualizado | `401`, `403`, `404`, `422` |
| `DELETE /resource/{id}` | `204 No Content` | `401`, `403`, `404` |

---

## 14. Convenciones de Naming y Estructura de Archivos

### 14.1. Naming general

| Elemento | Convención | Ejemplo |
|---|---|---|
| Proyectos | `GOP.{Capa}` | `GOP.Domain`, `GOP.Application` |
| Namespaces | Siguen la estructura de carpetas | `GOP.Application.Features.Wells.Commands.CreateWell` |
| Clases | PascalCase, sufijo por tipo | `CreateWellCommand`, `WellDetailDto`, `WellConfiguration` |
| Interfaces | Prefijo `I` + PascalCase | `IWellRepository`, `ICurrentUserService` |
| Métodos | PascalCase, verbo + sustantivo | `GetByIdAsync`, `ExistsByUwiAsync`, `CreateWell` |
| Propiedades | PascalCase | `WellName`, `OperatorId`, `IsActive` |
| Variables locales | camelCase | `wellRepository`, `cancellationToken` |
| Constantes | PascalCase | `MaxPageSize`, `DefaultPageSize` |
| Archivos | PascalCase, match con clase | `CreateWellCommand.cs`, `WellConfiguration.cs` |
| Enums | PascalCase singular | `WellStatus`, `UserRole`, `FormState` |
| Enum values | UPPER_SNAKE_CASE | Definidos como `string` en la DB, pero `PascalCase` en C# |

### 14.2. Sufijos obligatorios

| Tipo | Sufijo | Ejemplo |
|---|---|---|
| Commands | `Command` | `CreateWellCommand` |
| Queries | `Query` | `GetWellByIdQuery` |
| Handlers | `CommandHandler` / `QueryHandler` | `CreateWellCommandHandler` |
| Validators | `CommandValidator` / `QueryValidator` | `CreateWellCommandValidator` |
| DTOs de respuesta | `Dto` | `WellDetailDto`, `WellListItemDto` |
| Repositorios (interfaz) | `Repository` con prefijo `I` | `IWellRepository` |
| Repositorios (impl) | `Repository` | `WellRepository` |
| Configuraciones EF | `Configuration` | `WellConfiguration` |
| Controllers | `Controller` | `WellsController` (plural) |
| Mapping Profiles | `MappingProfile` | `WellMappingProfile` |
| Domain Events | `Event` | `WellCreatedEvent` |
| Behaviors | `Behavior` | `ValidationBehavior` |

### 14.3. Estructura interna de un feature

```
Features/Wells/
├── Commands/
│   ├── CreateWell/
│   │   ├── CreateWellCommand.cs            # Record: datos de entrada
│   │   ├── CreateWellCommandHandler.cs     # Handler: lógica del caso de uso
│   │   └── CreateWellCommandValidator.cs   # Validator: reglas de formato
│   ├── UpdateWell/
│   │   └── ...
│   └── DeleteWell/
│       └── ...
├── Queries/
│   ├── GetWellById/
│   │   ├── GetWellByIdQuery.cs
│   │   ├── GetWellByIdQueryHandler.cs
│   │   └── WellDetailDto.cs               # DTO de respuesta
│   └── GetWellsList/
│       ├── GetWellsListQuery.cs
│       ├── GetWellsListQueryHandler.cs
│       └── WellListItemDto.cs
└── Mappings/
    └── WellMappingProfile.cs               # AutoMapper profile
```

**Regla:** Un archivo = una clase/record/interfaz. Prohibido agrupar múltiples tipos en un archivo.

### 14.4. Convenciones de async

| Regla | Descripción |
|---|---|
| Sufijo `Async` en interfaces de repositorio | `GetByIdAsync`, `ExistsByUwiAsync` |
| Sin sufijo `Async` en handlers | El método `Handle` ya es async por convención de MediatR. |
| `CancellationToken` siempre propagado | Todo método async recibe y propaga `CancellationToken`. |
| Sin `.Result` ni `.Wait()` | Prohibido bloquear threads. Siempre `await`. |

---

## 15. Configuración y Composition Root

### 15.1. Program.cs

```csharp
// GOP.API/Program.cs — estructura conceptual
var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// DI por capa (extension methods limpios)
builder.Services
    .AddApplication()       // MediatR, FluentValidation, AutoMapper, Behaviors
    .AddInfrastructure(builder.Configuration)  // EF Core, Repositories, Identity
    .AddPresentation();     // Controllers, Swagger, CORS, ProblemDetails

var app = builder.Build();

// Middleware pipeline (orden importa)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 15.2. Extension methods de DI

```csharp
// Application: AddApplication()
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
    services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
    services.AddAutoMapper(typeof(ApplicationAssemblyMarker).Assembly);

    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    return services;
}

// Infrastructure: AddInfrastructure(IConfiguration)
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<GopDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

    services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<GopDbContext>());
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<GopDbContext>());

    // Repositorios
    services.AddScoped<IWellRepository, WellRepository>();

    // Identity
    services.AddScoped<ICurrentUserService, CurrentUserService>();

    return services;
}
```

---

## 16. CORS y Headers de Seguridad

```csharp
// Solo en Development: permitir el origen del frontend Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// En Production: orígenes restringidos desde configuración
```

| Header | Valor | Propósito |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Previene MIME sniffing |
| `X-Frame-Options` | `DENY` | Previene clickjacking |
| `Strict-Transport-Security` | `max-age=31536000` | Fuerza HTTPS |
| `X-Correlation-Id` | UUID por request | Trazabilidad end-to-end |

---

## 17. Ambientes y Configuración

### 17.1. Archivos de configuración

```
GOP.API/
├── appsettings.json                  # Configuración base (shared)
├── appsettings.Development.json      # Dev: connection string local, verbose logging
├── appsettings.Staging.json          # QA/Staging
└── appsettings.Production.json       # Prod: nunca en Git si contiene secrets
```

### 17.2. Estructura de appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GOP360;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "Issuer": "GOP360",
    "Audience": "GOP360-Client",
    "ExpirationInHours": 8,
    "SigningKey": "NEVER_IN_SOURCE_CODE__USE_ENV_VARS_OR_KEY_VAULT"
  },
  "PaginationDefaults": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100
  },
  "RateLimiting": {
    "LoginMaxAttempts": 5,
    "LoginWindowMinutes": 15
  },
  "Serilog": { }
}
```

### 17.3. Reglas de configuración

| Regla | Descripción |
|---|---|
| Secrets nunca en código | Connection strings de producción, signing keys y credentials se leen de variables de entorno o key vault. |
| `appsettings.Production.json` en `.gitignore` | Solo `appsettings.json` y `appsettings.Development.json` se versionan. |
| Options Pattern | Toda sección de configuración se mapea a un `record` tipado con `IOptions<T>`. |
| Sin magic strings | Los nombres de secciones de configuración se referencian desde constantes, no strings sueltos. |

---

## 18. Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sqlserver")
    .AddDbContextCheck<GopDbContext>(name: "efcore");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**Endpoint:** `GET /health` — `[AllowAnonymous]`. Retorna el estado de la base de datos y dependencias.

---

## 19. Metodología SDD — Artefactos Backend

### 19.1. Estructura por feature

Para cada feature `NNN-nombre`, el arquitecto (GOP-Spec) produce artefactos específicos del backend:

```
specs/features/NNN-nombre/
├── spec.md            # El QUÉ (stack-agnostic)
├── contract.yml       # OpenAPI 3.1 — verdad compartida FE/BE
├── plan.fe.md         # El CÓMO en Angular
├── plan.be.md         # El CÓMO en .NET (estructura de este documento)
├── tasks.fe.md        # Tareas atómicas frontend
└── tasks.be.md        # Tareas atómicas backend
```

### 19.2. Contenido del plan.be.md

El `plan.be.md` de cada feature debe contener:

1. **Entidades de dominio** afectadas (nuevas o modificadas)
2. **Commands y Queries** con su firma (input → output)
3. **Validators** con las reglas de cada command
4. **DTOs** de respuesta con sus campos
5. **Mapping profiles** requeridos
6. **Repositorios** e interfaces nuevas o extendidas
7. **Migraciones** de base de datos necesarias
8. **Controller actions** que implementan el `contract.yml`
9. **Tests** mínimos requeridos por handler

### 19.3. Contenido del tasks.be.md

Las tareas son atómicas (1 tarea = 1 archivo) y se agrupan en bloques compilables:

```markdown
## Bloque 1 — Domain (dotnet build GOP.Domain)
- [ ] T001: Crear `Domain/Entities/Well.cs`
- [ ] T002: Crear `Domain/Enums/WellStatus.cs`
- [ ] T003: Crear `Domain/Errors/DomainErrors.Well.cs`
- [ ] T004: Crear `Domain/Interfaces/Repositories/IWellRepository.cs`

## Bloque 2 — Application (dotnet build GOP.Application)
- [ ] T005: Crear `Application/Features/Wells/Commands/CreateWell/CreateWellCommand.cs`
- [ ] T006: Crear `Application/Features/Wells/Commands/CreateWell/CreateWellCommandHandler.cs`
- [ ] T007: Crear `Application/Features/Wells/Commands/CreateWell/CreateWellCommandValidator.cs`

## Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)
- [ ] T008: Crear `Infrastructure/Persistence/Configurations/WellConfiguration.cs`
- [ ] T009: Crear `Infrastructure/Persistence/Repositories/WellRepository.cs`
- [ ] T010: Generar migración `AddWellsTable`

## Bloque 4 — API (dotnet build GOP.API)
- [ ] T011: Crear `API/Controllers/WellsController.cs`

## Bloque 5 — Tests (dotnet test)
- [ ] T012: Crear `Application.Tests/Features/Wells/Commands/CreateWellCommandHandlerTests.cs`
```

### 19.4. Reglas de bloques

| Regla | Descripción |
|---|---|
| Compilación al final de cada bloque | `dotnet build {Proyecto}` debe pasar. Los bloques siguen el orden de dependencias: Domain → Application → Infrastructure → API. |
| Tests en bloque separado | Los tests se ejecutan después de que todos los proyectos de producción compilen. |
| Tareas `[P]` paralelas | Tareas sin dependencia mutua dentro del mismo bloque pueden ejecutarse en paralelo. |
| Cada tarea afecta un solo archivo | Si se necesitan dos archivos, son dos tareas. |

---

## 20. Reglas Inmutables — Resumen Ejecutivo

Este es el checklist de validación rápida. Cualquier violación es un defecto que debe corregirse antes del merge:

1. **Domain sin dependencias externas.** Solo .NET BCL.
2. **Nunca `throw` para flujo de negocio.** Siempre `Result.Failure()`.
3. **Nunca `try/catch` en handlers.** Las excepciones las atrapa el middleware.
4. **Handlers son `internal sealed`.** No se exponen públicamente.
5. **Commands pasan por repositorio + UoW.** Queries pueden leer directo del DbContext.
6. **`AsNoTracking()` obligatorio en toda Query.** Sin excepciones.
7. **FluentValidation para formato, handler para negocio.** Sin mezclar.
8. **AutoMapper solo Entity → DTO.** Nunca al revés.
9. **DTOs son `sealed record`.** Inmutables.
10. **Controllers máximo 3 líneas por action.** Send + ToActionResult.
11. **Todo error HTTP es ProblemDetails.** RFC 7807 sin excepciones.
12. **Paginación: `page` (1-indexed), `pageSize` (max 100).** Respuesta con `items`, `total`, `page`, `pageSize`.
13. **Todos los endpoints bajo `/api/v1/`.** Versionado por URL.
14. **JWT Bearer obligatorio.** Solo login y health son públicos.
15. **Multi-tenant automático.** Query filters + interceptor de escritura.
16. **Serilog structured logging.** Sin concatenación de strings. Sin datos sensibles.
17. **Tests: mínimo 3 escenarios por handler.** Happy path, error negocio, not found.
18. **Un archivo = una clase.** Sin excepciones.
19. **Migraciones con nombre descriptivo.** Una migración por cambio lógico.
20. **`CancellationToken` siempre propagado.** En todo método async.
