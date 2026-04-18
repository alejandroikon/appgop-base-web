# Tareas Backend — Creación de Pozo Nuevo V2.0

**Referencia:** `plan.be.md`, `contract.yml`, `data-model.md`
**Verificación por bloque:** `dotnet build` al final de cada bloque, `dotnet test` en bloque de tests

---

## Bloque 1 — Domain: Enums y Errors (dotnet build GOP.Domain)

- [ ] **T-BE-001**: Modificar `Domain/Enums/WellStatus.cs` — reducir a `Borrador`, `Creado`.
- [ ] **T-BE-002**: Crear `Domain/Enums/SubClasificacionExploratoria.cs` — enum: A3, A2a, A2b, A2c, A1.
- [ ] **T-BE-003**: Modificar `Domain/Enums/TipoObjetivo.cs` — agregar C, GT, O.
- [ ] **T-BE-004**: Modificar `Domain/Errors/DomainErrors.Well.cs` — agregar: DuplicateUwi, DuplicateName, Forma101Locked, InvalidClasificacionForAnh, CampoRequiredForDesarrollo, NotEditable, NotDeletable.

**Verificación:** `dotnet build GOP.Domain` ✅

---

## Bloque 2 — Domain: Value Object UWI (dotnet build GOP.Domain)

- [ ] **T-BE-005**: Crear `Domain/ValueObjects/Uwi.cs` — Value Object con factory method `Generate(...)`. Implementa algoritmo PPDM completo según `uwi-algorithm.md`. Retorna `Result<Uwi>`.

**Verificación:** `dotnet build GOP.Domain` ✅

---

## Bloque 3 — Domain: Entidad Well refactorizada (dotnet build GOP.Domain)

- [ ] **T-BE-006**: Modificar `Domain/Entities/Well.cs` — refactorizar según `data-model.md` §1: agregar SubClasificacion, Forma101Radicada, aplanar ubicación. Factory methods CreateDraft() y CreateFinalized(). Reglas de negocio: IsEditable(), IsDeletable().
- [ ] **T-BE-007**: Modificar `Domain/Interfaces/Repositories/IWellRepository.cs` — agregar ExistsByUwiAsync(), ExistsByNameAsync().

**Verificación:** `dotnet build GOP.Domain` ✅

---

## Bloque 4 — Domain: Entidad Cluster actualizada (dotnet build GOP.Domain)

- [ ] **T-BE-008**: Modificar `Domain/Entities/Cluster.cs` (o crear si no existe) — agregar propiedad `Abreviatura` (string, 2 chars).

**Verificación:** `dotnet build GOP.Domain` ✅

---

## Bloque 5 — Application: DTOs (dotnet build GOP.Application)

- [ ] **T-BE-009**: Crear `Application/Features/Wells/Queries/GetWellById/WellDetailDto.cs` — record espejo del schema WellDetail en contract.yml.
- [ ] **T-BE-010**: Crear `Application/Features/Wells/Queries/GetWellsList/WellListItemDto.cs` — record espejo del schema WellListItem.
- [ ] **T-BE-011**: Crear `Application/Features/Wells/Queries/PreviewUwi/UwiPreviewDto.cs` — record: uwi, isUnique, components.
- [ ] **T-BE-012**: Crear `Application/Features/Wells/Queries/PreviewWellName/WellNamePreviewDto.cs` — record: nombrePozo, isUnique.

**Verificación:** `dotnet build GOP.Application` ✅

---

## Bloque 6 — Application: Queries (dotnet build GOP.Application)

- [ ] **T-BE-013**: Crear `Application/Features/Wells/Queries/GetWellById/GetWellByIdQuery.cs` — record con WellId.
- [ ] **T-BE-014**: Crear `Application/Features/Wells/Queries/GetWellById/GetWellByIdQueryHandler.cs` — AsNoTracking, multi-tenant filter.
- [ ] **T-BE-015**: Crear `Application/Features/Wells/Queries/GetWellsList/GetWellsListQuery.cs` — record con page, pageSize, search, sortBy, sortDir, estado, contratoId.
- [ ] **T-BE-016**: Crear `Application/Features/Wells/Queries/GetWellsList/GetWellsListQueryHandler.cs` — paginación, filtros condicionales, AsNoTracking.
- [ ] **T-BE-017**: Crear `Application/Features/Wells/Queries/PreviewUwi/PreviewUwiQuery.cs` — record con parámetros UWI.
- [ ] **T-BE-018**: Crear `Application/Features/Wells/Queries/PreviewUwi/PreviewUwiQueryHandler.cs` — invoca Uwi.Generate(), verifica unicidad.
- [ ] **T-BE-019**: Crear `Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQuery.cs` — record con contratoId, campoId, denominacion, consecutivo.
- [ ] **T-BE-020**: Crear `Application/Features/Wells/Queries/PreviewWellName/PreviewWellNameQueryHandler.cs` — genera nombre, verifica unicidad por tenant.

**Verificación:** `dotnet build GOP.Application` ✅

---

## Bloque 7 — Application: Commands + Validators (dotnet build GOP.Application)

- [ ] **T-BE-021**: Crear `Application/Features/Wells/Commands/CreateWell/CreateWellCommand.cs` — record con action (DRAFT|FINALIZE) + campos nullable.
- [ ] **T-BE-022**: Crear `Application/Features/Wells/Commands/CreateWell/CreateWellCommandHandler.cs` — lógica: si DRAFT validación mínima; si FINALIZE validación completa + Uwi.Generate() + unicidad. Aplica RN-15 (ANH → solo Estratigráfico), RN-12 (Desarrollo → Campo required).
- [ ] **T-BE-023**: Crear `Application/Features/Wells/Commands/CreateWell/CreateWellCommandValidator.cs` — FluentValidation: denominación regex (RN-07), consecutivo 1-9999 (RN-08), action required.
- [ ] **T-BE-024**: Crear `Application/Features/Wells/Commands/UpdateWell/UpdateWellCommand.cs` — record con wellId + action (SAVE|FINALIZE) + campos.
- [ ] **T-BE-025**: Crear `Application/Features/Wells/Commands/UpdateWell/UpdateWellCommandHandler.cs` — verificar editable (IsEditable()). Si FINALIZE borrador: generar UWI.
- [ ] **T-BE-026**: Crear `Application/Features/Wells/Commands/UpdateWell/UpdateWellCommandValidator.cs` — mismas reglas que CreateWell.
- [ ] **T-BE-027**: Crear `Application/Features/Wells/Commands/DeleteWell/DeleteWellCommand.cs` — record con wellId.
- [ ] **T-BE-028**: Crear `Application/Features/Wells/Commands/DeleteWell/DeleteWellCommandHandler.cs` — verificar IsDeletable(). Soft delete.
- [ ] **T-BE-029**: Crear `Application/Features/Wells/Commands/CreateCluster/CreateClusterCommand.cs` — record con nombre, campoId.
- [ ] **T-BE-030**: Crear `Application/Features/Wells/Commands/CreateCluster/CreateClusterCommandHandler.cs` — verificar unicidad, generar abreviatura.
- [ ] **T-BE-031**: Crear `Application/Features/Wells/Commands/CreateCluster/CreateClusterCommandValidator.cs` — nombre required, maxLength 100.

**Verificación:** `dotnet build GOP.Application` ✅

---

## Bloque 8 — Application: Mappings (dotnet build GOP.Application)

- [ ] **T-BE-032**: Modificar `Application/Features/Wells/Mappings/WellMappingProfile.cs` — actualizar: Well → WellDetailDto, Well → WellListItemDto. Agregar Cluster → ClusterItemDto si aplica.

**Verificación:** `dotnet build GOP.Application` ✅

---

## Bloque 9 — Infrastructure: Configurations + Migración (dotnet build GOP.Infrastructure)

- [ ] **T-BE-033**: Modificar `Infrastructure/Persistence/Configurations/WellConfiguration.cs` — actualizar: SubClasificacion (string nullable), Consecutivo (int), Forma101Radicada (bool default false), aplanar ubicación. Índices: IX_Wells_Uwi_Global (unique, filtered), IX_Wells_TenantId_NombrePozo (unique, filtered).
- [ ] **T-BE-034**: Modificar `Infrastructure/Persistence/Configurations/ClusterConfiguration.cs` — agregar Abreviatura (maxLength 2).
- [ ] **T-BE-035**: Generar migración `UpdateWellsForV2_PpdmUwi` — `dotnet ef migrations add UpdateWellsForV2_PpdmUwi -p src/GOP.Infrastructure -s src/GOP.API`.

**Verificación:** `dotnet build GOP.Infrastructure` ✅

---

## Bloque 10 — Infrastructure: Repository (dotnet build GOP.Infrastructure)

- [ ] **T-BE-036**: Modificar `Infrastructure/Persistence/Repositories/WellRepository.cs` — implementar ExistsByUwiAsync() y ExistsByNameAsync().

**Verificación:** `dotnet build GOP.Infrastructure` ✅

---

## Bloque 11 — API: Controllers (dotnet build GOP.API)

- [ ] **T-BE-037**: Modificar `API/Controllers/WellsController.cs` — actualizar actions: CreateWell (POST), UpdateWell (PUT), DeleteWell (DELETE). Agregar PreviewUwi (GET /wells/preview-uwi). Actualizar PreviewWellName. Eliminar TransitionWell si existía.
- [ ] **T-BE-038**: Modificar `API/Controllers/CatalogsController.cs` — agregar action CreateCluster (POST /catalogs/clusters).

**Verificación:** `dotnet build GOP.API` ✅

---

## Bloque 12 — Tests: Domain (dotnet test GOP.Domain.Tests)

- [ ] **T-BE-039**: Crear `Domain.Tests/ValueObjects/UwiTests.cs` — tests: Generate happy path (3 ejemplos de uwi-algorithm.md), padding sigla, excepción ANH, trayectoria Original = vacío, unicidad de componentes, longitud ≤ 50.
- [ ] **T-BE-040**: Crear `Domain.Tests/Entities/WellTests.cs` — tests: CreateDraft con datos mínimos, CreateFinalized con datos completos, IsEditable() true/false, IsDeletable() true/false, estado correcto.

**Verificación:** `dotnet test GOP.Domain.Tests` ✅

---

## Bloque 13 — Tests: Application (dotnet test GOP.Application.Tests)

- [ ] **T-BE-041**: Crear `Application.Tests/Features/Wells/Commands/CreateWellCommandHandlerTests.cs` — tests: Draft OK, Finalize OK, UWI duplicado (409), nombre duplicado (409), ANH solo estratigráfico (422), Desarrollo sin campo (422).
- [ ] **T-BE-042**: Crear `Application.Tests/Features/Wells/Commands/UpdateWellCommandHandlerTests.cs` — tests: Update borrador OK, Forma101 bloqueado (422), Finalize borrador OK, not found (404).
- [ ] **T-BE-043**: Crear `Application.Tests/Features/Wells/Commands/DeleteWellCommandHandlerTests.cs` — tests: Delete borrador OK, Forma101 bloqueado (422), not found (404).
- [ ] **T-BE-044**: Crear `Application.Tests/Features/Wells/Queries/PreviewUwiQueryHandlerTests.cs` — tests: UWI correcto, unicidad true, unicidad false.
- [ ] **T-BE-045**: Crear `Application.Tests/Features/Wells/Commands/CreateClusterCommandHandlerTests.cs` — tests: OK, duplicado (409).

**Verificación:** `dotnet test GOP.Application.Tests` ✅

---

## Resumen

| Bloque | Tareas | Proyecto | Verificación |
|--------|--------|----------|-------------|
| 1 — Enums/Errors | T-BE-001 a T-BE-004 | Domain | `dotnet build` |
| 2 — VO Uwi | T-BE-005 | Domain | `dotnet build` |
| 3 — Entidad Well | T-BE-006, T-BE-007 | Domain | `dotnet build` |
| 4 — Cluster | T-BE-008 | Domain | `dotnet build` |
| 5 — DTOs | T-BE-009 a T-BE-012 | Application | `dotnet build` |
| 6 — Queries | T-BE-013 a T-BE-020 | Application | `dotnet build` |
| 7 — Commands | T-BE-021 a T-BE-031 | Application | `dotnet build` |
| 8 — Mappings | T-BE-032 | Application | `dotnet build` |
| 9 — Config+Migración | T-BE-033 a T-BE-035 | Infrastructure | `dotnet build` |
| 10 — Repository | T-BE-036 | Infrastructure | `dotnet build` |
| 11 — Controllers | T-BE-037, T-BE-038 | API | `dotnet build` |
| 12 — Tests Domain | T-BE-039, T-BE-040 | Tests | `dotnet test` |
| 13 — Tests Application | T-BE-041 a T-BE-045 | Tests | `dotnet test` |
| **Total** | **45 tareas** | | |
