# Plan Backend: Persistencia de Users y RefreshTokens

**Feature ID:** 007-users-persistence
**Spec de referencia:** `specs/features/007-users-persistence/spec.md`
**Contrato de referencia:** `specs/features/004-auth-api/contract.yml` (sin cambios — los endpoints mantienen shape idéntico)
**Constitución de referencia:** `CONSTITUTION.backend.md`
**Depende de:** 004-auth-api (completado e implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Resumen Arquitectónico

Esta feature reemplaza el almacenamiento in-memory de usuarios y refresh tokens por persistencia en SQL Server vía EF Core. Afecta las 4 capas del backend + tests.

**Cambio fundamental:** los handlers de autenticación (`LoginCommandHandler`, `RefreshTokenCommandHandler`) pasan de depender de `IUserSeedStore` + `IRefreshTokenStore` (in-memory) a depender de `IUserRepository` + `IRefreshTokenRepository` (EF Core) + `IUnitOfWork`.

**Lo que NO cambia:**
- `AuthController` — mismas 3 actions, mismo routing, mismos status codes.
- `TokenResponseDto` / `UserProfileDto` — misma estructura.
- `LoginCommand`, `RefreshTokenCommand`, `GetCurrentUserQuery` — mismos records.
- `LoginCommandValidator`, `RefreshTokenCommandValidator` — mismas reglas.
- `IJwtTokenService`, `JwtTokenService`, `JwtSettings` — intactos.
- `CurrentUserService` — intacto.

### 1.1. Decisión transitoria: claims de Role/TenantId

La entidad `User` no incluye `Role`, `TenantId`, `TenantName` (se agregan en Iter 9 — RBAC). Sin embargo, `UserProfileDto` los requiere para generar el JWT.

**Solución transitoria:** se introduce `IUserClaimsResolver` (interfaz en Application, implementación en Infrastructure). Este servicio recibe una entidad `User` y retorna un `UserProfileDto` enriquecido con claims de rol/tenant:

- Para usuarios conocidos del seed estático (`admin@gop.co`, `supervisor@gop.co`, etc.) → lee claims de `SeedUsers.GetById`.
- Para usuarios no reconocidos en el seed estático (los admin reales: `alejandro.gutierrez@interkont.co`, `admin@interkont.co`) → defaults: `ADMIN` / `"1"` / `"Agencia Nacional de Hidrocarburos"`.

Esto es **deuda técnica consciente** documentada en §7.

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
backend/src/
├── GOP.Domain/
│   └── Entities/
│       ├── User.cs                                             # Entidad: Id, Email, PasswordHash, FullName, IsActive, CreatedAt, UpdatedAt, LastLoginAt
│       └── RefreshToken.cs                                     # Entidad: Id, UserId, Token, ExpiresAt, CreatedAt, RevokedAt, ReplacedByToken
│
├── GOP.Application/
│   └── Common/
│       └── Interfaces/
│           ├── IUserRepository.cs                              # GetByEmailAsync, GetByIdAsync, AddAsync, UpdateAsync, ExistsAsync
│           ├── IRefreshTokenRepository.cs                      # GetByTokenAsync, AddAsync, RevokeAsync, RevokeAllForUserAsync
│           └── IUserClaimsResolver.cs                          # BuildProfile(User) → UserProfileDto (transitorio hasta Iter 9)
│
├── GOP.Infrastructure/
│   ├── Identity/
│   │   └── SeedUserClaimsResolver.cs                           # Implementa IUserClaimsResolver: SeedUsers lookup + defaults
│   └── Persistence/
│       ├── Configurations/
│       │   ├── UserConfiguration.cs                            # IEntityTypeConfiguration<User>: tabla Users, índice único Email CI
│       │   └── RefreshTokenConfiguration.cs                    # IEntityTypeConfiguration<RefreshToken>: tabla RefreshTokens, FK, índices
│       ├── Repositories/
│       │   ├── UserRepository.cs                               # Implementa IUserRepository con GopDbContext
│       │   └── RefreshTokenRepository.cs                       # Implementa IRefreshTokenRepository con GopDbContext
│       └── UserSeeder.cs                                       # Seeder idempotente: lee IConfiguration, hashea BCrypt, persiste
│
└── GOP.API/
    (sin archivos nuevos)

backend/tests/
├── GOP.Application.Tests/
│   └── Features/
│       └── Auth/
│           └── (archivos existentes se MODIFICAN, no se crean nuevos)
│
├── GOP.Infrastructure.Tests/
│   └── Persistence/
│       ├── UserRepositoryTests.cs                              # CRUD + constraint de email único
│       ├── RefreshTokenRepositoryTests.cs                      # Lifecycle: add → get → revoke → revokeAll
│       └── UserSeederTests.cs                                  # Idempotencia, password faltante, sin sección config
│
└── GOP.API.Tests/
    └── (archivos existentes se MODIFICAN)
```

### 2.2. Archivos a MODIFICAR

```
backend/src/
├── GOP.Application/
│   └── Common/
│       └── Interfaces/
│           └── IApplicationDbContext.cs                         # + DbSet<User> Users, DbSet<RefreshToken> RefreshTokens
│
├── GOP.Application/
│   └── Features/
│       └── Auth/
│           └── Commands/
│               ├── Login/
│               │   └── LoginCommandHandler.cs                  # Refactor: IUserRepository + IRefreshTokenRepo + IUserClaimsResolver + IUnitOfWork
│               └── RefreshToken/
│                   └── RefreshTokenCommandHandler.cs            # Refactor: IRefreshTokenRepo + IUserRepository + IUserClaimsResolver + IUnitOfWork
│
├── GOP.Infrastructure/
│   ├── Persistence/
│   │   └── GopDbContext.cs                                     # + DbSet<User> Users, DbSet<RefreshToken> RefreshTokens
│   └── DependencyInjection.cs                                  # + repos, claims resolver, user seeder; − old stores/adapter
│
├── GOP.API/
│   └── Extensions/
│       └── MigrationExtension.cs                               # + UserSeeder.SeedAsync() después de DbSeeder
│
└── CLAUDE.md                                                   # + subsección "Secrets de seed de usuarios"

backend/tests/
├── GOP.Application.Tests/
│   └── Features/
│       └── Auth/
│           ├── LoginCommandHandlerTests.cs                     # Reescribir mocks: IUserRepository, IRefreshTokenRepo, IUserClaimsResolver, IUnitOfWork
│           └── RefreshTokenCommandHandlerTests.cs              # Reescribir mocks: IRefreshTokenRepo, IUserRepository, IUserClaimsResolver, IUnitOfWork
│
└── GOP.API.Tests/
    ├── Fixtures/
    │   └── GopTestWebApplicationFactory.cs                     # + seed de usuarios dev en InMemory DB
    └── Controllers/
        └── AuthControllerTests.cs                              # + test de persistencia de RT en DB, + test de token revocado
```

### 2.3. Archivos a ELIMINAR

```
backend/src/
├── GOP.Application/
│   └── Common/
│       └── Interfaces/
│           ├── IUserSeedStore.cs                               # Reemplazado por IUserRepository + IUserClaimsResolver
│           └── IRefreshTokenStore.cs                           # Reemplazado por IRefreshTokenRepository (incluye RefreshTokenEntry record)
│
└── GOP.Infrastructure/
    └── Identity/
        ├── InMemoryRefreshTokenStore.cs                        # Reemplazado por RefreshTokenRepository (EF Core)
        └── UserSeedStoreAdapter.cs                             # Reemplazado por UserRepository + SeedUserClaimsResolver
```

### 2.4. Archivos que se CONSERVAN sin cambios

```
backend/src/
├── GOP.Infrastructure/Identity/SeedUsers.cs                    # Se conserva: usado por UserSeeder (dev) y SeedUserClaimsResolver
├── GOP.Infrastructure/Identity/JwtSettings.cs                  # Intacto
├── GOP.Infrastructure/Identity/JwtTokenService.cs              # Intacto
├── GOP.Infrastructure/Identity/CurrentUserService.cs           # Intacto
├── GOP.API/Controllers/AuthController.cs                       # Intacto (mismo routing, mismas actions)
├── GOP.Application/Features/Auth/Commands/Login/LoginCommand.cs
├── GOP.Application/Features/Auth/Commands/Login/LoginCommandValidator.cs
├── GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs
├── GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandValidator.cs
├── GOP.Application/Features/Auth/Dtos/TokenResponseDto.cs
├── GOP.Application/Features/Auth/Queries/GetCurrentUser/*     # Intactos (lee de ICurrentUserService, no del DB)
└── GOP.Application/Common/Interfaces/IJwtTokenService.cs       # Intacto
```

---

## 3. Detalle de Archivos Clave

### 3.1. Domain

**`User.cs`** — `GOP.Domain/Entities/User.cs`

```csharp
public sealed class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
}
```

- Hereda de `Entity` (Guid Id) — **no** de `AuditableEntity`. Ver §7 (Deuda técnica).
- Setters privados. Factory method estático `User.Create(email, passwordHash, fullName)` para construcción.
- Método `RecordLogin()` actualiza `LastLoginAt = DateTime.UtcNow` y `UpdatedAt`.
- Email se almacena ya normalizado (lowercase). La normalización es responsabilidad del caller (handler).

**`RefreshToken.cs`** — `GOP.Domain/Entities/RefreshToken.cs`

```csharp
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }

    // Navigation property (no expuesta en DTOs)
    public User User { get; private set; } = null!;
}
```

- Factory method estático `RefreshToken.Create(userId, token, expiresAt)`.
- Método `Revoke(replacedByToken?)` setea `RevokedAt = DateTime.UtcNow` y opcionalmente `ReplacedByToken`.
- Propiedad computed `bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow`.

### 3.2. Application

**`IUserRepository.cs`** — `GOP.Application/Common/Interfaces/IUserRepository.cs`

```csharp
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    void Update(User user);
    Task<bool> ExistsAsync(string email, CancellationToken ct);
}
```

> `Update` es `void` sincrónico: EF Core trackea la entidad, `SaveChangesAsync` se llama aparte vía `IUnitOfWork`. Alineado con el patrón de `WellRepository.Update` existente (ver `CONSTITUTION.backend.md` §4.2: "Commands pasan por repositorio + UoW").

**`IRefreshTokenRepository.cs`** — `GOP.Application/Common/Interfaces/IRefreshTokenRepository.cs`

```csharp
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct);
    Task AddAsync(RefreshToken rt, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct);
}
```

> `RevokeAsync` de un token individual se maneja invocando `rt.Revoke(replacedBy)` sobre la entidad ya trackeada, sin método de repositorio adicional. El handler obtiene la entidad vía `GetByTokenAsync` (con tracking), llama `rt.Revoke(...)`, y `IUnitOfWork.SaveChangesAsync` persiste el cambio. Esto es más limpio que un método de repo que recibe `(Guid id, string? replacedBy)` y hace una query interna.

**`IUserClaimsResolver.cs`** — `GOP.Application/Common/Interfaces/IUserClaimsResolver.cs`

```csharp
public interface IUserClaimsResolver
{
    UserProfileDto BuildProfile(User user);
}
```

> **Transitorio hasta Iter 9.** Encapsula la resolución de `Role`, `TenantId`, `TenantName` para un `User` que aún no tiene esos campos. Cuando Iter 9 agregue columnas de rol/tenant a `User`, esta interfaz se reemplaza por lectura directa de la entidad.

**`LoginCommandHandler.cs` (refactorizado)** — Cambios en constructor:

| Antes | Después |
|---|---|
| `IUserSeedStore userStore` | `IUserRepository userRepo` |
| `IRefreshTokenStore refreshTokenStore` | `IRefreshTokenRepository rtRepo` |
| — | `IUserClaimsResolver claimsResolver` |
| — | `IUnitOfWork unitOfWork` |
| `IJwtTokenService jwtTokenService` | (sin cambio) |
| `IOptions<JwtTokenOptions> jwtOptions` | (sin cambio) |

Flujo refactorizado:

1. `normalizedEmail = request.Email.Trim().ToLowerInvariant()` — sin cambio.
2. `var user = await userRepo.GetByEmailAsync(normalizedEmail, ct)` — **cambia** de sincrónico a async.
3. `if (user is null) → Failure(InvalidCredentials)`.
4. `if (!user.IsActive) → Failure(InvalidCredentials)` — **nuevo** (RN-029).
5. `if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) → Failure(InvalidCredentials)` — **cambia** de `userStore.VerifyPassword` a BCrypt directo.
6. `var profile = claimsResolver.BuildProfile(user)` — **cambia** de `userStore.FindByEmail` que retornaba DTO directo.
7. `var accessToken = jwtTokenService.GenerateAccessToken(profile)` — sin cambio.
8. `var refreshTokenStr = jwtTokenService.GenerateRefreshToken()` — sin cambio.
9. `var rt = RefreshToken.Create(user.Id, refreshTokenStr, expiresAt)` — **nuevo** (entidad de dominio).
10. `await rtRepo.AddAsync(rt, ct)` — **cambia** de `refreshTokenStore.Store(...)`.
11. `user.RecordLogin()` — **nuevo** (actualiza LastLoginAt).
12. `await unitOfWork.SaveChangesAsync(ct)` — **nuevo** (persiste ambos cambios en una transacción).
13. Retornar `Result.Success(new TokenResponseDto(...))` — sin cambio en shape.

> **Nota:** El handler ahora es `async` en su lógica interna. Antes retornaba `Task.FromResult(...)` porque todo era in-memory sincrónico. Ahora usa `await` real.

**`RefreshTokenCommandHandler.cs` (refactorizado)** — Cambios en constructor:

| Antes | Después |
|---|---|
| `IRefreshTokenStore refreshTokenStore` | `IRefreshTokenRepository rtRepo` |
| `IUserSeedStore userStore` | `IUserRepository userRepo` |
| — | `IUserClaimsResolver claimsResolver` |
| — | `IUnitOfWork unitOfWork` |
| `IJwtTokenService jwtTokenService` | (sin cambio) |
| `IOptions<JwtTokenOptions> jwtOptions` | (sin cambio) |

Flujo refactorizado:

1. `var oldRt = await rtRepo.GetByTokenAsync(request.RefreshToken, ct)` — retorna entidad con tracking.
2. `if (oldRt is null) → Failure(InvalidRefreshToken)`.
3. `if (!oldRt.IsActive)` → distinguir: si `RevokedAt != null` → `InvalidRefreshToken`; si `ExpiresAt < UtcNow` → `TokenExpired`.
4. `var user = await userRepo.GetByIdAsync(oldRt.UserId, ct)`.
5. `if (user is null || !user.IsActive) → Failure(InvalidRefreshToken)` — **nuevo** (RN-029).
6. `var profile = claimsResolver.BuildProfile(user)`.
7. Generar nuevo access token + refresh token string.
8. `var newRt = RefreshToken.Create(user.Id, newRefreshTokenStr, newExpiresAt)`.
9. `await rtRepo.AddAsync(newRt, ct)`.
10. `oldRt.Revoke(replacedByToken: newRt.Token)` — mutación de la entidad trackeada.
11. `await unitOfWork.SaveChangesAsync(ct)`.
12. Retornar `Result.Success(new TokenResponseDto(...))`.

### 3.3. Infrastructure

**`UserConfiguration.cs`** — `GOP.Infrastructure/Persistence/Configurations/UserConfiguration.cs`

```csharp
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");
        // SQL Server default collation (CI_AS) hace el índice case-insensitive

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // UpdatedAt, LastLoginAt — nullable, sin config adicional
    }
}
```

> Sigue el patrón de `WellConfiguration` existente: `IEntityTypeConfiguration` separada, índice con nombre explícito (`CONSTITUTION.backend.md` §7.3).

**`RefreshTokenConfiguration.cs`** — `GOP.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`

```csharp
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => new { rt.UserId, rt.RevokedAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_RevokedAt");

        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.CreatedAt).IsRequired();

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(512);

        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**`UserRepository.cs`** — `GOP.Infrastructure/Persistence/Repositories/UserRepository.cs`

```csharp
internal sealed class UserRepository(GopDbContext context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        // Email almacenado normalizado → comparación directa

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct)
        => await context.Users.AddAsync(user, ct);

    public void Update(User user)
        => context.Users.Update(user);

    public async Task<bool> ExistsAsync(string email, CancellationToken ct)
        => await context.Users.AnyAsync(u => u.Email == email, ct);
}
```

> **`GetByEmailAsync` NO usa `AsNoTracking`** porque en `LoginCommandHandler` necesitamos tracking para llamar `user.RecordLogin()` → `Update` → `SaveChangesAsync`. `GetByIdAsync` tampoco usa `AsNoTracking` por la misma razón (usado en refresh para verificar `IsActive`). Esto es una desviación justificada de `CONSTITUTION.backend.md` §4.4 ("Queries usan AsNoTracking") porque estamos en el contexto de un **Command**, no de una Query.

**`RefreshTokenRepository.cs`** — `GOP.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`

```csharp
internal sealed class RefreshTokenRepository(GopDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct)
        => await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token, ct);
        // Con tracking: el handler llama rt.Revoke() y luego SaveChangesAsync

    public async Task AddAsync(RefreshToken rt, CancellationToken ct)
        => await context.RefreshTokens.AddAsync(rt, ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var rt in activeTokens)
            rt.Revoke(replacedByToken: null);
        // Cambios trackeados → SaveChangesAsync del caller los persiste
    }
}
```

**`SeedUserClaimsResolver.cs`** — `GOP.Infrastructure/Identity/SeedUserClaimsResolver.cs`

```csharp
internal sealed class SeedUserClaimsResolver : IUserClaimsResolver
{
    public UserProfileDto BuildProfile(User user)
    {
        // Intenta resolver claims desde los datos estáticos de seed (dev users)
        var seedProfile = SeedUsers.GetById(user.Id);
        if (seedProfile is not null)
            return new UserProfileDto(
                user.Id, user.Email, user.FullName,
                seedProfile.Role, seedProfile.TenantId, seedProfile.TenantName);

        // Usuarios no reconocidos en seed estático → defaults ADMIN / ANH
        return new UserProfileDto(
            user.Id, user.Email, user.FullName,
            "ADMIN", "1", "Agencia Nacional de Hidrocarburos");
    }
}
```

> **Transitorio.** Desaparece cuando Iter 9 agregue `Role`/`TenantId` a la entidad `User`.

**`UserSeeder.cs`** — `GOP.Infrastructure/Persistence/UserSeeder.cs`

```csharp
public sealed class UserSeeder(
    IUserRepository userRepo,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<UserSeeder> logger)
```

Comportamiento (pseudocódigo del spec §7.4):

```
1. Leer sección SeedUsers de IConfiguration
2. SI sección no existe:
     a. SI es Development:
          - Sembrar 4 dev users desde SeedUsers estático (hashes conocidos)
          - Los IDs y hashes se toman directamente de SeedUsers.cs
     b. SI NO es Development:
          - Log.Warning("SeedUsers section not configured. User seed skipped.")
     c. Retornar
3. SI sección existe:
     a. Para cada admin configurado (ExecAdmin, OpAdmin):
          - Leer Email (con default), Password (required), FullName (con default)
          - SI Password vacío → throw InvalidOperationException (fail fast)
          - SI await userRepo.ExistsAsync(email) → log + skip
          - SINO → BCrypt.HashPassword(password), User.Create(...), AddAsync
     b. await unitOfWork.SaveChangesAsync()
```

**`GopDbContext.cs` (modificación):**

Agregar:
```csharp
public DbSet<User> Users => Set<User>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

**`IApplicationDbContext.cs` (modificación):**

Agregar:
```csharp
DbSet<User> Users { get; }
DbSet<RefreshToken> RefreshTokens { get; }
```

**`DependencyInjection.cs` (modificación):**

Agregar:
```csharp
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
services.AddScoped<IUserClaimsResolver, SeedUserClaimsResolver>();
services.AddScoped<UserSeeder>();
```

Eliminar:
```csharp
// ELIMINAR estas líneas:
services.AddScoped<IUserSeedStore, UserSeedStoreAdapter>();
services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
```

**`MigrationExtension.cs` (modificación):**

Después de `seeder.SeedAsync()` (seed DANE), agregar:
```csharp
var userSeeder = scope.ServiceProvider.GetRequiredService<UserSeeder>();
logger.LogInformation("Ejecutando seed de usuarios...");
await userSeeder.SeedAsync();
logger.LogInformation("Seed de usuarios completado.");
```

### 3.4. API

Sin archivos nuevos. Solo `MigrationExtension.cs` se modifica (documentado arriba).

**`appsettings.Development.json` (sin cambio requerido):** los 4 dev users se siembran desde `SeedUsers` estático cuando la sección `SeedUsers` no existe en config. No es necesario agregar claves de configuración en dev.

### 3.5. Migración EF Core

**Nombre:** `AddUsersAndRefreshTokens`

**Genera las tablas:**

| Tabla | Columnas principales | Índices |
|---|---|---|
| `Users` | Id (PK), Email, PasswordHash, FullName, IsActive, CreatedAt, UpdatedAt, LastLoginAt | `IX_Users_Email` (unique) |
| `RefreshTokens` | Id (PK), UserId (FK), Token, ExpiresAt, CreatedAt, RevokedAt, ReplacedByToken | `IX_RefreshTokens_Token` (unique), `IX_RefreshTokens_UserId_RevokedAt` (composite) |

**Compatibilidad:**
- DB vacía → crea ambas tablas.
- DB con 7 tablas existentes (Wells, Campos, Clusters, Contratos, Departamentos, Municipios, WellTransitionHistory) → agrega las 2 tablas nuevas sin tocar las existentes.

**Comando para generar:**
```bash
cd backend
dotnet ef migrations add AddUsersAndRefreshTokens -p src/GOP.Infrastructure -s src/GOP.API
```

### 3.6. Tests

**`GopTestWebApplicationFactory.cs` (modificación):**

El test factory usa EF Core InMemory. Actualmente, `MigrateAsync` falla silenciosamente con InMemory (excepción capturada en `MigrationExtension`). Esto también impide que los seeders corran.

Después del refactor, `LoginCommandHandler` lee usuarios de la DB. Si la DB de test está vacía → todos los login tests fallan con 401.

**Solución:** agregar seed de usuarios dev directamente en el factory:

```csharp
// Después de registrar el InMemory DbContext
using var seedScope = sp.CreateScope();
var db = seedScope.ServiceProvider.GetRequiredService<GopDbContext>();
db.Database.EnsureCreated();
// Insertar dev users usando SeedUsers static data
```

Alternativa más limpia: invocar `UserSeeder.SeedAsync()` después de `EnsureCreated()`. Esto requiere que `UserSeeder` esté registrado en DI del test.

**`LoginCommandHandlerTests.cs` (reescritura de mocks):**

| Mock actual | Mock nuevo |
|---|---|
| `IUserSeedStore` | `IUserRepository` |
| `IRefreshTokenStore` | `IRefreshTokenRepository` |
| — | `IUserClaimsResolver` |
| — | `IUnitOfWork` |

Los 4 tests existentes se mantienen (mismo escenario) + 1 test nuevo:
- `Handle_InactiveUser_ReturnsFailure` — `user.IsActive = false` → `InvalidCredentials`.

**`RefreshTokenCommandHandlerTests.cs` (reescritura de mocks):**

Los 4 tests existentes se adaptan al nuevo flow + 1 test nuevo:
- `Handle_InactiveUserRefresh_ReturnsFailure` — user desactivado → `InvalidRefreshToken`.

**Tests nuevos de Infrastructure:**

`UserRepositoryTests`, `RefreshTokenRepositoryTests`, `UserSeederTests` — usan EF Core InMemory (`UseInMemoryDatabase` con nombre único por test) como en `GopDbContextTests` existente.

---

## 4. NuGet — Sin adiciones

No se requieren paquetes NuGet nuevos. `BCrypt.Net-Next` ya está en `GOP.Infrastructure`. `Microsoft.EntityFrameworkCore.InMemory` ya está en los proyectos de test.

---

## 5. Bloques Compilables

### Bloque 1 — Domain (dotnet build GOP.Domain)

Crear las entidades de dominio. Sin dependencias externas.

- `User.cs`
- `RefreshToken.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0.

### Bloque 2 — Application: Interfaces + Refactor Handlers (dotnet build GOP.Application)

Crear interfaces de repositorio y claims resolver. Refactorizar handlers. Eliminar interfaces viejas.

**Orden estricto dentro del bloque:**

1. Crear `IUserRepository.cs`, `IRefreshTokenRepository.cs`, `IUserClaimsResolver.cs` (sin dependencias entre sí).
2. Modificar `IApplicationDbContext.cs` (agregar DbSets).
3. Refactorizar `LoginCommandHandler.cs` (depende de las nuevas interfaces).
4. Refactorizar `RefreshTokenCommandHandler.cs` (depende de las nuevas interfaces).
5. Eliminar `IUserSeedStore.cs` y `IRefreshTokenStore.cs` (ya no referenciados).

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0.

### Bloque 3 — Infrastructure: Repos + Configs + Seeder + Migración (dotnet build GOP.Infrastructure)

Implementar repositorios, configuraciones EF, seeder, actualizar DI. Generar migración.

**Orden estricto dentro del bloque:**

1. Crear `UserConfiguration.cs` y `RefreshTokenConfiguration.cs` (sin dependencias mutuas).
2. Modificar `GopDbContext.cs` (agregar DbSets — necesario antes de repos y migración).
3. Crear `UserRepository.cs` y `RefreshTokenRepository.cs` (dependen de GopDbContext).
4. Crear `SeedUserClaimsResolver.cs` (depende de SeedUsers estático y IUserClaimsResolver).
5. Crear `UserSeeder.cs` (depende de IUserRepository, IUnitOfWork, IConfiguration).
6. Modificar `DependencyInjection.cs` (registrar nuevos servicios, eliminar viejos).
7. Eliminar `InMemoryRefreshTokenStore.cs` y `UserSeedStoreAdapter.cs`.
8. Generar migración: `dotnet ef migrations add AddUsersAndRefreshTokens -p src/GOP.Infrastructure -s src/GOP.API`.

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0.

### Bloque 4 — API: Startup + CLAUDE.md (dotnet build GOP.API)

Integrar seeder en startup. Actualizar documentación.

1. Modificar `MigrationExtension.cs` (agregar UserSeeder).
2. Actualizar `CLAUDE.md` (subsección "Secrets de seed de usuarios").

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → exit code 0. Verificar que `dotnet build backend/GOP.sln` pasa limpio.

### Bloque 5 — Tests (dotnet test GOP.sln)

Actualizar tests existentes y crear tests nuevos.

1. Modificar `GopTestWebApplicationFactory.cs` (seed de dev users en InMemory DB).
2. Reescribir `LoginCommandHandlerTests.cs` (nuevos mocks + test de usuario inactivo).
3. Reescribir `RefreshTokenCommandHandlerTests.cs` (nuevos mocks + test de usuario inactivo).
4. Crear `UserRepositoryTests.cs`.
5. Crear `RefreshTokenRepositoryTests.cs`.
6. Crear `UserSeederTests.cs`.
7. Modificar `AuthControllerTests.cs` (+ tests de persistencia de RT y token revocado).

**Checkpoint:** `cd backend && dotnet test GOP.sln` → todos los tests pasan, exit code 0.

---

### 5.2. Estrategia de Test Provider

**Decisión: hybrid SQLite in-memory + EF InMemory.**

| Proyecto de test | Provider | Justificación |
|---|---|---|
| `GOP.Infrastructure.Tests` (tests nuevos) | **SQLite in-memory** (`Microsoft.Data.Sqlite` + `UseInternalServiceProvider`) | Valida constraints reales: unique index en `Email`, unique index en `Token`, cascade delete `RefreshTokens → Users`, tipos de datos. Costo de adopción bajo porque los tests nuevos se escriben desde cero. |
| `GOP.API.Tests` (tests existentes + modificados) | **EF InMemory** (sin cambio) | `WellConfiguration.HasFilter("[Uwi] IS NOT NULL AND [IsDeleted] = 0")` usa sintaxis de brackets SQL Server — SQLite falla en `EnsureCreated()`. Migrar requiere cambiar los filtros a sintaxis cross-provider, lo cual está fuera de alcance. |

**Setup SQLite in-memory para Infrastructure.Tests:**

```csharp
// Patrón para cada test class
private SqliteConnection _connection = null!;
private GopDbContext _context = null!;

public async Task InitializeAsync()
{
    _connection = new SqliteConnection("DataSource=:memory:");
    await _connection.OpenAsync();

    var options = new DbContextOptionsBuilder<GopDbContext>()
        .UseSqlite(_connection)
        .Options;

    _context = new GopDbContext(options);
    await _context.Database.EnsureCreatedAsync();
    // EnsureCreated con SQLite crea schema desde el modelo,
    // validando constraints reales (unique, FK, cascade)
}

public async Task DisposeAsync()
{
    await _context.DisposeAsync();
    await _connection.DisposeAsync();
}
```

> `EnsureCreated` (no `MigrateAsync`) porque SQLite no ejecuta migraciones SQL Server. Crea el schema desde el modelo EF Core, lo cual es suficiente para validar constraints.

**NuGet requerido:** `Microsoft.EntityFrameworkCore.Sqlite` en `GOP.Infrastructure.Tests.csproj`.

**Limitaciones aceptadas de EF InMemory en API.Tests:**

Los integration tests de `GopTestWebApplicationFactory` (API.Tests) **no detectan** las siguientes categorías de bugs:

| Categoría | Ejemplo no detectado | Mitigación |
|---|---|---|
| Unique constraints | Insertar 2 users con mismo email → no falla | Cubierto en `UserRepositoryTests` (SQLite) |
| Cascading deletes | Borrar user → refresh tokens huérfanos | Cubierto en `RefreshTokenRepositoryTests` (SQLite) |
| Transacciones / rollback | Fallo parcial en SaveChangesAsync | Cubierto en tests unitarios de handler (mocks verifican llamadas) |
| Case sensitivity de collation | Comparación de email CI vs CS | Cubierto en `UserRepositoryTests` + lógica de normalización en handler |
| Filtered indexes | HasFilter con condición SQL | No aplica a User/RefreshToken (no usan HasFilter) |

Esta limitación queda documentada como deuda técnica en §7.5.

---

## 6. Orden de Dependencias entre Bloques

```
B1 (Domain) → B2 (Application) → B3 (Infrastructure) → B4 (API) → B5 (Tests)
```

Secuencial estricto. Cada bloque depende del anterior. Cada bloque es commitable con tests verdes al cierre.

> **Nota sobre B3:** la generación de la migración requiere que B4 compile parcialmente (el startup project es `GOP.API`). El comando `dotnet ef migrations add` necesita que `GOP.API` pueda construir el `DbContext`. Dado que `GOP.API` referencia `GOP.Infrastructure`, y el `DbContext` ya está modificado en B3, la migración se genera al final de B3. Si `dotnet ef` requiere que `GOP.API` compile limpio primero, mover la generación de migración al inicio de B4.

---

## 7. Deuda y Consideraciones Futuras

### 7.1. `User` no hereda de `AuditableEntity`

**Estado actual:** `User` hereda de `Entity` (solo `Guid Id`). Tiene campos propios `CreatedAt` y `UpdatedAt`, pero carece de `CreatedBy`, `LastModifiedBy`, `IsDeleted`, `DeletedAt`.

**Justificación:** en esta iteración no hay contexto de "quién creó el usuario" (el seeder no tiene usuario autenticado). Agregar `AuditableEntity` implicaría campos vacíos o valores artificiales. Además, el `AuditableEntityInterceptor` existente lee `ICurrentUserService` para setear `CreatedBy`/`LastModifiedBy`, lo cual fallaría durante el seed (no hay request HTTP → no hay usuario autenticado).

**Acción futura (Iter 9 — RBAC + audit trail):**
- Evaluar si `User` debe heredar de `AuditableEntity` cuando exista un CRUD de usuarios operado por administradores autenticados.
- Evaluar si `User` necesita soft delete (`IsDeleted` / `DeletedAt`) o si desactivación (`IsActive = false`) es suficiente.
- Si se adopta `AuditableEntity`, crear una migración que agregue las columnas y poblar con valores default para registros existentes.
- Actualizar el `AuditableEntityInterceptor` para manejar el caso de seed sin usuario autenticado (ej. `CreatedBy = "SYSTEM"`).

### 7.2. Endpoint `POST /api/v1/auth/logout`

**Estado actual:** `IRefreshTokenRepository` expone `RevokeAllForUserAsync` y la entidad `RefreshToken` expone `Revoke(replacedByToken?)`. La infraestructura de revocación está completa.

**Lo que falta:**
- Controller action `[HttpPost("logout")]` en `AuthController`.
- Un `LogoutCommand` + `LogoutCommandHandler` que lea el refresh token del body (o del JWT sub) y lo revoque.
- Decisión de estrategia frontend: ¿el FE envía el refresh token en el body? ¿O el backend revoca todos los tokens del usuario basándose en el `sub` del JWT?
- Actualización del `contract.yml` de 004-auth-api (requiere aprobación de nuevo endpoint).

**Acción futura:** crear feature `NNN-auth-logout` con su propio ciclo SDD (`spec.md` → `contract.yml` → `plan.md` → `tasks.md`) una vez se defina la estrategia de session invalidation a nivel frontend (¿despacha `AuthActions.logout` → Effect llama al endpoint → limpia sessionStorage?).

### 7.3. `IUserClaimsResolver` es transitorio

**Estado actual:** necesario porque `User` no tiene `Role`, `TenantId`, `TenantName`. La implementación (`SeedUserClaimsResolver`) usa datos estáticos hardcodeados.

**Acción futura (Iter 9):**
- Agregar columnas `Role`, `TenantId`, `TenantName` a `User`.
- Migración EF Core para las nuevas columnas.
- Reemplazar `IUserClaimsResolver.BuildProfile(User)` por lectura directa: `new UserProfileDto(user.Id, user.Email, user.FullName, user.Role, user.TenantId.ToString(), user.TenantName)`.
- Eliminar `IUserClaimsResolver`, `SeedUserClaimsResolver`.
- Actualizar `SeedUsers` y `UserSeeder` para incluir Role/TenantId en la creación de usuarios.

### 7.5. EF InMemory en API.Tests no valida constraints SQL

**Estado actual:** `GopTestWebApplicationFactory` usa `UseInMemoryDatabase`. Esto es una limitación heredada de 004-auth-api (documentada en el factory como `BE-M02`). EF InMemory no valida unique constraints, foreign keys con cascade delete, ni transacciones.

**Impacto en esta iteración:** los integration tests de `AuthControllerTests` no detectan si el backend permite insertar dos usuarios con el mismo email, o si borrar un usuario deja refresh tokens huérfanos. Estas categorías de bugs **sí** se detectan en los tests de `Infrastructure.Tests` que usan SQLite in-memory.

**Acción futura:**
- Migrar `GopTestWebApplicationFactory` a SQLite in-memory. Blocker: `WellConfiguration.HasFilter("[Uwi] IS NOT NULL AND [IsDeleted] = 0")` usa sintaxis SQL Server con brackets `[Column]`. Requiere cambiar a sintaxis cross-provider o usar conditional model building.
- Alternativa: migrar a Testcontainers SQL Server. Mayor fidelidad, mayor costo de setup y tiempo de CI.
- Cualquiera de las dos opciones es independiente de esta feature y puede abordarse como mejora de infraestructura de testing.

### 7.4. Usuarios de desarrollo vs. usuarios de producción

**Estado actual:** los 4 usuarios de desarrollo (`admin@gop.co`, etc.) tienen passwords conocidos hardcodeados como hashes BCrypt en `SeedUsers.cs`. Esto es aceptable para desarrollo local y CI.

**Acción futura:**
- Cuando exista un CRUD de usuarios (Iter 9+), los usuarios de desarrollo podrían migrarse o eliminarse.
- Los passwords de los usuarios de desarrollo **nunca** deben usarse en staging/producción.
- Evaluar si `SeedUsers.cs` debe eliminarse completamente cuando `UserSeeder` pueda leer todos los usuarios desde configuración.

### 7.6. Limpieza de `JwtTokenOptions` duplicada (deuda cosmética)

La clase `Application/Common/Interfaces/JwtTokenOptions.cs` duplica campos de `Infrastructure/Identity/JwtSettings.cs` (`AccessTokenExpirationMinutes`, `RefreshTokenExpirationDays`). Ambas leen la misma sección `JwtSettings` de `appsettings.json`. No afecta comportamiento ni correctitud — es estrictamente duplicación de binding.

**Omitida en Iter 7** porque eliminarla implicaría cambiar las firmas de `LoginCommandHandler` y `RefreshTokenCommandHandler` (que inyectan `IOptions<JwtTokenOptions>`), propagar el cambio a `IJwtTokenService` o introducir otro mecanismo para la expiración del refresh token, y reescribir todos los mocks de tests que inyectan esa dependencia. Cascada de cambios fuera del alcance de la iteración, cuyo objetivo es exclusivamente reemplazar storage in-memory → SQL Server.

**Plan:** limpiarla como parte de Iter 9, cuando los constructores de los handlers se reescriban para agregar `Role`/`TenantId` — en ese momento el costo marginal de quitar `JwtTokenOptions` es cercano a cero.
