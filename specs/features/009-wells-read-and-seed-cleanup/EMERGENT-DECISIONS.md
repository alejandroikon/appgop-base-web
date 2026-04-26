# EMERGENT-DECISIONS — Iter 9 · Wells Read API + Seed Cleanup

Decisiones que surgieron durante el análisis de código y la revisión humana, y que difieren de las propuestas iniciales del spec-agent o de las instrucciones originales.

---

## ED-08: `clusterUbicacionId` no existe en el codebase — no hay rename que hacer

**Contexto:** Las instrucciones de Iter 9 dicen: "El DTO Create Well actualmente declara `clusterUbicacionId` pero el response trae `clusterId` y el campo no se persiste. Renombrar la propiedad del DTO a `clusterId`."

**Hallazgo:** Tras inspeccionar todo el codebase (`grep -rn "clusterUbicacion" --include="*.cs" --include="*.ts" --include="*.html" -i` = **0 resultados**):

| Archivo | Campo actual |
|---------|-------------|
| `CreateWellCommand.cs` | `int? ClusterId` |
| `Well.cs` (entidad) | `int? ClusterId` |
| `WellDetailDto.cs` | `int? ClusterId` |
| `WellConfiguration.cs` | `builder.HasOne<Cluster>().WithMany().HasForeignKey(w => w.ClusterId)` |
| `CreateWellCommandHandler.cs` | `request.ClusterId.HasValue` |

**El nombre ya es consistente** en toda la cadena: DTO → Command → Entity → Configuration → Response.

**Decisión:** ✅ Aceptada por humano. No se hace ningún rename. Se agrega un test de regresión que confirme que crear un pozo con `clusterId` funciona E2E.

**Impacto:** Se elimina 1 de los 3 items de trabajo del "fixes derivados de Iter 8". Ahorro neto.

---

## ED-09: IDs de catálogos geográficos — secuenciales (1,2,3), NO códigos DANE

**Contexto:** El spec-agent propuso usar códigos DANE como IDs de tabla (Meta Id=50, Casanare Id=85, etc.) basándose en el archivo `departamentos.json` del DbSeeder.

**Verificación empírica en staging (por Alejandro):**

```
Departamentos en staging: Id=1 Meta (DANE 50), Id=2 Casanare (DANE 85), Id=3 Santander (DANE 68)
Municipios en staging:    Id=1 Puerto Gaitán (50568), Id=2 Puerto López (50573), Id=3 Tauramena (85410), Id=4 Aguazul, Id=5 Barrancabermeja (68081)
```

El pozo de Iter 8 (`88531e37-...`) se persistió con `departamentoId: 1, municipioId: 1`. Esa es la prueba empírica de que staging usa IDs secuenciales.

**Explicación de la discrepancia:** El `DbSeeder` lee JSON con IDs DANE (50, 85, 68...) pero staging fue sembrado manualmente con IDs secuenciales **antes** de que el DbSeeder existiera. El DbSeeder nunca sobrescribió esos registros porque su check `existingIds` vio que ya había datos.

**Decisión (resuelta por revisión humana):** ❌ ED-09 revertida. Usar IDs secuenciales 1-N alineados con staging. `CodigoDane` es una columna nvarchar separada de display/lookup.

**Impacto:** Migración, tests y factory actualizados para usar IDs 1-5 en vez de DANE codes.

---

## ED-10: HasData en Configuration vs. migración manual con SQL idempotente

**Contexto:** Las instrucciones dicen "Agregar HasData() en DepartamentoConfiguration.cs y MunicipioConfiguration.cs". Sin embargo:

1. Las configurations actuales dicen explícitamente:
   ```
   // T-INFRA-20: HasData mínimo removido — datos completos DANE via DbSeeder
   ```
2. `HasData()` de EF Core genera `migrationBuilder.InsertData()` que produce `INSERT INTO` sin `IF NOT EXISTS`.
3. En staging, los registros ya existen. La migración con `InsertData()` fallaría con PK violation.
4. Cada vez que se genera una migración autogenerada, EF Core compara el modelo snapshot con el HasData declarado y puede generar `DeleteData()` + `InsertData()` inesperados.

**Decisión:** ✅ Aceptada por humano. NO agregar `HasData()` a las Configuration classes. En su lugar, crear una migración manual con SQL idempotente (`IF NOT EXISTS ... INSERT`). Cumple el mismo objetivo sin efectos colaterales.

**Referencia:** `plan.md` §2.3 para el trade-off completo.

---

## ED-11: Aguazul tiene DANE 85010, no 85015

**Contexto:** Las instrucciones originales decían "Aguazul (Id=4, Dpto=2, DANE=85015)".

**Hallazgo:** Verificado contra datos DANE oficiales y confirmado por Alejandro:
- **Aguazul:** DANE 85010
- **DANE 85015** corresponde a **Chámeza**, no a Aguazul

**Decisión:** ✅ Aceptada por humano. Usar DANE correcto 85010. Adicionalmente, la migración incluye un UPDATE correctivo:

```sql
UPDATE [Municipios] SET [CodigoDane] = N'85010'
WHERE [Id] = 4 AND [CodigoDane] = N'85015';
```

Esto corrige el dato en staging si fue insertado manualmente con el código erróneo.

**Impacto:** Dato DANE correcto en BD. El UPDATE es no-op si el dato ya es 85010.

---

## ED-12: `dotnet` no en PATH — instalación vía script oficial

**Contexto:** El entorno de CI/agente no tenía `dotnet` en PATH al arrancar Iter 9.

**Hallazgo:** El script oficial de Microsoft (`dot.net/v1/dotnet-install.sh --channel 10.0`) instaló correctamente el SDK 10.0.203 en `$HOME/.dotnet`. Funciona idénticamente a iteraciones previas.

**Decisión:** ✅ No requiere acción post-Iter 9. El script es idempotente en futuros arranques.

---

## ED-13: Denominacion filter en InMemory es case-sensitive

**Contexto:** El spec AC-06 dice "filtro denominacion case-insensitive". El handler usa `.Contains(denom)` que en SQL Server con collation `CI_AS` es case-insensitive. En InMemory EF Core es case-SENSITIVE.

**Hallazgo en tests:** Para que los tests de Application y API pasen en InMemory, se usa el mismo case en el filtro. El test `Handle_WithDenominacionFilter_MatchesPartialString` filtra "RUBI" para encontrar "RUBIALES" (same-case).

**Decisión:** ✅ Aceptado. La case-insensitivity REAL está garantizada en producción (SQL Server CI_AS). Los tests de Application/API con InMemory usan same-case y verifican el MECANISMO de filtro (not the collation). Comentario en el test documenta esta limitación. Para cobertura real de CI, se necesitaría Testcontainers SQL Server (pendiente Iter 13).

---

## ED-14: Tenant isolation test — ADMIN (TenantId=1) crea, SUPERVISOR (TenantId=2) lee → 404

**Contexto:** El test `GetWell_WellFromOtherTenant_Returns404NotForbidden` requiere dos tenants distintos.

**Implementación:** Con los seed users actuales, ADMIN tiene TenantId=1 y SUPERVISOR tiene TenantId=2. El query filter de `GopDbContext` se aplica en la construcción del DbContext scoped (`_currentTenantId = currentUserService.TenantId`). Cuando ADMIN crea un well, queda con TenantId=1. Cuando SUPERVISOR intenta leerlo, el DbContext del request tiene `_currentTenantId=2`, y el query filter `w.TenantId == _currentTenantId` esconde el well → 404. ✅

**Decisión:** ✅ No se agregaron usuarios nuevos. Los seed users existentes cubren el escenario.

---

## Resumen de decisiones

| # | Tema | Propuesta spec-agent | Decisión final | Estado |
|---|------|---------------------|----------------|--------|
| ED-08 | clusterUbicacionId rename | No-op (ya es ClusterId) | No-op | ✅ Aceptada |
| ED-09 | IDs DANE como PKs | Usar IDs DANE (50, 85, 68) | **IDs secuenciales (1, 2, 3)** | ❌ Revertida |
| ED-10 | HasData vs migración manual | Migración manual SQL | Migración manual SQL | ✅ Aceptada |
| ED-11 | Aguazul DANE 85015 | Corregir a 85010 | Corregir a 85010 + UPDATE staging | ✅ Aceptada |
| ED-12 | dotnet no en PATH | Script oficial dot.net | Script oficial → SDK 10.0.203 | ✅ Resuelta |
| ED-13 | Denominacion CI en InMemory | Same-case en tests | Same-case + comentario | ✅ Aceptada |
| ED-14 | Tenant isolation test | Dos tokens distintos | ADMIN(t1) crea, SUPERVISOR(t2) lee | ✅ Resuelta |
