# Spec: Backend Scaffolding — Estructura Base .NET 10

**Feature ID:** 003-backend-scaffold
**Dominio:** Infraestructura transversal — no pertenece a ningún dominio de negocio
**Estado:** Pendiente de revisión humana

---

## 1. Contexto y Alcance

Esta feature establece la **estructura inicial del backend** de GOP 360°. No implementa lógica de negocio — solo la carcasa compilable y funcional sobre la cual se construirán las features de negocio a partir de la iteración siguiente.

El resultado es un backend .NET 10 con Clean Architecture que:
- Compila sin errores (`dotnet build`)
- Pasa tests triviales (`dotnet test`)
- Levanta en local y responde en `/health`
- Se conecta a SQL Server vía Docker Compose
- Expone Swagger UI para exploración de contratos
- Implementa el pipeline cross-cutting completo (logging, validación, errores)

**Incluido en este spec:**
- Solución `GOP.sln` con 4 proyectos de producción + 4 de test
- `Program.cs` con composition root (DI, Serilog, CORS, Swagger, ProblemDetails)
- `GopDbContext` vacío con connection string por ambiente
- Pipeline MediatR: `LoggingBehavior`, `ValidationBehavior`
- Middleware de excepciones globales → ProblemDetails RFC 7807
- Health check endpoint (`GET /api/v1/health`)
- Docker Compose con SQL Server 2022 para desarrollo local
- Swagger/OpenAPI con NSwag

**NO incluido (features posteriores):**
- Entidades de dominio (Well, User, Operator, Form100, etc.)
- Commands, Queries o handlers de negocio
- JWT/Autenticación (se implementará como feature 004 o posterior)
- Migraciones de base de datos con tablas reales
- Endpoints de negocio

---

## 2. Historias Técnicas

> Al ser una feature de scaffolding sin funcionalidad de usuario final, se definen **historias técnicas (HT)** en lugar de historias de usuario. Los criterios de aceptación siguen el formato Given/When/Then adaptado a verificación técnica.

### HT-001: Compilación de la Solución Completa

**Como** desarrollador backend del equipo GOP 360°,
**quiero** una solución .NET 10 con 4 proyectos en Clean Architecture que compile sin errores,
**para** tener una base verificable sobre la cual construir features de negocio.

#### Criterios de Aceptación

**Escenario 1 — Build exitoso de la solución completa**
- **Dado** que el directorio `backend/` contiene `GOP.sln` con los 4 proyectos de producción y 4 de test
- **Cuando** se ejecuta `dotnet build GOP.sln` desde `backend/`
- **Entonces** la compilación termina con exit code 0 sin warnings tratados como errores
- **Y** los 8 proyectos reportan "Build succeeded"

**Escenario 2 — Dependencias correctas entre proyectos**
- **Dado** que los proyectos están referenciados según CONSTITUTION.backend.md §3
- **Cuando** se inspecciona `GOP.Domain.csproj`
- **Entonces** no tiene `<ProjectReference>` a ningún otro proyecto
- **Y** `GOP.Application.csproj` solo referencia `GOP.Domain`
- **Y** `GOP.Infrastructure.csproj` solo referencia `GOP.Application`
- **Y** `GOP.API.csproj` referencia `GOP.Application` y `GOP.Infrastructure`

**Escenario 3 — Tests pasan en la solución vacía**
- **Dado** que cada proyecto de test tiene al menos un test trivial (canary test)
- **Cuando** se ejecuta `dotnet test GOP.sln`
- **Entonces** todos los tests pasan con exit code 0

---

### HT-002: API Levanta y Responde Health Check

**Como** desarrollador backend,
**quiero** que la API levante en local y responda con su estado de salud,
**para** verificar que el pipeline HTTP funciona de punta a punta.

#### Criterios de Aceptación

**Escenario 1 — Health check exitoso**
- **Dado** que la API está corriendo en `http://localhost:5000`
- **Cuando** se envía `GET /api/v1/health`
- **Entonces** el servidor responde con `200 OK`
- **Y** el body contiene `{ "status": "Healthy", "checks": [...] }`
- **Y** el content-type es `application/json`

**Escenario 2 — Health check con SQL Server caído**
- **Dado** que la API está corriendo pero SQL Server no está disponible
- **Cuando** se envía `GET /api/v1/health`
- **Entonces** el servidor responde con `503 Service Unavailable`
- **Y** el body contiene `{ "status": "Unhealthy", "checks": [{ "name": "sqlserver", "status": "Unhealthy" }] }`

**Escenario 3 — Endpoint no requiere autenticación**
- **Dado** que la API está corriendo
- **Cuando** se envía `GET /api/v1/health` sin header `Authorization`
- **Entonces** el servidor responde con `200 OK` (no con `401`)
- **Y** el endpoint es público por diseño

---

### HT-003: Middleware de Errores Globales (ProblemDetails)

**Como** desarrollador backend,
**quiero** que toda excepción no controlada retorne ProblemDetails RFC 7807,
**para** que el frontend siempre reciba errores en formato predecible.

#### Criterios de Aceptación

**Escenario 1 — Excepción no controlada en producción**
- **Dado** que el ambiente es Production
- **Cuando** un endpoint lanza una excepción no controlada
- **Entonces** el middleware retorna `500` con content-type `application/problem+json`
- **Y** el body contiene `{ "status": 500, "title": "Internal Server Error", "detail": "..." }`
- **Y** el stack trace NO se incluye en la respuesta
- **Y** la excepción completa se registra en Serilog

**Escenario 2 — Excepción no controlada en desarrollo**
- **Dado** que el ambiente es Development
- **Cuando** un endpoint lanza una excepción no controlada
- **Entonces** el middleware retorna `500` con content-type `application/problem+json`
- **Y** el body incluye el stack trace para facilitar debugging

**Escenario 3 — Ruta inexistente**
- **Dado** que la API está corriendo
- **Cuando** se envía una request a una ruta que no existe (ej. `GET /api/v1/nonexistent`)
- **Entonces** el servidor responde con `404` y ProblemDetails

---

### HT-004: Pipeline MediatR Funcional

**Como** desarrollador backend,
**quiero** que el pipeline de MediatR esté configurado con behaviors de logging y validación,
**para** que cada command/query futuro pase automáticamente por el pipeline cross-cutting.

#### Criterios de Aceptación

**Escenario 1 — MediatR registrado correctamente**
- **Dado** que `AddApplication()` se invoca en `Program.cs`
- **Cuando** se resuelve `ISender` desde DI
- **Entonces** la instancia no es null y MediatR puede enviar requests

**Escenario 2 — LoggingBehavior registrado**
- **Dado** que un request pasa por el pipeline
- **Cuando** el handler lo procesa
- **Entonces** Serilog registra un log `Information` al inicio con nombre del request, UserId y TenantId
- **Y** registra un log `Information` al final con duración en milisegundos

**Escenario 3 — ValidationBehavior cortocircuita si hay errores**
- **Dado** que un command tiene un validator registrado con reglas que fallan
- **Cuando** se envía el command por MediatR
- **Entonces** el handler NO se ejecuta
- **Y** el pipeline retorna un `Result.Failure` con los errores de validación

---

### HT-005: Serilog Structured Logging

**Como** desarrollador backend,
**quiero** logging estructurado con Serilog desde el primer momento,
**para** tener trazabilidad desde la primera request.

#### Criterios de Aceptación

**Escenario 1 — Startup logging**
- **Dado** que la API arranca
- **Cuando** el proceso inicia exitosamente
- **Entonces** Serilog escribe al menos un log `Information` indicando que el host inició
- **Y** los logs de Microsoft.AspNetCore están filtrados a nivel `Warning`

**Escenario 2 — Request logging**
- **Dado** que la API está corriendo
- **Cuando** se recibe una request HTTP a cualquier endpoint
- **Entonces** Serilog registra la request con método HTTP, path, status code y duración

**Escenario 3 — Logs en consola y archivo**
- **Dado** que `appsettings.json` configura sinks de Console y File
- **Cuando** se genera un log
- **Entonces** aparece en la consola y en `logs/gop-{fecha}.log`

---

### HT-006: Swagger UI Disponible en Desarrollo

**Como** desarrollador frontend o backend,
**quiero** que Swagger UI esté disponible en el ambiente de desarrollo,
**para** explorar y probar los endpoints disponibles.

#### Criterios de Aceptación

**Escenario 1 — Swagger UI accesible en desarrollo**
- **Dado** que la API está corriendo en el ambiente Development
- **Cuando** se navega a `/swagger`
- **Entonces** se muestra la interfaz de Swagger UI con la documentación OpenAPI
- **Y** el health check endpoint aparece listado

**Escenario 2 — Swagger deshabilitado en producción**
- **Dado** que la API está corriendo en el ambiente Production
- **Cuando** se navega a `/swagger`
- **Entonces** la ruta no existe (404)

---

### HT-007: Docker Compose con SQL Server

**Como** desarrollador backend,
**quiero** levantar SQL Server con un solo comando,
**para** tener un ambiente de desarrollo local sin dependencias externas.

#### Criterios de Aceptación

**Escenario 1 — SQL Server levanta con Docker Compose**
- **Dado** que Docker está instalado en la máquina del desarrollador
- **Cuando** se ejecuta `docker compose up -d` desde `backend/`
- **Entonces** un contenedor de SQL Server 2022 inicia en el puerto 1433
- **Y** la base de datos `GOP360` es accesible con las credenciales del compose

**Escenario 2 — Connection string de desarrollo apunta al contenedor**
- **Dado** que el contenedor SQL Server está corriendo
- **Cuando** la API levanta con el perfil Development
- **Entonces** `GopDbContext` se conecta exitosamente al SQL Server del contenedor
- **Y** el health check de SQL Server reporta `Healthy`

**Escenario 3 — Datos persisten entre reinicios del contenedor**
- **Dado** que el Docker Compose define un volume para los datos de SQL Server
- **Cuando** se ejecuta `docker compose down` y luego `docker compose up -d`
- **Entonces** los datos previamente almacenados siguen disponibles

---

### HT-008: Clases Base del Dominio

**Como** desarrollador backend,
**quiero** las clases abstractas base (`Entity`, `AuditableEntity`, `Result<T>`, `Error`) disponibles en el Domain,
**para** que las features de negocio futuras hereden de ellas sin reescribir patrones.

#### Criterios de Aceptación

**Escenario 1 — Result\<T\> funciona según CONSTITUTION.backend.md §6**
- **Dado** que `Result<T>` y `Error` existen en `Domain/Common/`
- **Cuando** se crea un `Result.Success(value)` y un `Result.Failure(error)`
- **Entonces** `IsSuccess` y `IsFailure` retornan valores correctos
- **Y** acceder a `Value` en un failure lanza `InvalidOperationException`

**Escenario 2 — Entity genera GUID automáticamente**
- **Dado** que `Entity` es la clase base abstracta
- **Cuando** se instancia una entidad concreta
- **Entonces** `Id` es un `Guid` no vacío generado automáticamente

**Escenario 3 — AuditableEntity incluye campos de auditoría**
- **Dado** que `AuditableEntity` hereda de `Entity`
- **Cuando** se inspecciona la clase
- **Entonces** contiene `CreatedAt`, `CreatedBy`, `LastModifiedAt`, `LastModifiedBy`, `IsDeleted`, `DeletedAt`

---

## 3. Reglas Técnicas

| Regla | Descripción |
|---|---|
| RT-001 | El Domain no tiene paquetes NuGet. Solo .NET BCL. Ver CONSTITUTION.backend.md §3. |
| RT-002 | `GopDbContext` se implementa vacío — sin `DbSet<>` de entidades. Los DbSets se agregan en features de negocio. |
| RT-003 | No se generan migraciones en esta feature. La primera migración se genera cuando la primera entidad se agregue. |
| RT-004 | `ICurrentUserService` se declara en Application pero se implementa como stub en Infrastructure (retorna valores dummy). La implementación real viene con la feature de JWT. |
| RT-005 | No hay `[Authorize]` en ningún endpoint de esta feature. La autenticación JWT se agrega en una feature posterior. |
| RT-006 | Los behaviors de MediatR se registran pero no hay handlers de negocio que los ejecuten. Se validan con tests unitarios aislados. |
| RT-007 | El `appsettings.Development.json` contiene el connection string al SQL Server del Docker Compose. `appsettings.json` tiene un placeholder. |
| RT-008 | Target framework: `net10.0` para todos los proyectos. |
| RT-009 | Nullable reference types habilitado (`<Nullable>enable</Nullable>`) en todos los proyectos. |
| RT-010 | Implicit usings habilitado (`<ImplicitUsings>enable</ImplicitUsings>`) en todos los proyectos. |

---

## 4. Paquetes NuGet por Proyecto

| Proyecto | Paquete | Versión | Propósito |
|---|---|---|---|
| **GOP.Domain** | *(ninguno)* | — | Solo .NET BCL |
| **GOP.Application** | `MediatR` | 12.x | CQRS dispatch |
| **GOP.Application** | `FluentValidation` | 11.x | Validación de commands |
| **GOP.Application** | `FluentValidation.DependencyInjectionExtensions` | 11.x | Auto-registro de validators |
| **GOP.Application** | `AutoMapper` | 13.x | Entity → DTO mapping |
| **GOP.Application** | `AutoMapper.Extensions.Microsoft.DependencyInjection` | 12.x | Auto-registro de profiles |
| **GOP.Application** | `Microsoft.Extensions.Logging.Abstractions` | 10.x | `ILogger<T>` para behaviors |
| **GOP.Infrastructure** | `Microsoft.EntityFrameworkCore` | 10.x | ORM |
| **GOP.Infrastructure** | `Microsoft.EntityFrameworkCore.SqlServer` | 10.x | Provider SQL Server |
| **GOP.Infrastructure** | `Microsoft.EntityFrameworkCore.Tools` | 10.x | CLI de migraciones |
| **GOP.Infrastructure** | `Serilog.AspNetCore` | 9.x | Logging integrado |
| **GOP.Infrastructure** | `Serilog.Sinks.File` | 6.x | Sink de archivo |
| **GOP.API** | `NSwag.AspNetCore` | 14.x | Swagger UI + OpenAPI generation |
| **GOP.API** | `AspNetCore.HealthChecks.SqlServer` | 9.x | Health check de SQL Server |
| **GOP.API** | `Serilog.AspNetCore` | 9.x | Request logging middleware |
| **Tests** | `xunit` | 2.x | Framework de test |
| **Tests** | `FluentAssertions` | 7.x | Assertions |
| **Tests** | `NSubstitute` | 5.x | Mocking |
| **Tests** | `Microsoft.NET.Test.Sdk` | 17.x | Test runner |
| **Tests** | `xunit.runner.visualstudio` | 2.x | VS Test adapter |

> Las versiones exactas se determinan al ejecutar `dotnet add package`. Las indicadas son las mayores estables compatibles con .NET 10.

---

## 5. Edge Cases Identificados

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-020 | SQL Server no disponible al arrancar la API | La API arranca igualmente. El health check reporta `Unhealthy`. Los endpoints que dependan de la DB fallarán con ProblemDetails 500. |
| EC-021 | Connection string inválido o ausente | La API falla al arrancar con error explicativo en Serilog. No arranca parcialmente. |
| EC-022 | Puerto 5000 ya ocupado | La API reporta `AddressInUseException` en Serilog. El desarrollador debe liberar el puerto o configurar otro en `launchSettings.json`. |
| EC-023 | Docker no instalado | El `docker-compose.yml` no puede ejecutarse. Se documenta como prerequisito en el README. |
| EC-024 | `dotnet ef` no instalado como tool | Se documenta el comando `dotnet tool install` en el README. No bloquea esta feature (no hay migraciones). |

---

## 6. Checklist de Revisión

Antes de aprobar este spec y generar el contrato + plan:

- [ ] Las historias técnicas cubren todos los componentes de infraestructura listados en el alcance
- [ ] Las reglas técnicas son consistentes con CONSTITUTION.backend.md
- [ ] Los paquetes NuGet listados corresponden a las dependencias reales de cada capa
- [ ] Los edge cases cubren los escenarios de ambiente de desarrollo local
- [ ] No se incluye ninguna entidad de dominio, comando ni query de negocio
- [ ] El health check endpoint es el único endpoint real (sin autenticación)
- [ ] La feature es completamente autónoma — no depende de ninguna otra feature backend
