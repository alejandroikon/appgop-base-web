# Spec: Persistencia de Users y RefreshTokens

**Feature ID:** 007-users-persistence
**Dominio:** Autenticación — Infraestructura de persistencia
**Dependencias:** 004-auth-api (completado e implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Contexto y Alcance

### 1.1. Problema

La implementación de 004-auth-api almacena usuarios y refresh tokens **en memoria**:

- **Usuarios:** clase estática `SeedUsers` con 4 usuarios hardcodeados. Implementa `IUserSeedStore` via `UserSeedStoreAdapter`.
- **Refresh tokens:** `InMemoryRefreshTokenStore` con `ConcurrentDictionary<string, RefreshTokenEntry>`, registrado como Singleton. Implementa `IRefreshTokenStore`.

En Azure App Service, cada reinicio (despliegues, cold starts, mantenimiento) **borra todos los usuarios y tokens**. Esto hace imposible:

- Mantener sesiones activas entre despliegues.
- Hacer seed de usuarios reales en staging/producción.
- Preparar ambientes de QA con datos persistentes.
- Avanzar a Iter 8 (state machine) e Iter 9 (RBAC + multi-tenancy), que asumen usuarios persistidos.

### 1.2. Solución

Reemplazar el almacenamiento in-memory por entidades de dominio persistidas en SQL Server via EF Core, manteniendo **idéntica** la interfaz pública de los endpoints de autenticación.

### 1.3. En alcance

1. Entity `User` en Domain.
2. Entity `RefreshToken` en Domain.
3. `DbSet<User>` y `DbSet<RefreshToken>` en `GopDbContext` con `EntityTypeConfiguration` separadas.
4. Migración EF Core: `AddUsersAndRefreshTokens`.
5. Interfaces de repositorio en Application: `IUserRepository`, `IRefreshTokenRepository`.
6. Implementaciones EF Core de repositorios en Infrastructure.
7. Refactor de `LoginCommandHandler` y `RefreshTokenCommandHandler` para consumir los repositorios.
8. Seeder idempotente de usuarios administradores leídos desde configuración (Key Vault en staging).
9. Nuevos errores de dominio: `DomainErrors.Auth.UserInactive`.
10. Tests unitarios e integración.
11. Actualización de `CLAUDE.md` con subsección "Secrets de seed de usuarios".

### 1.4. Fuera de alcance

| Exclusión | Iteración destino |
|---|---|
| RBAC, roles y permisos granulares | Iter 9 |
| Multi-tenancy sobre Users | Iter 8 + Iter 9 |
| Password reset, email verification, MFA | Backlog |
| Rotación del password SQL admin (`gopadmin`) | Deuda técnica separada |
| Refactor del esquema JWT, claims o SigningKey | No necesario |
| Bug FE `NG0203` del interceptor HTTP | Frontend, no backend |
| Cambios en Bicep, Dockerfile o pipelines | No requerido |
| Endpoint `POST /api/v1/auth/logout` (controller action) | Preparado en servicio, controller en iteración posterior |

---

## 2. Historias de Usuario

### HU-012: Login con usuario persistido en SQL Server

**Como** usuario registrado del sistema GOP 360°,
**quiero** que mi cuenta sobreviva a reinicios del App Service,
**para** no tener que re-crear mi sesión después de cada despliegue en Azure.

#### Criterios de Aceptación

**Escenario 1 — Login exitoso post-reinicio**
- **Dado** que el usuario `admin@gop.co` fue creado en la base de datos (vía seed o registro futuro)
- **Y** el App Service se reinició (simulado con restart del proceso)
- **Cuando** se envía `POST /api/v1/auth/login` con `{ "email": "admin@gop.co", "password": "Admin123*" }`
- **Entonces** el servidor responde con `200 OK`
- **Y** el body tiene la misma estructura que la definida en el contrato de 004-auth-api: `accessToken`, `refreshToken`, `expiresIn`, `user`
- **Y** el campo `user.lastLoginAt` se actualiza en la base de datos a la hora UTC actual

**Escenario 2 — Login con credenciales incorrectas**
- **Dado** que el usuario `admin@gop.co` existe en la base de datos
- **Cuando** se envía `POST /api/v1/auth/login` con password incorrecto
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"Correo o contraseña incorrectos."*
- **Y** no se revela si el email existe o no (misma respuesta para email inexistente)

**Escenario 3 — Login con usuario inactivo**
- **Dado** que el usuario existe pero tiene `IsActive = false`
- **Cuando** se envía `POST /api/v1/auth/login` con credenciales correctas
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"Correo o contraseña incorrectos."*
- **Y** no se distingue del escenario de credenciales incorrectas (RN-025 de 004-auth-api)

**Escenario 4 — Email case-insensitive**
- **Dado** que el usuario `admin@gop.co` existe en la base de datos
- **Cuando** se envía `{ "email": "ADMIN@GOP.CO", "password": "Admin123*" }`
- **Entonces** el login es exitoso (mismo comportamiento que Escenario 1)

**Escenario 5 — Campos vacíos (validación)**
- **Dado** que el endpoint está disponible
- **Cuando** se envía un body con email vacío o password vacío
- **Entonces** el servidor responde con `422 Unprocessable Entity`
- **Y** el body es ProblemDetails con `errors` por campo

---

### HU-013: Refresh token persistido en SQL Server

**Como** usuario con sesión activa,
**quiero** que mi refresh token sobreviva a reinicios del App Service,
**para** mantener mi sesión sin necesidad de re-autenticarme después de un despliegue.

#### Criterios de Aceptación

**Escenario 1 — Refresh exitoso post-reinicio**
- **Dado** que el usuario hizo login y recibió un `refreshToken` vigente
- **Y** el App Service se reinició
- **Cuando** se envía `POST /api/v1/auth/refresh` con `{ "refreshToken": "<token-vigente>" }`
- **Entonces** el servidor responde con `200 OK`
- **Y** el body contiene un nuevo `accessToken`, un nuevo `refreshToken` y `expiresIn`
- **Y** en la base de datos el refresh token original tiene `RevokedAt` no nulo y `ReplacedByToken` apuntando al nuevo token

**Escenario 2 — Refresh token revocado**
- **Dado** que el refresh token fue revocado (por un refresh previo o por logout)
- **Cuando** se envía el refresh token revocado
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"Token de actualización inválido."*

**Escenario 3 — Refresh token expirado**
- **Dado** que el refresh token ha superado los 7 días de vigencia
- **Cuando** se envía el refresh token expirado
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"El token de actualización ha expirado. Inicie sesión nuevamente."*

**Escenario 4 — Refresh con usuario desactivado**
- **Dado** que el refresh token es vigente y no revocado
- **Pero** el usuario fue desactivado (`IsActive = false`) después de emitir el token
- **Cuando** se envía el refresh token
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"Token de actualización inválido."*

**Escenario 5 — Refresh token inexistente**
- **Dado** que el string enviado no corresponde a ningún token en la base de datos
- **Cuando** se envía `POST /api/v1/auth/refresh` con ese string
- **Entonces** el servidor responde con `401 Unauthorized`

---

### HU-014: Seed idempotente de usuarios administradores

**Como** administrador de infraestructura,
**quiero** que al arrancar la aplicación se creen automáticamente los usuarios administradores configurados,
**para** poder hacer login en un ambiente nuevo sin pasos manuales de creación de usuarios.

#### Criterios de Aceptación

**Escenario 1 — Primera ejecución en base de datos vacía**
- **Dado** que la tabla `Users` no tiene registros
- **Y** las claves `SeedUsers:ExecAdmin:Password` y `SeedUsers:OpAdmin:Password` están configuradas (vía Key Vault o appsettings)
- **Cuando** la aplicación arranca
- **Entonces** se crean 2 usuarios:
  - `alejandro.gutierrez@interkont.co` (Alejandro Gutiérrez)
  - `admin@interkont.co` (Admin Operativo)
- **Y** los passwords se almacenan como hash BCrypt
- **Y** ambos usuarios tienen `IsActive = true`

**Escenario 2 — Ejecución repetida (idempotencia)**
- **Dado** que los 2 usuarios administradores ya existen en la base de datos
- **Cuando** la aplicación arranca nuevamente
- **Entonces** el seeder detecta que ambos emails ya existen
- **Y** NO actualiza passwords ni ningún otro campo
- **Y** NO inserta duplicados
- **Y** logguea "Usuario {email} ya existe, omitiendo seed."

**Escenario 3 — Password obligatorio faltante**
- **Dado** que la clave `SeedUsers:ExecAdmin:Password` NO está configurada
- **Cuando** la aplicación intenta ejecutar el seeder
- **Entonces** el seeder lanza una excepción explícita
- **Y** logguea `"SeedUsers:ExecAdmin:Password not configured. Seed aborted."`
- **Y** la aplicación **no arranca** (fail fast)
- **Y** no se crea ningún usuario parcial

**Escenario 4 — Entorno de desarrollo sin Key Vault**
- **Dado** que el entorno es `Development`
- **Y** los passwords de seed NO están configurados en Key Vault (no hay Key Vault en dev)
- **Cuando** la aplicación arranca
- **Entonces** el seeder se omite sin error (comportamiento configurable)
- **Y** los 4 usuarios originales de `SeedUsers` estático siguen funcionando para desarrollo local

> **Nota:** El Escenario 4 requiere una decisión de diseño en el plan.md. La opción recomendada: el seeder solo se ejecuta si al menos una clave `SeedUsers:*:Password` está presente en la configuración. Si ninguna está presente, se omite silenciosamente y se logguea un warning.

---

## 3. Reglas de Negocio

| Regla | Descripción | Referencia |
|---|---|---|
| RN-020 | El email es case-insensitive. Se normaliza a minúsculas antes de comparar y almacenar. Índice único case-insensitive en SQL Server. | Heredada de 004-auth-api |
| RN-021 | La contraseña es case-sensitive. | Heredada de 004-auth-api |
| RN-022 | El access token expira en 30 minutos (configurable en `JwtSettings`). | Heredada de 004-auth-api |
| RN-023 | El refresh token expira en 7 días (configurable en `JwtSettings`). | Heredada de 004-auth-api |
| RN-024 | Cada refresh token es de un solo uso. Al usarlo se genera uno nuevo, se revoca el anterior con `ReplacedByToken` apuntando al nuevo, y se persiste el nuevo. | Ampliada respecto a 004-auth-api (ahora con trazabilidad de reemplazo) |
| RN-025 | La respuesta de login erróneo no revela si el email existe ni si el usuario está inactivo. Siempre *"Correo o contraseña incorrectos."* | Heredada de 004-auth-api |
| RN-029 | Un usuario con `IsActive = false` no puede hacer login ni usar refresh tokens vigentes. | **Nueva** en esta iteración |
| RN-030 | El seeder de usuarios es **idempotente**: si el email ya existe, no modifica el registro. | **Nueva** |
| RN-031 | Los passwords de seed se leen de configuración (Key Vault en staging), nunca hardcodeados. Si un password obligatorio falta, la aplicación no arranca. | **Nueva** |
| RN-032 | Los passwords se hashean con BCrypt antes de persistir. Work factor: el mismo que ya usa la aplicación (definido por `BCrypt.Net.BCrypt.HashPassword` — default 11). | **Nueva** |
| RN-033 | `User.LastLoginAt` se actualiza en cada login exitoso. | **Nueva** |
| RN-034 | Al revocar un refresh token, se registra `RevokedAt` (UTC) y opcionalmente `ReplacedByToken` (si fue por rotación, no por logout). | **Nueva** |
| RN-035 | Cascade delete: al eliminar un usuario, todos sus refresh tokens se eliminan automáticamente (FK con `ON DELETE CASCADE`). | **Nueva** |

---

## 4. Entidades de Dominio

### 4.1. Entity: `User`

| Campo | Tipo C# | Constraint SQL | Descripción |
|---|---|---|---|
| `Id` | `Guid` | PK, generado en código | Identificador único |
| `Email` | `string` | required, max 256, unique index CI | Email normalizado a minúsculas |
| `PasswordHash` | `string` | required, max 255 | Hash BCrypt del password |
| `FullName` | `string` | required, max 200 | Nombre completo |
| `IsActive` | `bool` | default `true` | Flag de activación |
| `CreatedAt` | `DateTime` | required, UTC | Fecha de creación |
| `UpdatedAt` | `DateTime?` | nullable, UTC | Última modificación |
| `LastLoginAt` | `DateTime?` | nullable, UTC | Último login exitoso |

**Decisiones de diseño:**
- `User` **no hereda** de `AuditableEntity` porque no necesita `CreatedBy`, `LastModifiedBy`, `IsDeleted`, `DeletedAt` (el seed no tiene contexto de "quién creó"). Usa campos propios `CreatedAt` y `UpdatedAt`.
- No incluye `TenantId`, `Role`, `TenantName` — esos datos se resolverán en Iter 9 (RBAC + multi-tenancy). Por ahora, los claims del JWT siguen leyéndose de la configuración/seed, no de columnas de la tabla.
- El email se almacena normalizado a minúsculas. El índice único usa collation case-insensitive de SQL Server como segunda defensa.

### 4.2. Entity: `RefreshToken`

| Campo | Tipo C# | Constraint SQL | Descripción |
|---|---|---|---|
| `Id` | `Guid` | PK | Identificador único |
| `UserId` | `Guid` | FK → `Users.Id`, CASCADE delete | Propietario del token |
| `Token` | `string` | required, max 512, unique index | String opaco del token |
| `ExpiresAt` | `DateTime` | required, UTC | Expiración |
| `CreatedAt` | `DateTime` | required, UTC | Emisión |
| `RevokedAt` | `DateTime?` | nullable, UTC | Momento de revocación |
| `ReplacedByToken` | `string?` | nullable, max 512 | Token que lo reemplazó (rotación) |

**Índices:**
- Unique index en `Token` (`IX_RefreshTokens_Token`).
- Composite index en `(UserId, RevokedAt)` (`IX_RefreshTokens_UserId_RevokedAt`) — optimiza consultas de "tokens activos del usuario X".

---

## 5. Contratos de Repositorio

### 5.1. `IUserRepository` (en Application)

```
GetByEmailAsync(string email, CancellationToken ct) → User?
GetByIdAsync(Guid id, CancellationToken ct) → User?
AddAsync(User user, CancellationToken ct) → void
UpdateAsync(User user, CancellationToken ct) → void
ExistsAsync(string email, CancellationToken ct) → bool
```

- Ubicación: `GOP.Application/Common/Interfaces/IUserRepository.cs`
- Lecturas con `AsNoTracking` (ver CONSTITUTION.backend.md §4.4).
- `GetByEmailAsync` compara contra email normalizado a minúsculas.

### 5.2. `IRefreshTokenRepository` (en Application)

```
GetByTokenAsync(string token, CancellationToken ct) → RefreshToken?
AddAsync(RefreshToken rt, CancellationToken ct) → void
RevokeAsync(Guid id, string? replacedByToken, CancellationToken ct) → void
RevokeAllForUserAsync(Guid userId, CancellationToken ct) → void
```

- Ubicación: `GOP.Application/Common/Interfaces/IRefreshTokenRepository.cs`
- `GetByTokenAsync` lee con tracking habilitado porque el caller puede necesitar revocar.
- `RevokeAsync` setea `RevokedAt = DateTime.UtcNow` y opcionalmente `ReplacedByToken`.
- `RevokeAllForUserAsync` revoca todos los tokens no revocados del usuario (para logout global / desactivación).

---

## 6. Refactor del Servicio de Autenticación

### 6.1. Cambios en `LoginCommandHandler`

**Estado actual:** depende de `IUserSeedStore` y `IRefreshTokenStore`.
**Estado objetivo:** depende de `IUserRepository` y `IRefreshTokenRepository` + `IUnitOfWork`.

**Flujo de login refactorizado:**

1. Normalizar email a minúsculas (sin cambio — RN-020).
2. `userRepo.GetByEmailAsync(normalizedEmail)` → si no existe, retornar `Result.Failure(DomainErrors.Auth.InvalidCredentials)`.
3. Verificar `user.IsActive` — si `false`, retornar `Result.Failure(DomainErrors.Auth.InvalidCredentials)` (**nuevo** — RN-029).
4. `BCrypt.Verify(password, user.PasswordHash)` → si falla, retornar `Result.Failure(DomainErrors.Auth.InvalidCredentials)`.
5. Construir `UserProfileDto` desde la entidad User (mapeo manual, sin AutoMapper).
6. `jwtTokenService.GenerateAccessToken(userProfile)` — sin cambio en firma.
7. `jwtTokenService.GenerateRefreshToken()` — sin cambio.
8. Crear entidad `RefreshToken` nueva, persistir vía `rtRepo.AddAsync(rt)`.
9. Actualizar `user.LastLoginAt = DateTime.UtcNow`, persistir vía `userRepo.UpdateAsync(user)`.
10. `unitOfWork.SaveChangesAsync()`.
11. Retornar `Result.Success(TokenResponseDto)` — mismo shape que hoy.

**Firma pública del método `Handle` no cambia.** Solo cambian las dependencias del constructor.

### 6.2. Cambios en `RefreshTokenCommandHandler`

**Estado actual:** depende de `IRefreshTokenStore` y `IUserSeedStore`.
**Estado objetivo:** depende de `IRefreshTokenRepository`, `IUserRepository` + `IUnitOfWork`.

**Flujo de refresh refactorizado:**

1. `rtRepo.GetByTokenAsync(request.RefreshToken)`.
2. Si no existe → `Result.Failure(DomainErrors.Auth.InvalidRefreshToken)`.
3. Si `RevokedAt != null` → `Result.Failure(DomainErrors.Auth.InvalidRefreshToken)`.
4. Si `ExpiresAt < DateTime.UtcNow` → `Result.Failure(DomainErrors.Auth.TokenExpired)`.
5. `userRepo.GetByIdAsync(rt.UserId)` → si no existe o `IsActive == false` → `Result.Failure(DomainErrors.Auth.InvalidRefreshToken)`.
6. Generar nuevo access token y nuevo refresh token string.
7. Crear entidad `RefreshToken` nueva.
8. `rtRepo.AddAsync(newRefreshToken)`.
9. `rtRepo.RevokeAsync(oldRt.Id, replacedByToken: newRefreshToken.Token)`.
10. `unitOfWork.SaveChangesAsync()`.
11. Retornar `Result.Success(TokenResponseDto)`.

### 6.3. Soporte para logout (preparación)

El `IRefreshTokenRepository.RevokeAsync` soporta el caso de logout (`replacedByToken: null`). El controller action `POST /api/v1/auth/logout` **no se crea en esta iteración** — se deja preparado el servicio para que una iteración posterior solo agregue el endpoint.

### 6.4. Interfaces a eliminar

Una vez completado el refactor:

- `IUserSeedStore` — reemplazada por `IUserRepository`.
- `IRefreshTokenStore` — reemplazada por `IRefreshTokenRepository`.
- `UserSeedStoreAdapter` — ya no necesario.
- `InMemoryRefreshTokenStore` — ya no necesario.
- `RefreshTokenEntry` record (en `IRefreshTokenStore.cs`) — reemplazado por la entidad `RefreshToken` de dominio.

La clase estática `SeedUsers` se **mantiene** pero se simplifica: solo conserva los datos necesarios para desarrollo local (los 4 usuarios mock). El seeder de producción lee de `IConfiguration`.

> **Decisión crítica:** Los 4 usuarios hardcodeados de `SeedUsers` deben migrarse a la tabla `Users` en el arranque de desarrollo. El seeder cubre esto: en `Development`, si los passwords de SeedUsers originales están configurados en `appsettings.Development.json`, se siembran. Si no, se siembran los 4 usuarios hardcodeados directamente desde la clase estática `SeedUsers` existente, re-usando sus hashes BCrypt.

---

## 7. Seeder

### 7.1. Ubicación

`Infrastructure/Persistence/UserSeeder.cs` — junto al `DbSeeder` existente para datos DANE.

### 7.2. Invocación

En `MigrationExtension.ApplyMigrationsAndSeedAsync`, después de `DbSeeder.SeedAsync()`:

```
1. Database.MigrateAsync()       ← ya existe
2. DbSeeder.SeedAsync()          ← ya existe (datos DANE)
3. UserSeeder.SeedAsync()        ← NUEVO
```

### 7.3. Configuración

| Clave | Obligatorio | Default | Secret en Key Vault |
|---|---|---|---|
| `SeedUsers:ExecAdmin:Email` | No | `alejandro.gutierrez@interkont.co` | — |
| `SeedUsers:ExecAdmin:Password` | Condicional* | — | `SeedUsers--ExecAdmin--Password` |
| `SeedUsers:ExecAdmin:FullName` | No | `Alejandro Gutiérrez` | — |
| `SeedUsers:OpAdmin:Email` | No | `admin@interkont.co` | — |
| `SeedUsers:OpAdmin:Password` | Condicional* | — | `SeedUsers--OpAdmin--Password` |
| `SeedUsers:OpAdmin:FullName` | No | `Admin Operativo` | — |

\* **Condicional:** obligatorio si cualquier clave `SeedUsers:*` está presente en la configuración. Si la sección `SeedUsers` no existe en absoluto, el seeder se omite con un log warning.

### 7.4. Comportamiento

```
IF sección SeedUsers no existe en config:
  → Log warning: "SeedUsers section not configured. User seed skipped."
  → Sembrar los 4 usuarios de desarrollo desde SeedUsers estático (solo en Development)
  → Retornar sin error

IF sección SeedUsers existe PERO falta un Password:
  → Throw InvalidOperationException: "SeedUsers:{key}:Password not configured. Seed aborted."
  → La aplicación NO arranca

FOR EACH usuario a sembrar:
  IF await repo.ExistsAsync(email):
    → Log info: "User {email} already exists, skipping."
    → Continue
  ELSE:
    → Hash password con BCrypt
    → Crear entidad User
    → repo.AddAsync(user)
    → Log info: "User {email} seeded successfully."

await unitOfWork.SaveChangesAsync()
```

### 7.5. Usuarios de desarrollo

En ambiente `Development`, además de (o en lugar de) los usuarios de Key Vault, el seeder siembra los 4 usuarios originales de `SeedUsers` para mantener compatibilidad con el frontend mock y los tests existentes:

| Email | Password Hash | FullName | Origen |
|---|---|---|---|
| `admin@gop.co` | Hash BCrypt existente en `SeedUsers.cs` | Administrador ANH | `SeedUsers` estático |
| `supervisor@gop.co` | Hash BCrypt existente | Supervisor Ecopetrol | `SeedUsers` estático |
| `operador@gop.co` | Hash BCrypt existente | Operador Ecopetrol | `SeedUsers` estático |
| `auditor@gop.co` | Hash BCrypt existente | Auditor ANH | `SeedUsers` estático |

Esto garantiza que `ng serve` + `dotnet run` localmente siguen funcionando sin configuración adicional.

---

## 8. Impacto en Contratos Públicos

**NINGUNO.** Los endpoints `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh` y `GET /api/v1/auth/me` mantienen idénticos:

- Request body shape.
- Response body shape (incluyendo `TokenResponseDto` y `UserProfileDto`).
- Status codes (200, 401, 422).
- ProblemDetails format en errores.

No se requiere `contract.yml` nuevo ni modificación del existente (004-auth-api). Los clientes (Angular frontend, Swagger, scripts de QA) no deben notar diferencia salvo que ahora los datos sobreviven a reinicios.

---

## 9. Edge Cases

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-040 | Migración sobre DB vacía (ambiente nuevo) | La migración crea tablas `Users` y `RefreshTokens`. El seeder las puebla. Login funciona inmediatamente. |
| EC-041 | Migración sobre DB con las 7 tablas existentes (staging actual) | La migración agrega solo las 2 tablas nuevas. Las 7 tablas existentes no se afectan. |
| EC-042 | Dos instancias de la API ejecutando el seeder simultáneamente | El índice único en `Email` previene duplicados. La segunda instancia recibe excepción de constraint, que el seeder maneja con `ExistsAsync` check + catch de constraint violation como fallback. |
| EC-043 | Refresh token rotación concurrente (mismo token, dos requests) | La primera request revoca el token y crea uno nuevo. La segunda encuentra el token ya revocado → 401. |
| EC-044 | Usuario desactivado entre login y refresh | El refresh verifica `user.IsActive` en cada request. Si fue desactivado → 401. |
| EC-045 | Password de seed con caracteres especiales (UTF-8, emojis) | BCrypt maneja correctamente UTF-8. No hay restricción de charset. |
| EC-046 | Key Vault no disponible al arranque (timeout de red) | `AddKeyVaultIfConfigured` ya maneja esto. Si KV no carga, los passwords de seed no están en config → seeder aborta → fail fast. |
| EC-047 | `SeedUsers` estáticos (dev) tienen emails que colisionan con seed de config | El check `ExistsAsync` previene duplicados. El primero en sembrarse gana. El seeder logguea que el otro ya existe. |
| EC-048 | Tabla `Users` vaciada manualmente en staging | En el siguiente reinicio, el seeder recrea los admin users automáticamente (idempotente). |

---

## 10. Restricciones Técnicas

| Restricción | Descripción |
|---|---|
| `User` no hereda de `AuditableEntity` | Los campos de auditoría (`CreatedBy`, `IsDeleted`) no aplican a la tabla de Users en esta iteración. Se simplifica intencionalmente. |
| `User` no tiene `Role`, `TenantId`, `TenantName` | Estos datos se agregan en Iter 9 (RBAC). Por ahora, el `UserProfileDto` sigue construyéndose con datos del seed estático (dev) o con defaults para los admin users. |
| Login handler mapea `User` → `UserProfileDto` | Mientras no existan roles en la tabla, el mapeo usa el campo `FullName` del User para `Name` y valores hardcoded para `Role`, `TenantId`, `TenantName` que se resolverán en Iter 9. **Decisión transitoria** documentada en plan.md. |
| Sin AutoMapper para User → UserProfileDto | El mapeo es manual (factory method o extensión) porque solo tiene 6 campos y es específico del handler. Ver CONSTITUTION.backend.md §12.3 (Regla: "Sin lógica en mappings"). |
| Migración compatible con state anterior | La migración `AddUsersAndRefreshTokens` no modifica tablas existentes. Solo crea `Users` y `RefreshTokens`. |
| `SeedUsers` estático se conserva en Infrastructure | Se usa exclusivamente como fuente de datos para el seeder en modo Development. No se inyecta más en handlers. |
| BCrypt work factor | Se usa el default de `BCrypt.Net-Next` (actualmente 11). No se configura explícitamente salvo que ya esté customizado. |

---

## 11. Tests Requeridos

### 11.1. Unit Tests — Application

**`LoginCommandHandlerTests` (actualizar existentes):**

| Test | Escenario |
|---|---|
| `Handle_ValidCredentials_ReturnsTokenResponse` | User existe, activo, password correcto → `IsSuccess`, token no vacío, `LastLoginAt` actualizado |
| `Handle_InvalidPassword_ReturnsFailure` | Password incorrecto → `DomainErrors.Auth.InvalidCredentials` |
| `Handle_NonexistentEmail_ReturnsFailure` | Email no existe → mismo error (no revela info) |
| `Handle_CaseInsensitiveEmail_ReturnsSuccess` | `ADMIN@GOP.CO` → login exitoso |
| `Handle_InactiveUser_ReturnsFailure` | `IsActive = false` → `DomainErrors.Auth.InvalidCredentials` |

**`RefreshTokenCommandHandlerTests` (actualizar existentes):**

| Test | Escenario |
|---|---|
| `Handle_ValidRefreshToken_ReturnsNewTokenPair` | Token vigente, no revocado, user activo → nuevo par |
| `Handle_RevokedRefreshToken_ReturnsFailure` | Token con `RevokedAt != null` → `InvalidRefreshToken` |
| `Handle_ExpiredRefreshToken_ReturnsFailure` | `ExpiresAt < UtcNow` → `TokenExpired` |
| `Handle_InactiveUserRefresh_ReturnsFailure` | User `IsActive = false` → `InvalidRefreshToken` |
| `Handle_NonexistentRefreshToken_ReturnsFailure` | Token no existe en repo → `InvalidRefreshToken` |

### 11.2. Unit Tests — Infrastructure

**`UserRepositoryTests`:**

| Test | Escenario |
|---|---|
| `GetByEmailAsync_ExistingUser_ReturnsUser` | Insertar user, buscar por email → user encontrado |
| `GetByEmailAsync_NonExistent_ReturnsNull` | Buscar email inexistente → null |
| `AddAsync_DuplicateEmail_ThrowsDbUpdateException` | Insertar 2 users con mismo email → excepción de constraint |
| `ExistsAsync_ExistingEmail_ReturnsTrue` | Email existe → true |
| `ExistsAsync_NonExistent_ReturnsFalse` | Email no existe → false |
| `UpdateAsync_UpdatesLastLoginAt` | Actualizar LastLoginAt → persistido correctamente |

**`RefreshTokenRepositoryTests`:**

| Test | Escenario |
|---|---|
| `AddAndGetByToken_ReturnsToken` | Insertar RT, buscar por token string → RT encontrado |
| `GetByTokenAsync_NonExistent_ReturnsNull` | Token inexistente → null |
| `RevokeAsync_SetsRevokedAtAndReplacedBy` | Revocar con replacement → `RevokedAt` y `ReplacedByToken` seteados |
| `RevokeAllForUserAsync_RevokesAllActive` | 3 tokens del user, 1 ya revocado → revoca los 2 activos |

### 11.3. Unit Tests — Seeder

**`UserSeederTests`:**

| Test | Escenario |
|---|---|
| `SeedAsync_EmptyDb_InsertsConfiguredUsers` | DB vacía + config presente → 2 admin users creados |
| `SeedAsync_UsersAlreadyExist_NoChanges` | Users ya existen → no duplica, no actualiza password |
| `SeedAsync_MissingPassword_ThrowsException` | Config con email pero sin password → `InvalidOperationException` |
| `SeedAsync_NoSeedSection_SkipsGracefully` | Sin sección `SeedUsers` → log warning, sin error |

### 11.4. Integration Tests — API

**`AuthControllerTests` (actualizar existentes):**

| Test | Escenario |
|---|---|
| `Login_ValidCredentials_Returns200WithTokens` | POST login con user seeded → 200 + JWT + RT |
| `Login_InvalidCredentials_Returns401` | POST login con password incorrecto → 401 |
| `Refresh_ValidToken_Returns200WithNewTokens` | Login, luego refresh → 200 + nuevo par |
| `Refresh_AfterUsed_Returns401` | Login, refresh, re-usar viejo RT → 401 |
| `Refresh_RevokedToken_Returns401` | Refresh exitoso, intentar con token revocado → 401 |
| `GetMe_WithValidToken_Returns200` | Login, usar accessToken para /me → 200 + perfil |
| `Login_RefreshToken_PersistedInDatabase` | Login → verificar que RT existe en DB (acceso directo al DbContext) |

### 11.5. Objetivo de calidad

- **100% de tests existentes siguen verdes.** Los tests de 004-auth-api se actualizan para el nuevo behavior (repositorios en vez de in-memory stores).
- No bajar coverage global.
- Los tests de integración usan SQLite in-memory o la configuración que ya tenga `GopTestWebApplicationFactory`.

---

## 12. Definition of Done

- [ ] Branch `007-users-persistence` mergeable a `gop-base-web`, CI verde.
- [ ] Migración `AddUsersAndRefreshTokens` corre limpia en DB vacía y en DB con el estado actual de staging (7 tablas de dominio).
- [ ] `grep -r "ConcurrentDictionary" backend/src/` en lo relativo a auth devuelve **cero matches**.
- [ ] `IUserSeedStore` e `IRefreshTokenStore` eliminados del código base.
- [ ] `InMemoryRefreshTokenStore` y `UserSeedStoreAdapter` eliminados.
- [ ] Los contratos públicos de `/api/v1/auth/*` no cambian su shape de request/response.
- [ ] Seeder idempotente validado con tests.
- [ ] `CLAUDE.md` actualizado con subsección "Secrets de seed de usuarios".
- [ ] Los 4 usuarios de desarrollo (`admin@gop.co`, `supervisor@gop.co`, `operador@gop.co`, `auditor@gop.co`) siguen funcionando localmente.
- [ ] Ambos admins (`alejandro.gutierrez@interkont.co` + `admin@interkont.co`) pueden hacer login contra staging una vez los passwords estén en Key Vault y se haga redeploy.
- [ ] Reinicio del App Service **no** pierde users ni tokens.

---

## 13. Checklist de Revisión

- [x] Las historias cubren todos los flujos de autenticación (login, refresh) con escenarios de happy path, validación, usuario inactivo y post-reinicio
- [x] Se definen las 2 entidades de dominio con todos sus campos, tipos y constraints
- [x] Los contratos de repositorio cubren todas las operaciones necesarias
- [x] El refactor del servicio de autenticación está documentado paso a paso
- [x] El seeder cubre los 4 escenarios: DB vacía, repetición, password faltante, sin sección de config
- [x] Los edge cases cubren concurrencia, migración en ambos estados de DB, y Key Vault no disponible
- [x] Los tests cubren unit (handlers, repos, seeder) e integration (API endpoints)
- [x] No se incluyen cambios en contratos públicos
- [x] No se incluyen cambios frontend, Bicep ni pipelines
- [x] Roles: la entidad User no tiene Role — se resuelve en Iter 9
