# Tareas Backend: 006 — Well Creation Form (Preview Name)

**Feature ID:** 006-well-creation-form
**Plan:** `specs/features/006-well-creation-form/plan.be.md`
**Contrato:** `specs/features/006-well-creation-form/contract.yml`

---

## Bloque 1 — Domain (`dotnet build GOP.Domain`)

- [ ] **T001**: Crear/verificar `Domain/Errors/DomainErrors.Contrato.cs`
  - Agregar clase parcial `DomainErrors.Contrato` con error `NotFound`
  - Si ya existe de iteración 005, verificar que incluya `NotFound` y marcar como completada
  - Archivo: `src/GOP.Domain/Errors/DomainErrors.Contrato.cs`

**Verificación:** `dotnet build src/GOP.Domain`

---

## Bloque 2 — Application (`dotnet build GOP.Application`)

- [ ] **T002** [P]: Crear `Application/Features/Wells/Queries/PreviewWellName/WellNamePreviewDto.cs`
  - `public sealed record WellNamePreviewDto(string NombrePozo, bool Available);`
  - Archivo: `src/GOP.Application/Features/Wells/Queries/PreviewWellName/WellNamePreviewDto.cs`

- [ ] **T003** [P]: Crear `Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQuery.cs`
  - Record inmutable con: `ContratoId (int)`, `Denominacion (string)`, `Consecutivo (string)`, `ExcludeWellId (Guid?)`
  - Implementa `IRequest<Result<WellNamePreviewDto>>`
  - Archivo: `src/GOP.Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQuery.cs`

- [ ] **T004**: Crear `Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQueryHandler.cs`
  - Inyecta `IApplicationDbContext`
  - Resuelve cuenca del contrato, calcula nombre, verifica existencia
  - Retorna `Result<WellNamePreviewDto>`
  - Usa `AsNoTracking()` (CONSTITUTION.backend.md §4.4)
  - Archivo: `src/GOP.Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQueryHandler.cs`

- [ ] **T005**: Crear `Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQueryValidator.cs`
  - Valida ContratoId > 0, Denominacion no vacía + pattern letras, Consecutivo pattern 2 dígitos
  - Mensajes en español (CONSTITUTION.backend.md §5.3)
  - Archivo: `src/GOP.Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQueryValidator.cs`

**Verificación:** `dotnet build src/GOP.Application`

---

## Bloque 3 — API (`dotnet build GOP.API`)

- [ ] **T006**: Modificar `API/Controllers/WellsController.cs`
  - Agregar action `PreviewWellName` con `[HttpGet("preview-name")]`
  - Roles: `[Authorize(Roles = "ADMIN,SUPERVISOR,OPERADOR")]`
  - Recibe query params: `contratoId`, `denominacion`, `consecutivo`, `excludeWellId?`
  - Despacha `PreviewWellNameQuery` via MediatR
  - Máximo 3 líneas (CONSTITUTION.backend.md §13.3)
  - Archivo: `src/GOP.API/Controllers/WellsController.cs`

**Verificación:** `dotnet build src/GOP.API`

---

## Bloque 4 — Tests (`dotnet test`)

- [ ] **T007**: Crear `Application.Tests/Features/Wells/Queries/PreviewWellNameQueryHandlerTests.cs`
  - Test 1: `Handle_ValidQuery_ReturnsAvailableName` — contrato existe, nombre no duplicado → Success + Available=true
  - Test 2: `Handle_DuplicateName_ReturnsUnavailable` — nombre ya existe → Success + Available=false
  - Test 3: `Handle_ContratoNotFound_ReturnsFailure` — contrato no existe → Failure con Contrato.NotFound
  - Test 4: `Handle_ExcludeWellId_ExcludesFromCheck` — nombre existe pero es el pozo excluido → Available=true
  - Patrón AAA con NSubstitute (CONSTITUTION.backend.md §11.4)
  - Archivo: `tests/GOP.Application.Tests/Features/Wells/Queries/PreviewWellNameQueryHandlerTests.cs`

**Verificación:** `dotnet test`

---

## Resumen de Tareas

| ID | Archivo | Tipo | Bloque | Paralelo |
|---|---|---|---|---|
| T001 | `DomainErrors.Contrato.cs` | Crear/Verificar | 1 | — |
| T002 | `WellNamePreviewDto.cs` | Crear | 2 | [P] con T003 |
| T003 | `PreviewWellNameQuery.cs` | Crear | 2 | [P] con T002 |
| T004 | `PreviewWellNameQueryHandler.cs` | Crear | 2 | Después de T002+T003 |
| T005 | `PreviewWellNameQueryValidator.cs` | Crear | 2 | Después de T003 |
| T006 | `WellsController.cs` | Modificar | 3 | — |
| T007 | `PreviewWellNameQueryHandlerTests.cs` | Crear | 4 | — |

**Total: 7 tareas atómicas en 4 bloques compilables.**
