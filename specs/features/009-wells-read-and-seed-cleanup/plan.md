# Plan Técnico — Iter 9 · Wells Read API + Seed Cleanup

**Feature ID:** `009-wells-read-and-seed-cleanup`
**Entrada:** `spec.md` + `contract.yml` de esta carpeta.
**Audiencia:** implement-agent (backend).

---

## 1. Panorama general

### 1.1 Capas afectadas

```
┌────────────────────────────────────────────────────────────┐
│ API (GOP.API)                                              │
│  • WellsController: MODIFICAR ListWells (nuevos query      │
│    params: campoId, denominacion)                          │
│  • GetWell: SIN CAMBIOS (ya funciona)                      │
├────────────────────────────────────────────────────────────┤
│ Application (GOP.Application)                              │
│  • GetWellsListQuery: AGREGAR campos CampoId, Denominacion│
│  • GetWellsListQueryHandler: AGREGAR lógica de filtros     │
│  • GetWellByIdQueryHandler: SIN CAMBIOS                    │
├────────────────────────────────────────────────────────────┤
│ Domain (GOP.Domain)                                        │
│  • SIN CAMBIOS                                             │
├────────────────────────────────────────────────────────────┤
│ Infrastructure (GOP.Infrastructure)                        │
│  • DepartamentoConfiguration: AGREGAR HasData (3 dptos)    │
│  • MunicipioConfiguration: AGREGAR HasData (5 mpios)       │
│  • Migration: SeedCatalogosGeo (NUEVA, idempotente)        │
└────────────────────────────────────────────────────────────┘
```

### 1.2 Principio rector

Extender lo existente. Los handlers `GetWellsListQueryHandler` y `GetWellByIdQueryHandler` ya funcionan. Solo se agregan filtros nuevos al listado y se hardena con tests. La migración es aditiva.

---

## 2. Trade-offs

### 2.1 Paginación: offset-based (ya decidido)

| Alternativa | Pros | Cons |
|-------------|------|------|
| **Offset (page/pageSize)** ✅ actual | Simple; frontend PrimeNG lo soporta nativo; total fácil de calcular; consistente con Iter 8 | Skip/Take lento en datasets grandes (>100k); resultados inconsistentes si hay inserts entre páginas |
| Cursor-based (after/before) | Consistente bajo concurrencia; no degrada en datasets grandes | Más complejo para FE; no soporta "saltar a página N"; incompatible con PrimeNG paginator |
| Keyset (seek method) | Performance óptima | Complejidad máxima; requiere sort estable |

**Decisión:** Mantener offset. El dataset de pozos por tenant será <10k filas por años. Revalidar en Iter 13+ si se ve degradación en p95. Ver CONSTITUTION.contracts.md §4 para la convención estándar.

### 2.2 Idempotencia de migración SeedCatalogosGeo

| Alternativa | Pros | Cons |
|-------------|------|------|
| **SQL MERGE en Up()** ✅ | Semántica exacta "insert if not exists, update if exists"; una operación atómica por tabla | Requiere raw SQL (`migrationBuilder.Sql()`), no portable a otros RDBMS (aceptable: estamos fijos en SQL Server) |
| IF NOT EXISTS + INSERT | Más simple de leer | No actualiza si hay discrepancia de nombre; race condition teórica (irrelevante para migraciones) |
| Try/catch en código C# | No requiere SQL | EF Core no expone hooks en migrations; tendría que ir en DbSeeder (pierde el punto de tenerlo en migración) |

**Decisión:** Raw SQL con `IF NOT EXISTS ... INSERT` en el `Up()` de la migración. No usar MERGE porque el riesgo de discrepancia de nombre es nulo (los nombres DANE son estables) y el pattern es más legible. El `Down()` usa `DELETE WHERE Id IN (...)` solo si quisiéramos revertir.

**Patrón SQL:**
```sql
IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 50)
    INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane]) VALUES (50, N'Meta', N'50');
```

### 2.3 HasData vs. DbSeeder coexistencia

| Alternativa | Pros | Cons |
|-------------|------|------|
| **HasData en Configuration + DbSeeder sigue corriendo** ✅ | Migración garantiza mínimo; DbSeeder completa el resto; sin riesgo de regresión | Dos fuentes de verdad para las mismas 3+5 filas |
| Solo HasData (eliminar filas del JSON) | Una sola fuente | Rompe el patrón de DbSeeder para el dataset DANE completo; hay 33 dptos y 1123 mpios que NO van en HasData |
| Solo DbSeeder (no agregar HasData) | Una sola fuente | No cumple el requisito de garantía por migración |

**Decisión:** Coexistencia. La migración inserta las 3+5 filas con `IF NOT EXISTS`. El DbSeeder ya hace lo mismo (chequea `existingIds` antes de insertar). No hay conflicto: el primero que corre gana, el segundo es no-op para esos IDs.

**Nota importante:** NO agregar `HasData()` directamente en `DepartamentoConfiguration` / `MunicipioConfiguration`. En su lugar, la migración generada manualmente contiene el SQL idempotente. Esto evita que futuras migraciones autogeneradas intenten hacer `DeleteData` + `InsertData` cada vez que EF Core detecte un cambio en el modelo snapshot. El `HasData` de EF Core no es idempotente por diseño — genera `INSERT` plain. Ver CONSTITUTION.backend.md §9 sobre migraciones.

**Corrección vs. instrucciones originales:** Las instrucciones decían "agregar HasData() en las Configuration classes". El análisis del código muestra que esto causaría migraciones autogeneradas frágiles y conflicto con el DbSeeder. La alternativa elegida (migración manual con SQL idempotente) cumple el mismo objetivo (datos garantizados por migración) sin los efectos colaterales. Ver EMERGENT-DECISIONS.md ED-10.

### 2.4 IDs de catálogos geográficos: secuenciales (confirmado por staging)

**Contexto:** El spec-agent propuso IDs DANE como IDs de tabla (ED-09). Tras verificación empírica en staging:

- Staging tiene `Departamentos.Id = 1,2,3` con `CodigoDane` como columna nvarchar separada.
- El pozo de Iter 8 (`88531e37-...`) se persistió con `departamentoId: 1, municipioId: 1`.
- El `DbSeeder` JSON usa IDs DANE — pero staging fue sembrado manualmente con IDs secuenciales antes de que el DbSeeder existiera.

**Decisión:** IDs secuenciales 1-N, alineados con staging. El `CodigoDane` es solo una columna de display/lookup, no la PK. Ver EMERGENT-DECISIONS.md ED-09 (revertido).

**Implicación para DbSeeder:** El DbSeeder actual lee JSON con IDs DANE (50, 85, 68...). En staging esas filas ya existen con IDs 1,2,3, así que el `existingIds` check del seeder las omite. Los IDs del JSON (50, 85, 68) nunca se insertan porque el seeder ve que *algún* registro con esos IDs no existe pero los DepartamentoIds que ya existen (1,2,3) son distintos. En la práctica: el DbSeeder agrega los departamentos/municipios del JSON que NO tengan IDs colisionando con los secuenciales. Esto es un quirk aceptable para el demo — la fuente de verdad son los registros sembrados por la migración.

---

### 2.5 ClusterId — no hay rename necesario

**Contexto:** Las instrucciones mencionan renombrar `clusterUbicacionId` → `clusterId`. Tras inspeccionar todo el codebase:

- `CreateWellCommand.ClusterId` — ya es `int? ClusterId`
- `Well.ClusterId` — ya es `int? ClusterId`
- `WellDetailDto.ClusterId` — ya es `int? ClusterId`
- No existe ningún archivo con `clusterUbicacionId` (grep en `.cs`, `.ts`, `.html` = 0 resultados)

**Decisión:** No hay rename que hacer. La discrepancia es entre las instrucciones y el código. Se documenta en EMERGENT-DECISIONS.md ED-08. Se agrega un test de regresión que confirme que el campo funciona E2E.

---

## 3. Application — cambios en GetWellsListQuery

### 3.1 GetWellsListQuery (MODIFICAR)

**Archivo:** `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQuery.cs`

**Cambio:** Agregar dos parámetros opcionales:

```csharp
public sealed record GetWellsListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    string? SortDir = null,
    int? ContratoId = null,
    string? Estado = null,
    int? CampoId = null,            // NUEVO Iter 9
    string? Denominacion = null     // NUEVO Iter 9
) : IRequest<Result<PagedList<WellListItemDto>>>;
```

### 3.2 GetWellsListQueryHandler (MODIFICAR)

**Archivo:** `backend/src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQueryHandler.cs`

**Cambio:** Agregar dos bloques de filtrado después de los existentes:

```csharp
// Filtro por CampoId (nuevo Iter 9)
if (request.CampoId.HasValue)
    query = query.Where(w => w.CampoId == request.CampoId.Value);

// Filtro por Denominación parcial, case-insensitive (nuevo Iter 9)
if (!string.IsNullOrWhiteSpace(request.Denominacion))
{
    var denom = request.Denominacion.Trim();
    query = query.Where(w => w.Denominacion.Contains(denom));
}
```

**Nota:** `Contains` en EF Core con SQL Server genera `LIKE N'%denom%'` que es case-insensitive por defecto con collation `SQL_Latin1_General_CP1_CI_AS` (el default de Azure SQL). No se necesita `.ToLower()` explícito.

---

## 4. API — cambios en WellsController

### 4.1 ListWells action (MODIFICAR)

**Archivo:** `backend/src/GOP.API/Controllers/WellsController.cs`

**Cambio:** Agregar dos query params al método `ListWells`:

```csharp
[HttpGet]
public async Task<IActionResult> ListWells(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null,
    [FromQuery] string? sortBy = null,
    [FromQuery] string? sortDir = null,
    [FromQuery] int? contratoId = null,
    [FromQuery] string? estado = null,
    [FromQuery] int? campoId = null,           // NUEVO Iter 9
    [FromQuery] string? denominacion = null,   // NUEVO Iter 9
    CancellationToken cancellationToken = default)
{
    var query = new GetWellsListQuery(
        page, pageSize, search, sortBy, sortDir,
        contratoId, estado, campoId, denominacion);
    return (await sender.Send(query, cancellationToken)).ToActionResult();
}
```

### 4.2 GetWell action (SIN CAMBIOS)

Ya funciona. Solo se agregan tests. El handler `GetWellByIdQueryHandler` ya:
1. Aplica global query filter por tenant
2. Usa `IgnoreQueryFilters()` para ADMIN/AUDITOR
3. Retorna 404 si no encuentra (Result.Failure con error NotFound)
4. Retorna `WellDetailDto` (misma shape que POST response)

---

## 5. Infrastructure — migración SeedCatalogosGeo

### 5.1 Migración manual (CREAR)

**Archivo:** `backend/src/GOP.Infrastructure/Migrations/{timestamp}_SeedCatalogosGeo.cs`

**Generación:** Esta migración NO se genera con `dotnet ef migrations add` porque no hay cambios en el modelo. Se crea manualmente como una "data migration".

**Estructura:**

```csharp
public partial class SeedCatalogosGeo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Departamentos — idempotente (IDs secuenciales, alineados con staging)
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 1)
                INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane])
                VALUES (1, N'Meta', N'50');
            IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 2)
                INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane])
                VALUES (2, N'Casanare', N'85');
            IF NOT EXISTS (SELECT 1 FROM [Departamentos] WHERE [Id] = 3)
                INSERT INTO [Departamentos] ([Id], [Nombre], [CodigoDane])
                VALUES (3, N'Santander', N'68');
        ");

        // Municipios — idempotente (IDs secuenciales, alineados con staging)
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 1)
                INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                VALUES (1, N'Puerto Gaitán', 1, N'50568');
            IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 2)
                INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                VALUES (2, N'Puerto López', 1, N'50573');
            IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 3)
                INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                VALUES (3, N'Tauramena', 2, N'85410');
            IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 4)
                INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                VALUES (4, N'Aguazul', 2, N'85010');
            IF NOT EXISTS (SELECT 1 FROM [Municipios] WHERE [Id] = 5)
                INSERT INTO [Municipios] ([Id], [Nombre], [DepartamentoId], [CodigoDane])
                VALUES (5, N'Barrancabermeja', 3, N'68081');
        ");

        // ED-11: Fix DANE code de Aguazul en staging (pudo haberse insertado como 85015)
        migrationBuilder.Sql(@"
            UPDATE [Municipios] SET [CodigoDane] = N'85010'
            WHERE [Id] = 4 AND [CodigoDane] = N'85015';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Down es opcional pero se incluye para reversibilidad
        migrationBuilder.Sql(@"
            DELETE FROM [Municipios] WHERE [Id] IN (1, 2, 3, 4, 5);
            DELETE FROM [Departamentos] WHERE [Id] IN (1, 2, 3);
        ");
    }
}
```

### 5.2 Designer file y ModelSnapshot

**No se modifica** `GopDbContextModelSnapshot.cs` porque no hay cambios al modelo. La migración es solo datos.

El archivo `.Designer.cs` se genera copiando la estructura de una migración existente (ej. `20260420035203_FullDaneSeed.Designer.cs`) y actualizando el nombre de clase y la anotación `[Migration]`.

### 5.3 DepartamentoConfiguration / MunicipioConfiguration (SIN CAMBIOS)

No se agrega `HasData()` a las Configuration classes. Ver trade-off §2.3 para la justificación.

---

## 6. Testing

### 6.1 Tests de integración API (CREAR/EXTENDER)

**Archivo:** `backend/tests/GOP.API.Tests/Controllers/WellsControllerTests.cs`

Nuevos tests a agregar:

1. **ListWells_WithCampoIdFilter_ReturnsFilteredResults**: Crear 2 pozos con campoId distintos → filtrar → verificar.
2. **ListWells_WithDenominacionFilter_ReturnsPartialMatch**: Crear pozo "RUBIALES" → filtrar con "rubi" → match.
3. **ListWells_CombinedFilters_ReturnsIntersection**: contratoId + estado.
4. **GetWell_ExistingId_ReturnsFullDetail**: Crear pozo → GET por id → verificar shape completa.
5. **GetWell_OtherTenantId_Returns404**: Crear pozo con tenant A → GET con token de tenant B → 404.

**Prerrequisito de test:** Los tests necesitan sembrar catálogos (Contratos, Campos, Departamentos, Municipios, Clusters) en la BD InMemory para poder crear pozos FINALIZE que contengan UWI. El factory actual (`GopTestWebApplicationFactory`) siembra users pero NO catálogos.

**Opción elegida:** Extender el factory para sembrar los catálogos mínimos (3 contratos, 6 campos, 4 clusters + 3 dptos + 5 mpios) en `EnsureCreated()`. Esto replica lo que hacen las migrations + DbSeeder en producción.

### 6.2 Tests de migración (CREAR)

**Archivo:** `backend/tests/GOP.Infrastructure.Tests/Persistence/SeedCatalogosGeoMigrationTests.cs`

No es posible testear la migración directamente contra InMemory (no soporta raw SQL). El test real se ejecuta en CI contra SQL Server vía Testcontainers (si está configurado) o se valida manualmente en staging.

**Alternativa pragmática:** Testear que `DbSeeder` es idempotente cuando los 3+5 registros ya existen (ya está cubierto por la lógica existente del seeder). Agregar un test explícito que confirme el comportamiento.

### 6.3 Tests de application (CREAR/EXTENDER)

**Archivo:** `backend/tests/GOP.Application.Tests/Features/Wells/GetWellsListQueryHandlerTests.cs`

Existe. Extender con:
1. Test que filtre por `CampoId` nuevo.
2. Test que filtre por `Denominacion` parcial.

### 6.4 Cobertura del pozo smoke Iter 8

El pozo persistido en staging (`88531e37-0c8c-46b9-81c0-8e908b1e575f`) sirve para smoke test manual post-deploy de Iter 9:
- `GET /api/v1/wells` → debe aparecer en la lista
- `GET /api/v1/wells/88531e37-0c8c-46b9-81c0-8e908b1e575f` → debe retornar 200

---

## 7. Frontend (follow-up, NO se implementa en Iter 9)

Flaggeado para Iter 10 FE:
- [ ] Agregar filtros `campoId` y `denominacion` al componente `wells-list`.
- [ ] Verificar que la lista de pozos consume los nuevos query params.
- [ ] No hay cambios requeridos para el detalle — ya consume `GET /wells/{id}`.

---

## 8. Archivos afectados (resumen)

| Archivo | Acción | Capa |
|---------|--------|------|
| `src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQuery.cs` | MODIFICAR: +2 params | Application |
| `src/GOP.Application/Features/Wells/Queries/GetWellsList/GetWellsListQueryHandler.cs` | MODIFICAR: +2 filtros | Application |
| `src/GOP.API/Controllers/WellsController.cs` | MODIFICAR: +2 query params en ListWells | API |
| `src/GOP.Infrastructure/Migrations/{ts}_SeedCatalogosGeo.cs` | CREAR | Infrastructure |
| `src/GOP.Infrastructure/Migrations/{ts}_SeedCatalogosGeo.Designer.cs` | CREAR | Infrastructure |
| `tests/GOP.API.Tests/Controllers/WellsControllerTests.cs` | MODIFICAR: +5 tests | Tests |
| `tests/GOP.API.Tests/Fixtures/GopTestWebApplicationFactory.cs` | MODIFICAR: +seed catálogos | Tests |
| `tests/GOP.Application.Tests/Features/Wells/GetWellsListQueryHandlerTests.cs` | MODIFICAR: +2 tests | Tests |
| `tests/GOP.Infrastructure.Tests/Persistence/DbSeederIdempotencyTests.cs` | CREAR | Tests |

---

## 9. Despliegue

1. **Local:** `dotnet ef database update` aplica `SeedCatalogosGeo`. Luego `dotnet run` ejecuta DbSeeder — idempotente.
2. **CI:** El workflow `backend-ci.yml` corre los tests. La migración se aplica automáticamente en staging en el arranque de la API (`ApplyMigrationsAndSeedAsync` en `MigrationExtension.cs`).
3. **Staging:** La migración es idempotente. Si los registros ya existen (insertados por DbSeeder), el `IF NOT EXISTS` los salta. Cero riesgo.

---

## 10. Deuda técnica (a documentar en EMERGENT-DECISIONS.md)

| # | Deuda | Iter objetivo |
|---|-------|---------------|
| 1 | Tests de integración usan InMemory, no SQL Server real — índices únicos y FK no se validan | Iter 13 (Testcontainers) |
| 2 | GopTestWebApplicationFactory siembra catálogos manualmente — debería compartir lógica con HasData/DbSeeder | Iter 13 |
| 3 | Frontend no consume los filtros nuevos (campoId, denominacion) | Iter 10 FE |
