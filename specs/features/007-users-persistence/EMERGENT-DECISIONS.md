# Iter 7 — Decisiones emergentes

Documento complementario a `plan.md` y `tasks.md`. Registra las decisiones que surgieron durante la implementación de Iter 7 (persistencia de usuarios en Azure SQL) y que no estaban explícitas en la especificación original. Cada decisión incluye: qué dice el plan, qué se implementó, la razón, y la deuda técnica que queda abierta (si aplica).

## 1. `IPasswordHasher` como abstracción de BCrypt

**Plan original:** el handler de login llama a `BCrypt.Net.BCrypt.Verify(...)` directamente.

**Implementado:** se introdujo la interfaz `GOP.Application.Common.Interfaces.IPasswordHasher` con la implementación `GOP.Infrastructure.Identity.BCryptPasswordHasher`. El handler (`LoginCommandHandler`) depende de la interfaz, no del paquete `BCrypt.Net-Next` directamente.

**Razón:** sacar la dependencia concreta de BCrypt fuera de la capa `Application` permite testear el handler con un `IPasswordHasher` fake y mantener coherencia con el resto de abstracciones de infraestructura (`IJwtTokenService`, `IUserRepository`, etc.). No suma runtime cost medible y sí simplifica los tests.

**Deuda técnica:** ninguna. Queda como convención para cuando Iter 9 traiga password reset/change.

## 2. Reflexión para `User.IsActive` en tests

**Plan original:** el test `Handle_InactiveUser_ReturnsFailure` verifica el bypass de login para usuarios inactivos (RN-029).

**Implementado:** `User.IsActive` es un setter privado y la entidad no expone `Deactivate()` aún. En el test se forzó el valor con reflexión (`typeof(User).GetProperty("IsActive")!.SetValue(user, false)`).

**Razón:** evitar abrir API de dominio solo para satisfacer un test. Cuando Iter 9 traiga RBAC y lifecycle de usuario real, ahí se agrega `Deactivate()` con su regla de negocio correspondiente.

**Deuda técnica:** reemplazar la reflexión por `user.Deactivate()` en Iter 9 (ítem: `#tech-debt-iter9-user-deactivate`).

## 3. Seeding con IDs canónicos via reflexión sobre `Entity.Id`

**Plan original (T024):** `GopTestWebApplicationFactory` siembra los 4 usuarios de desarrollo antes de cada test usando `User.Create(email, hash, name)`.

**Implementado:** se agregó un helper `SeedUser(Guid id, string email, string hash, string name)` que, después de llamar a `User.Create(...)`, sobrescribe `Entity.Id` con el GUID canónico via reflexión (`typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(user, id)`).

**Razón:** `Entity.Id` es `protected init Guid` con default `Guid.NewGuid()`. Si el factory siembra con IDs aleatorios, el token JWT que se genera para el test contiene el `sub` aleatorio, pero `SeedUserClaimsResolver` solo sabe mapear IDs canónicos (`550e8400-...-00000{1..4}`) a roles. Al no encontrar match, el resolver caía al default (`ADMIN`), lo que explicaba el bug 403→400 en `CreateWell_WithAuditorRole_Returns403`: el auditor llegaba al controller como ADMIN y la validación V2.0 respondía 400 en vez de 403.

**Deuda técnica:** este bypass de `init` es específico de tests. La solución definitiva llega con Iter 9 cuando `User.Role` sea un campo real en la entidad y `SeedUserClaimsResolver` desaparezca. En ese momento se elimina el helper.

## 4. Sync de `TestDbContext` con `IApplicationDbContext`

**Plan original:** `TestDbContext` en `GOP.Application.Tests` implementa `IApplicationDbContext` para testear handlers con EF InMemory.

**Implementado:** al correr por primera vez el CI, los tests fallaron porque `TestDbContext` no tenía los `DbSet<User>` y `DbSet<RefreshToken>` que se agregaron a `IApplicationDbContext` en B2. Se hizo el sync manual.

**Razón:** no hay check automatizado que fuerce a `TestDbContext` a estar en sync con la interfaz (son dos implementaciones distintas: EF Core real vs EF InMemory para tests). Es un riesgo conocido de tener dos DbContexts.

**Deuda técnica:** considerar para Iter 9+ un test de contrato que falle si `IApplicationDbContext` expone un `DbSet<T>` que `TestDbContext` no implementa. Bajo riesgo, bajo valor — se deja registrado pero sin owner asignado.

## 5. Backend CI workflow

**Plan original:** no mencionaba CI. Se asumía que los tests corrían localmente.

**Implementado:** `.github/workflows/backend-ci.yml` corre `dotnet restore`, `build` y `test` en `ubuntu-latest` con .NET 10 SDK sobre cada push/PR a `gop-base-web`.

**Razón:** con la persistencia ya en código productivo y 4 proyectos de test, tener verificación automatizada antes de merge es barato (≈40s por run) y previene regresiones. El workflow es mínimo adrede — sin matrix, sin cache agresivo — para facilitar revisión.

**Deuda técnica:** agregar cache de paquetes NuGet para bajar de 40s a <20s cuando el repo crezca. No urgente.

---

## Resumen de deuda técnica abierta hacia Iter 9

| # | Ítem | Prioridad | Owner |
|---|------|-----------|-------|
| 2 | Reemplazar reflexión `User.IsActive` por `Deactivate()` cuando exista la regla | Media | Iter 9 (RBAC/lifecycle) |
| 3 | Eliminar helper de seeding con reflexión cuando `SeedUserClaimsResolver` desaparezca | Media | Iter 9 (RBAC real) |
| 4 | Test de contrato que fuerce sync de `TestDbContext` con `IApplicationDbContext` | Baja | sin asignar |
| 5 | Cache de NuGet en CI | Baja | sin asignar |
