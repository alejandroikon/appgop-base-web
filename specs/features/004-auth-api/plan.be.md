# Plan Backend: API de Autenticación JWT

**Feature ID:** 004-auth-api
**Spec de referencia:** `specs/features/004-auth-api/spec.md`
**Contrato de referencia:** `specs/features/004-auth-api/contract.yml`
**Constitución de referencia:** `CONSTITUTION.backend.md`
**Depende de:** 003-backend-scaffold (implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Resumen Arquitectónico

Esta feature agrega autenticación JWT real al backend scaffold existente. Afecta las 4 capas:

- **Domain:** Enum `UserRole`, errores de dominio `DomainErrors.Auth`
- **Application:** Command `Login`, Command `RefreshToken`, Query `GetCurrentUser`, interfaz `IJwtTokenService`, DTOs
- **Infrastructure:** Implementaciones de JWT (`JwtTokenService`, `CurrentUserService`), seed de usuarios, store de refresh tokens en memoria
- **API:** `AuthController` con 3 endpoints, configuración de JWT auth en `Program.cs`

Los usuarios son **seed en memoria** (no en base de datos). Los refresh tokens se almacenan en `ConcurrentDictionary` (Singleton). No se agrega ninguna tabla ni migración EF Core.

---

## 2. Árbol de Archivos

### 2.1. Archivos a CREAR

```
backend/src/
├── GOP.Domain/
│   ├── Enums/
│   │   └── UserRole.cs                                    # Enum: ADMIN, SUPERVISOR, OPERADOR, AUDITOR
│   └── Errors/
│       └── DomainErrors.cs                                # Clase parcial con DomainErrors.Auth (InvalidCredentials, TokenExpired, InvalidRefreshToken)
│
├── GOP.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       └── IJwtTokenService.cs                        # GenerateAccessToken, GenerateRefreshToken, GetPrincipalFromExpiredToken
│   └── Features/
│       └── Auth/
│           ├── Commands/
│           │   ├── Login/
│           │   │   ├── LoginCommand.cs                    # Record: Email, Password
│           │   │   ├── LoginCommandHandler.cs             # Valida credenciales, genera tokens, retorna TokenResponseDto
│           │   │   └── LoginCommandValidator.cs           # Email required + format, Password required
│           │   └── RefreshToken/
│           │       ├── RefreshTokenCommand.cs              # Record: RefreshToken string
│           │       ├── RefreshTokenCommandHandler.cs       # Valida refresh token, genera nuevo par
│           │       └── RefreshTokenCommandValidator.cs     # RefreshToken required
│           ├── Queries/
│           │   └── GetCurrentUser/
│           │       ├── GetCurrentUserQuery.cs              # Record vacío (lee datos de ICurrentUserService)
│           │       ├── GetCurrentUserQueryHandler.cs       # Lee ICurrentUserService, retorna UserProfileDto
│           │       └── UserProfileDto.cs                   # Id, Email, Name, Role, TenantId, TenantName
│           └── Dtos/
│               └── TokenResponseDto.cs                    # AccessToken, RefreshToken, ExpiresIn, User (UserProfileDto)
│
├── GOP.Infrastructure/
│   └── Identity/
│       ├── JwtSettings.cs                                 # Options class: Issuer, Audience, SigningKey, AccessTokenExpirationMinutes, RefreshTokenExpirationDays
│       ├── JwtTokenService.cs                             # Implementa IJwtTokenService — genera JWT HS256
│       ├── CurrentUserService.cs                          # Reemplaza stub — lee claims de HttpContext.User
│       ├── SeedUsers.cs                                   # Datos estáticos de los 4 usuarios con password hash
│       └── InMemoryRefreshTokenStore.cs                   # ConcurrentDictionary<string, RefreshTokenEntry> — Singleton
│
└── GOP.API/
    └── Controllers/
        └── AuthController.cs                              # 3 endpoints: login, refresh, me

backend/tests/
├── GOP.Domain.Tests/
│   └── Errors/
│       └── DomainErrorsTests.cs                           # Verifica que códigos de error no están vacíos
│
├── GOP.Application.Tests/
│   └── Features/
│       └── Auth/
│           ├── LoginCommandHandlerTests.cs                # Happy path, invalid credentials, case-insensitive email
│           ├── LoginCommandValidatorTests.cs              # Email vacío, email inválido, password vacío
│           └── RefreshTokenCommandHandlerTests.cs         # Happy path, expired, invalid, already-used
│
└── GOP.API.Tests/
    └── Controllers/
        └── AuthControllerTests.cs                         # Integración: login 200, login 401, me 200, me 401, refresh 200
```

### 2.2. Archivos a MODIFICAR

```
backend/src/
├── GOP.Infrastructure/
│   ├── DependencyInjection.cs                             # + registrar JwtTokenService, CurrentUserService (reemplaza stub), InMemoryRefreshTokenStore, JwtSettings
│   └── Services/
│       └── CurrentUserServiceStub.cs                      # ELIMINAR — reemplazado por Identity/CurrentUserService.cs
│
└── GOP.API/
    ├── Program.cs                                         # + AddAuthentication().AddJwtBearer(), AddAuthorization(), UseAuthentication(), UseAuthorization()
    ├── appsettings.json                                   # + sección JwtSettings
    └── appsettings.Development.json                       # + JwtSettings con signing key de desarrollo
```

---

## 3. Detalle de Archivos Clave

### 3.1. Domain

**`UserRole.cs`**
```csharp
public enum UserRole { Admin, Supervisor, Operador, Auditor }
```
> Los valores se serializan como `"ADMIN"`, `"SUPERVISOR"`, etc. usando string conversion en la capa de presentación.

**`DomainErrors.cs`**
```csharp
public static partial class DomainErrors
{
    public static class Auth
    {
        public static readonly Error InvalidCredentials = new(
            "Auth.InvalidCredentials", "Correo o contraseña incorrectos.");
        public static readonly Error TokenExpired = new(
            "Auth.TokenExpired", "El token de actualización ha expirado. Inicie sesión nuevamente.");
        public static readonly Error InvalidRefreshToken = new(
            "Auth.InvalidRefreshToken", "Token de actualización inválido.");
    }
}
```

### 3.2. Application

**`IJwtTokenService.cs`**
```csharp
public interface IJwtTokenService
{
    string GenerateAccessToken(UserProfileDto user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

**`LoginCommandHandler.cs`** — Flujo:
1. Normalizar email a minúsculas (RN-020)
2. Buscar usuario en `SeedUsers` por email
3. Verificar password hash con BCrypt
4. Si falla → `Result.Failure(DomainErrors.Auth.InvalidCredentials)`
5. Generar access token + refresh token via `IJwtTokenService`
6. Almacenar refresh token en `InMemoryRefreshTokenStore`
7. Retornar `Result.Success(TokenResponseDto)`

**`RefreshTokenCommandHandler.cs`** — Flujo:
1. Buscar refresh token en `InMemoryRefreshTokenStore`
2. Si no existe → `Result.Failure(DomainErrors.Auth.InvalidRefreshToken)`
3. Si expirado → `Result.Failure(DomainErrors.Auth.TokenExpired)`
4. Invalidar refresh token actual (remove from store)
5. Generar nuevo access token + nuevo refresh token
6. Almacenar nuevo refresh token
7. Retornar `Result.Success(TokenResponseDto)`

**`GetCurrentUserQueryHandler.cs`** — Flujo:
1. Leer `ICurrentUserService` (claims del JWT)
2. Mapear a `UserProfileDto`
3. Retornar `Result.Success(dto)`

**DTOs:**

```csharp
// TokenResponseDto.cs
public sealed record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserProfileDto User);

// UserProfileDto.cs
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    string TenantId,
    string TenantName);
```

### 3.3. Infrastructure

**`JwtSettings.cs`**
```csharp
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; init; } = "GOP360";
    public string Audience { get; init; } = "GOP360-Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; init; } = 30;
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
```

**`SeedUsers.cs`** — Clase estática con los 4 usuarios. Passwords almacenados como hash BCrypt generados una sola vez.

**`InMemoryRefreshTokenStore.cs`** — Singleton con `ConcurrentDictionary`. Entry contiene: `UserId`, `Token`, `ExpiresAt`, `IsUsed`.

**`CurrentUserService.cs`** — Lee claims del `HttpContext.User`:
- `sub` → `UserId`
- `email` → `Email`
- `name` → `Name`
- `role` → `Role`
- `tenant_id` → `TenantId` (parseado a int)
- `IsAuthenticated` → `HttpContext.User.Identity?.IsAuthenticated ?? false`

**`JwtTokenService.cs`** — Genera JWT con `SymmetricSecurityKey` + `HmacSha256`. Claims según spec.md §5.

### 3.4. API

**`AuthController.cs`** — Según contract.yml:
- `POST /api/v1/auth/login` → `ISender.Send(LoginCommand)` → `Result<TokenResponseDto>.ToActionResult()`
- `POST /api/v1/auth/refresh` → `ISender.Send(RefreshTokenCommand)` → `Result<TokenResponseDto>.ToActionResult()`
- `GET /api/v1/auth/me` → `[Authorize]` → `ISender.Send(GetCurrentUserQuery)` → `Result<UserProfileDto>.ToActionResult()`

**`Program.cs` modificaciones:**

```csharp
// Después de AddInfrastructure()
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// En el pipeline (después de UseCors, antes de MapControllers):
app.UseAuthentication();
app.UseAuthorization();
```

**`appsettings.json`** — Agregar sección:
```json
"JwtSettings": {
    "Issuer": "GOP360",
    "Audience": "GOP360-Client",
    "SigningKey": "CONFIGURE_IN_ENVIRONMENT_SPECIFIC_FILE",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
}
```

**`appsettings.Development.json`** — Agregar signing key de desarrollo:
```json
"JwtSettings": {
    "SigningKey": "gop360-dev-signing-key-minimum-32-chars-long!"
}
```

### 3.5. NuGet adicionales

| Proyecto | Paquete | Propósito |
|---|---|---|
| GOP.Infrastructure | `BCrypt.Net-Next` | Hashing de passwords |
| GOP.Infrastructure | `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth middleware |
| GOP.API | `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth en pipeline |

---

## 4. Mapeo Result → HTTP para Auth

Se extiende la lógica de `ResultExtensions` existente. Los errores de Auth mapean así:

| Error Code | HTTP Status | Tratamiento |
|---|---|---|
| `Auth.InvalidCredentials` | `401` | No usa el pattern genérico `.Unauthorized` → 403. Necesita retornar 401 explícito. |
| `Auth.TokenExpired` | `401` | Idem. |
| `Auth.InvalidRefreshToken` | `401` | Idem. |

Se agrega un pattern match para `Auth.*` → 401 en `ResultExtensions.MapErrorToActionResult()`.

---

## 5. Tests Mínimos

### 5.1. `LoginCommandHandlerTests.cs` (Application.Tests)

| Test | Escenario |
|---|---|
| `Handle_ValidCredentials_ReturnsTokenResponse` | Email + password correctos → `IsSuccess`, token no vacío |
| `Handle_InvalidPassword_ReturnsFailure` | Password incorrecto → `DomainErrors.Auth.InvalidCredentials` |
| `Handle_NonexistentEmail_ReturnsFailure` | Email no existe → mismo error (no revela info) |
| `Handle_CaseInsensitiveEmail_ReturnsSuccess` | `ADMIN@GOP.CO` → login exitoso |

### 5.2. `LoginCommandValidatorTests.cs` (Application.Tests)

| Test | Escenario |
|---|---|
| `Validate_EmptyEmail_ReturnsError` | Email vacío → error de validación |
| `Validate_InvalidEmailFormat_ReturnsError` | `"not-an-email"` → error |
| `Validate_EmptyPassword_ReturnsError` | Password vacío → error |
| `Validate_ValidInput_PassesValidation` | Input correcto → sin errores |

### 5.3. `RefreshTokenCommandHandlerTests.cs` (Application.Tests)

| Test | Escenario |
|---|---|
| `Handle_ValidRefreshToken_ReturnsNewTokenPair` | Refresh token vigente → nuevo par de tokens |
| `Handle_ExpiredRefreshToken_ReturnsFailure` | Token expirado → `DomainErrors.Auth.TokenExpired` |
| `Handle_InvalidRefreshToken_ReturnsFailure` | Token inexistente → `DomainErrors.Auth.InvalidRefreshToken` |
| `Handle_AlreadyUsedRefreshToken_ReturnsFailure` | Token ya consumido → `InvalidRefreshToken` |

### 5.4. `AuthControllerTests.cs` (API.Tests)

| Test | Escenario |
|---|---|
| `Login_ValidCredentials_Returns200WithTokens` | POST login → 200 + body con `accessToken` |
| `Login_InvalidCredentials_Returns401` | POST login con password incorrecto → 401 |
| `Login_EmptyBody_Returns422` | POST login con `{}` → 422 |
| `GetMe_WithValidToken_Returns200WithProfile` | GET /auth/me con JWT → 200 + UserProfile |
| `GetMe_WithoutToken_Returns401` | GET /auth/me sin header → 401 |
| `Refresh_ValidToken_Returns200WithNewTokens` | POST refresh → 200 + nuevo par |

---

## 6. Bloques Compilables

### Bloque 1 — Domain (dotnet build GOP.Domain)
- `UserRole.cs`, `DomainErrors.cs`

### Bloque 2 — Application (dotnet build GOP.Application)
- `IJwtTokenService.cs`
- `UserProfileDto.cs`, `TokenResponseDto.cs`
- `LoginCommand.cs`, `LoginCommandHandler.cs`, `LoginCommandValidator.cs`
- `RefreshTokenCommand.cs`, `RefreshTokenCommandHandler.cs`, `RefreshTokenCommandValidator.cs`
- `GetCurrentUserQuery.cs`, `GetCurrentUserQueryHandler.cs`

### Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)
- `JwtSettings.cs`, `SeedUsers.cs`, `InMemoryRefreshTokenStore.cs`
- `JwtTokenService.cs`, `CurrentUserService.cs`
- Modificar `DependencyInjection.cs` (reemplazar stub, registrar nuevos servicios)
- Eliminar `CurrentUserServiceStub.cs`

### Bloque 4 — API (dotnet build GOP.API)
- `AuthController.cs`
- Modificar `Program.cs` (JWT auth)
- Modificar `appsettings.json`, `appsettings.Development.json`
- Modificar `ResultExtensions.cs` (pattern Auth.* → 401)

### Bloque 5 — Tests (dotnet test GOP.sln)
- `DomainErrorsTests.cs`
- `LoginCommandHandlerTests.cs`, `LoginCommandValidatorTests.cs`
- `RefreshTokenCommandHandlerTests.cs`
- `AuthControllerTests.cs`

---

## 7. Orden de Dependencias entre Bloques

```
B1 (Domain) → B2 (Application) → B3 (Infrastructure) → B4 (API) → B5 (Tests)
```

Secuencial estricto. Cada bloque depende del anterior.
