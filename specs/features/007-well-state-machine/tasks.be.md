# Tareas Backend: Máquina de Estados del Pozo

**Feature:** 007-well-state-machine
**Plan:** `specs/features/007-well-state-machine/plan.be.md`
**Contrato:** `specs/features/007-well-state-machine/contract.yml`

---

## Bloque 1 — Domain (`dotnet build GOP.Domain`)

Entidades, enums, value objects, errores e interfaces. Sin dependencias externas.

- [x] **T001** `[P]` Crear `GOP.Domain/Enums/TransitionAction.cs`
  - Enum con valores: `Enviar`, `AprobarUwi`, `Devolver`, `Fiscalizar`
  - Almacenado como string (conversión explícita en EF config)

- [x] **T002** `[P]` Crear `GOP.Domain/ValueObjects/Uwi.cs`
  - Record inmutable con propiedad `Value` (string, max 50)
  - Factory method `Create(daneDpto, daneMpio, denominacion, consecutivo, trayectoria)` → `Result<Uwi>`
  - Formato: `CO-{daneDpto}-{daneMpio}-{DENOMINACION}-{consecutivo}-{trayectoria}`
  - Normalización: denominación a UPPERCASE, remoción de diacríticos (á→A, ñ→N)
  - Validación: resultado no excede 50 caracteres

- [x] **T003** `[P]` Crear `GOP.Domain/Entities/WellTransitionHistory.cs`
  - Hereda de `Entity` (Id: Guid)
  - Propiedades: WellId, FromState (WellStatus), ToState (WellStatus), Action (TransitionAction), Comment (string?, max 500), PerformedByUserId (Guid), PerformedByName (string, max 200), PerformedByRole (string, max 30), CreatedAt (DateTime)
  - Factory method estático `Create(...)` que instancia y valida

- [x] **T004** Modificar `GOP.Domain/Errors/DomainErrors.cs` — sección `Well`
  - Agregar errores: `InvalidTransition(action, currentState)`, `TransitionUnauthorized`, `DuplicateUwi`, `CommentRequired`, `IncompleteWellData`, `FiscalizedImmutable`
  - Patrón de código: `Well.InvalidTransition`, `Well.TransitionUnauthorized`, etc.

- [x] **T005** Crear `GOP.Domain/Interfaces/Services/IUwiGenerator.cs`
  - Interfaz: `Task<Result<string>> GenerateAsync(Well well, CancellationToken ct)`
  - Definida en Domain (regla de negocio), implementada en Infrastructure

- [x] **T006** Modificar `GOP.Domain/Interfaces/Repositories/IWellRepository.cs`
  - Agregar método: `Task<bool> ExistsByUwiAsync(string uwi, CancellationToken ct)`
  - Nota: archivo creado nuevo (no existía previamente)

- [x] **T007** Modificar `GOP.Domain/Entities/Well.cs`
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

- [x] **T008** `[P]` Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionWellCommand.cs`
  - Record: `TransitionWellCommand(Guid WellId, string Action, string? Comment) : IRequest<Result<TransitionResultDto>>`

- [x] **T009** `[P]` Crear `GOP.Application/Features/Wells/Queries/GetWellHistory/TransitionHistoryItemDto.cs`
  - Sealed record: `TransitionHistoryItemDto(Guid Id, string FromState, string ToState, string Action, string? Comment, Guid PerformedBy, string PerformedByName, string PerformedByRole, DateTime CreatedAt)`

- [x] **T010** `[P]` Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionResultDto.cs`
  - Sealed record: `TransitionResultDto(Guid Id, string Estado, string EstadoAnterior, string? Uwi, string Action, string? Comment, DateTime TransitionedAt, string TransitionedBy)`
  - Nota: Archivo separado del Command per CONSTITUTION.backend.md §14.3 (1 archivo = 1 clase)

- [x] **T011** Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionWellCommandValidator.cs`
  - Valida `Action`: NotEmpty, debe ser uno de ENVIAR/APROBAR_UWI/DEVOLVER/FISCALIZAR
  - Valida `Comment`: MinLength(10) cuando Action == "DEVOLVER", MaxLength(500) siempre
  - Valida `WellId`: NotEmpty
  - Mensajes en español

- [x] **T012** Crear `GOP.Application/Features/Wells/Commands/TransitionWell/TransitionWellCommandHandler.cs`
  - Inyecta: `IApplicationDbContext`, `IUnitOfWork`, `ICurrentUserService`, `IUwiGenerator`
  - Flujo implementado según plan.be.md §3.1

- [x] **T013** `[P]` Crear `GOP.Application/Features/Wells/Queries/GetWellHistory/GetWellHistoryQuery.cs`
  - Record: `GetWellHistoryQuery(Guid WellId) : IRequest<Result<IReadOnlyList<TransitionHistoryItemDto>>>`

- [x] **T014** Crear `GOP.Application/Features/Wells/Queries/GetWellHistory/GetWellHistoryQueryHandler.cs`
  - Inyecta: `IApplicationDbContext`, `IMapper`
  - Flujo: verificar pozo existe → query historial DESC → mapear → retornar

- [x] **T015** `[P]` Crear `GOP.Application/Features/Wells/Mappings/WellTransitionMappingProfile.cs`
  - Mapeo: `WellTransitionHistory` → `TransitionHistoryItemDto`
  - Conversión de enums a string en el mapeo

**Verificación:** `dotnet build GOP.Application` ✅

---

## Bloque 3 — Infrastructure (`dotnet build GOP.Infrastructure`)

Configuraciones EF, repositorio, servicio UWI, migración. Depende de Bloque 2.

- [x] **T016** Crear `GOP.Infrastructure/Persistence/Configurations/WellTransitionHistoryConfiguration.cs`
  - Tabla: `WellTransitionHistory`, enums como string, índice en WellId, FK cascade

- [x] **T017** Modificar `GOP.Infrastructure/Persistence/Configurations/WellConfiguration.cs`
  - Agregar: columna `Uwi` (nvarchar(50), nullable)
  - Agregar: índice único filtrado `[Uwi] IS NOT NULL`

- [x] **T018** Modificar `GOP.Infrastructure/Persistence/GopDbContext.cs`
  - Agregar: `public DbSet<WellTransitionHistory> WellTransitionHistory => Set<WellTransitionHistory>();`

- [x] **T019** Crear `GOP.Infrastructure/Persistence/Repositories/WellRepository.cs`
  - Implementa `IWellRepository` con `Add`, `GetByIdAsync`, `ExistsByUwiAsync`

- [x] **T020** Crear `GOP.Infrastructure/Services/UwiGenerator.cs`
  - Implementa `IUwiGenerator`
  - Usa códigos DANE desnormalizados en `Well.Location`
  - Delega formato/validación al value object `Uwi.Create(...)`

- [x] **T021** Registrar `IUwiGenerator` + `IWellRepository` en DI
  - Modificar `GOP.Infrastructure/DependencyInjection.cs`

- [x] **T022** Generar migración `AddWellTransitionHistoryAndUwi`
  - Comando: `dotnet ef migrations add AddWellTransitionHistoryAndUwi -p src/GOP.Infrastructure -s src/GOP.API`
  - Archivo generado: `20260417031110_AddWellTransitionHistoryAndUwi.cs`
  - Cambios: ALTER TABLE Wells (Uwi col + unique index filtrado), CREATE TABLE WellTransitionHistory con FK cascade

**Verificación:** `dotnet build GOP.Infrastructure` ✅

---

## Bloque 4 — API (`dotnet build GOP.API`)

Controller actions y request contracts. Depende de Bloque 3.

- [x] **T023** `[P]` Crear `GOP.API/Contracts/TransitionWellRequest.cs`
  - Sealed record: `TransitionWellRequest(string Action, string? Comment)`
  - Desacopla contrato HTTP del Command de Application

- [x] **T024** Modificar `GOP.API/Controllers/WellsController.cs`
  - Agregar action `TransitionWell`: `[HttpPatch("{id:guid}/transition")]`
  - Agregar action `GetWellHistory`: `[HttpGet("{id:guid}/history")]`

- [x] **T025** Modificar `GOP.API/Extensions/ResultExtensions.cs`
  - Agregar pattern matching para `*.InvalidTransition` → `409 Conflict`
  - Agregar pattern matching para `*.DuplicateUwi` → `409 Conflict` (ya cubierto por `.Duplicate`)
  - Agregar pattern matching para `*.FiscalizedImmutable` → `409 Conflict`
  - Agregar pattern matching para `*.TransitionUnauthorized` → `403 Forbidden`

**Verificación:** `dotnet build GOP.API` ✅

---

## Bloque 5 — Tests (`dotnet test`)

Tests unitarios de dominio y application. Depende de Bloque 4.

- [x] **T026** `[P]` Crear `GOP.Domain.Tests/ValueObjects/UwiTests.cs`
  - `Create_ValidData_ReturnsFormattedUwi` ✅
  - `Create_DenominacionWithAccents_NormalizesToAscii` ✅
  - `Create_ResultExceeds50Chars_ReturnsFailure` ✅
  - `Create_LowercaseDenominacion_ConvertsToUppercase` ✅

- [x] **T027** `[P]` Crear `GOP.Domain.Tests/Entities/WellTransitionTests.cs`
  - `CanTransition_BorradorEnviar_ReturnsSuccess` ✅
  - `CanTransition_PendingUwiAprobarUwi_ReturnsSuccess` ✅
  - `CanTransition_PendingUwiEnviar_ReturnsInvalidTransition` ✅
  - `CanTransition_OperadorAprobarUwi_ReturnsUnauthorized` ✅
  - `CanTransition_AuditorEnviar_ReturnsUnauthorized` ✅
  - `CanTransition_FiscalizadoAnyAction_ReturnsFailure` ✅
  - `ApplyTransition_DevolverWithComment_ChangesStateToBorrador` ✅
  - `ApplyTransition_DevolverWithoutComment_ReturnsCommentRequired` ✅

- [x] **T028** `[P]` Crear `GOP.Application.Tests/Features/Wells/Commands/TransitionWellCommandHandlerTests.cs`
  - `Handle_EnviarFromBorrador_ReturnsSuccessAndGeneratesUwi` ✅
  - `Handle_AprobarUwiFromPendingUwi_ReturnsSuccess` ✅
  - `Handle_DevolverWithComment_ReturnsSuccessWithComment` ✅
  - `Handle_FiscalizarFromReadyFiscal_ReturnsSuccess` ✅
  - `Handle_InvalidTransition_ReturnsConflict` ✅
  - `Handle_WellNotFound_ReturnsNotFound` ✅
  - `Handle_DuplicateUwi_ReturnsConflict` ✅
  - `Handle_ResubmitWithExistingUwi_PreservesUwi` ✅

- [x] **T029** `[P]` Crear `GOP.Application.Tests/Features/Wells/Queries/GetWellHistoryQueryHandlerTests.cs`
  - `Handle_WellWithHistory_ReturnsOrderedList` ✅
  - `Handle_WellWithoutHistory_ReturnsEmptyList` ✅
  - `Handle_WellNotFound_ReturnsNotFound` ✅

**Verificación:** `dotnet test` ✅ — 80/80 tests pasando (Domain: 21, Application: 43, Infrastructure: 1, API: 15)

---

## Resumen de Tareas

| Bloque | Tareas    | Archivos nuevos | Archivos modificados | Verificación              |
|--------|-----------|-----------------|----------------------|---------------------------|
| 1      | T001–T007 | 4               | 3                    | `dotnet build GOP.Domain` ✅ |
| 2      | T008–T015 | 8               | 1 (IApplicationDbContext) | `dotnet build GOP.Application` ✅ |
| 3      | T016–T022 | 3               | 4                    | `dotnet build GOP.Infrastructure` ✅ |
| 4      | T023–T025 | 1               | 2                    | `dotnet build GOP.API` ✅ |
| 5      | T026–T029 | 4               | 1 (TestDbContext)    | `dotnet test` ✅ |
| **Total** | **29** | **20**          | **11**               | ✅ **80/80 tests** |
