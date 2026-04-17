# Tareas Backend: Máquina de Estados del Pozo

**Feature:** 007-well-state-machine
**Plan:** `specs/features/007-well-state-machine/plan.be.md`
**Contrato:** `specs/features/007-well-state-machine/contract.yml`

---

## Bloque 1 — Domain (`dotnet build GOP.Domain`)

Entidades, enums, value objects, errores e interfaces. Sin dependencias externas.

- [ ] **T001** `[P]` Crear `GOP.Domain/Enums/TransitionAction.cs`
  - Enum con valores: `Enviar`, `AprobarUwi`, `Devolver`, `Fiscalizar`
  - Almacenado como string (conversión explícita en EF config)

- [ ] **T002** `[P]` Crear `GOP.Domain/ValueObjects/Uwi.cs`
  - Record inmutable con propiedad `Value` (string, max 50)
  - Factory method `Create(daneDpto, daneMpio, denominacion, consecutivo, trayectoria)` → `Result<Uwi>`
  - Formato: `CO-{daneDpto}-{daneMpio}-{DENOMINACION}-{consecutivo}-{trayectoria}`
  - Normalización: denominación a UPPERCASE, remoción de diacríticos (á→A, ñ→N)
  - Validación: resultado no excede 50 caracteres

- [ ] **T003** `[P]` Crear `GOP.Domain/Entities/WellTransitionHistory.cs`
  - Hereda de `Entity` (Id: Guid)
  - Propiedades: WellId, FromState (WellStatus), ToState (WellStatus), Action (TransitionAction), Comment (string?, max 500), PerformedByUserId (Guid), PerformedByName (string, max 200), PerformedByRole (string, max 30), CreatedAt (DateTime)
  - Factory method estático `Create(...)` que instancia y valida

- [ ] **T004** Modificar `GOP.Domain/Errors/DomainErrors.cs` — sección `Well`
  - Agregar errores: `InvalidTransition(action, currentState)`, `TransitionUnauthorized`, `DuplicateUwi`, `CommentRequired`, `IncompleteWellData`, `FiscalizedImmutable`
  - Patrón de código: `Well.InvalidTransition`, `Well.TransitionUnauthorized`, etc.

- [ ] **T005** Crear `GOP.Domain/Interfaces/Services/IUwiGenerator.cs`
  - Interfaz: `Task<Result<string>> GenerateAsync(Well well, CancellationToken ct)`
  - Definida en Domain (regla de negocio), implementada en Infrastructure

- [ ] **T006** Modificar `GOP.Domain/Interfaces/Repositories/IWellRepository.cs`
  - Agregar método: `Task<bool> ExistsByUwiAsync(string uwi, CancellationToken ct)`

- [ ] **T007** Modificar `GOP.Domain/Entities/Well.cs`
  - Agregar propiedad: `string? Uwi { get; private set; }`
  - Agregar método: `void SetUwi(string uwi)`
  - Agregar diccionario estático `ValidTransitions`: `Dictionary<(WellStatus, TransitionAction), WellStatus>`
  - Agregar diccionario estático `AuthorizedRoles`: `Dictionary<TransitionAction, string[]>`
  - Agregar método: `Result CanTransition(TransitionAction action, string userRole)` — valida estado + RBAC
  - Agregar método: `Result ApplyTransition(TransitionAction action, string userRole, string? comment)` — ejecuta CanTransition, cambia Status, retorna Result
  - Regla: si action es DEVOLVER y comment es null/empty → `Result.Failure(DomainErrors.Well.CommentRequired)`
  - Regla: si Status es Fiscalizado → `Result.Failure(DomainErrors.Well.FiscalizedImmutable)` para cualquier acción

**Verificación:** `dotnet build GOP.Domain` ✅

---

## Bloque 2 — Application (`dotnet build GOP.Application`)

Commands, queries, validators, DTOs y mappings. Depende de Bloque 1.

- [ ] **T008** `[P]` Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionWellCommand.cs`
  - Record: `TransitionWellCommand(Guid WellId, string Action, string? Comment) : IRequest<Result<TransitionResultDto>>`

- [ ] **T009** `[P]` Crear `GOP.Application/Features/Wells/Queries/GetWellHistory/TransitionHistoryItemDto.cs`
  - Sealed record: `TransitionHistoryItemDto(Guid Id, string FromState, string ToState, string Action, string? Comment, Guid PerformedBy, string PerformedByName, string PerformedByRole, DateTime CreatedAt)`

- [ ] **T010** `[P]` Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionResultDto.cs`
  - Sealed record: `TransitionResultDto(Guid Id, string Estado, string EstadoAnterior, string? Uwi, string Action, string? Comment, DateTime TransitionedAt, string TransitionedBy)`
  - Nota: Archivo separado del Command per CONSTITUTION.backend.md §14.3 (1 archivo = 1 clase)

- [ ] **T011** Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionWellCommandValidator.cs`
  - Valida `Action`: NotEmpty, debe ser uno de ENVIAR/APROBAR_UWI/DEVOLVER/FISCALIZAR
  - Valida `Comment`: MinLength(10) cuando Action == "DEVOLVER", MaxLength(500) siempre
  - Valida `WellId`: NotEmpty
  - Mensajes en español

- [ ] **T012** Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionWellCommandHandler.cs`
  - Inyecta: `IWellRepository`, `IUnitOfWork`, `ICurrentUserService`, `IUwiGenerator`, `IApplicationDbContext`
  - Flujo:
    1. Buscar well por ID (retorna NotFound si no existe)
    2. Parsear Action string → TransitionAction enum (retorna Failure si inválido)
    3. Si action == Enviar y well.Uwi == null → llamar `uwiGenerator.GenerateAsync(well)`
    4. Si UWI generado → verificar unicidad con `wellRepository.ExistsByUwiAsync`
    5. Si action == Enviar → validar campos completos del pozo (contratoId, campoId, etc.)
    6. Llamar `well.ApplyTransition(action, currentUser.Role, command.Comment)`
    7. Si UWI generado → `well.SetUwi(uwi)`
    8. Crear `WellTransitionHistory.Create(...)` con datos del usuario actual
    9. Agregar history al DbContext
    10. `SaveChangesAsync`
    11. Retornar `TransitionResultDto`

- [ ] **T013** `[P]` Crear `GOP.Application/Features/Wells/Queries/GetWellHistory/GetWellHistoryQuery.cs`
  - Record: `GetWellHistoryQuery(Guid WellId) : IRequest<Result<IReadOnlyList<TransitionHistoryItemDto>>>`

- [ ] **T014** Crear `GOP.Application/Features/Wells/Queries/GetWellHistory/GetWellHistoryQueryHandler.cs`
  - Inyecta: `IApplicationDbContext`, `IMapper`
  - Flujo:
    1. Verificar que el pozo existe: `dbContext.Wells.AnyAsync(w => w.Id == request.WellId)`
    2. Si no existe → `Result.Failure(DomainErrors.Well.NotFound)`
    3. Query: `dbContext.WellTransitionHistory.Where(h => h.WellId == request.WellId).OrderByDescending(h => h.CreatedAt).AsNoTracking()`
    4. Mapear a `TransitionHistoryItemDto[]` con AutoMapper
    5. Retornar Result.Success

- [ ] **T015** `[P]` Crear `GOP.Application/Features/Wells/Mappings/WellTransitionMappingProfile.cs`
  - Mapeo: `WellTransitionHistory` → `TransitionHistoryItemDto`
  - Conversión de enums a string en el mapeo

**Verificación:** `dotnet build GOP.Application` ✅

---

## Bloque 3 — Infrastructure (`dotnet build GOP.Infrastructure`)

Configuraciones EF, repositorio, servicio UWI, migración. Depende de Bloque 2.

- [ ] **T016** Crear `GOP.Infrastructure/Persistence/Configurations/WellTransitionHistoryConfiguration.cs`
  - Tabla: `WellTransitionHistory`
  - Key: `Id`
  - FromState, ToState: string conversion, maxLength 30
  - Action: string conversion, maxLength 30
  - Comment: maxLength 500, nullable
  - PerformedByUserId: required
  - PerformedByName: required, maxLength 200
  - PerformedByRole: required, maxLength 30
  - CreatedAt: required
  - Index en WellId
  - FK: WellId → Wells.Id, cascade delete

- [ ] **T017** Modificar `GOP.Infrastructure/Persistence/Configurations/WellConfiguration.cs`
  - Agregar: `builder.Property(w => w.Uwi).HasMaxLength(50);`
  - Agregar: `builder.HasIndex(w => w.Uwi).IsUnique().HasFilter("[Uwi] IS NOT NULL");`

- [ ] **T018** Modificar `GOP.Infrastructure/Persistence/GopDbContext.cs`
  - Agregar: `public DbSet<WellTransitionHistory> WellTransitionHistory => Set<WellTransitionHistory>();`

- [ ] **T019** Modificar `GOP.Infrastructure/Persistence/Repositories/WellRepository.cs`
  - Agregar implementación de `ExistsByUwiAsync`:
    ```csharp
    public async Task<bool> ExistsByUwiAsync(string uwi, CancellationToken ct)
        => await _context.Wells.AnyAsync(w => w.Uwi == uwi, ct);
    ```

- [ ] **T020** Crear `GOP.Infrastructure/Services/UwiGenerator.cs`
  - Implementa `IUwiGenerator`
  - Inyecta `IApplicationDbContext` para resolver códigos DANE del departamento y municipio del pozo
  - Flujo:
    1. Obtener WellLocation del pozo (departamentoId, municipioId)
    2. Query catálogos: departamento.CodigoDane, municipio.CodigoDane
    3. Normalizar denominación: UPPERCASE, remover diacríticos (usando `string.Normalize(NormalizationForm.FormD)` + filtro de categoría Unicode)
    4. Construir: `CO-{daneDpto}-{daneMpio}-{DENOMINACION}-{consecutivo}-{trayectoria}`
    5. Validar longitud ≤ 50
    6. Retornar `Result.Success(uwi)` o `Result.Failure` si excede

- [ ] **T021** Registrar `IUwiGenerator` en DI
  - Modificar `GOP.Infrastructure/DependencyInjection.cs` (o equivalente): `services.AddScoped<IUwiGenerator, UwiGenerator>();`

- [ ] **T022** Generar migración `AddWellTransitionHistoryAndUwi`
  - Comando: `dotnet ef migrations add AddWellTransitionHistoryAndUwi -p src/GOP.Infrastructure -s src/GOP.API`
  - Cambios esperados:
    - ALTER TABLE Wells: agregar columna Uwi (nvarchar(50), nullable)
    - CREATE UNIQUE INDEX IX_Wells_Uwi ON Wells(Uwi) WHERE Uwi IS NOT NULL
    - CREATE TABLE WellTransitionHistory con todas las columnas y FK

**Verificación:** `dotnet build GOP.Infrastructure` ✅

---

## Bloque 4 — API (`dotnet build GOP.API`)

Controller actions y request contracts. Depende de Bloque 3.

- [ ] **T023** `[P]` Crear `GOP.API/Contracts/TransitionWellRequest.cs`
  - Sealed record: `TransitionWellRequest(string Action, string? Comment)`
  - Desacopla contrato HTTP del Command de Application

- [ ] **T024** Modificar `GOP.API/Controllers/WellsController.cs`
  - Agregar action `TransitionWell`:
    - `[HttpPatch("{id:guid}/transition")]`
    - `[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]`
    - `[ProducesResponseType(typeof(TransitionResultDto), 200)]`
    - `[ProducesResponseType(typeof(ProblemDetails), 404)]`
    - `[ProducesResponseType(typeof(ProblemDetails), 409)]`
    - `[ProducesResponseType(typeof(ProblemDetails), 422)]`
    - Body: mapea `TransitionWellRequest` → `TransitionWellCommand`
  - Agregar action `GetWellHistory`:
    - `[HttpGet("{id:guid}/history")]`
    - `[Authorize]`
    - `[ProducesResponseType(typeof(IReadOnlyList<TransitionHistoryItemDto>), 200)]`
    - `[ProducesResponseType(typeof(ProblemDetails), 404)]`

- [ ] **T025** Modificar `GOP.API/Extensions/ResultExtensions.cs`
  - Agregar pattern matching para `*.InvalidTransition` → `409 Conflict`
  - Agregar pattern matching para `*.DuplicateUwi` → `409 Conflict`

**Verificación:** `dotnet build GOP.API` ✅

---

## Bloque 5 — Tests (`dotnet test`)

Tests unitarios de dominio y application. Depende de Bloque 4.

- [ ] **T026** `[P]` Crear `GOP.Domain.Tests/ValueObjects/UwiTests.cs`
  - `Create_ValidData_ReturnsFormattedUwi` — formato CO-50-50568-ALPHA-01-ST
  - `Create_DenominacionWithAccents_NormalizesToAscii` — NIÑO → NINO
  - `Create_ResultExceeds50Chars_ReturnsFailure` — denominación demasiado larga

- [ ] **T027** `[P]` Crear `GOP.Domain.Tests/Entities/WellTransitionTests.cs`
  - `CanTransition_BorradorEnviar_ReturnsSuccess`
  - `CanTransition_PendingUwiAprobarUwi_ReturnsSuccess`
  - `CanTransition_PendingUwiEnviar_ReturnsInvalidTransition` — acción no válida desde ese estado
  - `CanTransition_OperadorAprobarUwi_ReturnsUnauthorized` — RBAC
  - `CanTransition_AuditorEnviar_ReturnsUnauthorized` — RBAC
  - `CanTransition_FiscalizadoAnyAction_ReturnsFailure` — estado terminal
  - `ApplyTransition_DevolverWithComment_ChangesStateToBorrador`
  - `ApplyTransition_DevolverWithoutComment_ReturnsCommentRequired`

- [ ] **T028** `[P]` Crear `GOP.Application.Tests/Features/Wells/Commands/TransitionWellCommandHandlerTests.cs`
  - `Handle_EnviarFromBorrador_ReturnsSuccessAndGeneratesUwi` — happy path
  - `Handle_AprobarUwiFromPendingUwi_ReturnsSuccess`
  - `Handle_DevolverWithComment_ReturnsSuccessWithComment`
  - `Handle_FiscalizarFromReadyFiscal_ReturnsSuccess`
  - `Handle_InvalidTransition_ReturnsConflict` — ej. ENVIAR desde PENDING_UWI
  - `Handle_WellNotFound_ReturnsNotFound`
  - `Handle_DuplicateUwi_ReturnsConflict`
  - `Handle_ResubmitWithExistingUwi_PreservesUwi` — re-envío post devolución

- [ ] **T029** `[P]` Crear `GOP.Application.Tests/Features/Wells/Queries/GetWellHistoryQueryHandlerTests.cs`
  - `Handle_WellWithHistory_ReturnsOrderedList`
  - `Handle_WellWithoutHistory_ReturnsEmptyList`
  - `Handle_WellNotFound_ReturnsNotFound`

**Verificación:** `dotnet test` ✅

---

## Resumen de Tareas

| Bloque | Tareas    | Archivos nuevos | Archivos modificados | Verificación              |
|--------|-----------|-----------------|----------------------|---------------------------|
| 1      | T001–T007 | 4               | 3                    | `dotnet build GOP.Domain` |
| 2      | T008–T015 | 8               | 0                    | `dotnet build GOP.Application` |
| 3      | T016–T022 | 3               | 4                    | `dotnet build GOP.Infrastructure` |
| 4      | T023–T025 | 1               | 2                    | `dotnet build GOP.API`    |
| 5      | T026–T029 | 4               | 0                    | `dotnet test`             |
| **Total** | **29** | **20**          | **9**                |                           |
