# EMERGENT-DECISIONS — Iter 8 · Wells Persistence + UWI

Decisiones que surgieron durante la ejecución de Fase 0 (validación) y que no estaban previstas en `blueprint.md` ni `plan.md`.

---

## ED-01: El plan.md asume código que no existe — la mayoría ya está implementado

**Contexto:** El `plan.md` fue escrito asumiendo que habría que crear entidades, enums, VOs, handlers y controllers desde cero. Al inspeccionar la rama `gop-base-web`, TODO el backend CRUD de pozos ya existe:
- `Well` entity con `CreateDraft`/`CreateFinalized` factory methods
- 6 enums completos (`TipoTrayectoria`, `Clasificacion`, `WellStatus`, `TipoAngulo`, `TipoObjetivo`, `TipoTerminacion`)
- `Uwi` Value Object con algoritmo PPDM completo (`Uwi.Generate()`)
- `CreateWellCommand`/Handler con lógica DRAFT/FINALIZE
- `PreviewUwiQuery`/Handler funcional
- `WellsController` con POST/GET/PUT/DELETE + preview-uwi + preview-name
- `WellRepository` con `ExistsByUwiAsync`/`ExistsByNameAsync`
- 4 migraciones EF Core ya aplicadas

**Decisión:** Las tareas de Fase 1 (B1-B4) del `tasks.md` NO son "crear desde cero". Son **validar, ajustar y completar** lo existente contra el contrato V2. Redefinir alcance de cada tarea como delta sobre lo que ya existe.

**Impacto:** Reduce drásticamente el esfuerzo de Fase 1. Cambia el foco de "implementar" a "verificar conformidad + agregar lo que falta".

---

## ED-02: TenantId es `int` (no `Guid`), no hay tabla Operators

**Contexto:** El `plan.md` §4.3 habla de `OperatorId` como `Guid` y asume que existe una tabla `Operators`. En realidad:
- `Well.TenantId` es `int`
- `ICurrentUserService.TenantId` retorna `int`
- El JWT emite `tenant_id` como string que se parsea a int
- No existe entidad `Operator` con tabla propia
- El nombre de operadora se resuelve desde `SeedUsers.cs` (hardcoded como `TenantName`)

**Decisión:** Mantener `TenantId` (int) como está. No crear tabla Operators en Iter 8 — el patrón actual funciona para el demo single-operator. La tabla Operators es deuda para Iter 9 (RBAC multi-tenant).

**Impacto:** El plan.md §4.1 con `OwnsOne(w => w.WellName, ...)` y VO `FiscalizedUwi` como owned entities NO aplica — la Well entity usa propiedades planas. No modificar el diseño existente.

---

## ED-03: Discrepancia en formato UWI — trayectoria Original = vacío vs "O"

**Contexto:** 
- El `api-contract.md` §4.1 muestra UWI `"50568RUBI0157RU0000VOOPH–CC"` con "O" explícito para trayectoria Original
- El código `Uwi.GenerateTrayectoriaCode(TipoTrayectoria.O)` retorna `string.Empty` (sin "O")
- Esto produce `"50568RUBI0157CN0000VPH-CC"` (sin O entre V y PH)
- Además el código usa guión ASCII `-` y el contrato usa guión largo `–`

**Datos del contrato V2 (api-contract.md §4.7 preview-uwi response):**
```
components.trajectoryCode: "O"     ← código explícito
fiscalizedUwi: "50568RUBI0157RU0000VOOPH–CC"
```

**Decisión (resuelta 2026-04-23):** "O" explícito. Se modificó `Uwi.GenerateTrayectoriaCode()`:
```csharp
TipoTrayectoria.O => "O",  // antes: string.Empty
```

**Commit:** `fix(domain): ED-03 — trayectoria Original emite 'O' explícito en UWI`

**Impacto:** Staging no tiene pozos reales → sin migración de datos necesaria.

---

## ED-04: Discrepancia en formato del separador UWI — guión ASCII vs guión largo

**Contexto:** El código `Uwi.Generate()` usa guión ASCII `-` para separar el cuerpo de la terminación. El api-contract.md y los specs V2 usan guión largo `–` (en-dash, U+2013).

Ejemplo código: `50568RUBI0157CN0000VPH-CC`
Ejemplo contrato: `50568RUBI0157RU0000VOOPH–CC`

**Decisión (resuelta 2026-04-23):** ASCII `-`. Mantener el código actual. Es más seguro para BD, URLs, logs y comparaciones string.

**Impacto:** Ninguno. El código ya usa ASCII `-`.

---

## ED-05: No hay Global Query Filter por TenantId — gap de tenant isolation

**Contexto:** El `plan.md` §4.1 dice: "Global query filter por `OperatorId` vía `ICurrentUserService`". En realidad:
- `WellConfiguration` tiene `HasQueryFilter(w => !w.IsDeleted)` (soft delete) pero **NO** filtra por TenantId
- `GetWellsListQueryHandler` filtra manualmente: `query.Where(w => w.TenantId == currentUser.TenantId)` 
- `GetWellByIdQueryHandler` NO filtra por TenantId — un usuario de tenant 2 puede hacer GET de un pozo de tenant 1 si conoce el ID

**Decisión:** Agregar global query filter por TenantId al `WellConfiguration` es la corrección correcta (CONSTITUTION.backend.md §7.2 pattern). Pero esto requiere:
1. Inyectar `ICurrentUserService` en `WellConfiguration` (o usar `IServiceProvider` en `OnModelCreating`)
2. Los queries que necesitan cross-tenant (como `ExistsByNameAsync`) deben usar `IgnoreQueryFilters()`
3. El seed no corre con HTTP context → el filter debe manejar `TenantId = 0` gracefully

**Impacto:** Cambio de infraestructura que afecta todas las queries de Wells. Prioridad alta por ser gap de seguridad (A01 OWASP).

**Decisión (resuelta 2026-04-23):** Implementado en esta iteración.

**Commit:** `feat(infra): ED-05 — global query filter por TenantId en Wells`

**Implementación:**
- `GopDbContext` inyecta `ICurrentUserService`, captura `_currentTenantId` en constructor
- `HasQueryFilter`: `!IsDeleted && (_currentTenantId == 0 || w.TenantId == _currentTenantId)`
- `TenantId == 0` = bypass (seed, migrations, sin HTTP context)
- `WellRepository.ExistsByUwiAsync/ExistsByNameAsync` usan `IgnoreQueryFilters()` + `!IsDeleted` manual
- `GetWellByIdQueryHandler` y `GetWellsListQueryHandler` ya tenían bypass para ADMIN/AUDITOR

---

## ED-06: El campo `Consecutivo` en la entidad es `int`, pero el contrato V2 lo envía como `string`

**Contexto:**
- `Well.Consecutivo` es `int` en la entidad
- `CreateWellCommand.Consecutivo` es `int?`
- El api-contract.md §4.1 muestra `"consecutive": "157"` como **string**
- La regla RN-18 dice "solo dígitos" — el plan.md valida `^\d+$` (regex de string)

**Decisión:** El código actual ya parsea el consecutivo como int, lo cual implícitamente valida "solo dígitos". La discrepancia es solo en el wire format (string vs int). El frontend envía un int numérico desde el form. Mantener como está (int). Si en el futuro se necesitan consecutivos con leading zeros (ej. "0157"), cambiar a string.

**Impacto:** Ninguno inmediato. El wire format del POST acepta tanto `"157"` (string) como `157` (int) gracias a `System.Text.Json` coercion.

---

## ED-07: No hay .NET SDK en el entorno de ejecución del agente

**Contexto:** El entorno de CI/ejecución disponible tiene Node.js 22 pero NO tiene .NET SDK instalado. No es posible:
- Compilar el backend (`dotnet build`)
- Generar migraciones (`dotnet ef migrations add`)
- Ejecutar tests (`dotnet test`)

**Decisión:** El trabajo de backend en esta sesión se limita a:
1. Validación por lectura de código (Fase 0)
2. Escritura de archivos .cs que compilen correctamente (verificable por review)
3. Documentación de cambios necesarios

La compilación, migraciones y tests se delegan al CI pipeline o a una sesión con .NET SDK.

**Impacto:** Los PRs de backend no pueden incluir verificación de `dotnet build` en esta sesión. Se confía en el CI para validación.

---

## Resumen de estado por tarea del tasks.md

| Tarea | Estado post-validación |
|---|---|
| T0.1 Estructura feature | ✅ Completada (README + EMERGENT-DECISIONS creados) |
| T0.2 Validación catálogos | ✅ Catálogos suficientes para demo. Sin discrepancias de codes |
| T0.3 Validación JWT | ✅ tenant_id ya en JWT. No requiere cambio |
| T0.4 Seed user → operator | ✅ operador@gop.co (TenantId=2, Ecopetrol) usable |
| T1.1-T1.5 Domain | ⚡ Ya existe. Ajustar solo si ED-03/ED-04 se resuelven |
| T1.6-T1.10 Application | ⚡ Ya existe. Ajustar shapes de DTO si cambio de contrato |
| T1.11 EF Configuration | ⚠️ Falta global query filter TenantId (ED-05) |
| T1.12 Migration | ⚠️ Sin .NET SDK, no generable en esta sesión |
| T1.13 Repository | ✅ Ya existe |
| T1.14 JWT operator claim | ✅ Ya resuelto con tenant_id |
| T1.15-T1.17 API | ⚡ Ya existe. 501 placeholders para diferidos: no implementados |
| T1.18 Tests integración | ⚠️ Sin .NET SDK |
| T2.1-T2.9 Frontend | 🔄 Pendiente — requiere npm install + análisis |

Leyenda: ✅ hecho, ⚡ ya existe (verificar), ⚠️ bloqueado/parcial, 🔄 pendiente
