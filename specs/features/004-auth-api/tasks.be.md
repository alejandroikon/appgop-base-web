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

- [ ] T001 [P] [HU-009] Crear enum `UserRole` — valores: `Admin`, `Supervisor`, `Operador`, `Auditor` (serializados como `"ADMIN"`, etc. en la capa de presentación) — `backend/src/GOP.Domain/Enums/UserRole.cs`
- [ ] T002 [P] [HU-009] Crear clase parcial `DomainErrors` con inner class `Auth` — errores estáticos: `InvalidCredentials` (`"Auth.InvalidCredentials"`, `"Correo o contraseña incorrectos."`), `TokenExpired` (`"Auth.TokenExpired"`, `"El token de actualización ha expirado. Inicie sesión nuevamente."`), `InvalidRefreshToken` (`"Auth.InvalidRefreshToken"`, `"Token de actualización inválido."`) — `backend/src/GOP.Domain/Errors/DomainErrors.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Domain` → exit code 0.

---

## Bloque 2 — Application (dotnet build GOP.Application)

**Propósito:** Implementar los casos de uso de autenticación: Login command, RefreshToken command, GetCurrentUser query, DTOs y la interfaz del servicio JWT. Sin dependencias de Infrastructure.

**⚠️ BLOQUEANTE:** Los DTOs e interfaz deben existir antes de los handlers.

- [ ] T003 [P] [HU-011] Crear DTO `UserProfileDto` — sealed record con: `Id` (Guid), `Email`, `Name`, `Role`, `TenantId`, `TenantName` (todos string) — `backend/src/GOP.Application/Features/Auth/Queries/GetCurrentUser/UserProfileDto.cs`
- [ ] T004 [P] [HU-009] Crear DTO `TokenResponseDto` — sealed record con: `AccessToken`, `RefreshToken` (string), `ExpiresIn` (int), `User` (UserProfileDto) — `backend/src/GOP.Application/Features/Auth/Dtos/TokenResponseDto.cs`
- [ ] T005 [P] [HU-009] Crear interfaz `IJwtTokenService` — métodos: `GenerateAccessToken(UserProfileDto user)` → string, `GenerateRefreshToken()` → string, `GetPrincipalFromExpiredToken(string token)` → ClaimsPrincipal? — `backend/src/GOP.Application/Common/Interfaces/IJwtTokenService.cs`
- [ ] T006 [HU-009] Crear record `LoginCommand` — `Email` (string), `Password` (string), implementa `IRequest<Result<TokenResponseDto>>` — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommand.cs`
- [ ] T007 [HU-009] Crear `LoginCommandValidator` — reglas: Email NotEmpty + EmailAddress format, Password NotEmpty; mensajes en español — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommandValidator.cs`
- [ ] T008 [BLOQUEANTE] [HU-009] Crear `LoginCommandHandler` — flujo: normalizar email a minúsculas → buscar en SeedUsers → verificar BCrypt hash → generar access+refresh tokens via IJwtTokenService → almacenar refresh en InMemoryRefreshTokenStore → retornar Result.Success(TokenResponseDto); en error → Result.Failure(DomainErrors.Auth.InvalidCredentials) — `backend/src/GOP.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs`
- [ ] T009 [HU-010] Crear record `RefreshTokenCommand` — `RefreshToken` (string), implementa `IRequest<Result<TokenResponseDto>>` — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommand.cs`
- [ ] T010 [HU-010] Crear `RefreshTokenCommandValidator` — regla: RefreshToken NotEmpty; mensaje en español — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandValidator.cs`
- [ ] T011 [BLOQUEANTE] [HU-010] Crear `RefreshTokenCommandHandler` — flujo: buscar token en InMemoryRefreshTokenStore → si no existe: InvalidRefreshToken → si expirado: TokenExpired → invalidar token actual (remove) → buscar usuario por userId → generar nuevo par tokens → almacenar nuevo refresh → retornar Result.Success(TokenResponseDto) — `backend/src/GOP.Application/Features/Auth/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
- [ ] T012 [P] [HU-011] Crear record `GetCurrentUserQuery` — record vacío, implementa `IRequest<Result<UserProfileDto>>` — `backend/src/GOP.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQuery.cs`
- [ ] T013 [HU-011] Crear `GetCurrentUserQueryHandler` — lee ICurrentUserService, mapea a UserProfileDto, retorna Result.Success(dto) — `backend/src/GOP.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Application` → exit code 0.

---

## Bloque 3 — Infrastructure (dotnet build GOP.Infrastructure)

**Propósito:** Implementar los servicios de JWT, seed de usuarios, store de refresh tokens en memoria y reemplazar el stub de `ICurrentUserService`.

- [ ] T014 [P] [HU-009] Crear `JwtSettings` — options class con `SectionName = "JwtSettings"`, propiedades: `Issuer`, `Audience`, `SigningKey`, `AccessTokenExpirationMinutes` (default 30), `RefreshTokenExpirationDays` (default 7) — `backend/src/GOP.Infrastructure/Identity/JwtSettings.cs`
- [ ] T015 [P] [HU-009] Crear `SeedUsers` — clase estática con lista de 4 usuarios (admin@gop.co, supervisor@gop.co, operador@gop.co, auditor@gop.co); passwords como hash BCrypt pre-generados; método `FindByEmail(string)` y `GetById(Guid)` — `backend/src/GOP.Infrastructure/Identity/SeedUsers.cs`
- [ ] T016 [P] [HU-010] Crear `InMemoryRefreshTokenStore` — Singleton con `ConcurrentDictionary<string, RefreshTokenEntry>`; entry: `UserId` (Guid), `Token` (string), `ExpiresAt` (DateTime), `IsUsed` (bool); métodos: `Store()`, `TryConsume()`, `Remove()` — `backend/src/GOP.Infrastructure/Identity/InMemoryRefreshTokenStore.cs`
- [ ] T017 [HU-009] Crear `JwtTokenService` — implementa `IJwtTokenService`; genera JWT HS256 con claims (sub, email, name, role, tenant_id, tenant_name, iat, exp); usa `JwtSettings` via IOptions; genera refresh token como Guid.NewGuid().ToString() — `backend/src/GOP.Infrastructure/Identity/JwtTokenService.cs`
- [ ] T018 [HU-011] Crear `CurrentUserService` — implementa `ICurrentUserService` leyendo claims de `HttpContext.User` via `IHttpContextAccessor`; mapea sub→UserId, email→Email, name→Name, role→Role, tenant_id→TenantId; IsAuthenticated lee HttpContext.User.Identity — `backend/src/GOP.Infrastructure/Identity/CurrentUserService.cs`
- [ ] T019 [BLOQUEANTE] Modificar `DependencyInjection.cs` — agregar: `Configure<JwtSettings>()`, registrar `IJwtTokenService` → `JwtTokenService` (Scoped), `ICurrentUserService` → `CurrentUserService` (Scoped, reemplaza `CurrentUserServiceStub`), `InMemoryRefreshTokenStore` (Singleton), `IHttpContextAccessor` (Singleton); agregar NuGet `BCrypt.Net-Next` — `backend/src/GOP.Infrastructure/DependencyInjection.cs`
- [ ] T020 Eliminar `CurrentUserServiceStub` — ya no es necesario, reemplazado por `CurrentUserService` real — `backend/src/GOP.Infrastructure/Services/CurrentUserServiceStub.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.Infrastructure` → exit code 0.

---

## Bloque 4 — API (dotnet build GOP.API)

**Propósito:** Crear el controller de autenticación, configurar JWT Bearer auth en el pipeline y extender el mapeo Result → HTTP para errores de auth (401).

- [ ] T021 [P] [HU-009] Modificar `appsettings.json` — agregar sección `JwtSettings` con: Issuer `"GOP360"`, Audience `"GOP360-Client"`, SigningKey `"CONFIGURE_IN_ENVIRONMENT_SPECIFIC_FILE"`, AccessTokenExpirationMinutes `30`, RefreshTokenExpirationDays `7` — `backend/src/GOP.API/appsettings.json`
- [ ] T022 [P] [HU-009] Modificar `appsettings.Development.json` — agregar sección `JwtSettings` con: SigningKey `"gop360-dev-signing-key-minimum-32-chars-long!"` (override del placeholder) — `backend/src/GOP.API/appsettings.Development.json`
- [ ] T023 [HU-009] Modificar `ResultExtensions.cs` — agregar pattern match: error codes que empiecen con `"Auth."` → retornar `401 Unauthorized` (UnauthorizedObjectResult con ProblemDetails); insertar antes del pattern `.Unauthorized` → 403 existente — `backend/src/GOP.API/Extensions/ResultExtensions.cs`
- [ ] T024 [HU-009] [HU-010] [HU-011] Crear `AuthController` — `[ApiController]`, `[Route("api/v1/auth")]`, `[Produces("application/json")]`; inyecta `ISender` via primary constructor; 3 actions: `[HttpPost("login")]` sin auth → Send(LoginCommand), `[HttpPost("refresh")]` sin auth → Send(RefreshTokenCommand), `[HttpGet("me")]` con `[Authorize]` → Send(GetCurrentUserQuery); cada action máximo 3 líneas; ProducesResponseType para Swagger — `backend/src/GOP.API/Controllers/AuthController.cs`
- [ ] T025 [BLOQUEANTE] [HU-009] Modificar `Program.cs` — agregar: leer `JwtSettings` de configuración, `AddAuthentication(JwtBearerDefaults)` + `AddJwtBearer()` con `TokenValidationParameters` (ValidateIssuer, ValidateAudience, ValidateIssuerSigningKey con SymmetricSecurityKey, ValidateLifetime, ClockSkew=Zero), `AddAuthorization()`; en pipeline agregar `UseAuthentication()` y `UseAuthorization()` después de `UseCors()` y antes de `MapControllers()` — `backend/src/GOP.API/Program.cs`

**Checkpoint:** `cd backend && dotnet build src/GOP.API` → exit code 0. Con Docker SQL Server corriendo: `dotnet run --project src/GOP.API` levanta y `POST /api/v1/auth/login` con credenciales válidas retorna 200 con JWT.

---

## Bloque 5 — Tests (dotnet test GOP.sln)

**Propósito:** Validar toda la lógica de autenticación con tests unitarios y de integración.

**Depende de:** Bloques 1-4 completos.

- [ ] T026 [P] [HU-009] Crear `DomainErrorsTests` — tests: `Auth_InvalidCredentials_HasCodeAndMessage`, `Auth_TokenExpired_HasCodeAndMessage`, `Auth_InvalidRefreshToken_HasCodeAndMessage` (verificar que Code y Message no están vacíos) — `backend/tests/GOP.Domain.Tests/Errors/DomainErrorsTests.cs`
- [ ] T027 [P] [HU-009] Crear `LoginCommandValidatorTests` — 4 tests: `Validate_EmptyEmail_ReturnsError`, `Validate_InvalidEmailFormat_ReturnsError`, `Validate_EmptyPassword_ReturnsError`, `Validate_ValidInput_PassesValidation` — `backend/tests/GOP.Application.Tests/Features/Auth/LoginCommandValidatorTests.cs`
- [ ] T028 [HU-009] Crear `LoginCommandHandlerTests` — 4 tests: `Handle_ValidCredentials_ReturnsTokenResponse` (mock IJwtTokenService, SeedUsers, InMemoryRefreshTokenStore), `Handle_InvalidPassword_ReturnsFailure`, `Handle_NonexistentEmail_ReturnsFailure` (mismo error, no revela info), `Handle_CaseInsensitiveEmail_ReturnsSuccess` (`ADMIN@GOP.CO`) — `backend/tests/GOP.Application.Tests/Features/Auth/LoginCommandHandlerTests.cs`
- [ ] T029 [P] [HU-010] Crear `RefreshTokenCommandHandlerTests` — 4 tests: `Handle_ValidRefreshToken_ReturnsNewTokenPair`, `Handle_ExpiredRefreshToken_ReturnsFailure`, `Handle_InvalidRefreshToken_ReturnsFailure`, `Handle_AlreadyUsedRefreshToken_ReturnsFailure` — `backend/tests/GOP.Application.Tests/Features/Auth/RefreshTokenCommandHandlerTests.cs`
- [ ] T030 [HU-009] [HU-010] [HU-011] Crear `AuthControllerTests` — 6 tests de integración con `WebApplicationFactory<Program>`: `Login_ValidCredentials_Returns200WithTokens`, `Login_InvalidCredentials_Returns401`, `Login_EmptyBody_Returns422`, `GetMe_WithValidToken_Returns200WithProfile` (login primero, usar accessToken para /me), `GetMe_WithoutToken_Returns401`, `Refresh_ValidToken_Returns200WithNewTokens` (login, luego refresh) — `backend/tests/GOP.API.Tests/Controllers/AuthControllerTests.cs`

**Checkpoint:** `cd backend && dotnet test GOP.sln` → todos los tests pasan (11 existentes + 21 nuevos = 32 tests), exit code 0.

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

### Dependencias Dentro de Bloques

```
Bloque 1:
  T001, T002 paralelos (sin deps entre sí)

Bloque 2:
  T003, T004, T005 paralelos (DTOs e interfaz son independientes)
  T006 depende de T004 (LoginCommand retorna TokenResponseDto)
  T007 depende de T006 (validator valida LoginCommand)
  T008 depende de T003, T004, T005, T006 (handler usa todos)
  T009 depende de T004 (RefreshTokenCommand retorna TokenResponseDto)
  T010 depende de T009 (validator valida RefreshTokenCommand)
  T011 depende de T003, T004, T005, T009 (handler usa todos)
  T012 depende de T003 (query retorna UserProfileDto)
  T013 depende de T003, T012 (handler usa query + dto)

Bloque 3:
  T014, T015, T016 paralelos (settings, seed, store son independientes)
  T017 depende de T014 (JwtTokenService usa JwtSettings)
  T018 independiente (lee HttpContext, no depende de otros del bloque)
  T019 depende de T014, T015, T016, T017, T018 (DI registra todo)
  T020 depende de T018, T019 (eliminar stub solo cuando el reemplazo está registrado)

Bloque 4:
  T021, T022 paralelos (archivos de configuración)
  T023 independiente (modifica ResultExtensions)
  T024 depende de T023 (controller usa ResultExtensions para Auth errors)
  T025 depende de T021, T022, T024 (Program.cs configura JWT y mapea controllers)

Bloque 5:
  T026, T027 paralelos (tests de domain y validator son independientes)
  T028 depende de T027 (handler tests pueden ejecutarse después de validator tests, aunque no se bloquean técnicamente)
  T029 paralelo con T028 (tests de handlers diferentes)
  T030 depende de T028, T029 (integration tests requieren que toda la lógica esté testeada primero)
```

### Oportunidades de Paralelismo

**Bloque 1:** T001 y T002 en paralelo.

**Bloque 2:** T003, T004, T005 en paralelo → T006 y T009 y T012 en paralelo → T007 y T010 en paralelo → T008 y T011 y T013 en paralelo.

**Bloque 3:** T014, T015, T016 en paralelo → T017 y T018 en paralelo → T019 → T020.

**Bloque 4:** T021, T022, T023 en paralelo → T024 → T025.

**Bloque 5:** T026, T027, T029 en paralelo → T028 → T030.

---

## Implementation Blocks (Secuencia de Ejecución)

### Bloque 1 — Domain

```
T001, T002            # paralelas: UserRole + DomainErrors
```
**Validación:** `cd backend && dotnet build src/GOP.Domain` ✅

### Bloque 2 — Application

```
T003, T004, T005      # paralelas: UserProfileDto, TokenResponseDto, IJwtTokenService
T006, T009, T012      # paralelas: LoginCommand, RefreshTokenCommand, GetCurrentUserQuery
T007, T010            # paralelas: LoginCommandValidator, RefreshTokenCommandValidator
T008, T011, T013      # paralelas: LoginCommandHandler, RefreshTokenCommandHandler, GetCurrentUserQueryHandler
```
**Validación:** `cd backend && dotnet build src/GOP.Application` ✅

### Bloque 3 — Infrastructure

```
T014, T015, T016      # paralelas: JwtSettings, SeedUsers, InMemoryRefreshTokenStore
T017, T018            # paralelas: JwtTokenService, CurrentUserService
T019                  # DependencyInjection.cs (registra todo, reemplaza stub)
T020                  # Eliminar CurrentUserServiceStub.cs
```
**Validación:** `cd backend && dotnet build src/GOP.Infrastructure` ✅

### Bloque 4 — API

```
T021, T022, T023      # paralelas: appsettings.json, appsettings.Development.json, ResultExtensions
T024                  # AuthController
T025                  # Program.cs (JWT pipeline)
```
**Validación:** `cd backend && dotnet build src/GOP.API` ✅

### Bloque 5 — Tests

```
T026, T027, T029      # paralelas: DomainErrorsTests, LoginValidatorTests, RefreshTokenHandlerTests
T028                  # LoginCommandHandlerTests
T030                  # AuthControllerTests (integración)
```
**Validación:** `cd backend && dotnet test GOP.sln` ✅ — 32 tests pass.

---

## Resumen

| Bloque | Propósito | Tareas | Archivos nuevos | Archivos modificados | Verificación |
|---|---|---|---|---|---|
| B1 — Domain | Enum + errores | T001–T002 (2) | 2 | 0 | `dotnet build src/GOP.Domain` |
| B2 — Application | Commands + Query + DTOs | T003–T013 (11) | 11 | 0 | `dotnet build src/GOP.Application` |
| B3 — Infrastructure | JWT + seed + store | T014–T020 (7) | 5 | 1 + 1 eliminado | `dotnet build src/GOP.Infrastructure` |
| B4 — API | Controller + JWT pipeline | T021–T025 (5) | 1 | 4 | `dotnet build src/GOP.API` |
| B5 — Tests | Unit + integration | T026–T030 (5) | 5 | 0 | `dotnet test GOP.sln` |
| **Total** | | **30 tareas** | **24 nuevos** | **5 modif + 1 eliminado** | |
