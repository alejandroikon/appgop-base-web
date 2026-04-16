# Spec: API de Autenticación JWT

**Feature ID:** 004-auth-api
**Dominio:** Autenticación transversal (`/api/v1/auth/*`)
**Dependencias:** 003-backend-scaffold (aprobado e implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Contexto y Alcance

Esta feature implementa la **autenticación real** del sistema GOP 360° mediante JWT. Reemplaza los mocks del frontend (feature 001-auth) con endpoints de backend que generan tokens reales. Los usuarios se almacenan como **seed en memoria** (sin tabla en base de datos), ya que la gestión completa de usuarios es una feature posterior.

**Endpoints incluidos:**
- `POST /api/v1/auth/login` — Autenticación con email + password → access token + refresh token
- `POST /api/v1/auth/refresh` — Renovar access token con refresh token vigente
- `GET /api/v1/auth/me` — Obtener perfil del usuario autenticado (leído del JWT)

**NO incluido (features posteriores):**
- `POST /api/v1/auth/logout` (invalidación server-side de refresh token)
- Gestión de usuarios en base de datos (CRUD, tabla Users)
- Recuperación de contraseña con email real
- OAuth / SSO / proveedores externos
- Rate limiting en login (especificado, no implementado en MVP)

---

## 2. Historias de Usuario

### HU-009: Login vía API con JWT

**Como** usuario registrado del sistema GOP 360°,
**quiero** autenticarme con mi correo y contraseña contra el backend real,
**para** recibir un token JWT que me permita acceder a los endpoints protegidos.

#### Criterios de Aceptación

**Escenario 1 — Login exitoso**
- **Dado** que el endpoint `POST /api/v1/auth/login` está disponible
- **Cuando** se envía `{ "email": "admin@gop.co", "password": "Admin123*" }`
- **Entonces** el servidor responde con `200 OK`
- **Y** el body contiene `accessToken` (JWT firmado), `refreshToken`, `expiresIn` (1800 segundos) y `user` (perfil completo)
- **Y** el `accessToken` contiene los claims: `sub`, `email`, `name`, `role`, `tenant_id`, `tenant_name`

**Escenario 2 — Credenciales incorrectas**
- **Dado** que el endpoint está disponible
- **Cuando** se envía un email o password que no corresponden a ningún usuario seed
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"Correo o contraseña incorrectos."*
- **Y** no se revela si el email existe o no (misma respuesta para email inexistente y password incorrecto)

**Escenario 3 — Campos vacíos o inválidos**
- **Dado** que el endpoint está disponible
- **Cuando** se envía un body con email vacío, password vacío o email con formato inválido
- **Entonces** el servidor responde con `422 Unprocessable Entity`
- **Y** el body es ProblemDetails con `errors` por campo

**Escenario 4 — Email case-insensitive**
- **Dado** que existe el usuario `admin@gop.co`
- **Cuando** se envía `{ "email": "ADMIN@GOP.CO", "password": "Admin123*" }`
- **Entonces** el login es exitoso (mismo comportamiento que Escenario 1)

---

### HU-010: Renovación de Token (Refresh)

**Como** usuario con una sesión activa,
**quiero** renovar mi access token antes de que expire usando mi refresh token,
**para** mantener la sesión sin necesidad de re-autenticarme.

#### Criterios de Aceptación

**Escenario 1 — Refresh exitoso**
- **Dado** que el usuario tiene un refresh token vigente
- **Cuando** se envía `POST /api/v1/auth/refresh` con `{ "refreshToken": "<token-vigente>" }`
- **Entonces** el servidor responde con `200 OK`
- **Y** el body contiene un nuevo `accessToken`, un nuevo `refreshToken` y `expiresIn`
- **Y** el refresh token anterior queda invalidado (single-use)

**Escenario 2 — Refresh token expirado**
- **Dado** que el refresh token ha superado los 7 días de vigencia
- **Cuando** se envía el refresh token expirado
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"El token de actualización ha expirado. Inicie sesión nuevamente."*

**Escenario 3 — Refresh token inválido o ya utilizado**
- **Dado** que el refresh token no existe o ya fue usado
- **Cuando** se envía el refresh token
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el body es ProblemDetails con `detail`: *"Token de actualización inválido."*

---

### HU-011: Consultar Perfil del Usuario Autenticado

**Como** usuario autenticado,
**quiero** consultar mi perfil desde el JWT,
**para** que el frontend pueda renderizar mi nombre, rol y tenant.

#### Criterios de Aceptación

**Escenario 1 — Perfil exitoso**
- **Dado** que el usuario tiene un access token válido
- **Cuando** se envía `GET /api/v1/auth/me` con header `Authorization: Bearer <access-token>`
- **Entonces** el servidor responde con `200 OK`
- **Y** el body contiene `id`, `email`, `name`, `role`, `tenantId`, `tenantName`

**Escenario 2 — Token ausente**
- **Dado** que no se envía header `Authorization`
- **Cuando** se envía `GET /api/v1/auth/me`
- **Entonces** el servidor responde con `401 Unauthorized`

**Escenario 3 — Token expirado**
- **Dado** que el access token ha superado los 30 minutos de vigencia
- **Cuando** se envía `GET /api/v1/auth/me` con el token expirado
- **Entonces** el servidor responde con `401 Unauthorized`
- **Y** el frontend intercepta y ejecuta flujo de refresh o session expired

---

## 3. Reglas de Negocio

| Regla | Descripción |
|---|---|
| RN-020 | El email es case-insensitive: `Admin@gop.co` y `admin@gop.co` son equivalentes. Se normaliza a minúsculas antes de comparar. |
| RN-021 | La contraseña es case-sensitive. |
| RN-022 | El access token expira en 30 minutos (configurable en `JwtSettings`). |
| RN-023 | El refresh token expira en 7 días (configurable en `JwtSettings`). |
| RN-024 | Cada refresh token es de un solo uso. Al usarlo se genera uno nuevo y el anterior se invalida. |
| RN-025 | La respuesta de login erróneo no revela si el email existe. Siempre *"Correo o contraseña incorrectos."* |
| RN-026 | Los endpoints `/auth/login` y `/auth/refresh` son públicos (sin JWT). `/auth/me` requiere JWT válido. |
| RN-027 | El JWT se firma con HMAC-SHA256 (`HS256`). El signing key se lee de configuración, nunca hardcodeado. |
| RN-028 | Los refresh tokens se almacenan en memoria (singleton `ConcurrentDictionary`). Se pierden al reiniciar la API. Aceptable para MVP. |

---

## 4. Usuarios Seed (Datos en Memoria)

Los mismos 4 perfiles del mock frontend (001-auth), adaptados al formato del backend:

| Email | Password | Nombre | Rol | TenantId | Tenant Name |
|---|---|---|---|---|---|
| `admin@gop.co` | `Admin123*` | Administrador ANH | `ADMIN` | `1` | Agencia Nacional de Hidrocarburos |
| `supervisor@gop.co` | `Super123*` | Supervisor Ecopetrol | `SUPERVISOR` | `2` | Ecopetrol S.A. |
| `operador@gop.co` | `Oper123*` | Operador Ecopetrol | `OPERADOR` | `2` | Ecopetrol S.A. |
| `auditor@gop.co` | `Audit123*` | Auditor ANH | `AUDITOR` | `1` | Agencia Nacional de Hidrocarburos |

> **Nota de migración:** Los emails cambian de `@gop360.com` (mock frontend) a `@gop.co` (backend). El `plan.fe.md` documenta la actualización correspondiente en el mock frontend.

Los passwords se almacenan como hash BCrypt en el seed. No se almacenan en texto plano en el código.

---

## 5. JWT Claims

Alineados con CONSTITUTION.backend.md §8.2:

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "admin@gop.co",
  "name": "Administrador ANH",
  "role": "ADMIN",
  "tenant_id": "1",
  "tenant_name": "Agencia Nacional de Hidrocarburos",
  "iat": 1700000000,
  "exp": 1700001800
}
```

| Claim | Tipo | Descripción |
|---|---|---|
| `sub` | string (UUID) | ID único del usuario |
| `email` | string | Correo del usuario |
| `name` | string | Nombre completo |
| `role` | string | Rol: `ADMIN`, `SUPERVISOR`, `OPERADOR`, `AUDITOR` |
| `tenant_id` | string | ID del tenant/operadora |
| `tenant_name` | string | Nombre del tenant (para display en UI) |
| `iat` | number | Timestamp de emisión |
| `exp` | number | Timestamp de expiración (iat + 1800s) |

---

## 6. Restricciones Técnicas

- Los usuarios NO viven en base de datos en esta feature. Se almacenan como seed estático en la capa Infrastructure.
- Los refresh tokens se almacenan en un `ConcurrentDictionary<string, RefreshTokenEntry>` registrado como Singleton.
- `ICurrentUserService` (actualmente stub) se reemplaza por una implementación real que lee del `HttpContext.User`.
- Se agrega `AddAuthentication().AddJwtBearer()` y `AddAuthorization()` al pipeline en `Program.cs`.
- El health check existente (`GET /api/v1/health`) sigue siendo público.
- Esta feature no agrega `[Authorize]` a otros controllers — solo al `AuthController` en `/auth/me`.

---

## 7. Edge Cases

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-030 | Body del login vacío (`{}`) | 422 con errores de validación por campo |
| EC-031 | Content-Type no es `application/json` | 415 Unsupported Media Type (manejo nativo de ASP.NET Core) |
| EC-032 | Access token bien firmado pero con claims manipulados | El token se valida por firma, no por contenido. Si la firma es válida, los claims se confían. |
| EC-033 | Dos refreshes simultáneos con el mismo refresh token | Solo el primero tiene éxito. El segundo recibe 401 (token ya consumido). |
| EC-034 | Access token con issuer/audience incorrectos | 401 — la validación JWT rechaza tokens con issuer/audience no matching. |
| EC-035 | Request con `Authorization: Bearer null` o `Bearer undefined` | 401 — el middleware JWT rechaza tokens malformados. |
| EC-036 | Reinicio de la API con refresh tokens activos | Todos los refresh tokens se pierden (in-memory). Los usuarios deben re-autenticarse. Aceptable para MVP. |

---

## 8. Impacto en el Frontend (Feature 001-auth)

Esta feature requiere actualizar el frontend para consumir la API real en lugar del mock. Los cambios se documentan en `plan.fe.md`:

| Componente Frontend | Cambio Requerido |
|---|---|
| `auth.service.ts` | Reemplazar `mockLogin()` por `POST /api/v1/auth/login`. Agregar `refresh()` y `me()`. |
| `auth.mock.ts` | Actualizar emails a `@gop.co`. Mantener como fallback si `useMocks: true`. |
| `auth-user.dto.ts` | Actualizar propiedades para reflejar el contrato camelCase del backend. |
| `auth-user.mapper.ts` | Simplificar: el contrato ya retorna camelCase alineado con el modelo. |
| `auth.effects.ts` | Agregar lógica de token refresh. Almacenar `accessToken` y `refreshToken`. |
| `auth.interceptor.ts` | Sin cambios (ya inyecta Bearer token). |
| `error.interceptor.ts` | Agregar lógica: en 401, intentar refresh antes de despachar `sessionExpired()`. |
| `api-endpoints.ts` | Agregar ruta `/auth/me`. La ruta `/auth/refresh` ya existe. |
| `environment.ts` | Actualizar `gopApi` si el backend corre en puerto distinto a 3000. |

---

## 9. Checklist de Revisión

- [ ] Las historias cubren los 3 endpoints con escenarios happy path, error de validación y error de autenticación
- [ ] Los usuarios seed son los mismos 4 roles del sistema (ADMIN, SUPERVISOR, OPERADOR, AUDITOR)
- [ ] Los claims JWT están alineados con CONSTITUTION.backend.md §8.2
- [ ] Las reglas de negocio cubren expiración, case-sensitivity, single-use refresh
- [ ] Los edge cases cubren concurrencia de refresh y reinicio del servidor
- [ ] El impacto en frontend está identificado sin definir la implementación (eso va en plan.fe.md)
- [ ] No se incluyen entidades de base de datos — los usuarios son seed en memoria
