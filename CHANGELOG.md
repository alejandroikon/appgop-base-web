# CHANGELOG — GOP 360°

**Proyecto:** Gestión de Operaciones Petroleras 360°  
**Cliente:** Agencia Nacional de Hidrocarburos (ANH) — Colombia  
**Período documentado:** 2026-02-15 a 2026-04-17

---

## Iteración 0–1 — Backend Scaffold (Completed)

**Fecha:** 2026-02-15 a 2026-02-28  
**Feature:** 003-backend-scaffold  
**Duración agent-time:** ~8 horas  
**Status:** ✅ Producción

### Qué se entregó

**Backend:**
- Solución `.NET 10` con 4 proyectos + 4 de tests en Clean Architecture
- Domain sin dependencias externas (solo .NET BCL)
- Application con MediatR 12.x, FluentValidation 11.x, AutoMapper 13.x
- Infrastructure con EF Core 10.x + SQL Server provider
- API con composition root DI, CORS, Swagger, ProblemDetails middleware
- Behaviors de pipeline: LoggingBehavior, ValidationBehavior
- Health check endpoint (`GET /api/v1/health`) público
- Serilog structured logging (console + file sinks)
- Docker Compose con SQL Server 2022 para desarrollo

**Tests:**
- 11 tests unitarios (canary tests en cada proyecto)

**Documentación:**
- CONSTITUTION.backend.md — Arquitectura Clean Architecture
- CONSTITUTION.contracts.md — Estándares OpenAPI 3.1
- spec.md con 8 historias técnicas (HT-001 a HT-008)

### Cómo probarlo

```bash
# Backend
cd backend
docker compose up -d                          # SQL Server en puerto 1433
dotnet build GOP.sln                          # Compilación exitosa ✅
dotnet test GOP.sln                           # 11 tests ✅
dotnet run --project src/GOP.API              # API en http://localhost:5000
# Navegar a http://localhost:5000/swagger     # Swagger UI visible

# Health check
curl http://localhost:5000/api/v1/health      # { "status": "Healthy", ... }
```

### Rompe compatibilidad

- ❌ N/A (iteración cero)

### Dependencias nuevas

- Backend: MediatR 12, FluentValidation 11, AutoMapper 13, EF Core 10, Serilog 9, NSwag 14

---

## Iteración 2 — Auth API + Login UI (Completed)

**Fecha:** 2026-03-01 a 2026-03-15  
**Feature:** 004-auth-api  
**Dependencias:** 003 (completada)  
**Duración agent-time:** ~10 horas  
**Status:** ✅ Producción

### Qué se entregó

**Backend:**
- 3 endpoints de autenticación:
  - `POST /api/v1/auth/login` — Autenticación email + password → JWT Bearer
  - `POST /api/v1/auth/refresh` — Renovar access token (single-use refresh tokens)
  - `GET /api/v1/auth/me` — Perfil del usuario autenticado
- JWT con claims: `sub`, `email`, `name`, `role`, `tenant_id`, `tenant_name`
- 4 usuarios seed en memoria (ADMIN, SUPERVISOR, OPERADOR, AUDITOR)
- CurrentUserService implementado para inyectar identidad en handlers
- AuthenticationBehavior en pipeline MediatR

**Frontend:**
- LoginComponent (ruta `/login`)
- AuthService refactorizado para consumir API real (endpoints `/auth/login`, `/auth/refresh`, `/auth/me`)
- NgRx Auth State (selectIsAuthenticated, selectCurrentUser)
- AuthEffects con lógica de refresh + session expired
- AuthInterceptor inyecta Bearer token en requests
- SessionStorage para tokens (no localStorage)
- Mock handlers actualizados pero deshabilitados (useMocks: false en prod)

**Tests:**
- 12 tests backend (LoginCommandHandler 4, RefreshTokenCommandHandler 4, integration 4)

**Documentación:**
- 004-auth-api/spec.md con 3 HUs
- 004-auth-api/contract.yml con 3 endpoints

### Cómo probarlo

```bash
# Backend: Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gop.co","password":"Admin123*"}'
# Respuesta: { "accessToken": "eyJ...", "refreshToken": "...", "expiresIn": 1800, "user": {...} }

# Frontend: Login UI
npm start
# Navegar a http://localhost:4200/login
# Ingresar: admin@gop.co / Admin123*
# Redirigir a /dashboard
```

### Rompe compatibilidad

- ⚠️ Emails cambian de `@gop360.com` a `@gop.co` (spec.md Sección 4)
- ⚠️ `useMocks` debe ser `false` en producción (ver QA-REPORT.md BUG-001)

### Dependencias nuevas

- Backend: System.IdentityModel.Tokens.Jwt (JWT generation)
- Frontend: @ngrx/store, @ngrx/effects (NgRx estado auth)

---

## Iteración 3 — Wells Catalog CRUD (Completed)

**Fecha:** 2026-03-16 a 2026-03-31  
**Feature:** 005-wells-catalog-crud  
**Dependencias:** 003, 004 (completadas)  
**Duración agent-time:** ~12 horas  
**Status:** ✅ Producción

### Qué se entregó

**Backend:**
- Entidad Well (52 campos: contrato, ubicación, datos técnicos, estado)
- 5 entidades catálogo (Contrato, Campo, Departamento, Municipio, Cluster) con datos seed
- CRUD endpoints (CREATE, READ, UPDATE, DELETE — solo BORRADOR)
- 5 endpoints de catálogo con filtros jerárquicos
- Paginación estándar (page, pageSize, items, total)
- Multi-tenant automático (query filter por tenantId)
- Soft delete (IsDeleted flag + query filter)
- Primeras migraciones EF Core
- 43 tests (CreateWellCommandHandler 4, GetWellsListQueryHandler 3, etc.)

**Frontend:**
- WellsManageComponent (listado `/wells/manage`)
- WellListItem model + DTO + mapper
- WellsApiService para llamadas HTTP
- NgRx Wells state (selectWells, selectWellsLoading, etc.)
- Catálogos pre-cargados en store al iniciar (CatalogService)
- Paginación en tabla PrimeNG
- RBAC visual: botones habilitados/deshabilitados según rol

**Tests:**
- 43 tests unitarios backend
- 15 tests integración API

**Documentación:**
- 005-wells-catalog-crud/spec.md con 6 HUs
- 005-wells-catalog-crud/contract.yml con 10 endpoints

### Cómo probarlo

```bash
# Listar pozos (vacío al inicio)
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/v1/wells

# Crear pozo
curl -X POST http://localhost:5000/api/v1/wells \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"contratoId":1,"campoId":1,"denominacion":"ALPHA",...}'
# Respuesta: 201 Created { id: "...", nombrePozo: "Llanos Orientales-ALPHA-01", estado: "BORRADOR" }

# Frontend: Listar pozos
npm start
# Navegar a /wells/manage (after login)
# Tabla vacía (sin semilla de pozos)
```

### Rompe compatibilidad

- ❌ Endpoint `/wells` ahora requiere autenticación (antes no existía)

### Dependencias nuevas

- Backend: Aspose.Words (reportes — no usado en MVP)
- Frontend: PrimeNG tabla, PrimeNG paginator, PrimeNG input

---

## Iteración 4 — Well Creation Form (Completed)

**Fecha:** 2026-04-01 a 2026-04-08  
**Feature:** 006-well-creation-form  
**Dependencias:** 005 (completada)  
**Duración agent-time:** ~9 horas  
**Status:** ✅ Producción

### Qué se entregó

**Backend:**
- Nuevo endpoint: `GET /api/v1/wells/preview-name?contratoId=X&denominacion=Y&consecutivo=Z`
- Calcula nombre del pozo + verifica disponibilidad
- 4 tests (PreviewWellNameQueryHandler)

**Frontend:**
- WellCreateComponent — Wizard de 4 pasos con PrimeNG Stepper
  - Paso 1: Información del Contrato (cascadas: Contrato→campos, Campo→clusters)
  - Paso 2: Datos Técnicos (dropdowns de tipos)
  - Paso 3: Ubicación Geográfica (cascada: Departamento→municipios)
  - Paso 4: Resumen y Confirmación
- Cascadas reactivas (combineLatest + switchMap)
- Preview en tiempo real de nombre del pozo
- Verificación de disponibilidad (GET /preview-name)
- Modo edición reutiliza wizard con datos pre-cargados
- Validación por paso (no avanza si hay errores)
- Modo OPERADOR: solo acceso a su tenant

**Tests:**
- 4 tests backend
- Frontend: incluidos en app.spec.ts (roto en QA)

**Documentación:**
- 006-well-creation-form/spec.md con 7 HUs
- 006-well-creation-form/contract.yml con 1 nuevo endpoint

### Cómo probarlo

```bash
# Frontend: Crear pozo con wizard
npm start
# Navegar a /wells/create (after login as OPERADOR)
# Llenar pasos 1-4
# Step 4: Botón "Guardar Borrador"
# POST /api/v1/wells
# Toast éxito + redirección a /wells/manage
```

### Rompe compatibilidad

- ❌ Nuevo endpoint no-breaking (adición)

### Dependencias nuevas

- Frontend: PrimeNG Stepper, reactive forms

---

## Iteración 5 — State Machine + UWI + RBAC (Completed)

**Fecha:** 2026-04-09 a 2026-04-16  
**Feature:** 007-well-state-machine  
**Dependencias:** 005, 006 (completadas)  
**Duración agent-time:** ~11 horas  
**Status:** ✅ Producción + QA Audit

### Qué se entregó

**Backend:**
- Máquina de estados: BORRADOR → PENDING_UWI → READY_FISCAL → FISCALIZADO
- 4 acciones de transición: ENVIAR, APROBAR_UWI, DEVOLVER, FISCALIZAR
- Generación automática de UWI al ejecutar ENVIAR
  - Patrón: `CO-{daneDpto}-{daneMpio}-{denominacion}-{consecutivo}-{trayectoria}`
  - Ejemplo: `CO-50-50568-ALPHA-01-ST`
- Historial inmutable de transiciones (WellTransitionHistory)
- RBAC por transición:
  - ENVIAR: OPERADOR, SUPERVISOR, ADMIN
  - APROBAR_UWI, DEVOLVER, FISCALIZAR: SUPERVISOR, ADMIN
  - AUDITOR: solo lectura
- 2 nuevos endpoints:
  - `PATCH /api/v1/wells/{id}/transition` — Cambio de estado
  - `GET /api/v1/wells/{id}/history` — Historial
- 16 tests unitarios (TransitionWellCommandHandler 8 + domain 8)

**Frontend:**
- WellDetailComponent con detalle completo + acciones contextuales
- WellStatusBadge con colores semánticos (gris, naranja, azul, verde)
- Botones de transición según rol y estado actual
- Diálogo de devolución con campo de motivo (mínimo 10 caracteres)
- Timeline de historial (WellHistoryTimelineComponent)
- Navegación post-transición vía NgRx Effects
- AUDITOR: sin botones de transición (solo lectura)

**Tests:**
- 16 tests backend
- 0 tests frontend (incluidos en app.spec.ts roto)

**Documentación:**
- 007-well-state-machine/spec.md con 7 HUs (estados, transiciones, UWI)
- 007-well-state-machine/contract.yml con 2 endpoints

### Cómo probarlo

```bash
# Crear → Enviar → Aprobar → Fiscalizar
# Login as OPERADOR
npm start
# Crear pozo en /wells/create
# Lista muestra pozo en estado "Borrador"
# Botón "Enviar para UWI"
# PATCH /api/v1/wells/{id}/transition {"action": "ENVIAR"}
# Backend genera UWI automáticamente
# Estado cambia a "Pendiente UWI"

# Login as SUPERVISOR (different session)
# Ver detalle del pozo
# Botón "Aprobar UWI"
# PATCH /api/v1/wells/{id}/transition {"action": "APROBAR_UWI"}
# Estado cambia a "Listo Fiscal"

# Botón "Fiscalizar"
# PATCH /api/v1/wells/{id}/transition {"action": "FISCALIZAR"}
# Estado cambia a "Fiscalizado" (terminal, sin botones)

# Ver historial: GET /api/v1/wells/{id}/history
# Timeline con 3 entradas: ENVIAR, APROBAR_UWI, FISCALIZAR
```

### Rompe compatibilidad

- ✅ No-breaking: adición de campo `uwi` a WellDetail/WellListItem
- ✅ Nuevos endpoints no-breaking

### Dependencias nuevas

- Backend: (ninguna)
- Frontend: PrimeNG Dialog, PrimeNG Tag, animations

---

## QA Audit + Fixes (In Progress)

**Fecha:** 2026-04-17  
**Feature:** Auditoría post-implementación  
**Responsable:** GOP-QA  
**Status:** ⚠️ Bloqueado (5 violaciones críticas)

### Hallazgos Críticos

| Código | Severidad | Descripción | Impacto |
|--------|-----------|------------|--------|
| FE-C01 | 🔴 CRÍTICO | `useMocks: true` en `environment.prod.ts` | Backend nunca recibe llamadas en producción |
| FE-C02 | 🔴 CRÍTICO | Tests frontend rotos (No provider Store) | `ng test` 2/2 FAILING |
| BE-C01 | 🔴 CRÍTICO | `UpdateWellRequest` en WellsController.cs | Viola regla "un archivo = una clase" |
| BE-C02 | 🔴 CRÍTICO | `Enum.Parse()` puede lanzar excepción | Violación Result Pattern |
| BE-C03 | 🔴 CRÍTICO | Mismo que BE-C02 en UpdateWellCommandHandler | Violación Result Pattern |

### Violaciones Mayores

| Código | Descripción | Corrección estimada |
|--------|------------|---------------------|
| BE-M01 | Lógica en HealthController.GetHealth (18 líneas) | 30 min |
| BE-M02 | Tests API usan EF InMemory (no Testcontainers) | 2–3 h |
| BE-M03 | Faltan tests: UpdateWell, DeleteWell, GetWellById | 3 h |
| FE-M01 | catchError silencia errores HTTP (no llega al interceptor) | 30 min |
| FE-M02 | Navegación post-acción en componentes (violar NgRx Effects) | 2 h |
| FE-M03 | Navegación en well-detail sin Effects | 2 h |

### Metrics

| Métrica | Valor |
|---------|-------|
| Tests backend (unitarios + integración) | 80 tests |
| Tests frontend | 2 (2 FAILING) |
| Violaciones críticas | 5 |
| Violaciones mayores | 6 |
| Bundle inicial Angular | 748.65 kB (presupuesto: 500 kB) |
| npm audit | 5 vulnerabilities (1 HIGH) |

### Gate Result

```
[ ] APTO para merge
[x] BLOQUEADO

Razones:
1. useMocks=true en producción
2. ng test 2/2 FAILING
3. UpdateWellRequest en controller
4. Enum.Parse puede lanzar excepción

Correcciones P0 estimadas: 90 minutos
Re-auditoría requerida post-corrección
```

---

## Resumen de Entrega

| Iteración | Feature | Endpoints | Handlers | Tests | Status |
|-----------|---------|-----------|----------|-------|--------|
| 0–1 | Backend Scaffold | 1 (health) | 0 | 11 | ✅ |
| 2 | Auth API | 3 | 2 | 12 | ✅ |
| 3 | Wells CRUD | 10 | 5 | 43+15 | ✅ |
| 4 | Well Form | 1 | 1 | 4 | ✅ |
| 5 | State Machine | 2 | 1 | 16 | ✅ |
| **Total** | **5 features** | **17 endpoints** | **9 handlers** | **80 tests BE** | **⚠️ Bloqueado QA** |

---

## Próximas Prioridades

### P0 — Blockers de Merge (Iter 5.1 — PostQA)

1. ✅ FE-C01: Cambiar `useMocks: false` en `environment.prod.ts`
2. ✅ FE-C02: Actualizar `app.spec.ts` (proveer Store o eliminar)
3. ✅ BE-C01: Mover `UpdateWellRequest` a archivo separado
4. ✅ BE-C02/C03: Reemplazar `Enum.Parse()` con `TryParse` + `Result.Failure`

**Estimado:** 90 minutos total  
**Re-auditoría:** Post-corrección

### P1 — Mayores (Iter 5.2 — Before Release)

5. BE-M01: Extraer lógica HealthController
6. BE-M02: Migrar tests API a Testcontainers SQL Server
7. BE-M03: Tests para UpdateWell, DeleteWell, GetWellById
8. FE-M01/M02/M03: Refactorizar navegación post-acción a NgRx Effects

**Estimado:** ~8 horas total

### P2 — Menores (Iter 6)

9. FE-m01: Reemplazar colores primitivos por tokens semánticos
10. npm audit fix

---

**Generado por:** GOP-Docs  
**Fecha:** 2026-04-17  
**Rama:** `gop-base-web`
