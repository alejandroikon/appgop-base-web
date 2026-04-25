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

## Resumen de decisiones

| # | Tema | Propuesta spec-agent | Decisión final | Estado |
|---|------|---------------------|----------------|--------|
| ED-08 | clusterUbicacionId rename | No-op (ya es ClusterId) | No-op | ✅ Aceptada |
| ED-09 | IDs DANE como PKs | Usar IDs DANE (50, 85, 68) | **IDs secuenciales (1, 2, 3)** | ❌ Revertida |
| ED-10 | HasData vs migración manual | Migración manual SQL | Migración manual SQL | ✅ Aceptada |
| ED-11 | Aguazul DANE 85015 | Corregir a 85010 | Corregir a 85010 + UPDATE staging | ✅ Aceptada |
