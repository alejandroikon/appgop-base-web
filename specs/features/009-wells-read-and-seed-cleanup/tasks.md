# Tasks — Iter 9 · Wells Read API + Seed Cleanup

**Feature ID:** `009-wells-read-and-seed-cleanup`
**Entrada:** `plan.md` de esta carpeta.
**Audiencia:** implement-agent (backend).

---

## Cómo usar este documento

Cada fase tiene **precondiciones** y **definición de hecho**. Las tareas son atómicas (1 tarea = 1 archivo). Dentro de cada bloque, tareas marcadas `[P]` pueden ejecutarse en paralelo.

**Regla dura:** si el agente descubre que necesita algo no listado, lo documenta en `EMERGENT-DECISIONS.md` y sigue. Si descubre que **no puede** completar una tarea, para y reporta.

---

## Fase 0 — Validación y setup

**Owner:** implement-agent
**Precondiciones:** Rama `009-wells-read-and-seed-cleanup` creada desde commit `2c21c85`.

### T0.1 — Validar estado del código base

- [ ] Verificar que `dotnet build` pasa en `backend/`.
- [ ] Verificar que `dotnet test` pasa (todos los tests existentes verdes).
- [ ] Confirmar que `WellsController.ListWells` acepta los params actuales (`page`, `pageSize`, `search`, `sortBy`, `sortDir`, `contratoId`, `estado`).
- [ ] Confirmar que `GetWellsListQuery` tiene 7 parámetros.
- [ ] Confirmar que `GetWellByIdQueryHandler` retorna `WellDetailDto` (misma shape que POST).

**Definición de hecho:** Estado baseline confirmado. Sin sorpresas.

---

## Fase 1 — Backend: filtros nuevos en ListWells

**Owner:** implement-agent
**Precondiciones:** Fase 0 completada.
**Bloque compilable:** B1 (las 3 tareas juntas + `dotnet build`).

### T1.1 — Agregar params a GetWellsListQuery `[P]`

**Archivo:** `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQuery.cs`
**Acción:** MODIFICAR
**Cambio:** Agregar `int? CampoId = null` y `string? Denominacion = null` al final del record.

```csharp
public sealed record GetWellsListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    string? SortDir = null,
    int? ContratoId = null,
    string? Estado = null,
    int? CampoId = null,
    string? Denominacion = null
) : IRequest<Result<PagedList<WellListItemDto>>>;
```

### T1.2 — Agregar lógica de filtros al handler `[P]`

**Archivo:** `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQueryHandler.cs`
**Acción:** MODIFICAR
**Cambio:** Después del bloque de filtro `Estado` (línea ~38), agregar:

```csharp
if (request.CampoId.HasValue)
    query = query.Where(w => w.CampoId == request.CampoId.Value);

if (!string.IsNullOrWhiteSpace(request.Denominacion))
{
    var denom = request.Denominacion.Trim();
    query = query.Where(w => w.Denominacion.Contains(denom));
}
```

### T1.3 — Agregar query params al controller

**Archivo:** `backend/src/GOP.API/Controllers/WellsController.cs`
**Acción:** MODIFICAR
**Cambio:** Agregar `[FromQuery] int? campoId = null` y `[FromQuery] string? denominacion = null` al método `ListWells`. Pasarlos al constructor de `GetWellsListQuery`.

**Firma actualizada:**
```csharp
public async Task<IActionResult> ListWells(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] string? sortDir = null,
    [FromQuery] int? contratoId = null,
    [FromQuery] string? estado = null,
    [FromQuery] int? campoId = null,
    [FromQuery] string? denominacion = null,
    CancellationToken cancellationToken = default)
{
    var query = new GetWellsListQuery(
        page, pageSize, search, sortBy, sortDir,
        contratoId, estado, campoId, denominacion);
    return (await sender.Send(query, cancellationToken)).ToActionResult();
}
```

**Verificación:** `dotnet build backend/` ✅

---

## Fase 2 — Backend: migración SeedCatalogosGeo

**Owner:** implement-agent
**Precondiciones:** Fase 1 compilando.
**Bloque compilable:** B2 (2 tareas + `dotnet build`).

### T2.1 — Crear migración SeedCatalogosGeo

**Archivo:** `backend/src/GOP.Infrastructure/Migrations/{timestamp}_SeedCatalogosGeo.cs`
**Acción:** CREAR (manual, no autogenerada)
**Contenido:** Ver `plan.md` §5.1 para el SQL idempotente completo.

Datos insertados (IDs secuenciales, alineados con staging):

| Tabla | Id | Nombre | Dato extra |
|-------|-----|--------|------------|
| Departamentos | 1 | Meta | CodigoDane=50 |
| Departamentos | 2 | Casanare | CodigoDane=85 |
| Departamentos | 3 | Santander | CodigoDane=68 |
| Municipios | 1 | Puerto Gaitán | DptoId=1, DANE=50568 |
| Municipios | 2 | Puerto López | DptoId=1, DANE=50573 |
| Municipios | 3 | Tauramena | DptoId=2, DANE=85410 |
| Municipios | 4 | Aguazul | DptoId=2, DANE=85010 |
| Municipios | 5 | Barrancabermeja | DptoId=3, DANE=68081 |

**Patrón SQL:** `IF NOT EXISTS (SELECT 1 FROM [X] WHERE [Id] = Y) INSERT INTO [X] ...`

**Bonus ED-11:** Incluir al final del `Up()`:
```sql
UPDATE [Municipios] SET [CodigoDane] = N'85010' WHERE [Id] = 4 AND [CodigoDane] = N'85015';
```
Esto corrige el DANE erróneo de Aguazul si fue insertado manualmente con 85015 en staging.

### T2.2 — Crear Designer file para la migración

**Archivo:** `backend/src/GOP.Infrastructure/Migrations/{timestamp}_SeedCatalogosGeo.Designer.cs`
**Acción:** CREAR
**Contenido:** Copiar estructura del designer de la migración más reciente (`20260421221649_AddUsersAndRefreshTokens.Designer.cs`), cambiar nombre de clase y atributo `[Migration("{timestamp}_SeedCatalogosGeo")]`.

**Verificación:** `dotnet build backend/` ✅

---

## Fase 3 — Tests: seed de catálogos en test factory

**Owner:** implement-agent
**Precondiciones:** Fases 1 y 2 compilando.
**Bloque compilable:** B3 (1 tarea + `dotnet build`).

### T3.1 — Sembrar catálogos en GopTestWebApplicationFactory

**Archivo:** `backend/tests/GOP.API.Tests/Fixtures/GopTestWebApplicationFactory.cs`
**Acción:** MODIFICAR
**Cambio:** Después de `db.Users.AddRange(...)` y `db.SaveChanges()`, agregar seed de catálogos mínimos:

```csharp
// Seed catálogos para tests de integración Wells (Iter 9)
if (!db.Contratos.Any())
{
    db.Contratos.AddRange(
        new Contrato { Id = 1, Nombre = "Contrato E&P Llanos", Tipo = "E&P", Cuenca = "Llanos Orientales" },
        new Contrato { Id = 2, Nombre = "Contrato E&P Piedemonte", Tipo = "E&P", Cuenca = "Piedemonte Llanero" },
        new Contrato { Id = 3, Nombre = "Contrato E&P Magdalena", Tipo = "E&P", Cuenca = "Valle Medio del Magdalena" });

    db.Campos.AddRange(
        new Campo { Id = 1, Nombre = "Campo Rubiales", ContratoId = 1 },
        new Campo { Id = 2, Nombre = "Campo Quifa", ContratoId = 1 },
        new Campo { Id = 3, Nombre = "Campo Cusiana", ContratoId = 2 });

    db.Clusters.AddRange(
        new Cluster { Id = 1, Nombre = "Cluster Norte", Abreviatura = "CN", CampoId = 1 },
        new Cluster { Id = 2, Nombre = "Cluster Sur", Abreviatura = "CS", CampoId = 1 });

    // IDs secuenciales alineados con staging (ED-09 revertido)
    db.Departamentos.AddRange(
        new Departamento { Id = 1, Nombre = "Meta", CodigoDane = "50" },
        new Departamento { Id = 2, Nombre = "Casanare", CodigoDane = "85" },
        new Departamento { Id = 3, Nombre = "Santander", CodigoDane = "68" });

    db.Municipios.AddRange(
        new Municipio { Id = 1, Nombre = "Puerto Gaitán", DepartamentoId = 1, CodigoDane = "50568" },
        new Municipio { Id = 2, Nombre = "Puerto López", DepartamentoId = 1, CodigoDane = "50573" },
        new Municipio { Id = 3, Nombre = "Tauramena", DepartamentoId = 2, CodigoDane = "85410" },
        new Municipio { Id = 4, Nombre = "Aguazul", DepartamentoId = 2, CodigoDane = "85010" },
        new Municipio { Id = 5, Nombre = "Barrancabermeja", DepartamentoId = 3, CodigoDane = "68081" });

    db.SaveChanges();
}
```

**Nota:** Los IDs de Contratos, Campos, Clusters coinciden con los HasData de sus respectivas configurations. Los IDs de Departamentos y Municipios son secuenciales 1-N, alineados con lo que existe en staging (confirmado empíricamente). El `CodigoDane` es una columna separada de display/lookup.

**Verificación:** `dotnet build backend/tests/GOP.API.Tests/` ✅

---

## Fase 4 — Tests: nuevos tests de integración

**Owner:** implement-agent
**Precondiciones:** Fase 3 compilando.
**Bloque compilable:** B4 (3 tareas + `dotnet test`).

### T4.1 — Tests de ListWells con filtros nuevos

**Archivo:** `backend/tests/GOP.API.Tests/Controllers/WellsControllerTests.cs`
**Acción:** MODIFICAR (agregar tests)
**Tests nuevos:**

1. `ListWells_WithCampoIdFilter_ReturnsOnlyMatchingWells`
   - Crear 2 pozos DRAFT con campoId=1 y 1 con campoId=2.
   - GET /wells?campoId=1 → total=2.

2. `ListWells_WithDenominacionFilter_ReturnsCaseInsensitiveMatch`
   - Crear pozo con denominacion="RUBIALES".
   - GET /wells?denominacion=rubi → total≥1.

3. `ListWells_CombinedFilters_ReturnsIntersection`
   - Crear pozos variados.
   - GET /wells?contratoId=1&estado=BORRADOR → verifica intersección.

### T4.2 — Tests de GetWellById robustos

**Archivo:** `backend/tests/GOP.API.Tests/Controllers/WellsControllerTests.cs`
**Acción:** MODIFICAR (agregar tests)
**Tests nuevos:**

4. `GetWell_ExistingWell_ReturnsFullWellDetailShape`
   - Crear pozo DRAFT → GET por id → verificar que el response tiene TODOS los campos de WellDetailDto.

5. `GetWell_WellFromOtherTenant_Returns404NotForbidden`
   - Crear pozo con un token (tenant A) → GET con otro token (tenant B) → 404.
   - **Nota:** Este test requiere poder crear tokens para tenants distintos. Si el factory actual solo tiene un tenant, documentar la limitación y probar con ADMIN (que bypasea el filter).

### T4.3 — Tests de application layer (filtros nuevos)

**Archivo:** `backend/tests/GOP.Application.Tests/Features/Wells/GetWellsListQueryHandlerTests.cs`
**Acción:** MODIFICAR (agregar tests)
**Tests nuevos:**

1. `Handle_WithCampoIdFilter_FiltersCorrectly`
2. `Handle_WithDenominacionFilter_MatchesCaseInsensitive`

**Verificación:** `dotnet test backend/` ✅ (todos los tests verdes, nuevos y existentes).

---

## Fase 5 — Tests: idempotencia del seeder

**Owner:** implement-agent
**Precondiciones:** Fase 4 verdes.
**Bloque compilable:** B5 (1 tarea + `dotnet test`).

### T5.1 — Test de idempotencia del DbSeeder

**Archivo:** `backend/tests/GOP.Infrastructure.Tests/Persistence/DbSeederIdempotencyTests.cs`
**Acción:** CREAR
**Contenido:**

Test que:
1. Crea un `GopDbContext` InMemory.
2. Inserta manualmente los 3 departamentos y 5 municipios (simulando la migración).
3. Ejecuta `DbSeeder.SeedAsync()`.
4. Verifica que NO se duplicaron registros (count sigue en 3+5 o más por el JSON completo, pero sin duplicados).

**Nota:** El DbSeeder lee de archivos JSON. En el entorno de test, los archivos JSON pueden no estar disponibles. Si el DbSeeder lanza warning y no inserta nada (archivo no encontrado), el test verifica que no rompe. Si los archivos están disponibles, verifica que el count es >= 3+5 y sin duplicados (distinct por Id).

**Verificación:** `dotnet test backend/tests/GOP.Infrastructure.Tests/` ✅

---

## Fase 6 — Documentación y cleanup

**Owner:** implement-agent
**Precondiciones:** Todas las fases anteriores verdes.

### T6.1 — Actualizar EMERGENT-DECISIONS.md

**Archivo:** `specs/features/009-wells-read-and-seed-cleanup/EMERGENT-DECISIONS.md`
**Acción:** MODIFICAR (agregar decisiones que surjan durante implementación)

### T6.2 — Commit y push

**Acción:** Commit con mensaje:
```
feat(iter-9): Wells Read API filters + SeedCatalogosGeo migration

- Enhanced GET /wells with campoId and denominacion filters
- Manual idempotent migration for 3 departamentos + 5 municipios
- Integration tests for tenant isolation, pagination, filters
- Catalog seed in test factory for robust API tests

Refs: specs/features/009-wells-read-and-seed-cleanup/
```

Push a rama `009-wells-read-and-seed-cleanup`.

---

## Resumen de secuencia

```
Fase 0 (Validación)
  └──→ Fase 1 (Filtros: Query + Handler + Controller)  [B1: dotnet build]
        └──→ Fase 2 (Migración seed)  [B2: dotnet build]
              └──→ Fase 3 (Test factory seed)  [B3: dotnet build]
                    └──→ Fase 4 (Tests integración + app)  [B4: dotnet test]
                          └──→ Fase 5 (Test idempotencia)  [B5: dotnet test]
                                └──→ Fase 6 (Docs + push)
```

Fases estrictamente secuenciales. Cada bloque termina con el comando de verificación indicado.

---

## Métricas de éxito

Al cierre, el implement-agent debe poder confirmar:

- [ ] `dotnet test backend/` — todos verdes (existentes + nuevos).
- [ ] `GET /api/v1/wells?campoId=1` filtra correctamente.
- [ ] `GET /api/v1/wells?denominacion=rubi` filtra case-insensitive.
- [ ] `GET /api/v1/wells/{id}` retorna 200 para pozo del tenant, 404 para otro.
- [ ] Migración `SeedCatalogosGeo` aplicable sin error sobre BD vacía o con datos existentes.
- [ ] Ningún test existente roto.
