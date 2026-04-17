# Plan Backend: Máquina de Estados del Pozo

**Feature:** 007-well-state-machine
**Contrato:** `specs/features/007-well-state-machine/contract.yml`
**Constitución:** CONSTITUTION.backend.md

---

## 1. Proyectos Afectados

| Proyecto             | Cambios                                                              |
|----------------------|----------------------------------------------------------------------|
| `GOP.Domain`         | Nueva entidad, nuevo enum, value object UWI, errores, interfaz       |
| `GOP.Application`    | Command + Query + Validators + DTOs + Mapping                        |
| `GOP.Infrastructure` | Configuration, Repository, Migration, UwiGenerator                   |
| `GOP.API`            | 2 nuevos actions en WellsController                                  |
| `GOP.Domain.Tests`   | Tests de lógica de transición en Well entity                         |
| `GOP.Application.Tests` | Tests de handlers                                                 |

---

## 2. Entidades de Dominio

### 2.1. Modificación: `Well` (Domain/Entities/Well.cs)

Se agregan métodos de transición de estado y campo UWI a la entidad existente.

```csharp
// Nuevos campos
public string? Uwi { get; private set; }

// Nuevos métodos de dominio
public Result CanTransition(TransitionAction action, string userRole)
public Result ApplyTransition(TransitionAction action, string userRole, string? comment)
public void SetUwi(string uwi)

// Tabla de transiciones válidas (encapsulada en la entidad)
private static readonly Dictionary<(WellStatus From, TransitionAction Action), WellStatus> ValidTransitions = new()
{
    { (WellStatus.Borrador, TransitionAction.Enviar), WellStatus.PendingUwi },
    { (WellStatus.PendingUwi, TransitionAction.AprobarUwi), WellStatus.ReadyFiscal },
    { (WellStatus.PendingUwi, TransitionAction.Devolver), WellStatus.Borrador },
    { (WellStatus.ReadyFiscal, TransitionAction.Fiscalizar), WellStatus.Fiscalizado },
    { (WellStatus.ReadyFiscal, TransitionAction.Devolver), WellStatus.Borrador },
};

// Tabla de roles autorizados por acción
private static readonly Dictionary<TransitionAction, string[]> AuthorizedRoles = new()
{
    { TransitionAction.Enviar, new[] { "ADMIN", "SUPERVISOR", "OPERADOR" } },
    { TransitionAction.AprobarUwi, new[] { "ADMIN", "SUPERVISOR" } },
    { TransitionAction.Devolver, new[] { "ADMIN", "SUPERVISOR" } },
    { TransitionAction.Fiscalizar, new[] { "ADMIN", "SUPERVISOR" } },
};
```

**Regla CONSTITUTION.backend.md §6:** La lógica de negocio (transiciones válidas, RBAC) vive en la entidad de dominio. El handler orquesta pero no decide.

### 2.2. Nueva entidad: `WellTransitionHistory` (Domain/Entities/WellTransitionHistory.cs)

```csharp
public sealed class WellTransitionHistory : Entity
{
    public Guid WellId { get; private init; }
    public WellStatus FromState { get; private init; }
    public WellStatus ToState { get; private init; }
    public TransitionAction Action { get; private init; }
    public string? Comment { get; private init; }       // max 500
    public Guid PerformedByUserId { get; private init; }
    public string PerformedByName { get; private init; } // max 200, denormalized
    public string PerformedByRole { get; private init; } // max 30
    public DateTime CreatedAt { get; private init; }

    // Factory method
    public static WellTransitionHistory Create(
        Guid wellId, WellStatus from, WellStatus to, TransitionAction action,
        string? comment, Guid userId, string userName, string userRole)
}
```

### 2.3. Nuevo enum: `TransitionAction` (Domain/Enums/TransitionAction.cs)

```csharp
public enum TransitionAction
{
    Enviar,
    AprobarUwi,
    Devolver,
    Fiscalizar
}
```

Almacenado como `string` en la DB. Ver CONSTITUTION.backend.md §7.3.

### 2.4. Value Object: `Uwi` (Domain/ValueObjects/Uwi.cs)

```csharp
public sealed record Uwi
{
    public string Value { get; }
    private Uwi(string value) => Value = value;

    // Factory con validación
    public static Result<Uwi> Create(string daneDpto, string daneMpio,
        string denominacion, string consecutivo, string trayectoria)

    // Formato: CO-{daneDpto}-{daneMpio}-{DENOMINACION}-{consecutivo}-{trayectoria}
    // MaxLength: 50
}
```

### 2.5. Errores de dominio: `DomainErrors.Well` (Domain/Errors/DomainErrors.Well.cs)

Nuevos errores a agregar a la clase parcial existente:

```csharp
public static Error InvalidTransition(string action, string currentState) => new(
    "Well.InvalidTransition",
    $"La acción {action} no es válida desde el estado {currentState}.");

public static readonly Error TransitionUnauthorized = new(
    "Well.TransitionUnauthorized",
    "No tiene permisos para ejecutar esta transición.");

public static readonly Error DuplicateUwi = new(
    "Well.DuplicateUwi",
    "Ya existe un pozo con el UWI generado.");

public static readonly Error CommentRequired = new(
    "Well.CommentRequired",
    "El motivo de devolución es requerido.");

public static readonly Error IncompleteWellData = new(
    "Well.IncompleteWellData",
    "El pozo tiene campos requeridos sin completar.");

public static readonly Error FiscalizedImmutable = new(
    "Well.FiscalizedImmutable",
    "No se puede modificar un pozo fiscalizado.");
```

**Patrón:** `Entidad.Accion`. Ver CONSTITUTION.backend.md §6.4.

### 2.6. Interfaz: `IUwiGenerator` (Domain/Interfaces/Services/IUwiGenerator.cs)

```csharp
public interface IUwiGenerator
{
    Task<Result<string>> GenerateAsync(Well well, CancellationToken cancellationToken = default);
}
```

Se define en Domain porque es una regla de negocio (formato UWI). Se implementa en Infrastructure porque necesita acceso a datos (catálogos DANE).

### 2.7. Extensión: `IWellRepository` (Domain/Interfaces/Repositories/IWellRepository.cs)

Método nuevo:

```csharp
Task<bool> ExistsByUwiAsync(string uwi, CancellationToken cancellationToken = default);
```

---

## 3. Application Layer

### 3.1. Command: TransitionWell

```
Features/Wells/Commands/TransitionWell/
├── TransitionWellCommand.cs
├── TransitionWellCommandHandler.cs
└── TransitionWellCommandValidator.cs
```

**Command:**
```csharp
public sealed record TransitionWellCommand(
    Guid WellId,
    string Action,      // "ENVIAR", "APROBAR_UWI", "DEVOLVER", "FISCALIZAR"
    string? Comment
) : IRequest<Result<TransitionResultDto>>;
```

**Handler (lógica):**
1. Buscar pozo por ID (con multi-tenant)
2. Parsear `Action` string → `TransitionAction` enum
3. Llamar `well.CanTransition(action, currentUser.Role)` → valida estado + RBAC
4. Si acción es `ENVIAR` y pozo no tiene UWI → generar UWI via `IUwiGenerator`
5. Verificar unicidad de UWI con `IWellRepository.ExistsByUwiAsync`
6. Llamar `well.ApplyTransition(action, currentUser.Role, command.Comment)`
7. Crear `WellTransitionHistory` record
8. Persistir via `IUnitOfWork.SaveChangesAsync`
9. Retornar `TransitionResultDto`

**Validator:**
```csharp
RuleFor(x => x.Action)
    .NotEmpty()
    .Must(BeValidTransitionAction)
    .WithMessage("La acción no es válida. Valores permitidos: ENVIAR, APROBAR_UWI, DEVOLVER, FISCALIZAR.");

RuleFor(x => x.Comment)
    .MinimumLength(10).When(x => x.Action == "DEVOLVER")
    .WithMessage("El motivo de devolución debe tener al menos 10 caracteres.")
    .MaximumLength(500)
    .WithMessage("El comentario no puede exceder 500 caracteres.");
```

### 3.2. Query: GetWellHistory

```
Features/Wells/Queries/GetWellHistory/
├── GetWellHistoryQuery.cs
├── GetWellHistoryQueryHandler.cs
└── TransitionHistoryItemDto.cs
```

**Query:**
```csharp
public sealed record GetWellHistoryQuery(Guid WellId) : IRequest<Result<IReadOnlyList<TransitionHistoryItemDto>>>;
```

**Handler:**
1. Verificar que el pozo existe (con multi-tenant filter)
2. Consultar `WellTransitionHistory` filtrada por `WellId`, ordenada por `CreatedAt DESC`
3. Mapear a `TransitionHistoryItemDto[]`

### 3.3. DTOs

**TransitionResultDto** (respuesta del PATCH):
```csharp
public sealed record TransitionResultDto(
    Guid Id,
    string Estado,
    string EstadoAnterior,
    string? Uwi,
    string Action,
    string? Comment,
    DateTime TransitionedAt,
    string TransitionedBy
);
```

**TransitionHistoryItemDto** (respuesta del GET history):
```csharp
public sealed record TransitionHistoryItemDto(
    Guid Id,
    string FromState,
    string ToState,
    string Action,
    string? Comment,
    Guid PerformedBy,
    string PerformedByName,
    string PerformedByRole,
    DateTime CreatedAt
);
```

### 3.4. Mapping Profile

```
Features/Wells/Mappings/WellTransitionMappingProfile.cs
```

```csharp
CreateMap<WellTransitionHistory, TransitionHistoryItemDto>();
// TransitionResultDto se construye manualmente en el handler (no es un mapeo 1:1 de entidad)
```

---

## 4. Infrastructure Layer

### 4.1. EF Core Configuration: `WellTransitionHistoryConfiguration`

```csharp
builder.ToTable("WellTransitionHistory");
builder.HasKey(h => h.Id);
builder.Property(h => h.WellId).IsRequired();
builder.Property(h => h.FromState).HasConversion<string>().HasMaxLength(30);
builder.Property(h => h.ToState).HasConversion<string>().HasMaxLength(30);
builder.Property(h => h.Action).HasConversion<string>().HasMaxLength(30);
builder.Property(h => h.Comment).HasMaxLength(500);
builder.Property(h => h.PerformedByUserId).IsRequired();
builder.Property(h => h.PerformedByName).IsRequired().HasMaxLength(200);
builder.Property(h => h.PerformedByRole).IsRequired().HasMaxLength(30);
builder.Property(h => h.CreatedAt).IsRequired();
builder.HasIndex(h => h.WellId);                        // Consultas frecuentes por pozo
builder.HasOne<Well>().WithMany().HasForeignKey(h => h.WellId).OnDelete(DeleteBehavior.Cascade);
```

### 4.2. Modificación: `WellConfiguration`

Agregar campo UWI:
```csharp
builder.Property(w => w.Uwi).HasMaxLength(50);
builder.HasIndex(w => w.Uwi).IsUnique().HasFilter("Uwi IS NOT NULL");  // Unique pero permite nulls
```

### 4.3. Migración

Nombre: `AddWellTransitionHistoryAndUwi`

Cambios:
1. Agregar columna `Uwi` (nullable, max 50) a tabla `Wells`
2. Agregar índice único filtrado en `Wells.Uwi` (WHERE Uwi IS NOT NULL)
3. Crear tabla `WellTransitionHistory` con FK a `Wells`

### 4.4. UwiGenerator (Infrastructure/Services/UwiGenerator.cs)

Implementa `IUwiGenerator`. Resuelve códigos DANE consultando las tablas de catálogo:

```csharp
public async Task<Result<string>> GenerateAsync(Well well, CancellationToken ct)
{
    // 1. Obtener datos de ubicación del pozo (daneDpto, daneMpio)
    // 2. Normalizar denominación (UPPERCASE, remove diacritics)
    // 3. Construir: CO-{daneDpto}-{daneMpio}-{DENOMINACION}-{consecutivo}-{trayectoria}
    // 4. Retornar Result.Success(uwi)
}
```

### 4.5. Extensión: `WellRepository`

Agregar implementación de `ExistsByUwiAsync`:
```csharp
public async Task<bool> ExistsByUwiAsync(string uwi, CancellationToken ct)
    => await _context.Wells.AnyAsync(w => w.Uwi == uwi, ct);
```

### 4.6. Extensión: `GopDbContext`

Agregar `DbSet<WellTransitionHistory>`:
```csharp
public DbSet<WellTransitionHistory> WellTransitionHistory => Set<WellTransitionHistory>();
```

---

## 5. API Layer

### 5.1. WellsController — Nuevos Actions

```csharp
// PATCH /api/v1/wells/{id}/transition
[HttpPatch("{id:guid}/transition")]
[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]
[ProducesResponseType(typeof(TransitionResultDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
public async Task<IActionResult> TransitionWell(Guid id, TransitionWellRequest request)
    => (await sender.Send(new TransitionWellCommand(id, request.Action, request.Comment))).ToActionResult();

// GET /api/v1/wells/{id}/history
[HttpGet("{id:guid}/history")]
[Authorize]
[ProducesResponseType(typeof(IReadOnlyList<TransitionHistoryItemDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetWellHistory(Guid id)
    => (await sender.Send(new GetWellHistoryQuery(id))).ToActionResult();
```

**Nota:** El request body del PATCH se recibe como `TransitionWellRequest` (record del API layer) y se mapea al Command. Esto desacopla el contrato HTTP de la capa Application. Ver CONSTITUTION.backend.md §13.3.

```csharp
// API/Contracts/TransitionWellRequest.cs
public sealed record TransitionWellRequest(string Action, string? Comment);
```

### 5.2. Extensión: `ResultExtensions`

Agregar mapeo para `*.InvalidTransition` → 409 Conflict:

```csharp
var c when c.Contains(".InvalidTransition") => new ConflictObjectResult(
    CreateProblemDetails(409, "Conflict", result.Error.Message)),
```

---

## 6. Tests Mínimos Requeridos

### 6.1. Domain Tests

**Archivo:** `GOP.Domain.Tests/Entities/WellTransitionTests.cs`

| Test | Escenario |
|------|-----------|
| `CanTransition_BorradorEnviar_ReturnsSuccess` | Happy path |
| `CanTransition_PendingUwiEnviar_ReturnsFailure` | Transición inválida |
| `CanTransition_OperadorAprobarUwi_ReturnsUnauthorized` | RBAC |
| `CanTransition_FiscalizadoAny_ReturnsFailure` | Estado terminal |
| `ApplyTransition_Devolver_RequiresComment` | Validación de comentario |

**Archivo:** `GOP.Domain.Tests/ValueObjects/UwiTests.cs`

| Test | Escenario |
|------|-----------|
| `Create_ValidData_ReturnsFormattedUwi` | Formato correcto |
| `Create_DenominacionWithAccents_NormalizesToAscii` | Normalización |
| `Create_ResultExceeds50Chars_ReturnsFailure` | MaxLength |

### 6.2. Application Tests

**Archivo:** `GOP.Application.Tests/Features/Wells/Commands/TransitionWellCommandHandlerTests.cs`

| Test | Escenario |
|------|-----------|
| `Handle_EnviarFromBorrador_ReturnsSuccessAndGeneratesUwi` | Happy path ENVIAR |
| `Handle_AprobarUwiFromPendingUwi_ReturnsSuccess` | Happy path APROBAR_UWI |
| `Handle_DevolverWithComment_ReturnsSuccess` | Happy path DEVOLVER |
| `Handle_FiscalizarFromReadyFiscal_ReturnsSuccess` | Happy path FISCALIZAR |
| `Handle_InvalidTransition_ReturnsConflict` | Transición inválida |
| `Handle_WellNotFound_ReturnsNotFound` | 404 |
| `Handle_DuplicateUwi_ReturnsConflict` | UWI duplicado |
| `Handle_EnviarResubmitWithExistingUwi_PreservesUwi` | Re-envío post devolución |

**Archivo:** `GOP.Application.Tests/Features/Wells/Queries/GetWellHistoryQueryHandlerTests.cs`

| Test | Escenario |
|------|-----------|
| `Handle_WellWithHistory_ReturnsOrderedList` | Happy path |
| `Handle_WellWithoutHistory_ReturnsEmptyList` | Sin historial |
| `Handle_WellNotFound_ReturnsNotFound` | 404 |

---

## 7. Árbol de Archivos (resumen)

```
backend/src/
├── GOP.Domain/
│   ├── Entities/
│   │   ├── Well.cs                          # MODIFICAR: agregar Uwi, métodos de transición
│   │   └── WellTransitionHistory.cs         # NUEVO
│   ├── Enums/
│   │   └── TransitionAction.cs              # NUEVO
│   ├── ValueObjects/
│   │   └── Uwi.cs                           # NUEVO
│   ├── Errors/
│   │   └── DomainErrors.Well.cs             # MODIFICAR: agregar errores de transición
│   └── Interfaces/
│       ├── Repositories/
│       │   └── IWellRepository.cs           # MODIFICAR: agregar ExistsByUwiAsync
│       └── Services/
│           └── IUwiGenerator.cs             # NUEVO
│
├── GOP.Application/
│   └── Features/Wells/
│       ├── Commands/TransitionWell/
│       │   ├── TransitionWellCommand.cs     # NUEVO
│       │   ├── TransitionWellCommandHandler.cs # NUEVO
│       │   └── TransitionWellCommandValidator.cs # NUEVO
│       ├── Queries/GetWellHistory/
│       │   ├── GetWellHistoryQuery.cs       # NUEVO
│       │   ├── GetWellHistoryQueryHandler.cs # NUEVO
│       │   └── TransitionHistoryItemDto.cs  # NUEVO
│       └── Mappings/
│           └── WellTransitionMappingProfile.cs # NUEVO
│
├── GOP.Infrastructure/
│   ├── Persistence/
│   │   ├── GopDbContext.cs                  # MODIFICAR: agregar DbSet
│   │   ├── Configurations/
│   │   │   ├── WellConfiguration.cs         # MODIFICAR: agregar Uwi column + index
│   │   │   └── WellTransitionHistoryConfiguration.cs # NUEVO
│   │   ├── Repositories/
│   │   │   └── WellRepository.cs            # MODIFICAR: agregar ExistsByUwiAsync
│   │   └── Migrations/
│   │       └── {timestamp}_AddWellTransitionHistoryAndUwi.cs # NUEVO (generada)
│   └── Services/
│       └── UwiGenerator.cs                  # NUEVO
│
├── GOP.API/
│   ├── Controllers/
│   │   └── WellsController.cs              # MODIFICAR: agregar 2 actions
│   ├── Contracts/
│   │   └── TransitionWellRequest.cs         # NUEVO
│   └── Extensions/
│       └── ResultExtensions.cs              # MODIFICAR: agregar mapeo 409

tests/
├── GOP.Domain.Tests/
│   ├── Entities/
│   │   └── WellTransitionTests.cs          # NUEVO
│   └── ValueObjects/
│       └── UwiTests.cs                     # NUEVO
└── GOP.Application.Tests/
    └── Features/Wells/
        ├── Commands/
        │   └── TransitionWellCommandHandlerTests.cs # NUEVO
        └── Queries/
            └── GetWellHistoryQueryHandlerTests.cs   # NUEVO
```
