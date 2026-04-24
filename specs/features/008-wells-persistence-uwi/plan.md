# Plan Técnico — Iter 8 · Creación de Pozo Nuevo (Persistencia + UWI)

**Feature ID:** `008-wells-persistence-uwi`
**Entrada:** `blueprint.md` de esta misma carpeta + specs V2 en `/mnt/uploads/`.
**Audiencia:** GOP-Spec → GOP-Backend → GOP-Frontend → GOP-QA → GOP-Docs agents.

---

## 1. Panorama general

### 1.1 Capas afectadas (Clean Architecture)

```
┌───────────────────────────────────────────────────────────┐
│ Api (Gop.Api)                                              │
│  • WellsController: POST, GET list, GET detail, POST       │
│    preview-uwi                                             │
│  • Problem Details handler: mapea exceptions → RFC 7807    │
├────────────────────────────────────────────────────────────┤
│ Application (Gop.Application)                              │
│  • Commands: CreateWell                                     │
│  • Queries: GetWellById, ListWells, PreviewUwi             │
│  • Validators (FluentValidation)                            │
│  • DTOs: WellCreateRequest, WellDetailResponse,            │
│    PreviewUwiRequest/Response, WellListItem                │
│  • Services: IUwiGenerator, IWellNameComposer (interfaces) │
├────────────────────────────────────────────────────────────┤
│ Domain (Gop.Domain)                                        │
│  • Entities extendidas: Well (nuevas props + VOs)          │
│  • Value Objects: WellName, FiscalizedUwi, UwiComponents   │
│  • Enums: TrajectoryType, ClassificationType,              │
│    WellStatus, AngleType, ObjectiveType, CompletionType    │
│  • Domain Services: UwiGenerator, WellNameComposer         │
│    (implementaciones — lógica pura, sin I/O)               │
├────────────────────────────────────────────────────────────┤
│ Infrastructure (Gop.Infrastructure)                        │
│  • EF Core Configuration: WellConfiguration actualizada    │
│  • Migration: AddWellPersistenceColumns                    │
│  • Global Query Filter: por OperatorId                     │
│  • Repositorios: WellRepository (implementa IWellRepo)     │
└────────────────────────────────────────────────────────────┘
```

### 1.2 Principio rector
Extender, no reescribir. La Well entity ya existe desde Iter 3. Se le añaden propiedades, no se parte de cero.

---

## 2. Domain

### 2.1 Extensión de `Well` entity

`src/Gop.Domain/Entities/Wells/Well.cs` (archivo existente, extender):

**Propiedades actuales (Iter 3) a conservar:**
```csharp
public Guid Id { get; private set; }
public string Name { get; private set; }          // legacy — ver §2.1.2
public Guid OperatorId { get; private set; }
public Guid ContractId { get; private set; }
public WellLocation Location { get; private set; } // owned
// ... resto
```

**Propiedades a añadir:**

```csharp
// Identidad pozo fiscalizado
public WellName WellName { get; private set; }            // VO
public FiscalizedUwi FiscalizedUwi { get; private set; }  // VO

// Clasificación
public TrajectoryType TrajectoryType { get; private set; }
public ClassificationType Classification { get; private set; }
public string? SubClassificationCode { get; private set; }   // null en Iter 8

// Nombre compositivo (campos fuente)
public string Denomination { get; private set; }
public string Consecutive { get; private set; }              // "157", solo dígitos
public Guid? ParentWellId { get; private set; }              // null en Iter 8

// Campo y ubicación extendida
public Guid? FieldId { get; private set; }                   // null si RN-11/14
public AngleType AngleType { get; private set; }
public ObjectiveType Objective { get; private set; }
public CompletionType CompletionType { get; private set; }
public Guid ClusterLocationId { get; private set; }

// Estado y lifecycle
public WellStatus Status { get; private set; }              // DRAFT | CREATED (Iter 8 solo CREATED)
public bool IsLegacyWell { get; private set; }              // false en Iter 8
public bool IsFinalized { get; private set; }               // true en Iter 8 (single-shot)

// Auditoría estándar (ya presente desde Iter 3)
public DateTimeOffset CreatedAt { get; private set; }
public string CreatedBy { get; private set; }
public DateTimeOffset UpdatedAt { get; private set; }
public string UpdatedBy { get; private set; }
```

**Factory method (reemplaza el constructor actual):**

```csharp
public static Well CreateNew(
    WellName name,
    FiscalizedUwi uwi,
    Guid operatorId,
    Guid contractId,
    TrajectoryType trajectory,
    ClassificationType classification,
    string denomination,
    string consecutive,
    Guid? fieldId,
    AngleType angleType,
    ObjectiveType objective,
    CompletionType completion,
    WellLocation location,
    Guid clusterLocationId,
    string createdBy,
    IDateTimeProvider clock)
{
    // Validaciones de invariantes de dominio (RN-17, RN-18)
    if (ContainsReservedSigla(denomination))
        throw new DomainException(DomainErrorCodes.ForbiddenSiglaInName, ...);
    if (!consecutive.All(char.IsDigit))
        throw new DomainException(DomainErrorCodes.InvalidConsecutive, ...);
    
    return new Well {
        Id = Guid.NewGuid(),
        WellName = name,
        FiscalizedUwi = uwi,
        // ... resto
        Status = WellStatus.Created,
        IsFinalized = true,
        CreatedAt = clock.UtcNow,
        CreatedBy = createdBy,
        UpdatedAt = clock.UtcNow,
        UpdatedBy = createdBy
    };
}
```

#### 2.1.1 Decisión sobre `Name` (legacy) vs `WellName` (nuevo)

La propiedad `Name` de Iter 3 se mantiene por compatibilidad con el seed y los 54 tests existentes. En Iter 8:
- Al crear vía `CreateNew`, `Name` se setea a `WellName.Value` (mismo string).
- La lógica de composición usa `WellName`, no `Name`.
- **Deuda:** retirar `Name` en Iter 10 cuando se haga el state machine — entonces se renombra todo a `WellName`.

### 2.2 Value Objects

`src/Gop.Domain/Entities/Wells/ValueObjects/WellName.cs`:

```csharp
public sealed class WellName : ValueObject
{
    public string Value { get; }
    
    private WellName(string value) => Value = value;
    
    public static WellName Compose(
        string denomination,
        string consecutive,
        TrajectoryType trajectory)
    {
        // Iter 8: solo trayectoria O — sufijo "O"
        // Trayectorias futuras: sufijo según RN-04 a RN-07
        var suffix = trajectory switch {
            TrajectoryType.Original => "O",
            TrajectoryType.Perforation => "P",
            TrajectoryType.Multilateral => "ML",
            TrajectoryType.Sidetrack => throw new NotSupportedInIter8Exception(...),
            TrajectoryType.Preproducer => throw new NotSupportedInIter8Exception(...),
            TrajectoryType.Gas => throw new NotSupportedInIter8Exception(...),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        // Formato según spec §4.1.1 ejemplo: "RUBIALES 157O"
        var composed = $"{denomination.Trim().ToUpperInvariant()} {consecutive}{suffix}";
        
        return new WellName(composed);
    }
    
    public static WellName FromPersisted(string value) => new(value);
    
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    
    public override string ToString() => Value;
}
```

`src/Gop.Domain/Entities/Wells/ValueObjects/FiscalizedUwi.cs`:

```csharp
public sealed class FiscalizedUwi : ValueObject
{
    public string Value { get; }
    public UwiComponents Components { get; }
    
    private FiscalizedUwi(string value, UwiComponents components)
    {
        Value = value;
        Components = components;
    }
    
    public static FiscalizedUwi FromComponents(UwiComponents c)
    {
        // Formato PPDM: [Dpto][Mpio][Sigla][Num][Cluster][Angle][Trajectory][Objective]–[Completion]
        var body = $"{c.DepartmentCode}{c.MunicipalityCode}{c.WellNameSigle}{c.WellNumber}{c.ClusterCode}{c.AngleCode}{c.TrajectoryCode}{c.ObjectiveCode}";
        var value = $"{body}–{c.CompletionCode}";
        return new FiscalizedUwi(value, c);
    }
    
    public static FiscalizedUwi FromPersisted(string value, UwiComponents components) => new(value, components);
    
    protected override IEnumerable<object> GetEqualityComponents() { yield return Value; }
    
    public override string ToString() => Value;
}
```

`src/Gop.Domain/Entities/Wells/ValueObjects/UwiComponents.cs`:

```csharp
public sealed class UwiComponents : ValueObject
{
    public string DepartmentCode { get; }    // 2 dígitos DANE (ej. "50")
    public string MunicipalityCode { get; }  // 3 dígitos DANE (ej. "568")
    public string WellNameSigle { get; }     // 4 chars (primeras 4 letras denominación) (ej. "RUBI")
    public string WellNumber { get; }        // 4 dígitos zero-padded (ej. "0157")
    public string ClusterCode { get; }       // 6 chars (primeras 2 letras cluster + 4 dígitos) (ej. "RU0000")
    public string AngleCode { get; }         // 1 char (V/H/D/J)
    public string TrajectoryCode { get; }    // 1 char (O/ST/P/PR/ML/G → primera letra en Iter 8)
    public string ObjectiveCode { get; }     // 2 chars (PH/PA/IW/etc.)
    public string CompletionCode { get; }    // 2 chars (CC/OH/etc.)
    
    // Factory + equality
}
```

### 2.3 Enums

`src/Gop.Domain/Entities/Wells/Enums/`:

```csharp
public enum TrajectoryType { Original = 1, Sidetrack = 2, Preproducer = 3, Multilateral = 4, Gas = 5, Perforation = 6 }

public enum ClassificationType { Development = 1, Exploratory = 2, Stratigraphic = 3 }

public enum WellStatus { Draft = 1, Created = 2 }

public enum AngleType { Vertical = 1, Horizontal = 2, Directional = 3, HighAngle = 4 }

public enum ObjectiveType { ProducerHydrocarbons = 1, WaterInjector = 2, GasInjector = 3, /* ... */ }

public enum CompletionType { CasingCementedPerforated = 1, OpenHole = 2, /* ... */ }
```

**Regla:** los enums incluyen **todos** los valores del V2 spec. Los métodos de dominio que no los soportan en Iter 8 lanzan `NotSupportedInIter8Exception` (excepción específica, no `NotImplementedException` — señal clara de deuda intencional).

### 2.4 Domain Services

`src/Gop.Domain/Services/UwiGenerator.cs`:

```csharp
public interface IUwiGenerator
{
    UwiComponents BuildComponents(UwiGenerationInput input);
    FiscalizedUwi Generate(UwiGenerationInput input);
}

public sealed class UwiGenerator : IUwiGenerator
{
    public UwiComponents BuildComponents(UwiGenerationInput input)
    {
        // Iter 8: solo rama Original + Development
        var sigle = ComputeWellNameSigle(input.Denomination);
        var number = input.Consecutive.PadLeft(4, '0');
        var clusterCode = ComputeClusterCode(input.ClusterLocationName);
        var trajectoryCode = input.Trajectory switch {
            TrajectoryType.Original => "O",
            _ => throw new NotSupportedInIter8Exception(...)
        };
        
        return new UwiComponents(
            departmentCode: input.DepartmentDaneCode,    // "50"
            municipalityCode: input.MunicipalityDaneCode, // "568"
            wellNameSigle: sigle,
            wellNumber: number,
            clusterCode: clusterCode,
            angleCode: input.Angle.ToCode(),
            trajectoryCode: trajectoryCode,
            objectiveCode: input.Objective.ToCode(),
            completionCode: input.Completion.ToCode()
        );
    }
    
    public FiscalizedUwi Generate(UwiGenerationInput input) =>
        FiscalizedUwi.FromComponents(BuildComponents(input));
    
    private static string ComputeWellNameSigle(string denomination)
    {
        // Normalizar: quitar acentos, mayúsculas, sin espacios
        var normalized = RemoveDiacritics(denomination.Trim().ToUpperInvariant())
            .Replace(" ", "");
        // Tomar 4 primeras letras; si no llega a 4, pad con 'X'
        return (normalized + "XXXX").Substring(0, 4);
    }
    
    private static string ComputeClusterCode(string clusterLocationName)
    {
        // Primeras 2 letras del nombre + padding 4 dígitos
        // En Iter 8: padding fijo "0000" porque no hay numeración de cluster dentro del catálogo
        // Deuda: Iter 10, cuando haya auditoría de clusters, reemplazar por número real
        var normalized = RemoveDiacritics(clusterLocationName.Trim().ToUpperInvariant())
            .Replace(" ", "");
        var prefix = (normalized + "XX").Substring(0, 2);
        return prefix + "0000";
    }
}
```

**Ejemplo verificado contra spec V2:**
- Input: `Denomination="RUBIALES", Consecutive="157", Trajectory=Original, Dpto="50", Mpio="568", Cluster="RUBIALES A", Angle=Vertical, Objective=PH, Completion=CC`
- Sigle: `"RUBI"` (primeras 4 de "RUBIALES")
- Number: `"0157"` (padleft 4)
- ClusterCode: `"RU0000"` (primeras 2 de "RUBIALES A" sin espacios = "RU" + "0000")
- AngleCode: `"V"`
- TrajectoryCode: `"O"`
- ObjectiveCode: `"PH"`
- CompletionCode: `"CC"`
- UWI: `"50568RUBI0157RU0000VOPH–CC"` ≈ spec example `"50568RUBI0157RU0000VOOPH–CC"` ⚠️

**⚠️ Discrepancia a resolver con GOP-Spec:** el ejemplo del spec tiene `VOOPH` (3 chars: angle + trajectory + objective) donde mi decodificación da `VOPH` (4 chars: angle V + trajectory O + objective PH). Hipótesis: el objetivo `OOPH` es un código de 4 chars (no 2) y el spec del api-contract miente en los componentes. **Acción:** spec-agent revisa el catálogo `catalog.WellObjective` real en BD (Iter 3 seed) y confirma si los códigos son 2 o 4 chars. Plan B: ajustar `UwiComponents.ObjectiveCode` al tamaño real del catálogo.

### 2.5 Domain Errors

`src/Gop.Domain/Errors/DomainErrorCodes.cs`:

```csharp
public static class DomainErrorCodes
{
    public const string ForbiddenSiglaInName = "FORBIDDEN_SIGLA_IN_NAME";
    public const string InvalidConsecutive = "VALIDATION_ERROR";
    public const string DuplicateWellName = "DUPLICATE_WELL_NAME";
    public const string DuplicateUwi = "DUPLICATE_UWI";
    // ... resto
}
```

---

## 3. Application

### 3.1 Commands

`src/Gop.Application/Wells/Commands/CreateWell/CreateWellCommand.cs`:

```csharp
public sealed record CreateWellCommand(
    Guid ContractId,
    string TrajectoryTypeCode,       // "O"
    Guid? ParentWellId,              // null en Iter 8
    string ClassificationCode,       // "DEVELOPMENT"
    string? SubClassificationCode,   // null
    string Denomination,
    string Consecutive,
    Guid? FieldId,
    string AngleTypeCode,
    string ObjectiveCode,
    string CompletionTypeCode,
    string DepartmentDaneCode,
    string MunicipalityDaneCode,
    Guid ClusterLocationId
) : IRequest<CreateWellResult>;

public sealed record CreateWellResult(
    Guid Id,
    string WellName,
    string FiscalizedUwi,
    string Status,            // "CREATED"
    Guid OperatorId,
    string OperatorName,
    DateTimeOffset CreatedAt,
    string CreatedBy
);
```

**Handler responsabilities:**
1. Validar input (delegado a `CreateWellValidator`).
2. Bloquear trayectorias ≠ `O` y clasificaciones ≠ `DEVELOPMENT` con `NotSupportedInIter8Exception` → controlador traduce a `422 NOT_IMPLEMENTED_IN_ITER_8`.
3. Resolver `OperatorId` desde `ICurrentUserService` (JWT).
4. Componer `WellName` vía `IWellNameComposer`.
5. Generar `FiscalizedUwi` vía `IUwiGenerator`.
6. Verificar unicidad (nombre y UWI) — query a repo; si duplicado, lanzar excepción específica.
7. Instanciar `Well` vía factory.
8. Persistir vía `IWellRepository.Add(well)`.
9. `await _unitOfWork.SaveChangesAsync(cancellationToken)` — aquí puede saltar DB-unique-violation → catch → traducir.
10. Retornar `CreateWellResult`.

### 3.2 Queries

```csharp
public sealed record GetWellByIdQuery(Guid Id) : IRequest<WellDetailResponse>;

public sealed record ListWellsQuery(
    int Page = 1,
    int PageSize = 20,
    string Sort = "createdAt:desc",
    string? Status = null,
    Guid? ContractId = null,
    string? WellName = null,
    string? FiscalizedUwi = null,
    string? ClassificationCode = null,
    bool? IsLegacyWell = null
) : IRequest<PagedResult<WellListItem>>;

public sealed record PreviewUwiQuery(
    string DepartmentDaneCode,
    string MunicipalityDaneCode,
    string Denomination,
    string Consecutive,
    string ClusterLocationName,
    string AngleTypeCode,
    string TrajectoryTypeCode,
    string ObjectiveCode,
    string CompletionTypeCode,
    string ClassificationCode
) : IRequest<PreviewUwiResponse>;
```

**`PreviewUwi` handler:**
1. Valida input.
2. Bloquea trayectorias/clasificaciones no soportadas.
3. Llama `IUwiGenerator.BuildComponents(input)` → `UwiComponents`.
4. Construye `FiscalizedUwi.FromComponents(...)`.
5. Consulta repo: `await _wellRepo.ExistsWithUwiAsync(uwi.Value)`.
6. Retorna `PreviewUwiResponse { FiscalizedUwi, WellName, Components, IsUnique, ConflictingUwi }`.

### 3.3 Validators (FluentValidation)

`src/Gop.Application/Wells/Commands/CreateWell/CreateWellValidator.cs`:

```csharp
public class CreateWellValidator : AbstractValidator<CreateWellCommand>
{
    public CreateWellValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Denomination)
            .NotEmpty()
            .MaximumLength(100)
            .Must(d => !ContainsReservedSigla(d))
            .WithErrorCode("FORBIDDEN_SIGLA_IN_NAME")
            .WithMessage("No se permiten las siglas ST, G, PR, P, ML ni la palabra PILOTO");
        RuleFor(x => x.Consecutive)
            .NotEmpty()
            .Matches(@"^\d+$")
            .WithErrorCode("VALIDATION_ERROR");
        RuleFor(x => x.TrajectoryTypeCode)
            .NotEmpty()
            .Must(c => c == "O")
            .WithErrorCode("NOT_IMPLEMENTED_IN_ITER_8")
            .WithMessage("Solo trayectoria Original (O) está soportada en esta iteración");
        RuleFor(x => x.ClassificationCode)
            .NotEmpty()
            .Must(c => c == "DEVELOPMENT")
            .WithErrorCode("NOT_IMPLEMENTED_IN_ITER_8");
        RuleFor(x => x.DepartmentDaneCode).Matches(@"^\d{2}$");
        RuleFor(x => x.MunicipalityDaneCode).Matches(@"^\d{3}$");
        RuleFor(x => x.FieldId).NotEmpty();  // DEVELOPMENT requiere campo
        RuleFor(x => x.ClusterLocationId).NotEmpty();
    }
    
    private static bool ContainsReservedSigla(string denomination)
    {
        var reserved = new[] { "ST", "G", "PR", "P", "ML", "PILOTO" };
        var tokens = denomination.ToUpperInvariant().Split(new[] { ' ', '-' });
        return tokens.Any(t => reserved.Contains(t));
    }
}
```

### 3.4 Interfaces

`src/Gop.Application/Abstractions/IWellRepository.cs`:

```csharp
public interface IWellRepository
{
    Task<Well?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByWellNameAsync(string wellName, Guid contractId, CancellationToken ct);
    Task<bool> ExistsByUwiAsync(string uwi, CancellationToken ct);
    Task<PagedResult<Well>> ListAsync(ListWellsFilter filter, CancellationToken ct);
    void Add(Well well);
}
```

---

## 4. Infrastructure

### 4.1 EF Core Configuration

`src/Gop.Infrastructure/Persistence/Configurations/WellConfiguration.cs` (existente — extender):

```csharp
public class WellConfiguration : IEntityTypeConfiguration<Well>
{
    public void Configure(EntityTypeBuilder<Well> builder)
    {
        builder.ToTable("Wells", schema: "ops");
        builder.HasKey(w => w.Id);

        // Campos existentes (Iter 3)
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.OperatorId).IsRequired();
        builder.Property(w => w.ContractId).IsRequired();
        builder.OwnsOne(w => w.Location, loc => { /* ... */ });

        // NUEVO Iter 8
        builder.OwnsOne(w => w.WellName, wn =>
        {
            wn.Property(x => x.Value).HasColumnName("WellNameValue").HasMaxLength(200).IsRequired();
            wn.HasIndex(x => x.Value).IsUnique().HasDatabaseName("UX_Wells_WellName");
        });

        builder.OwnsOne(w => w.FiscalizedUwi, uwi =>
        {
            uwi.Property(x => x.Value).HasColumnName("FiscalizedUwi").HasMaxLength(64).IsRequired();
            uwi.HasIndex(x => x.Value).IsUnique().HasDatabaseName("UX_Wells_FiscalizedUwi");
            
            uwi.OwnsOne(x => x.Components, c =>
            {
                c.Property(x => x.DepartmentCode).HasColumnName("UwiDepartmentCode").HasMaxLength(2);
                c.Property(x => x.MunicipalityCode).HasColumnName("UwiMunicipalityCode").HasMaxLength(3);
                c.Property(x => x.WellNameSigle).HasColumnName("UwiWellNameSigle").HasMaxLength(4);
                c.Property(x => x.WellNumber).HasColumnName("UwiWellNumber").HasMaxLength(4);
                c.Property(x => x.ClusterCode).HasColumnName("UwiClusterCode").HasMaxLength(6);
                c.Property(x => x.AngleCode).HasColumnName("UwiAngleCode").HasMaxLength(1);
                c.Property(x => x.TrajectoryCode).HasColumnName("UwiTrajectoryCode").HasMaxLength(4);
                c.Property(x => x.ObjectiveCode).HasColumnName("UwiObjectiveCode").HasMaxLength(4);
                c.Property(x => x.CompletionCode).HasColumnName("UwiCompletionCode").HasMaxLength(4);
            });
        });

        builder.Property(w => w.TrajectoryType).HasConversion<int>();
        builder.Property(w => w.Classification).HasConversion<int>();
        builder.Property(w => w.Status).HasConversion<int>();
        builder.Property(w => w.AngleType).HasConversion<int>();
        builder.Property(w => w.Objective).HasConversion<int>();
        builder.Property(w => w.CompletionType).HasConversion<int>();

        builder.Property(w => w.Denomination).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Consecutive).HasMaxLength(10).IsRequired();
        builder.Property(w => w.FieldId);
        builder.Property(w => w.ParentWellId);
        builder.Property(w => w.SubClassificationCode).HasMaxLength(20);
        builder.Property(w => w.ClusterLocationId).IsRequired();
        builder.Property(w => w.IsLegacyWell).IsRequired();
        builder.Property(w => w.IsFinalized).IsRequired();

        // Índices adicionales
        builder.HasIndex(w => new { w.ContractId, w.Denomination, w.Consecutive })
               .HasDatabaseName("IX_Wells_Contract_Denom_Consec");
        builder.HasIndex(w => w.OperatorId).HasDatabaseName("IX_Wells_OperatorId");

        // Global query filter — tenant isolation
        builder.HasQueryFilter(w => w.OperatorId == _currentUserService.OperatorId);
    }
}
```

### 4.2 Migration

**Nombre:** `AddWellPersistenceColumns`
**Generación:** `dotnet ef migrations add AddWellPersistenceColumns -c ApplicationDbContext -p src/Gop.Infrastructure -s src/Gop.Api`

**Contenido esperado:**
- `ALTER TABLE ops.Wells` añadiendo 20+ columnas nuevas (VOs + enums + 8 UWI components).
- `CREATE UNIQUE INDEX UX_Wells_WellName` y `UX_Wells_FiscalizedUwi`.
- `CREATE INDEX IX_Wells_Contract_Denom_Consec` y `IX_Wells_OperatorId`.

**Validación en CI:** `dotnet ef migrations script` genera SQL idempotente. Revisar que no haga `DROP`.

### 4.3 Global Query Filter por OperatorId

El filter requiere `ICurrentUserService.OperatorId`. Ya existe desde Iter 7 con `UserId`. **Extensión:** añadir `OperatorId` como claim en el JWT (backend-agent añade al `AuthService.GenerateToken`).

**Path crítico:** durante seed al arranque, `UserSeeder` debe guardar el `OperatorId` del usuario seed (mapearlo a un Operator ya sembrado en catálogos). Si el seed no tiene operator → el filtro excluye todo → smoke test falla.

### 4.4 Repositorio

`src/Gop.Infrastructure/Persistence/Repositories/WellRepository.cs`:

```csharp
public class WellRepository : IWellRepository
{
    private readonly ApplicationDbContext _db;
    
    public Task<Well?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Wells
            .Include(w => w.Location)  // owned — automático
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    
    public Task<bool> ExistsByWellNameAsync(string wellName, Guid contractId, CancellationToken ct) =>
        _db.Wells.IgnoreQueryFilters()  // check cross-tenant para nombres globales
            .AnyAsync(w => w.WellName.Value == wellName, ct);
    
    public Task<bool> ExistsByUwiAsync(string uwi, CancellationToken ct) =>
        _db.Wells.IgnoreQueryFilters()
            .AnyAsync(w => w.FiscalizedUwi.Value == uwi, ct);
    
    // ... ListAsync, Add
}
```

**Nota tenant isolation:** `ExistsByWellNameAsync` usa `IgnoreQueryFilters()` porque el nombre compuesto debe ser único **globalmente** (si un día se habilitan reportes cross-operadora, los nombres duplicados revientan). Mismo para UWI.

---

## 5. API

### 5.1 Controller

`src/Gop.Api/Controllers/WellsController.cs`:

```csharp
[ApiController]
[Route("api/v1/wells")]
[Authorize]
public class WellsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost]
    [Authorize(Roles = "OperatorAgent,AnhGopAdministrator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateWellRequest body,
        CancellationToken ct)
    {
        var cmd = body.ToCommand();
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListWellsQueryParams q, CancellationToken ct) =>
        Ok(await _mediator.Send(q.ToQuery(), ct));
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWellByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
    
    [HttpPost("preview-uwi")]
    public async Task<IActionResult> PreviewUwi(
        [FromBody] PreviewUwiRequest body,
        CancellationToken ct) =>
        Ok(await _mediator.Send(body.ToQuery(), ct));
}
```

### 5.2 Problem Details

`src/Gop.Api/Middleware/ExceptionHandlingMiddleware.cs` (existente desde Iter 2 — extender):

| Exception | HTTP | Code | Mensaje |
|-----------|------|------|---------|
| `ValidationException` (FluentValidation) | 400 | `VALIDATION_ERROR` | Detalles por campo |
| `UnauthorizedException` | 401 | `UNAUTHORIZED` | — |
| `ForbiddenException` | 403 | `FORBIDDEN` | — |
| `NotFoundException` | 404 | `NOT_FOUND` | — |
| `DuplicateWellNameException` | 409 | `DUPLICATE_WELL_NAME` | `{field}={value}` |
| `DuplicateUwiException` | 409 | `DUPLICATE_UWI` | — |
| `NotSupportedInIter8Exception` | 422 | `NOT_IMPLEMENTED_IN_ITER_8` | Feature diferida a Iter X |
| `DomainException` (rest) | 422 | `BUSINESS_RULE_VIOLATION` | — |
| `SqlException (2601/2627)` | 409 | `DUPLICATE_UWI` | Fallback si el check pre-insert no lo capturó |
| Unhandled | 500 | `INTERNAL_ERROR` | Sin stack trace en prod |

Todas las respuestas siguen RFC 7807 (`application/problem+json`), consistente con el resto del stack (Iter 7).

---

## 6. Frontend

### 6.1 Servicios

`src/app/features/wells/services/wells.service.ts` (nuevo o extender si existe desde Iter 4):

```typescript
@Injectable({ providedIn: 'root' })
export class WellsService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL_TOKEN);

  createWell(payload: CreateWellRequest): Observable<CreateWellResponse> {
    return this.http.post<CreateWellResponse>(`${this.apiUrl}/api/v1/wells`, payload);
  }

  previewUwi(payload: PreviewUwiRequest): Observable<PreviewUwiResponse> {
    return this.http.post<PreviewUwiResponse>(`${this.apiUrl}/api/v1/wells/preview-uwi`, payload);
  }

  listWells(params: ListWellsParams): Observable<PagedResponse<WellListItem>> {
    return this.http.get<PagedResponse<WellListItem>>(`${this.apiUrl}/api/v1/wells`, { params });
  }

  getWellById(id: string): Observable<WellDetailResponse> {
    return this.http.get<WellDetailResponse>(`${this.apiUrl}/api/v1/wells/${id}`);
  }
}
```

### 6.2 Wizard: wire-up del preview

`src/app/features/wells/pages/well-creation-wizard/well-creation-wizard.component.ts`:

**Cambios requeridos:**
1. Paso 3 (Preview): disparar `previewUwi()` al entrar al paso con los valores del form state.
2. Mostrar spinner mientras carga; si error, mostrar toast y no avanzar.
3. Al darle *Crear*: llamar `createWell()` con el mismo payload (el BE recalcula UWI server-side — el preview era informativo).
4. On success: navegar a `/wells/{id}` con toast de confirmación.
5. On error `DUPLICATE_WELL_NAME` / `DUPLICATE_UWI`: mostrar inline error en el wizard paso 3 con opción de "Ver el pozo existente" si el error response trae `conflictingUwi` + id.

### 6.3 Fix cascada departamento → municipio

**Bug actual:** al volver del paso 3 al paso 2, el `municipalityDaneCode` se pierde aunque el departamento siga seleccionado.

**Diagnóstico esperado:** el signal/computed del municipio está derivado de `departmentCode$` pero se reinicia al re-construirse el componente del paso 2. Se pierde la referencia al valor previo.

**Fix propuesto:**
- Mover el state del wizard a un signal-based store (`WellWizardStore`) que persista mientras dure el wizard.
- El select de municipio lee su valor inicial del store al montarse.
- El evento `onChange` actualiza el store, no solo el form local.

**Sin rediseñar la arquitectura reactiva.** Si el frontend-agent descubre que el fix es más profundo, para y crea ticket.

### 6.4 Vista lista de pozos

`src/app/features/wells/pages/wells-list/wells-list.component.ts`:
- Tabla PrimeNG `<p-table>` con paginación server-side (query params `page`, `pageSize`).
- Columnas: Nombre, UWI, Estado, Contrato, Campo, Creado.
- Click en fila → navega a `/wells/{id}`.
- Reutiliza el mock list que ya existe desde Iter 3 — solo cambia el source.

### 6.5 Vista detalle

`src/app/features/wells/pages/well-detail/well-detail.component.ts`:
- Layout de 2 columnas PrimeNG con cards para cada sección (Identidad, Clasificación, Ubicación, UWI Components, Auditoría).
- Read-only en Iter 8 (la edición llega en Iter 10).

### 6.6 Ruteo

`src/app/features/wells/wells.routes.ts`:
```typescript
export const wellsRoutes: Routes = [
  { path: '', redirectTo: 'list', pathMatch: 'full' },
  { path: 'list', component: WellsListComponent, canActivate: [authGuard] },
  { path: 'new', component: WellCreationWizardComponent, canActivate: [authGuard, roleGuard(['OperatorAgent'])] },
  { path: ':id', component: WellDetailComponent, canActivate: [authGuard] }
];
```

---

## 7. Contrato API (subset operativo de Iter 8)

### 7.1 `POST /api/v1/wells`

**Request** — respeta V2 spec §4.1:
```json
{
  "contractId": "uuid",
  "trajectoryTypeCode": "O",
  "parentWellId": null,
  "classificationCode": "DEVELOPMENT",
  "subClassificationCode": null,
  "denomination": "RUBIALES",
  "consecutive": "157",
  "fieldId": "uuid",
  "angleTypeCode": "V",
  "objectiveCode": "PH",
  "completionTypeCode": "CC",
  "departmentDaneCode": "50",
  "municipalityDaneCode": "568",
  "clusterLocationId": "uuid"
}
```

**Response 201** — respeta V2 spec §4.1:
```json
{
  "id": "uuid",
  "wellName": "RUBIALES 157O",
  "fiscalizedUwi": "50568RUBI0157RU0000VOPH–CC",
  "status": "CREATED",
  "operatorId": "uuid",
  "operatorName": "Ecopetrol S.A.",
  "createdAt": "2026-04-23T14:30:00Z",
  "createdBy": "alejandro.gutierrez@interkont.co"
}
```

**Errores soportados en Iter 8:**
- `400 VALIDATION_ERROR` — consecutivo con letras, payload malformado
- `401 UNAUTHORIZED`
- `403 FORBIDDEN`
- `409 DUPLICATE_WELL_NAME`
- `409 DUPLICATE_UWI`
- `422 FORBIDDEN_SIGLA_IN_NAME`
- `422 NOT_IMPLEMENTED_IN_ITER_8` (trayectoria ≠ O o clasificación ≠ DEVELOPMENT)

### 7.2 `GET /api/v1/wells`

Paginación y filtros como V2 spec §4.2. **Todos los filtros del spec se implementan** (son triviales en EF Core).

### 7.3 `GET /api/v1/wells/{id}`

Shape respeta V2 spec §4.3 al pie de la letra. Los campos no aplicables en Iter 8 van con valor por defecto:
- `parentWellId: null`
- `parentWellName: null`
- `subClassificationCode: null`
- `hasFiledForm101: false` (hardcoded en Iter 8)
- `currentStateCode: "REGISTERED"` (hardcoded)
- `currentStateName: "Registrado"`

### 7.4 `POST /api/v1/wells/preview-uwi`

Request/Response exactos de V2 spec §4.7.

### 7.5 Endpoints NO implementados en Iter 8 (responden 501)

| Endpoint | Respuesta en Iter 8 |
|----------|---------------------|
| `PUT /wells/{id}` | `501 NOT_IMPLEMENTED` — Iter 10 |
| `DELETE /wells/{id}` | `501 NOT_IMPLEMENTED` — Iter 10 |
| `POST /wells/{id}/finalize` | `501 NOT_IMPLEMENTED` — Iter 10 |
| `GET /wells/{id}/parent-well-data` | `501 NOT_IMPLEMENTED` — Iter 10 |

Se registran los routes con handler genérico para que el consumidor vea el 501 en lugar de 404 (mejor señal de "existe, no está listo").

---

## 8. Base de datos

### 8.1 Schema modificado — `ops.Wells`

Columnas añadidas (migration `AddWellPersistenceColumns`):

| Columna | Tipo | Null | Default | Índice |
|---------|------|------|---------|--------|
| `WellNameValue` | `nvarchar(200)` | NO | — | `UX_Wells_WellName` (unique) |
| `FiscalizedUwi` | `nvarchar(64)` | NO | — | `UX_Wells_FiscalizedUwi` (unique) |
| `UwiDepartmentCode` | `nvarchar(2)` | NO | — | — |
| `UwiMunicipalityCode` | `nvarchar(3)` | NO | — | — |
| `UwiWellNameSigle` | `nvarchar(4)` | NO | — | — |
| `UwiWellNumber` | `nvarchar(4)` | NO | — | — |
| `UwiClusterCode` | `nvarchar(6)` | NO | — | — |
| `UwiAngleCode` | `nvarchar(1)` | NO | — | — |
| `UwiTrajectoryCode` | `nvarchar(4)` | NO | — | — |
| `UwiObjectiveCode` | `nvarchar(4)` | NO | — | — |
| `UwiCompletionCode` | `nvarchar(4)` | NO | — | — |
| `TrajectoryType` | `int` | NO | — | — |
| `Classification` | `int` | NO | — | — |
| `SubClassificationCode` | `nvarchar(20)` | YES | NULL | — |
| `Denomination` | `nvarchar(100)` | NO | — | compuesto con ContractId, Consecutive |
| `Consecutive` | `nvarchar(10)` | NO | — | idem |
| `FieldId` | `uniqueidentifier` | YES | NULL | — |
| `ParentWellId` | `uniqueidentifier` | YES | NULL | — |
| `AngleType` | `int` | NO | — | — |
| `Objective` | `int` | NO | — | — |
| `CompletionType` | `int` | NO | — | — |
| `ClusterLocationId` | `uniqueidentifier` | NO | — | FK a `core.ClusterLocations` |
| `Status` | `int` | NO | `2` (Created) | — |
| `IsLegacyWell` | `bit` | NO | `0` | — |
| `IsFinalized` | `bit` | NO | `1` | — |

### 8.2 Seeds

- No se añaden filas nuevas de pozos (Iter 8 espera crearlos vía UI).
- **Sí se verifica** que existan catálogos mínimos para demo: al menos 1 Operator, 1 Contract, 1 Field (DEVELOPMENT), 1 ClusterLocation, 2 departamentos + 6 municipios DANE.
- Si faltan, el spec-agent los añade al `CatalogSeeder` existente (no archivo nuevo).

### 8.3 Fix catálogo de objetivos (si discrepancia UWI §2.4)

Si el code real de objetivo es 4 chars en vez de 2, el seed debe actualizarse. Spec-agent documenta en EMERGENT-DECISIONS.md.

---

## 9. Testing

### 9.1 Unit (Domain + Application)

`tests/Gop.Domain.Tests/Wells/UwiGeneratorTests.cs`:
- 8+ casos con inputs reales: RUBIALES/157/O → UWI esperado.
- Casos borde: denominación < 4 chars, con acentos, con espacios múltiples.
- Caso unsupported: trayectoria ST → `NotSupportedInIter8Exception`.

`tests/Gop.Application.Tests/Wells/CreateWellCommandHandlerTests.cs`:
- Happy path: mock repo, command → handler → factory → repo.Add.
- Duplicate name → `DuplicateWellNameException`.
- Duplicate UWI → `DuplicateUwiException`.
- Trayectoria ≠ O → `ValidationException` con código `NOT_IMPLEMENTED_IN_ITER_8`.
- Denominación con sigla reservada → `ValidationException` con código `FORBIDDEN_SIGLA_IN_NAME`.

### 9.2 Integration (Api + Infrastructure)

`tests/Gop.Api.IntegrationTests/Wells/WellsControllerTests.cs`:
- `WebApplicationFactory<Program>` con SQLite in-memory.
- Autentica como OperatorAgent fake (JWT con operatorId conocido).
- Test 1: POST happy path → 201 + GET devuelve el pozo.
- Test 2: POST duplicate UWI → 409.
- Test 3: GET de un pozo que existe en otra operadora → 404 (tenant isolation).
- Test 4: Preview UWI → 200 con UWI coincidente.
- Test 5: List paginado → respeta `page`/`pageSize`.

### 9.3 E2E (staging)

GOP-QA ejecuta manual post-deploy:
1. Login con `alejandro.gutierrez@interkont.co`.
2. Navegar a `/wells/new`.
3. Wizard: completar 18 campos — trayectoria Original, clasificación Desarrollo.
4. Volver al paso 2 — verificar municipio NO se perdió.
5. Continuar a paso 3 — verificar preview UWI muestra valor.
6. Click Crear — verificar redirect a `/wells/{id}`.
7. Verificar lista muestra el pozo recién creado.
8. Cerrar sesión, loguear como otro OperatorAgent → la lista NO muestra el pozo (tenant isolation).

Resultado esperable: todos los pasos verdes. Evidencias en screenshots dentro de `specs/features/008-wells-persistence-uwi/evidence/`.

### 9.4 Cobertura mínima

- Domain: 85%
- Application: 80%
- Global: 60%

CI rompe si `coverlet` reporta por debajo.

---

## 10. Despliegue

1. **Local:** `dotnet ef database update` + `dotnet run --project src/Gop.Api`.
2. **CI:** workflow existente (`backend-ci.yml`) corre tests + build + publica imagen a ACR.
3. **Staging manual:** Alejandro ejecuta `./scripts/ops/deploy-staging.sh` que hace:
   - `az acr build` si imagen no existe.
   - `az webapp config container set` con la imagen nueva.
   - `az webapp restart` + espera real a que el container viejo termine (script mejorado post-Iter 7).
   - Smoke test `curl /health/ready` + `curl /api/v1/wells` con JWT.

**Preflight obligatorio:** verificar que `kv-gop360-staging` tenga `ConnectionStrings--DefaultConnection`, `JwtSettings--SigningKey`, `SeedUsers--ExecAdmin--Password`, `SeedUsers--OpAdmin--Password`. Si falta alguno, `UserSeeder` rompe y `/health/ready` queda `Unhealthy`.

---

## 11. Deuda técnica de Iter 8 (para incluir en `EMERGENT-DECISIONS.md`)

Se esperan al menos estas entradas (puede añadirse más durante ejecución):

| # | Deuda | Origen | Iter objetivo |
|---|-------|--------|---------------|
| 1 | `Well.Name` (legacy Iter 3) coexiste con `Well.WellName` (nueva Iter 8). Retirar `Name`. | §2.1.1 | 10 |
| 2 | `ClusterCode` hardcoded como `"{prefix}0000"`. Reemplazar con numeración real cuando haya varios clusters por location. | §2.4 | 10 |
| 3 | `NotSupportedInIter8Exception` lanzada en 6 lugares (trayectorias/clasificaciones no soportadas). Reemplazar con lógica real. | §2.3, §2.4 | 10-12 |
| 4 | Endpoints `PUT`/`DELETE`/`finalize`/`parent-well-data` responden 501 hardcoded. Reemplazar con implementación. | §7.5 | 10 |
| 5 | `ExistsByWellNameAsync` ignora query filter — si multi-tenancy avanza con reportes cross-operadora, verificar que el nombre global sigue siendo el dominio correcto. | §4.4 | 9 |
| 6 | Global query filter no cubre operaciones admin futuras (ej: ANH cross-operadora). | §3.5, §4.3 | 9 |
| 7 | Catálogos sembrados manualmente vs. integración real SOLAR/VCH/VPAA. | §2.4, §8.2 | 13+ |

---

## 12. Enlaces

- Blueprint: `./blueprint.md`
- Tasks breakdown: `./tasks.md`
- Specs V2: `/mnt/uploads/{spec-frontend,spec-backend,api-contract,openapi}.{md,yaml}`
- Iter 7 EMERGENT-DECISIONS (patrón): `specs/features/007-users-persistence/EMERGENT-DECISIONS.md`
- Iter 4 wizard (código base FE): `src/app/features/wells/pages/well-creation-wizard/`
- Iter 3 Well entity + catálogos (código base Domain): `src/Gop.Domain/Entities/Wells/Well.cs`

---

*Este plan es contrato técnico. Los agentes pueden proponer alternativas mejores, pero deben documentarlas en EMERGENT-DECISIONS.md antes de mergear.
