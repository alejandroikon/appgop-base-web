# Spec — Iter 9 · Wells Read API + Seed Cleanup

**Feature ID:** `009-wells-read-and-seed-cleanup`
**Rama:** `009-wells-read-and-seed-cleanup`
**Prerequisito:** Iter 8 mergeado (`2c21c85`)

---

## 1. Objetivo

Cerrar los huecos funcionales que dejó Iter 8 al persistir el primer pozo E2E:

1. Hacer que los endpoints de lectura (`GET /wells`, `GET /wells/{id}`) sean robustos, con filtros útiles, paginación verificada y tenant isolation cubierta por tests.
2. Migrar el seed de catálogos geográficos (Departamentos/Municipios) a `HasData` en las configuraciones EF Core para que las 3+5 filas mínimas estén garantizadas por migración, no solo por runtime seeder.
3. Corregir cualquier inconsistencia de naming en el DTO de creación del pozo descubierta durante el smoke de Iter 8.

**Fuera de alcance:** Ver §3.

---

## 2. Historias de usuario

### US-01 — Listar pozos con paginación y filtros

**Como** usuario autenticado (ADMIN, SUPERVISOR, OPERADOR, AUDITOR),
**quiero** obtener un listado paginado de pozos del tenant actual con filtros opcionales,
**para** encontrar rápidamente los pozos que necesito gestionar.

#### Criterios de aceptación

| # | Given | When | Then |
|---|-------|------|------|
| AC-01 | El tenant tiene 25 pozos | GET /wells?page=1&pageSize=10 | 200 OK. items.length=10, total=25, page=1, pageSize=10 |
| AC-02 | El tenant tiene 0 pozos | GET /wells | 200 OK. items=[], total=0 |
| AC-03 | Filtro por contratoId válido (contrato con 3 pozos) | GET /wells?contratoId=1 | 200 OK. total=3 |
| AC-04 | Filtro por campoId válido (campo con 2 pozos) | GET /wells?campoId=1 | 200 OK. total=2 |
| AC-05 | Filtro por estado "CREADO" | GET /wells?estado=CREADO | Solo pozos con estado CREADO |
| AC-06 | Filtro por denominación parcial "RUBI" (case-insensitive) | GET /wells?denominacion=rubi | Retorna pozos cuya denominación contiene "RUBI" |
| AC-07 | Combinación de filtros: contratoId=1 + estado=CREADO | GET /wells?contratoId=1&estado=CREADO | Intersección de ambos filtros |
| AC-08 | pageSize > 100 | GET /wells?pageSize=200 | Se aplica cap a 100 |
| AC-09 | page < 1 | GET /wells?page=0 | Se normaliza a page=1 |
| AC-10 | Sin token JWT | GET /wells | 401 Unauthorized |
| AC-11 | Token de tenant A | GET /wells | Solo pozos del tenant A. Pozos del tenant B invisibles. |
| AC-12 | Rol ADMIN o AUDITOR | GET /wells | Ve pozos de TODOS los tenants (bypass de tenant filter) |
| AC-13 | Sin parámetros de sort | GET /wells | Orden por createdAt desc |

#### Reglas de negocio

- **RN-PAGINACION:** Offset-based. page ≥ 1 (1-indexed), pageSize ∈ [1,100] default 20. Respuesta incluye `items`, `total`, `page`, `pageSize`.
- **RN-FILTRO-DENOMINACION:** Búsqueda parcial, case-insensitive, sobre el campo `Denominacion` de la entidad `Well`.
- **RN-FILTRO-ESTADO:** Match exacto contra el enum `WellStatus` (case-insensitive parse).
- **RN-FILTRO-CONTRATO:** Match exacto contra `Well.ContratoId`.
- **RN-FILTRO-CAMPO:** Match exacto contra `Well.CampoId`.
- **RN-TENANT:** El global query filter en `GopDbContext` filtra por `TenantId`. ADMIN/AUDITOR usan `IgnoreQueryFilters()`.

---

### US-02 — Obtener detalle de pozo por ID

**Como** usuario autenticado,
**quiero** consultar el detalle completo de un pozo por su ID,
**para** ver toda la información registrada incluyendo datos técnicos, ubicación y UWI.

#### Criterios de aceptación

| # | Given | When | Then |
|---|-------|------|------|
| AC-14 | Pozo existe y pertenece al tenant del JWT | GET /wells/{id} | 200 OK. Shape idéntica al response de POST /wells (WellDetailDto) |
| AC-15 | Pozo no existe | GET /wells/{nonExistentGuid} | 404 Not Found (ProblemDetails) |
| AC-16 | Pozo existe pero pertenece a otro tenant | GET /wells/{otherTenantWellId} | 404 Not Found (ProblemDetails). NO 403 — no revelar existencia. |
| AC-17 | Pozo soft-deleted | GET /wells/{deletedWellId} | 404 Not Found |
| AC-18 | Sin token JWT | GET /wells/{id} | 401 Unauthorized |
| AC-19 | ADMIN o AUDITOR consulta pozo de cualquier tenant | GET /wells/{id} | 200 OK (bypass tenant filter) |

#### Reglas de negocio

- **RN-SHAPE-RESPONSE:** La forma del response debe ser exactamente `WellDetailDto` — la misma que retorna `POST /wells`. Esto ya está implementado.
- **RN-TENANT-404:** El filtro de tenant convierte la no-pertenencia en 404, no en 403. No se debe revelar la existencia de pozos de otros tenants.

---

### US-03 — Seed geográfico vía HasData (migración)

**Como** operador de plataforma,
**quiero** que los 3 departamentos y 5 municipios mínimos para el demo estén garantizados por una migración EF Core,
**para** no depender del runtime seeder para datos críticos de referencia.

#### Criterios de aceptación

| # | Given | When | Then |
|---|-------|------|------|
| AC-20 | BD vacía | Aplicar migraciones hasta `SeedCatalogosGeo` | Los 3 departamentos y 5 municipios existen en las tablas |
| AC-21 | BD de staging con las 3+5 filas ya insertadas por DbSeeder | Aplicar migración `SeedCatalogosGeo` | Migración pasa SIN error. No duplica filas. Idempotente. |
| AC-22 | BD con departamentos.json completo (33 dptos) | Aplicar migración `SeedCatalogosGeo` | No afecta los 30 departamentos restantes |
| AC-23 | Después de la migración, el DbSeeder corre en startup | Startup de la API | DbSeeder detecta que los 3+5 ya existen, no duplica. Log indica "sin cambios". |

#### Datos de seed

**Departamentos (IDs secuenciales, CodigoDane en columna separada — alineado con staging):**

| Id | Nombre | CodigoDane |
|----|--------|------------|
| 1 | Meta | 50 |
| 2 | Casanare | 85 |
| 3 | Santander | 68 |

**Municipios (IDs secuenciales, DepartamentoId referencia la tabla Departamentos):**

| Id | Nombre | DepartamentoId | CodigoDane |
|----|--------|----------------|------------|
| 1 | Puerto Gaitán | 1 | 50568 |
| 2 | Puerto López | 1 | 50573 |
| 3 | Tauramena | 2 | 85410 |
| 4 | Aguazul | 2 | 85010 |
| 5 | Barrancabermeja | 3 | 68081 |

> **Nota ED-11:** Aguazul tiene DANE oficial 85010 (no 85015). La migración incluye un UPDATE correctivo para staging donde pudo haberse insertado con el código erróneo.

---

### US-04 — Verificar consistencia del DTO ClusterId

**Como** desarrollador,
**quiero** confirmar que el nombre del campo de cluster en el DTO de creación (`CreateWellCommand`) es consistente con el response (`WellDetailDto`),
**para** evitar confusiones en la integración frontend-backend.

#### Criterios de aceptación

| # | Given | When | Then |
|---|-------|------|------|
| AC-24 | El campo en CreateWellCommand es `ClusterId` (int?) | POST /wells con clusterId=1 | El pozo se crea con ClusterId=1. WellDetailDto response incluye clusterId=1 |
| AC-25 | Los tests existentes de Create Well pasan | dotnet test | Todos los tests de CreateWellCommandHandler verdes |

---

## 3. Fuera de alcance

| Área | Razón |
|------|-------|
| Lifecycle del pozo (transiciones de estado) | Iter 10 |
| Datos de producción (BOPD, water cut) | Iter 13+ |
| Forma 101 radicación | Iter 11 |
| UI/frontend nuevo para listado | Follow-up Iter 10 FE |
| Endpoints de mutación (UPDATE, DELETE) | Ya existen desde Iter 8; no se modifican |
| Endpoint preview-uwi | Ya existe desde Iter 8; no se modifica |
| Endpoint preview-name | Ya existe desde Iter 8; no se modifica |

---

## 4. Roles del sistema

| Rol | Puede listar pozos | Scope del listado | Puede ver detalle | Puede crear |
|-----|---------------------|--------------------|--------------------|----|
| ADMIN | ✅ | Todos los tenants | ✅ Todos | ✅ |
| SUPERVISOR | ✅ | Solo su tenant | ✅ Solo su tenant | ❌ |
| OPERADOR | ✅ | Solo su tenant | ✅ Solo su tenant | ✅ |
| AUDITOR | ✅ | Todos los tenants | ✅ Todos | ❌ |

---

## 5. Tests requeridos

### 5.1 Integration tests (`GOP.API.Tests`)

| Test | Endpoint | Escenario | Assert |
|------|----------|-----------|--------|
| T-INT-01 | GET /wells | Happy path paginado | 200, items.length ≤ pageSize, total correcto |
| T-INT-02 | GET /wells | Tenant isolation: user A no ve pozos de user B | items vacío para tenant sin pozos |
| T-INT-03 | GET /wells | Filtros combinados (contratoId + estado) | Solo pozos que cumplen ambos |
| T-INT-04 | GET /wells | Filtro denominación parcial | Match case-insensitive |
| T-INT-05 | GET /wells/{id} | Pozo existe, mismo tenant | 200 + WellDetailDto completo |
| T-INT-06 | GET /wells/{id} | Pozo no existe | 404 ProblemDetails |
| T-INT-07 | GET /wells/{id} | Pozo de otro tenant | 404 (no 403) |
| T-INT-08 | GET /wells/{id} | Sin auth | 401 |

### 5.2 Infrastructure tests (`GOP.Infrastructure.Tests`)

| Test | Escenario | Assert |
|------|-----------|--------|
| T-INFRA-01 | Migración SeedCatalogosGeo sobre BD vacía | 3 dptos + 5 mpios insertados |
| T-INFRA-02 | Migración SeedCatalogosGeo sobre BD con registros existentes | No falla. Idempotente. |

### 5.3 Application tests (`GOP.Application.Tests`)

| Test | Escenario | Assert |
|------|-----------|--------|
| T-APP-01 | GetWellsListQuery con filtro campoId | Solo pozos del campo |
| T-APP-02 | GetWellsListQuery con filtro denominacion parcial | Match case-insensitive |
| T-APP-03 | GetWellByIdQuery pozo no encontrado | Result.Failure con error NotFound |

### 5.4 Regression (no-break)

| Test | Escenario | Assert |
|------|-----------|--------|
| T-REG-01 | CreateWell FINALIZE con clusterId (no clusterUbicacionId) | Tests existentes siguen verdes |
| T-REG-02 | Todos los tests existentes en GOP.API.Tests | Verdes |
