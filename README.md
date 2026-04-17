# GOP 360° — Gestión de Operaciones Petroleras

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](./CHANGELOG.md) [![Tests](https://img.shields.io/badge/tests-80%20BE%20%2B%202%20FE-yellow)](./QA-REPORT.md) [![License](https://img.shields.io/badge/license-ANH-blue)](#) [![Deployed](https://img.shields.io/badge/deployment-Netlify%20%2B%20TBD-informational)](#)

Sistema integral de gestión de pozos petroleros para la Agencia Nacional de Hidrocarburos (ANH) de Colombia. Rastrea el ciclo de vida completo de un pozo desde su concepto en borrador hasta su fiscalización definitiva, con generación automática de UWI (Unique Well Identifier) y máquina de estados robusta.

---

## 🎯 Visión General

GOP 360° implementa un **flujo de trabajo normativo** para operadoras de petróleo y gas en Colombia:

1. **Operador** crea un pozo en **estado Borrador** (formulario wizard multi-paso)
2. **Sistema** genera automáticamente un **UWI único** según formato ANH
3. **Supervisor** aprueba el UWI y avanza el pozo a **Listo Fiscal**
4. **Supervisor** marca el pozo como **Fiscalizado** (estado terminal)
5. **Historial inmutable** rastrea todas las transiciones con usuario, fecha y motivos

**Multi-tenancy integrado:** Cada operadora solo ve sus propios pozos. ADMIN y AUDITOR ven todos.

---

## 🏗️ Stack Tecnológico

### Backend
- **Framework:** .NET 10 (C# 13)
- **Arquitectura:** Clean Architecture (Domain → Application → Infrastructure → API)
- **Patrón:** CQRS + MediatR 12.x
- **Base de datos:** SQL Server 2022
- **ORM:** Entity Framework Core 10.x
- **Validación:** FluentValidation 11.x
- **Mapeo:** AutoMapper 13.x
- **Logging:** Serilog 9.x (structured logs)
- **API:** OpenAPI 3.1 + NSwag
- **Testing:** xUnit 2.x + NSubstitute 5.x + Testcontainers

### Frontend
- **Framework:** Angular 18 (standalone components)
- **Estado global:** NgRx 18.x (store, effects, selectors)
- **Estado local:** Signals (forms, UI state)
- **UI:** PrimeNG 21.x (stepper, table, dialog, etc.)
- **Estilos:** Tailwind CSS 3.x
- **HTTP:** Interceptores de autenticación y manejo de errores
- **Testing:** Jasmine + Karma (parcialmente implementado)
- **Lenguaje:** TypeScript 5.x (strict mode)

### DevOps
- **Frontend Deploy:** Netlify (auto-deploy desde GitHub)
- **Backend:** TBD (Azure App Service o AWS ECS)
- **Base de datos (dev):** Docker Compose (SQL Server 2022)
- **Base de datos (prod):** TBD (Azure SQL Database)
- **CI/CD:** GitHub Actions (planned)

---

## 📦 Módulos Implementados

| Módulo | Ruta | Iteración | Features | Estado |
|--------|------|-----------|----------|--------|
| **Wells** | `/wells` | 005–007 | CRUD + Wizard + State Machine | ✅ Producción |
| **Auth** | `/auth` (backend) | 004 | JWT Bearer + Refresh | ✅ Producción |
| **Catalogs** | `/catalogs` (backend) | 005 | Contratos, Campos, Ubicación | ✅ Producción |
| **Operaciones** | — | 8 (backlog) | Programación y seguimiento | ⏳ Próxima |
| **Producción** | — | 8 (backlog) | Registros de volumen | ⏳ Próxima |
| **Auditoría** | — | 9 (backlog) | Trazabilidad de acciones | ⏳ Próxima |

---

## 🚀 Cómo Levantar el Proyecto

### Requisitos

- **Backend:** .NET 10 SDK, Docker Desktop
- **Frontend:** Node.js 18.x, npm 10.x
- **Base de datos:** SQL Server 2022 (via Docker)
- **Navegador:** Chrome/Edge moderno para localhost:4200

### Inicio Rápido

#### 1. Clonar el repositorio

```bash
git clone https://github.com/iklabs/gop-360.git
cd gop-360
```

#### 2. Backend — SQL Server + API

```bash
cd backend

# Levantar SQL Server en Docker
docker compose up -d
# SQL Server estará en localhost:1433
# Credenciales (default): sa / YourPassword123

# Restaurar dependencias y compilar
dotnet build GOP.sln

# Ejecutar tests (80 tests backend)
dotnet test GOP.sln

# Iniciar API en http://localhost:5000
dotnet run --project src/GOP.API

# Verificar health check
curl http://localhost:5000/api/v1/health
# Respuesta: { "status": "Healthy", "checks": [...] }

# Swagger UI disponible en:
# http://localhost:5000/swagger
```

#### 3. Frontend — Angular

```bash
cd ../frontend

# Instalar dependencias
npm install

# Desarrollar (hot reload en localhost:4200)
npm start
# O: ng serve

# Compilación producción
npm run build:prod
# O: ng build --configuration=production

# Tests (nota: 2 tests fallando post-QA, ver FIXES abajo)
npm test
# O: ng test --watch=false
```

#### 4. Login

Una vez frontend y backend estén levantados:

1. Navega a **http://localhost:4200/login**
2. Usa cualquier usuario seed:
   - **Admin:** `admin@gop.co` / `Admin123*`
   - **Supervisor:** `supervisor@gop.co` / `Super123*`
   - **Operador:** `operador@gop.co` / `Oper123*`
   - **Auditor:** `auditor@gop.co` / `Audit123*`

3. Te loguearás y serás redirigido al dashboard

---

## 📁 Estructura de Carpetas

### Backend

```
backend/
├── GOP.sln                           # Solución raíz
├── docker-compose.yml                # SQL Server local
│
├── src/
│   ├── GOP.Domain/                   # Entidades, enums, errores, interfaces
│   │   ├── Entities/
│   │   │   ├── Well.cs               # Agregado raíz
│   │   │   └── WellTransitionHistory.cs
│   │   ├── Enums/
│   │   │   └── WellStatus.cs         # BORRADOR, PENDING_UWI, READY_FISCAL, FISCALIZADO
│   │   └── Errors/
│   │       └── DomainErrors.cs       # Catálogo de errores
│   │
│   ├── GOP.Application/              # Casos de uso (Commands, Queries)
│   │   ├── Features/
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/LoginCommand/
│   │   │   │   └── Queries/GetMeQuery/
│   │   │   └── Wells/
│   │   │       ├── Commands/CreateWell/, UpdateWell/, DeleteWell/, TransitionWell/
│   │   │       ├── Queries/GetWellsList/, GetWellById/, GetWellHistory/, PreviewWellName/
│   │   │       └── Mappings/WellMappingProfile.cs
│   │   └── Common/
│   │       ├── Behaviors/            # Pipeline behaviors (validation, logging, auth)
│   │       └── Interfaces/           # IApplicationDbContext, ICurrentUserService
│   │
│   ├── GOP.Infrastructure/           # EF Core, Repositories, Identity
│   │   ├── Persistence/
│   │   │   ├── GopDbContext.cs
│   │   │   ├── Configurations/       # Entity configurations (Fluent API)
│   │   │   ├── Migrations/           # EF migrations
│   │   │   └── Repositories/         # WellRepository.cs
│   │   └── Identity/
│   │       └── CurrentUserService.cs
│   │
│   └── GOP.API/                      # Composición root, controllers
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── WellsController.cs    # CRUD + transiciones + history
│       │   └── CatalogsController.cs
│       ├── Middleware/
│       │   └── GlobalExceptionHandlerMiddleware.cs
│       ├── Program.cs                # Startup, DI, middleware pipeline
│       └── appsettings.json
│
└── tests/
    ├── GOP.Domain.Tests/             # 21 unit tests
    ├── GOP.Application.Tests/        # 43 unit tests
    ├── GOP.Infrastructure.Tests/     # 1 smoke test
    └── GOP.API.Tests/                # 15 integration tests
```

### Frontend

```
frontend/src/
├── app/
│   ├── core/                         # Singleton, decoradores, guards
│   │   ├── auth/                     # AuthService, auth effects
│   │   ├── guards/                   # authGuard, roleGuard
│   │   ├── http/                     # authInterceptor, errorInterceptor
│   │   └── layout/                   # MainLayoutComponent, SidebarComponent, TopHeaderComponent
│   │
│   ├── shared/                       # Modelos, servicios transversales
│   │   ├── models/
│   │   │   ├── well.model.ts         # Interface Well (camelCase)
│   │   │   ├── well.dto.ts           # Interface WellDTO (snake_case)
│   │   │   └── well.mapper.ts        # Función mapWellDTOToModel
│   │   ├── services/
│   │   │   └── catalog.service.ts    # GET /catalogs/*
│   │   ├── ui/                       # Componentes dumb (button, modal, etc.)
│   │   └── locale/                   # APP_LOCALE (textos globales)
│   │
│   ├── domains/                      # Dominios de negocio
│   │   └── wells/
│   │       ├── components/           # WellStatusBadge, WellStatusDropdown
│   │       ├── features/
│   │       │   ├── well-create/      # Wizard de 4 pasos
│   │       │   │   ├── components/   # step-contract-info, step-technical, etc.
│   │       │   │   ├── well-create.component.ts
│   │       │   │   └── locale.ts     # WELL_CREATE_LOCALE
│   │       │   ├── well-manage/      # Listado con paginación
│   │       │   ├── well-detail/      # Detalle + acciones + historial
│   │       │   └── (otras features)
│   │       ├── models/               # Well, WellListItem, WellDetail DTOs
│   │       ├── services/             # WellsApiService, CatalogsApiService
│   │       ├── store/                # NgRx (actions, reducer, selectors, effects)
│   │       └── wells.routes.ts       # Rutas lazy-loaded
│   │
│   ├── app.routes.ts                 # Enrutador principal
│   └── app.config.ts                 # Proveedores globales
│
├── environments/
│   ├── environment.interface.ts      # Interface tipada
│   ├── environment.ts                # DEV
│   ├── environment.qa.ts             # QA (NO subir a git)
│   └── environment.prod.ts           # PROD (NO subir a git)
│
└── styles/
    ├── _tokens.css                   # Design tokens (colores, tipografía)
    ├── _typography.css
    └── _overrides.css                # Override variables PrimeNG
```

---

## 🔐 Autenticación y Autorización

### JWT Bearer

Todos los endpoints (excepto `/auth/login`, `/auth/refresh`, `/health`) requieren header:

```
Authorization: Bearer <accessToken>
```

### Roles del Sistema

| Rol | Descripción | Permisos |
|-----|------------|----------|
| **ADMIN** | Administrador ANH | Ver/editar todos los pozos de todos los tenants |
| **SUPERVISOR** | Supervisor de operadora | Ver/editar/aprobar pozos de su operadora |
| **OPERADOR** | Operador de operadora | Crear/editar pozos de su operadora; enviar para UWI |
| **AUDITOR** | Auditor ANH | Ver pozos (todos los tenants) — solo lectura |

### Multi-Tenancy

Cada usuario pertenece a un **tenant** (operadora). El sistema filtra automáticamente:

- **OPERADOR/SUPERVISOR:** Solo ven pozos de su tenant
- **ADMIN:** Ve todos los tenants
- **AUDITOR:** Ve todos (lectura)

---

## 🗄️ Base de Datos

### Migración Inicial

Las migraciones están pre-creadas en `src/GOP.Infrastructure/Persistence/Migrations/`:

1. `InitialWellsAndCatalogs` — Creación tablas y datos seed
2. `AddWellTransitionHistoryAndUwi` — Historial y campo UWI

Para aplicarlas:

```bash
cd backend
dotnet ef database update -p src/GOP.Infrastructure -s src/GOP.API
```

### Datos Seed

- **3 Operadores (Tenants):** ANH, Ecopetrol, Oxy
- **4 Usuarios:** admin, supervisor, operador, auditor
- **3 Contratos:** Llanos, Magdalena, Putumayo
- **6 Campos:** Rubiales, Castilla, La Cira, Infantas, Orito, San Miguel
- **3 Departamentos + 6 Municipios:** Meta, Santander, Putumayo
- **4 Clusters:** Norte, Sur, Central, Occidental

---

## 📊 Modelos de Datos Clave

### Well (Pozo)

```typescript
{
  id: UUID,
  nombrePozo: "Llanos Orientales-ALPHA-01",  // {cuenca}-{denominacion}-{consecutivo}
  operadora: "Ecopetrol S.A.",               // Auto-asignada del JWT
  uwi: "CO-50-50568-ALPHA-01-ST",            // Generada en transición ENVIAR
  contratoId: 1,
  campoId: 1,
  denominacion: "ALPHA",                     // Solo letras
  consecutivo: "01",                         // 2 dígitos
  tipoTrayectoria: "ST",
  clasificacion: "EXPLORATORIO",
  estado: "PENDING_UWI",                     // BORRADOR, PENDING_UWI, READY_FISCAL, FISCALIZADO
  ubicacion: {
    departamentoId: 50,
    codigoDaneDpto: "50",
    municipioId: 50568,
    codigoDaneMpio: "50568",
    clusterId: 1
  },
  createdAt: "2024-11-15T14:30:00Z"
}
```

### WellStatus (Máquina de Estados)

```
BORRADOR
    ↓ ENVIAR (genera UWI)
PENDING_UWI
    ↓ APROBAR_UWI
READY_FISCAL
    ↓ FISCALIZAR
FISCALIZADO (terminal)
```

---

## 🧪 Testing

### Backend (80 tests — ✅ Pasa)

```bash
cd backend
dotnet test GOP.sln --verbosity normal

# Desglose:
# - GOP.Domain.Tests: 21 unit tests (entidades, state machine)
# - GOP.Application.Tests: 43 unit tests (handlers)
# - GOP.API.Tests: 15 integration tests
# - GOP.Infrastructure.Tests: 1 smoke test
```

### Frontend (2 tests — ❌ Fallando post-QA)

```bash
cd frontend
npm test  # O: ng test --watch=false

# Bloqueador crítico (FE-C02):
# NG0201: No provider found for `Store`
# app.spec.ts no fue actualizado cuando se integró NgRx
# Corrección: proveer TestStore o eliminar tests obsoletos
```

---

## ⚠️ Estado Actual (Post-QA Audit)

### 🔴 Bloqueadores Críticos (Iter 5.1)

| Código | Descripción | Corrección |
|--------|------------|-----------|
| **FE-C01** | `useMocks: true` en `environment.prod.ts` | Cambiar a `false` (5 min) |
| **FE-C02** | Tests frontend rotos (No provider Store) | Actualizar `app.spec.ts` (30 min) |
| **BE-C01** | `UpdateWellRequest` en controller.cs | Mover a archivo separado (10 min) |
| **BE-C02/C03** | `Enum.Parse()` puede lanzar excepción | Reemplazar con `TryParse` + `Result.Failure` (45 min) |

**Estimado de corrección:** ~90 minutos

### 🟡 Violaciones Mayores (Antes de release)

- BE-M01: Lógica en HealthController
- BE-M02: Tests API usar Testcontainers (no EF InMemory)
- BE-M03: Tests faltantes (UpdateWell, DeleteWell, GetWellById)
- FE-M01/M02/M03: Navegación post-acción en NgRx Effects

Ver `QA-REPORT.md` Sección "Plan de Corrección Priorizado" para detalles.

---

## 🔗 Links Útiles

### Documentación

- **[WELLS-MODULE.md](./WELLS-MODULE.md)** — Documentación técnica del módulo Wells (modelos, reglas, flujos)
- **[CHANGELOG.md](./CHANGELOG.md)** — Historial de iteraciones y entregas
- **[QA-REPORT.md](./QA-REPORT.md)** — Auditoría post-implementación con violaciones y plan de corrección
- **[CONSTITUTION.md](./CONSTITUTION.md)** — Reglas arquitectónicas globales (frontend)
- **[CONSTITUTION.backend.md](./CONSTITUTION.backend.md)** — Reglas arquitectónicas backend
- **[CONSTITUTION.contracts.md](./CONSTITUTION.contracts.md)** — Estándares OpenAPI 3.1

### Código Fuente

- **Frontend:** `frontend/src/app/domains/wells/`
- **Backend:** `backend/src/GOP.API/Controllers/WellsController.cs`
- **Tests:** `backend/tests/`, `frontend/src/app/*.spec.ts`
- **Specs:** `specs/features/005-*/`, `specs/features/006-*/`, `specs/features/007-*/`

### Deployment

- **Frontend (Netlify):** [URL pending] — Auto-deploy desde rama `main`
- **Backend (TBD):** Azure App Service o AWS ECS — [URL pending]

---

## 🤝 Contribuir

Este proyecto sigue la metodología **Spec-Driven Development (SDD)** de INTERKONT:

1. **Especificación primero:** Todo código debe tener su `spec.md` aprobado
2. **Contratos de API:** Definir `contract.yml` (OpenAPI 3.1) antes de implementar
3. **Planes de implementación:** Documentar en `plan.md` la estrategia arquitectónica
4. **Tareas atómicas:** Desglosar en `tasks.md` (máximo 1 archivo por tarea)
5. **Revisión constitucional:** Validar contra `CONSTITUTION.*.md` antes de merge

Ver `CLAUDE.md` y `GEMINI.md` para detalles del contexto de desarrollo.

---

## 📄 Licencia

Proyecto confidencial desarrollado para la Agencia Nacional de Hidrocarburos (ANH) de Colombia. Distribuido bajo acuerdo de servicios con INTERKONT Ltda.

---

## 👥 Equipo

- **Backend:** Desarrollador .NET 10 + SQL Server
- **Frontend:** Desarrollador Angular 18 + Tailwind
- **QA:** Auditoría post-implementación
- **PM/Documentación:** GOP-Docs

---

**Actualizado:** 2026-04-17  
**Rama:** `gop-base-web`  
**Versión:** 1.0 (Post-QA, pre-correcciones P0)
