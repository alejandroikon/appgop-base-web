# Tasks Backend: API de Autenticación JWT (004-auth-api)

**Input:** `specs/features/004-auth-api/spec.md` + `specs/features/004-auth-api/plan.be.md`
**Contrato:** `specs/features/004-auth-api/contract.yml`
**Constitución:** `CONSTITUTION.backend.md`

**Formato:** `[ID] [P?] [HU?] Descripción — ruta/del/archivo`
- **[P]**: Paralelo — sin dependencias de tareas incompletas en el mismo bloque
- **[HU-N]**: Historia de usuario de referencia en spec.md
- **[BLOQUEANTE]**: Tareas posteriores no pueden iniciar sin esta completada

---

## Bloque 1 — Domain (dotnet build GOP.Domain)

**Propósito:** Agregar el enum `UserRole` y el catálogo de errores de autenticación al dominio. Sin dependencias externas.

- [x] T001 [P] [HU-009] Crear enum `UserRole` — valores: `Admin`, `Supervisor`, `Operador`, `Auditor` (serializados como `"ADMIN"`, etc. en la capa de presentación) — `backend/src/GOP.Domain/Enums/UserRole.cs`
- [x] T002 [P] [HU-009] Crear clase parcial `DomainErrors` con inner class `Auth` — errores estáticos: `InvalidCredentials` (`"Auth.InvalidCredentials"`, `"Correo o contraseña incorrectos."`), `TokenExpired` (`"Auth.TokenExpired"`, `"El token de actualización ha expirado. Inicie sesión nuevamente."`), `InvalidRefreshToken` (`"Auth.InvalidRefreshToken"`, `"Token de actualización inválido."`) — `backend/src/GOP.Domain/Errors/DomainErrors.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0. ✅

---

## Bloque 2 — Application (dotnet build GOP.Application)

**Propósito:** Implementar los casos de uso de autenticación: Login command, RefreshToken command, GetCurrentUser query, DTOs y la interfaz del servicio JWT. Sin dependencias de Infrastructure.

**⚠️ BLOQUEANTE:** Los DTOs e interfaz deben existir antes de los handlers.

- [x] T003 [P] [HU-011] Crear DTO `UserProfileDto` — sealed record con: `Id` (Guid), `Email`, `Name`, `Role`, `TenantId`, `TenantName` (todos string) — `backend/src/GOP.Application/Features/Auth/Queries/GetCurrentUser/UserProfileDto.cs`
- [x] T004 [P] [HU-009] Crear DTO `TokenResponseDto` — sealed record con: `AccessToken`, `RefreshToken` (string), `ExpiresIn` (int), `User` (UserProfileDto) — `backend/src/GOP.Application/Features/Auth/Dtos/TokenResponseDto.cs`
- [x] T005 [P] [HU-009] Crear interfaz `IJwtTokenService` — métodos: `GenerateAccessToken(UserProfileDto user)` → string, `GenerateRefreshToken()` → string, `GetPrincipalFromExpiredToken(string token)` → ClaimsPrincipal? — `backend/src/GOP.Application/Common/Interfaces/IJwtTokenService.cs`
- [x] T006 [HU-009] Crear record `LoginCommand` — `Email` (string), `Password` (string), implementa `IRequest<Result<TokenResponseDto>>` — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommand.cs`
- [x] T007 [HU-009] Crear `LoginCommandValidator` — reglas: Email NotEmpty + EmailAddress format, Password NotEmpty; mensajes en español — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommandValidator.cs`
- [x] T008 [BLOQUEANTE] [HU-009] Crear `LoginCommandHandler` — flujo: normalizar email a minúsculas → buscar en SeedUsers → verificar BCrypt hash → generar access+refresh tokens via IJwtTokenService → almacenar refresh en InMemoryRefreshTokenStore → retornar Result.Success(TokenResponseDto); en error → Result.Failure(DomainErrors.Auth.InvalidCredentials) — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- [x] T009 [HU-010] Crear record `RefreshTokenCommand` — `RefreshToken` (string), implementa `IRequest<Result<TokenResponseDto>>` — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs`
- [x] T010 [HU-010] Crear `RefreshTokenCommandValidator` — regla: RefreshToken NotEmpty; mensaje en español — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandValidator.cs`
- [x] T011 [BLOQUEANTE] [HU-010] Crear `RefreshTokenCommandHandler` — flujo: buscar token en InMemoryRefreshTokenStore → si no existe: InvalidRefreshToken → si expirado: TokenExpired → invalidar token actual (remove) → buscar usuario por userId → generar nuevo par tokens → almacenar nuevo refresh → retornar Result.Success(TokenResponseDto) — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
- [x] T012 [P] [HU-011] Crear record `GetCurrentUserQuery` — record vacío, implementa `IRequest<Result<UserProfileDto>>` — `backend/src/GOP.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQuery.cs`
- [x] T013 [HU-011] Crear `GetCurrentUserQueryHandler` — lee ICurrentUserService, mapea a UserProfileDto, retorna Result.Success(dto) — `backend/src/GOP.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0. ✅

---

## Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)

**Propósito:** Implementar los servicios de JWT, seed de usuarios, store de refresh tokens en memoria y reemplazar el stub de `ICurrentUserService`.

- [x] T014 [P] [HU-009] Crear `JwtSettings` — options class con `SectionName = "JwtSettings"`, propiedades: `Issuer`, `Audience`, `SigningKey`, `AccessTokenExpirationMinutes` (default 30), `RefreshTokenExpirationDays` (default 7) — `backend/src/GOP.Infrastructure/Identity/JwtSettings.cs`
- [x] T015 [P] [HU-009] Crear `SeedUsers` — clase estática con lista de 4 usuarios (admin@gop.co, supervisor@gop.co, operador@gop.co, auditor@gop.co); passwords como hash BCrypt pre-generados; método `FindByEmail(string)` y `GetById(Guid)` — `backend/src/GOP.Infrastructure/Identity/SeedUsers.cs`
- [x] T016 [P] [HU-010] Crear `InMemoryRefreshTokenStore` — Singleton con `ConcurrentDictionary<string, RefreshTokenEntry>`; entry: `UserId` (Guid), `Token` (string), `ExpiresAt` (DateTime), `IsUsed` (bool); métodos: `Store()`, `TryConsume()`, `Remove()` — `backend/src/GOP.Infrastructure/Identity/InMemoryRefreshTokenStore.cs`
- [x] T017 [HU-009] Crear `JwtTokenService` — implementa `IJwtTokenService`; genera JWT HS256 con claims (sub, email, name, role, tenant_id, tenant_name, iat, exp); usa `JwtSettings` via IOptions; genera refresh token como Guid.NewGuid().ToString() — `backend/src/GOP.Infrastructure/Identity/JwtTokenService.cs`
- [x] T018 [HU-011] Crear `CurrentUserService` — implementa `ICurrentUserService` leyendo claims de `HttpContext.User` via `IHttpContextAccessor`; mapea sub→UserId, email→Email, name→Name, role→Role, tenant_id→TenantId; IsAuthenticated lee HttpContext.User.Identity — `backend/src/GOP.Infrastructure/Identity/CurrentUserService.cs`
- [x] T019 [BLOQUEANTE] Modificar `DependencyInjection.cs` — agregar: `Configure<JwtSettings>()`, registrar `IJwtTokenService` → `JwtTokenService` (Scoped), `ICurrentUserService` → `CurrentUserService` (Scoped, reemplaza `CurrentUserServiceStub`), `InMemoryRefreshTokenStore` (Singleton), `IHttpContextAccessor` (Singleton); agregar NuGet `BCrypt.Net-Next` — `backend/src/GOP.Infrastructure/DependencyInjection.cs`
- [x] T020 Eliminar `CurrentUserServiceStub` — ya no es necesario, reemplazado por `CurrentUserService` real — `backend/src/GOP.Infrastructure/Services/CurrentUserServiceStub.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0. ✅

---

## Bloque 4 — API (dotnet build GOP.API)

**Propósito:** Crear el controller de autenticación, configurar JWT Bearer auth en el pipeline y extender el mapeo Result → HTTP para errores de auth (401).

- [x] T021 [P] [HU-009] Modificar `appsettings.json` — agregar sección `JwtSettings` con: Issuer `"GOP360"`, Audience `"GOP360-Client"`, SigningKey `"CONFIGURE_IN_ENVIRONMENT_SPECIFIC_FILE"`, AccessTokenExpirationMinutes `30`, RefreshTokenExpirationDays `7` — `backend/src/GOP.API/appsettings.json`
- [x] T022 [P] [HU-009] Modificar `appsettings.Development.json` — agregar sección `JwtSettings` con: SigningKey `"gop360-dev-signing-key-minimum-32-chars-long!"` (override del placeholder) — `backend/src/GOP.API/appsettings.Development.json`
- [x] T023 [HU-009] Modificar `ResultExtensions.cs` — agregar pattern match: error codes que empiecen con `"Auth."` → retornar `401 Unauthorized` (UnauthorizedObjectResult con ProblemDetails); insertar antes del pattern `.Unauthorized` → 403 existente — `backend/src/GOP.API/Extensions/ResultExtensions.cs`
- [x] T024 [HU-009] [HU-010] [HU-011] Crear `AuthController` — `[ApiController]`, `[Route("api/v1/auth")]`, `[Produces("application/json")]`; inyecta `ISender` via primary constructor; 3 actions: `[HttpPost("login")]` sin auth → Send(LoginCommand), `[HttpPost("refresh")]` sin auth → Send(RefreshTokenCommand), `[HttpGet("me")]` con `[Authorize]` → Send(GetCurrentUserQuery); cada action máximo 3 líneas; ProducesResponseType para Swagger — `backend/src/GOP.API/Controllers/AuthController.cs`
- [x] T025 [BLOQUEANTE] [HU-009] Modificar `Program.cs` — agregar: leer `JwtSettings` de configuración, `AddAuthentication(JwtBearerDefaults)` + `AddJwtBearer()` con `TokenValidationParameters` (ValidateIssuer, ValidateAudience, ValidateIssuerSigningKey con SymmetricSecurityKey, ValidateLifetime, ClockSkew=Zero), `AddAuthorization()`; en pipeline agregar `UseAuthentication()` y `UseAuthorization()` después de `UseCors()` y antes de `MapControllers()` — `backend/src/GOP.API/Program.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → exit code 0. ✅

---

## Bloque 5 — Tests (dotnet test GOP.sln)

**Propósito:** Validar toda la lógica de autenticación con tests unitarios y de integración.

**Depende de:** Bloques 1-4 completos.

- [x] T026 [P] [HU-009] Crear `DomainErrorsTests` — tests: `Auth_InvalidCredentials_HasCodeAndMessage`, `Auth_TokenExpired_HasCodeAndMessage`, `Auth_InvalidRefreshToken_HasCodeAndMessage` (verificar que Code y Message no están vacíos) — `backend/tests/GOP.Domain.Tests/Errors/DomainErrorsTests.cs`
- [x] T027 [P] [HU-009] Crear `LoginCommandValidatorTests` — 4 tests: `Validate_EmptyEmail_ReturnsError`, `Validate_InvalidEmailFormat_ReturnsError`, `Validate_EmptyPassword_ReturnsError`, `Validate_ValidInput_PassesValidation` — `backend/tests/GOP.Application.Tests/Features/Auth/LoginCommandValidatorTests.cs`
- [x] T028 [HU-009] Crear `LoginCommandHandlerTests` — 4 tests: `Handle_ValidCredentials_ReturnsTokenResponse` (mock IJwtTokenService, SeedUsers, InMemoryRefreshTokenStore), `Handle_InvalidPassword_ReturnsFailure`, `Handle_NonexistentEmail_ReturnsFailure` (mismo error, no revela info), `Handle_CaseInsensitiveEmail_ReturnsSuccess` (`ADMIN@GOP.CO`) — `backend/tests/GOP.Application.Tests/Features/Auth/LoginCommandHandlerTests.cs`
- [x] T029 [P] [HU-010] Crear `RefreshTokenCommandHandlerTests` — 4 tests: `Handle_ValidRefreshToken_ReturnsNewTokenPair`, `Handle_ExpiredRefreshToken_ReturnsFailure`, `Handle_InvalidRefreshToken_ReturnsFailure`, `Handle_AlreadyUsedRefreshToken_ReturnsFailure` — `backend/tests/GOP.Application.Tests/Features/Auth/RefreshTokenCommandHandlerTests.cs`
- [x] T030 [HU-009] [HU-010] [HU-011] Crear `AuthControllerTests` — 6 tests de integración con `WebApplicationFactory<Program>`: `Login_ValidCredentials_Returns200WithTokens`, `Login_InvalidCredentials_Returns401`, `Login_EmptyBody_Returns422`, `GetMe_WithValidToken_Returns200WithProfile` (login primero, usar accessToken para /me), `GetMe_WithoutToken_Returns401`, `Refresh_ValidToken_Returns200WithNewTokens` (login, luego refresh) — `backend/tests/GOP.API.Tests/Controllers/AuthControllerTests.cs`

**Checkpoint:** `cd backend && dotnet test GOP.sln` → todos los tests pasan (11 existentes + 21 nuevos = 32 tests), exit code 0. ✅

---

## Dependencies & Execution Order

### Dependencias entre Bloques

```
Bloque 1 (Domain)         → Sin dependencias de esta feature. Depende del scaffold (003).
Bloque 2 (Application)    → Depende de Bloque 1 (UserRole, DomainErrors).
Bloque 3 (Infrastructure) → Depende de Bloque 2 (IJwtTokenService, DTOs, ICurrentUserService).
Bloque 4 (API)            → Depende de Bloque 2 + Bloque 3 (DI extensions, handlers, servicios).
Bloque 5 (Tests)          → Depende de Bloques 1-4 completos.
```

### Diagrama

```
B1 ──→ B2 ──→ B3 ──→ B4 ──→ B5
```

Secuencial estricto.
