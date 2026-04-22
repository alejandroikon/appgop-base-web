# Tasks Backend: Persistencia de Users y RefreshTokens (007-users-persistence)

**Input:** `specs/features/007-users-persistence/spec.md` + `specs/features/007-users-persistence/plan.md`
**Contrato:** `specs/features/004-auth-api/contract.yml` (sin cambios)
**Constitución:** `CONSTITUTION.backend.md`

**Formato:** `[ID] [P?] [HU?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — sin dependencias de tareas incompletas en el mismo bloque
- **[HU-N]**: Historia de usuario de referencia en spec.md
- **[BLOQUEANTE]**: Tareas posteriores no pueden iniciar sin esta completada

---

## Bloque 1 — Domain (dotnet build GOP.Domain)

**Propósito:** Crear las entidades `User` y `RefreshToken` en el dominio. Sin dependencias externas.

- [ ] T001 [P] [HU-012] Crear entidad `User` — hereda de `Entity`; propiedades con setters privados: `Email` (string), `PasswordHash` (string), `FullName` (string), `IsActive` (bool, default true), `CreatedAt` (DateTime), `UpdatedAt` (DateTime?), `LastLoginAt` (DateTime?); factory method estático `User.Create(string email, string passwordHash, string fullName)` que setea `CreatedAt = DateTime.UtcNow`; método `RecordLogin()` que setea `LastLoginAt = DateTime.UtcNow` y `UpdatedAt = DateTime.UtcNow` — `backend/src/GOP.Domain/Entities/User.cs`

- [ ] T002 [P] [HU-013] Crear entidad `RefreshToken` — hereda de `Entity`; propiedades con setters privados: `UserId` (Guid), `Token` (string), `ExpiresAt` (DateTime), `CreatedAt` (DateTime), `RevokedAt` (DateTime?), `ReplacedByToken` (string?), `User` (navigation property, `User`, `= null!`); propiedad computed `bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow`; factory method estático `RefreshToken.Create(Guid userId, string token, DateTime expiresAt)` que setea `CreatedAt = DateTime.UtcNow`; método `Revoke(string? replacedByToken = null)` que setea `RevokedAt = DateTime.UtcNow` y `ReplacedByToken` — `backend/src/GOP.Domain/Entities/RefreshToken.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0.

---

## Bloque 2 — Application: Interfaces + Refactor Handlers (dotnet build GOP.Application)

**Propósito:** Crear las interfaces de repositorio y claims resolver. Refactorizar los handlers de autenticación para usar repositorios. Eliminar las interfaces in-memory viejas.

**⚠️ Orden estricto: las interfaces (T003–T005) deben existir antes del refactor de handlers (T008–T009). Los handlers refactorizados deben compilar antes de eliminar las interfaces viejas (T010–T011).**

- [ ] T003 [P] [HU-012] Crear interfaz `IUserRepository` — métodos: `GetByEmailAsync(string email, CancellationToken ct) → Task<User?>`, `GetByIdAsync(Guid id, CancellationToken ct) → Task<User?>`, `AddAsync(User user, CancellationToken ct) → Task`, `Update(User user) → void`, `ExistsAsync(string email, CancellationToken ct) → Task<bool>` — `backend/src/GOP.Application/Common/Interfaces/IUserRepository.cs`

- [ ] T004 [P] [HU-013] Crear interfaz `IRefreshTokenRepository` — métodos: `GetByTokenAsync(string token, CancellationToken ct) → Task<RefreshToken?>`, `AddAsync(RefreshToken rt, CancellationToken ct) → Task`, `RevokeAllForUserAsync(Guid userId, CancellationToken ct) → Task` — `backend/src/GOP.Application/Common/Interfaces/IRefreshTokenRepository.cs`

- [ ] T005 [P] [HU-012] Crear interfaz `IUserClaimsResolver` — método: `BuildProfile(User user) → UserProfileDto`; **transitoria** hasta Iter 9 (RBAC); encapsula resolución de Role/TenantId/TenantName para un `User` que aún no tiene esos campos — `backend/src/GOP.Application/Common/Interfaces/IUserClaimsResolver.cs`

- [ ] T006 [HU-012] [HU-013] Modificar `IApplicationDbContext` — agregar: `DbSet<User> Users { get; }`, `DbSet<RefreshToken> RefreshTokens { get; }`; agregar `using GOP.Domain.Entities` si `User` y `RefreshToken` no están ya importados — `backend/src/GOP.Application/Common/Interfaces/IApplicationDbContext.cs`

- [ ] T007 [BLOQUEANTE] Eliminar `JwtTokenOptions` — esta clase se reemplaza por lectura directa de `IOptions<JwtSettings>` inyectado ya existente en Infrastructure; los handlers refactorizados (T008, T009) usarán un simple record local o leerán directamente los valores de `JwtSettings` a través de la interfaz `IJwtTokenService`; **Nota:** antes de eliminar, verificar que T008 y T009 no dependen de `JwtTokenOptions` — si lo hacen, mantener esta clase y marcar como [EMERGENTE] su eliminación posterior; **Decisión alternativa:** si eliminar `JwtTokenOptions` genera cascada de cambios excesiva, conservarla y solo actualizar los handlers para usar también las nuevas interfaces. Evaluar al implementar — `backend/src/GOP.Application/Common/Interfaces/JwtTokenOptions.cs`

  > **Clarificación sobre T007:** `JwtTokenOptions` (en Application) duplica `JwtSettings` (en Infrastructure). Los handlers actuales dependen de `IOptions<JwtTokenOptions>` para leer `AccessTokenExpirationMinutes` y `RefreshTokenExpirationDays`. Dado que los handlers **ya** reciben `IJwtTokenService` (que internamente tiene acceso a `JwtSettings`), la opción más limpia es que los handlers lean la expiración del refresh token desde una nueva constante o parámetro del `IJwtTokenService`. **Sin embargo**, si esto genera cambios en la firma de `IJwtTokenService` que propaguen a `JwtTokenService` y todos los tests que mockean esta interfaz, es más pragmático **conservar `JwtTokenOptions`** y solo cambiar las dependencias de `IUserSeedStore`/`IRefreshTokenStore` en los handlers. **Decisión: conservar `JwtTokenOptions`, no eliminar en esta iteración.** Marcar T007 como OMITIDA.

- [ ] T008 [BLOQUEANTE] [HU-012] Refactorizar `LoginCommandHandler` — cambiar constructor: reemplazar `IUserSeedStore userStore` por `IUserRepository userRepo` + `IUserClaimsResolver claimsResolver` + `IUnitOfWork unitOfWork`; mantener `IJwtTokenService` y `IOptions<JwtTokenOptions>`; reemplazar `IRefreshTokenStore refreshTokenStore` por `IRefreshTokenRepository rtRepo`; cambiar flujo según plan.md §3.2: (1) normalizar email, (2) `await userRepo.GetByEmailAsync`, (3) verificar `IsActive` (RN-029), (4) BCrypt.Verify directo contra `user.PasswordHash`, (5) `claimsResolver.BuildProfile(user)`, (6-7) generar tokens, (8) `RefreshToken.Create(...)` + `await rtRepo.AddAsync`, (9) `user.RecordLogin()`, (10) `await unitOfWork.SaveChangesAsync`, (11) retornar TokenResponseDto; el método `Handle` ahora usa `await` real (no `Task.FromResult`) — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`

- [ ] T009 [BLOQUEANTE] [HU-013] Refactorizar `RefreshTokenCommandHandler` — cambiar constructor: reemplazar `IRefreshTokenStore` por `IRefreshTokenRepository rtRepo` + `IUnitOfWork unitOfWork`; reemplazar `IUserSeedStore userStore` por `IUserRepository userRepo` + `IUserClaimsResolver claimsResolver`; mantener `IJwtTokenService` y `IOptions<JwtTokenOptions>`; cambiar flujo según plan.md §3.2: (1) `await rtRepo.GetByTokenAsync` con tracking, (2) null → InvalidRefreshToken, (3) `!oldRt.IsActive` → distinguir revocado vs expirado, (4-5) `await userRepo.GetByIdAsync` + verificar `IsActive`, (6) `claimsResolver.BuildProfile`, (7-8) generar nuevo RT + `rtRepo.AddAsync`, (9) `oldRt.Revoke(newRt.Token)`, (10) `await unitOfWork.SaveChangesAsync`, (11) retornar; el método `Handle` ahora usa `await` real — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`

- [ ] T010 [P] Eliminar `IUserSeedStore` — ya no referenciada por ningún handler; toda la funcionalidad migrada a `IUserRepository` + `IUserClaimsResolver`; eliminar el archivo completo — `backend/src/GOP.Application/Common/Interfaces/IUserSeedStore.cs`

- [ ] T011 [P] Eliminar `IRefreshTokenStore` — ya no referenciada por ningún handler; toda la funcionalidad migrada a `IRefreshTokenRepository`; incluye el record `RefreshTokenEntry` que vivía en el mismo archivo; eliminar el archivo completo — `backend/src/GOP.Application/Common/Interfaces/IRefreshTokenStore.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0.

---

## Bloque 3 — Infrastructure: Repos + Configs + Seeder + Migración (dotnet build GOP.Infrastructure)

**Propósito:** Implementar repositorios EF Core, configuraciones de entidad, seeder de usuarios, actualizar DI, eliminar stores in-memory y generar migración.

**⚠️ Orden estricto: Configurations (T012–T013) y DbContext (T014) antes de Repositories (T015–T016). DI (T019) después de todos los archivos nuevos. Eliminaciones (T020–T021) después de DI. Migración (T022) al final.**

- [ ] T012 [P] [HU-012] Crear `UserConfiguration` — `IEntityTypeConfiguration<User>`; tabla `"Users"`; `HasKey(u => u.Id)`; `Email`: required, maxLength 256, HasIndex unique con nombre `"IX_Users_Email"`; `PasswordHash`: required, maxLength 255; `FullName`: required, maxLength 200; `IsActive`: default value `true`; `CreatedAt`: required; `UpdatedAt` y `LastLoginAt` sin config adicional (nullable por convención) — `backend/src/GOP.Infrastructure/Persistence/Configurations/UserConfiguration.cs`

- [ ] T013 [P] [HU-013] Crear `RefreshTokenConfiguration` — `IEntityTypeConfiguration<RefreshToken>`; tabla `"RefreshTokens"`; `HasKey(rt => rt.Id)`; `Token`: required, maxLength 512, HasIndex unique `"IX_RefreshTokens_Token"`; composite index `(UserId, RevokedAt)` con nombre `"IX_RefreshTokens_UserId_RevokedAt"`; `ExpiresAt`: required; `CreatedAt`: required; `ReplacedByToken`: maxLength 512; FK: `HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade)` — `backend/src/GOP.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`

- [ ] T014 [BLOQUEANTE] [HU-012] [HU-013] Modificar `GopDbContext` — agregar: `public DbSet<User> Users => Set<User>();` y `public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();`; agregar `using GOP.Domain.Entities` si `User` y `RefreshToken` no están ya importados (verificar que `Well` ya está importado desde ese namespace) — `backend/src/GOP.Infrastructure/Persistence/GopDbContext.cs`

- [ ] T015 [P] [HU-012] Crear `UserRepository` — `internal sealed class UserRepository(GopDbContext context) : IUserRepository`; `GetByEmailAsync`: `FirstOrDefaultAsync(u => u.Email == email)` con tracking (usado en Command para RecordLogin); `GetByIdAsync`: `FirstOrDefaultAsync(u => u.Id == id)` con tracking; `AddAsync`: `context.Users.AddAsync`; `Update`: `context.Users.Update` (void sincrónico); `ExistsAsync`: `context.Users.AnyAsync(u => u.Email == email)` — `backend/src/GOP.Infrastructure/Persistence/Repositories/UserRepository.cs`

- [ ] T016 [P] [HU-013] Crear `RefreshTokenRepository` — `internal sealed class RefreshTokenRepository(GopDbContext context) : IRefreshTokenRepository`; `GetByTokenAsync`: `FirstOrDefaultAsync(rt => rt.Token == token)` con tracking; `AddAsync`: `context.RefreshTokens.AddAsync`; `RevokeAllForUserAsync`: query tokens donde `UserId == userId && RevokedAt == null`, `ToListAsync`, luego `foreach rt.Revoke(null)` (cambios trackeados, caller llama SaveChanges) — `backend/src/GOP.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`

- [ ] T017 [P] [HU-012] Crear `SeedUserClaimsResolver` — `internal sealed class SeedUserClaimsResolver : IUserClaimsResolver`; `BuildProfile(User user)`: intenta `SeedUsers.GetById(user.Id)` → si no null, construye `UserProfileDto` con `user.Id, user.Email, user.FullName` + `seedProfile.Role, seedProfile.TenantId, seedProfile.TenantName`; si null → defaults `"ADMIN", "1", "Agencia Nacional de Hidrocarburos"`; **transitorio hasta Iter 9** — `backend/src/GOP.Infrastructure/Identity/SeedUserClaimsResolver.cs`

- [ ] T018 [HU-014] Crear `UserSeeder` — `public sealed class UserSeeder(IUserRepository userRepo, IUnitOfWork unitOfWork, IConfiguration configuration, IHostEnvironment environment, ILogger<UserSeeder> logger)`; método `SeedAsync(CancellationToken ct = default)`: (1) leer sección `SeedUsers` de config, (2) si sección no existe: si `Development` → sembrar 4 dev users desde `SeedUsers` estático (leer IDs y hashes de `SeedUsers.FindByEmail` / propiedades estáticas, crear entidades `User.Create(email, hash, name)`, `AddAsync` si no `ExistsAsync`), si no Development → log warning y retornar, (3) si sección existe → para cada admin (ExecAdmin, OpAdmin): leer Email/Password/FullName de config con defaults del spec §7.3, si Password vacío → `throw InvalidOperationException("SeedUsers:{key}:Password not configured. Seed aborted.")`, si `ExistsAsync(email)` → log skip, sino → `BCrypt.HashPassword(password)` + `User.Create(...)` + `AddAsync`, (4) `await unitOfWork.SaveChangesAsync(ct)` — `backend/src/GOP.Infrastructure/Persistence/UserSeeder.cs`

- [ ] T019 [BLOQUEANTE] Modificar `DependencyInjection.cs` — **Agregar** (después de los repositorios existentes): `services.AddScoped<IUserRepository, UserRepository>()`, `services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>()`, `services.AddScoped<IUserClaimsResolver, SeedUserClaimsResolver>()`, `services.AddScoped<UserSeeder>()`; **Eliminar**: la línea `services.AddScoped<IUserSeedStore, UserSeedStoreAdapter>()` y la línea `services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>()`; agregar los `using` necesarios para los nuevos tipos — `backend/src/GOP.Infrastructure/DependencyInjection.cs`

- [ ] T020 [P] Eliminar `InMemoryRefreshTokenStore.cs` — ya no referenciada en DI ni en ningún otro archivo; eliminar el archivo completo — `backend/src/GOP.Infrastructure/Identity/InMemoryRefreshTokenStore.cs`

- [ ] T021 [P] Eliminar `UserSeedStoreAdapter.cs` — ya no referenciada en DI ni en ningún otro archivo; eliminar el archivo completo — `backend/src/GOP.Infrastructure/Identity/UserSeedStoreAdapter.cs`

- [ ] T022 Generar migración EF Core — ejecutar: `cd backend && dotnet ef migrations add AddUsersAndRefreshTokens -p src/GOP.Infrastructure -s src/GOP.API`; verificar que la migración genera: tabla `Users` con PK, columnas, índice único `IX_Users_Email`; tabla `RefreshTokens` con PK, columnas, FK a `Users.Id` con cascade delete, índice único `IX_RefreshTokens_Token`, índice compuesto `IX_RefreshTokens_UserId_RevokedAt`; verificar que **ninguna** tabla existente (Wells, Contratos, Campos, etc.) se modifica en la migración generada — `backend/src/GOP.Infrastructure/Migrations/{timestamp}_AddUsersAndRefreshTokens.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0.

---

## Bloque 4 — API: Startup + Documentación (dotnet build GOP.API)

**Propósito:** Integrar el `UserSeeder` en el pipeline de arranque. Actualizar documentación de proyecto.

- [ ] T023 [HU-014] Modificar `MigrationExtension.cs` — después del bloque `await seeder.SeedAsync()` (seed DANE) y su log `"Seed completado."`, agregar: `var userSeeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();`, `logger.LogInformation("Ejecutando seed de usuarios...");`, `await userSeeder.SeedAsync();`, `logger.LogInformation("Seed de usuarios completado.");`; agregar `using GOP.Infrastructure.Persistence;` si `UserSeeder` no está ya importado (verificar namespace) — `backend/src/GOP.API/Extensions/MigrationExtension.cs`

- [ ] T024 Modificar `CLAUDE.md` — en la sección "## Infraestructura Azure (Staging)", después de la subsección "Convención de secrets en Key Vault", agregar nueva subsección `### Secrets de seed de usuarios` con: (1) tabla de secrets en Key Vault: `SeedUsers--ExecAdmin--Password` y `SeedUsers--OpAdmin--Password`, (2) nota de que estos secrets los carga Alejandro manualmente desde Azure Cloud Shell antes del primer redeploy post-merge, (3) nota de que los passwords deben ser aleatorios fuertes (16+ chars, mix alfanum + símbolos) y guardarse en password manager, (4) nota de que la User-Assigned Managed Identity del App Service ya tiene rol `Key Vault Secrets User` sobre `kv-gop360-staging`, (5) referencia a `specs/features/007-users-persistence/spec.md §7` para detalle de comportamiento del seeder; también actualizar sección "## Feature Activa" para reflejar esta feature — `CLAUDE.md`

**Checkpoint:** `cd backend && dotnet build GOP.sln` → exit code 0. Verificar que la solución completa compila sin errores.

---

## Bloque 5 — Tests (dotnet test GOP.sln)

**Propósito:** Actualizar tests existentes de auth para los nuevos mocks. Crear tests nuevos para repositorios y seeder usando SQLite in-memory. Actualizar integration tests.

**Depende de:** Bloques 1–4 completos.

**⚠️ Orden: T025 (NuGet) primero. T026 (factory) antes de T032 (integration tests). Tests unitarios (T027–T031) son paralelos entre sí.**

- [ ] T025 [BLOQUEANTE] Agregar paquete `Microsoft.EntityFrameworkCore.Sqlite` a `GOP.Infrastructure.Tests` — ejecutar: `cd backend && dotnet add tests/GOP.Infrastructure.Tests package Microsoft.EntityFrameworkCore.Sqlite`; verificar que se agrega al `.csproj` — `backend/tests/GOP.Infrastructure.Tests/GOP.Infrastructure.Tests.csproj`

- [ ] T026 [BLOQUEANTE] Modificar `GopTestWebApplicationFactory` — después de registrar el InMemory DbContext y sus interfaces, agregar seed de usuarios dev en la DB InMemory; dentro de `ConfigureServices`, después del bloque existente, agregar un bloque que: (1) construye un `ServiceProvider` temporal, (2) obtiene `GopDbContext`, (3) llama `db.Database.EnsureCreated()`, (4) inserta los 4 dev users directamente vía `db.Users.AddRange(...)` usando datos de `SeedUsers.FindByEmail` (IDs, emails, password hashes conocidos de `SeedUsers.cs`) con `User.Create(email, hash, fullName)`, (5) llama `db.SaveChanges()`; esto garantiza que los integration tests de login encuentran usuarios en la DB — `backend/tests/GOP.API.Tests/Fixtures/GopTestWebApplicationFactory.cs`

- [ ] T027 [P] [HU-012] Reescribir `LoginCommandHandlerTests` — reemplazar mocks: `IUserSeedStore` → `IUserRepository`, `IRefreshTokenStore` → `IRefreshTokenRepository`; agregar nuevos mocks: `IUserClaimsResolver`, `IUnitOfWork`; crear `User` de test vía `User.Create("admin@gop.co", BCrypt.HashPassword("Admin123*"), "Admin")` para poder mockear `userRepo.GetByEmailAsync` retornando entidad; mockear `claimsResolver.BuildProfile(Arg.Any<User>()).Returns(TestUserProfile)` donde `TestUserProfile` es un `UserProfileDto` con datos conocidos; actualizar constructor del SUT: `new LoginCommandHandler(userRepo, rtRepo, claimsResolver, jwtTokenService, jwtOptions, unitOfWork)`; mantener los 4 tests existentes adaptados + agregar **test nuevo** `Handle_InactiveUser_ReturnsFailure`: crear User con `IsActive = false` (usar reflection o método internal si no hay setter público), mockear `GetByEmailAsync` retornando el user inactivo → verificar `result.Error == DomainErrors.Auth.InvalidCredentials`; verificar en `Handle_ValidCredentials` que `unitOfWork.Received(1).SaveChangesAsync()` — `backend/tests/GOP.Application.Tests/Features/Auth/LoginCommandHandlerTests.cs`

- [ ] T028 [P] [HU-013] Reescribir `RefreshTokenCommandHandlerTests` — reemplazar mocks: `IRefreshTokenStore` → `IRefreshTokenRepository`, `IUserSeedStore` → `IUserRepository`; agregar: `IUserClaimsResolver`, `IUnitOfWork`; crear `RefreshToken` de test vía `RefreshToken.Create(userId, "test-token", DateTime.UtcNow.AddDays(7))`; mockear `rtRepo.GetByTokenAsync("test-token")` retornando la entidad; mockear `userRepo.GetByIdAsync(userId)` retornando `User` activo; mockear `claimsResolver.BuildProfile(...)` retornando `UserProfileDto`; mantener los 4 tests existentes adaptados: (1) `Handle_ValidRefreshToken_ReturnsNewTokenPair` — verificar `oldRt.Revoke()` fue llamado (verificar `RevokedAt != null` en la entidad o verificar `unitOfWork.SaveChangesAsync` recibido), (2) `Handle_ExpiredRefreshToken_ReturnsFailure` — crear RT con `ExpiresAt` en el pasado → `TokenExpired`, (3) `Handle_InvalidRefreshToken_ReturnsFailure` — `rtRepo.GetByTokenAsync` retorna null, (4) `Handle_AlreadyUsedRefreshToken_ReturnsFailure` — crear RT y llamar `rt.Revoke()` antes del test → `InvalidRefreshToken`; agregar **test nuevo** `Handle_InactiveUserRefresh_ReturnsFailure` — RT válido pero user tiene `IsActive = false` → `InvalidRefreshToken` — `backend/tests/GOP.Application.Tests/Features/Auth/RefreshTokenCommandHandlerTests.cs`

- [ ] T029 [P] [HU-012] Crear `UserRepositoryTests` — usar SQLite in-memory (patrón de plan.md §5.2: `SqliteConnection("DataSource=:memory:")`, `OpenAsync`, `UseSqlite`, `EnsureCreatedAsync`); implementar `IAsyncLifetime` para setup/teardown; 6 tests: (1) `GetByEmailAsync_ExistingUser_ReturnsUser` — insert via context, buscar via repo, (2) `GetByEmailAsync_NonExistent_ReturnsNull`, (3) `AddAsync_DuplicateEmail_ThrowsException` — insertar 2 users con mismo email → `await context.SaveChangesAsync()` lanza excepción (SQLite valida unique constraint), (4) `ExistsAsync_ExistingEmail_ReturnsTrue`, (5) `ExistsAsync_NonExistent_ReturnsFalse`, (6) `UpdateAsync_RecordLogin_PersistsLastLoginAt` — crear user, `user.RecordLogin()`, `repo.Update(user)`, `SaveChangesAsync`, re-leer y verificar `LastLoginAt` — `backend/tests/GOP.Infrastructure.Tests/Persistence/UserRepositoryTests.cs`

- [ ] T030 [P] [HU-013] Crear `RefreshTokenRepositoryTests` — SQLite in-memory (mismo patrón); implementar `IAsyncLifetime`; pre-insertar un `User` en setup (FK requirement); 4 tests: (1) `AddAndGetByToken_ReturnsToken` — create RT via repo, buscar por token string, (2) `GetByTokenAsync_NonExistent_ReturnsNull`, (3) `RevokeAllForUserAsync_RevokesAllActive` — insertar 3 RTs para un user (1 ya revocado via `rt.Revoke()`), llamar `RevokeAllForUserAsync`, `SaveChangesAsync`, verificar que los 2 activos ahora tienen `RevokedAt != null` y el ya revocado no cambió, (4) `CascadeDelete_DeletingUser_DeletesTokens` — insertar user + 2 RTs, borrar user via `context.Users.Remove`, `SaveChangesAsync`, verificar que `context.RefreshTokens.Count() == 0` (SQLite valida cascade) — `backend/tests/GOP.Infrastructure.Tests/Persistence/RefreshTokenRepositoryTests.cs`

- [ ] T031 [P] [HU-014] Crear `UserSeederTests` — SQLite in-memory (mismo patrón); necesita mockear `IConfiguration` y `IHostEnvironment` con `NSubstitute`; **agregar NuGet `NSubstitute`** a `GOP.Infrastructure.Tests.csproj` si no está (verificar — actualmente NO está en ese proyecto); 4 tests: (1) `SeedAsync_EmptyDb_InsertsDevUsers_WhenNoConfig` — environment = Development, sin sección SeedUsers en config → seeder inserta 4 dev users desde SeedUsers estático, (2) `SeedAsync_UsersAlreadyExist_NoChanges` — pre-insertar los 4 users, ejecutar seeder → count sigue siendo 4, (3) `SeedAsync_WithConfig_InsertsConfiguredAdmins` — configurar sección SeedUsers con ExecAdmin y OpAdmin (email, password, fullName) → inserta 2 users con emails configurados, (4) `SeedAsync_MissingPassword_ThrowsException` — configurar sección con email pero sin password → `Assert.ThrowsAsync<InvalidOperationException>` — `backend/tests/GOP.Infrastructure.Tests/Persistence/UserSeederTests.cs`

- [ ] T032 [HU-012] [HU-013] Modificar `AuthControllerTests` — mantener los 6 tests existentes (adaptarlos si cambia el seed en la factory); agregar 2 **tests nuevos**: (1) `Refresh_UsedToken_Returns401` — hacer login, refresh exitoso, intentar refresh con el token original (ya revocado) → 401, (2) `Login_PersistsRefreshTokenInDatabase` — hacer login, obtener el `GopDbContext` desde el factory `Services`, verificar que `context.RefreshTokens.AnyAsync(rt => rt.Token == refreshTokenFromResponse)` es `true` — `backend/tests/GOP.API.Tests/Controllers/AuthControllerTests.cs`

**Checkpoint:** `cd backend && dotnet test GOP.sln` → todos los tests pasan, exit code 0.

---

## Dependencies & Execution Order

### Dependencias entre Bloques

```
Bloque 1 (Domain)         → Sin dependencias de esta feature. Depende del código Domain existente (Entity base class).
Bloque 2 (Application)    → Depende de Bloque 1 (entidades User, RefreshToken).
Bloque 3 (Infrastructure) → Depende de Bloque 2 (interfaces IUserRepository, IRefreshTokenRepository, IUserClaimsResolver).
Bloque 4 (API + Docs)     → Depende de Bloque 3 (UserSeeder registrado en DI, migración generada).
Bloque 5 (Tests)          → Depende de Bloques 1-4 completos.
```

### Diagrama

```
B1 ──→ B2 ──→ B3 ──→ B4 ──→ B5
```

Secuencial estricto. Cada bloque es commitable con el proyecto compilando al cierre.

### Dependencias entre Tareas dentro de Bloques

**Bloque 2:**
```
T003, T004, T005 ──(paralelo)──→ T006 ──→ T008, T009 ──(secuencial)──→ T010, T011 (paralelo)
```
T007: OMITIDA (se conserva JwtTokenOptions — ver nota en la tarea).

**Bloque 3:**
```
T012, T013 ──(paralelo)──→ T014 ──→ T015, T016, T017, T018 ──(paralelo)──→ T019 ──→ T020, T021 ──(paralelo)──→ T022
```

**Bloque 5:**
```
T025 ──→ T026 ──→ T027, T028, T029, T030, T031 ──(paralelo)──→ T032
```

### Resumen de conteos

| Categoría | Cantidad |
|---|---|
| Archivos a crear | 12 |
| Archivos a modificar | 9 |
| Archivos a eliminar | 4 |
| Migración a generar | 1 |
| NuGets a agregar | 1 (+ posiblemente NSubstitute en Infrastructure.Tests) |
| Tests nuevos | ~22 test methods |
| Tests modificados | ~10 test methods (reescritos con nuevos mocks) |
