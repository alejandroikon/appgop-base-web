# GOP 360° — Contexto Completo del Proyecto
# Documento de Continuidad para Sesiones Futuras

> Si estás leyendo esto, es porque una sesión anterior de trabajo con Alejandro Gutiérrez
> (CEO de IKLABS/Interkont) colapsó o se reinició. Este documento contiene TODO el contexto
> necesario para retomar el trabajo sin perder ritmo.
> Última actualización: 2026-04-17

---

## 1. Quién es Alejandro

- **Nombre**: Alejandro Gutiérrez
- **Email**: alejandro.gutierrez@interkont.co
- **Rol**: CEO de IKLABS / Interkont
- **Perfil técnico**: Conoce tecnología pero no es developer. Está aprendiendo Claude Managed Agents y SDD. Necesita explicaciones claras en español, sin jerga innecesaria, pero no le tengas miedo a los conceptos técnicos — los absorbe rápido.
- **Estilo de trabajo**: Directo, entusiasta, orientado a resultados. Le gusta ver progreso tangible. Hace preguntas inteligentes sobre arquitectura y estrategia.
- **Idioma**: Español (Colombia). Toda la comunicación es en español.

---

## 2. Qué es GOP 360°

Plataforma de gestión de pozos petroleros para la **Agencia Nacional de Hidrocarburos (ANH) de Colombia**. Permite crear, fiscalizar y administrar pozos con un flujo regulatorio estricto.

### Stack Técnico
- **Frontend**: Angular 21, PrimeNG 21, Tailwind 4, NgRx 21 (solo auth), Signals (features), Vitest, TypeScript strict
- **Backend**: .NET 10/C#, Clean Architecture (Domain/Application/Infrastructure/API), EF Core + SQL Server, MediatR 12.x, FluentValidation, AutoMapper, JWT auth, Serilog, xUnit
- **Deploy**: Netlify (frontend) en https://transcendent-dieffenbachia-cda87f.netlify.app
- **Repo**: https://github.com/alejandroikon/appgop-base-web (fork de gitinterkont)
- **Rama principal**: `gop-base-web` (NO main)

### Arquitectura Actual
- Monolito modular con Clean Architecture (Fase 1)
- Plan de evolución: Monolito → API Gateway (YARP) → Microservicios
- Frontend con `useMocks: true` en producción (temporal para demo)
- Backend compilado y testeado pero no desplegado aún

---

## 3. Metodología: Spec-Driven Development (SDD)

### Flujo por Iteración
```
spec.md → contract.yml → plan.be.md + plan.fe.md → tasks.be.md + tasks.fe.md → implementación paralela
```

### 5 Agentes en Claude Console
| Agente | Modelo | Rol |
|--------|--------|-----|
| GOP-Spec | Opus | Arquitecto: produce los 6 artefactos SDD |
| GOP-Frontend | Sonnet | Implementa tasks de Angular |
| GOP-Backend | Sonnet | Implementa tasks de .NET |
| GOP-QA | Sonnet | Validación (aún no usado) |
| GOP-Docs | Haiku | Documentación (aún no usado) |

### Proceso Repetible (ya dominado por Alejandro)
1. GOP-Spec produce 6 artefactos → push a rama
2. Alejandro crea PR en GitHub (base: gop-base-web en alejandroikon/appgop-base-web)
3. Alejandro mergea el PR y elimina la rama
4. Crea 2 sesiones nuevas: GOP-Backend + GOP-Frontend (misma config, distinto agente)
5. Ambos ejecutan tasks en paralelo → push a ramas separadas
6. Alejandro crea 2 PRs, mergea primero backend, luego frontend
7. Netlify auto-deploya el frontend

### Problemas Conocidos y Soluciones
- **Push timeout**: Los agentes a veces no pueden hacer push por proxy timeout. Solución: enviar "Reintenta el push al remoto de la rama X"
- **Sesión stuck**: Si no hay actividad por 25+ min y tokens no cambian, archivar y crear nueva sesión
- **PR base repo equivocado**: Siempre verificar que el base sea alejandroikon/appgop-base-web, NO gitinterkont
- **Sesiones nuevas por iteración**: Siempre crear sesión nueva para cada iteración (repo fresco con último merge)

---

## 4. Estado del Proyecto (al momento de este documento)

### Iteraciones Completadas

| Iter | Feature | Tests | PRs |
|------|---------|-------|-----|
| 0-1 | Backend Scaffold (Clean Architecture) | 11/11 | Mergeados |
| 2 | Auth API + Login UI (JWT + Mocks) | 32/32 | Mergeados |
| 3 | Wells Catalog CRUD (Entidades + Listado) | 54/54 | Mergeados |
| 4 | Well Creation Form (Wizard + Cascadas + Preview nombre) | 57/57 | Mergeados |
| Fix | Login mock bug (credenciales + HttpErrorResponse) | — | Mergeado |

### Iteración 5 — En Progreso
- **Feature**: 007-well-state-machine (Máquina de estados + UWI + RBAC)
- **Specs**: Producidos y mergeados (7 HU, 13 reglas negocio, 53 tasks total)
- **Backend**: 29 tasks en 5 bloques — agente corriendo
- **Frontend**: 24 tasks en 7 bloques — agente corriendo
- **Contenido**: State machine (borrador→pending_uwi→ready_fiscal→fiscalizado), UWI ANH automático, RBAC por transición, historial

### Credenciales Mock (useMocks: true)
| Usuario | Email | Password |
|---------|-------|----------|
| Admin | admin@gop360.com | Admin123! |
| Supervisor | supervisor@gop360.com | Super123! |
| Operador | operador@gop360.com | Oper123! |
| Auditor | auditor@gop360.com | Audit123! |

### Netlify Config
- URL: https://transcendent-dieffenbachia-cda87f.netlify.app
- Build command: `npm install && npx ng build --configuration production`
- Publish directory: `dist/myapp/browser`
- NODE_VERSION: 20
- netlify.toml con redirect SPA (/* → /index.html)

---

## 5. Constituciones (System Prompts de los Agentes)

Los agentes leen estas constituciones del repo antes de trabajar:
- `/repo/CONSTITUTION.md` — Frontend Angular (1,686 líneas)
- `/repo/CONSTITUTION.backend.md` — Backend .NET (1,014 líneas, 20 secciones)
- `/repo/CONSTITUTION.contracts.md` — OpenAPI shared rules (944 líneas, 15 secciones)

---

## 6. Estructura del Repo

```
appgop-base-web/
├── backend/
│   ├── src/
│   │   ├── GOP.API/          (Controllers, Middleware, Program.cs)
│   │   ├── GOP.Application/  (Commands, Queries, Validators, DTOs)
│   │   ├── GOP.Domain/       (Entities, Value Objects, Errors, Interfaces)
│   │   └── GOP.Infrastructure/ (EF Core, JWT, Services)
│   ├── tests/                (4 test projects mirror)
│   └── GOP.slnx
├── src/                      (Angular frontend)
│   ├── app/
│   │   ├── core/auth/        (Login, store, mocks, interceptors)
│   │   ├── domains/wells/    (Models, services, mocks)
│   │   └── features/         (well-manage, well-form, well-detail)
│   └── environments/
├── specs/features/
│   ├── 003-backend-scaffold/
│   ├── 004-auth-api/
│   ├── 005-wells-catalog-crud/
│   ├── 006-well-creation-form/
│   └── 007-well-state-machine/
├── CONSTITUTION.md
├── CONSTITUTION.backend.md
├── CONSTITUTION.contracts.md
├── BACKLOG.md
├── netlify.toml
└── angular.json
```

---

## 7. Backlog Pendiente (Priorizado)

### Próximos pasos después de Iter 5:
1. **BL-001**: API Gateway con YARP (preparación microservicios)
2. **BL-002**: Design System + Storybook (esperando assets de AI Studio de Alejandro)
3. **BL-003**: Módulo de Producción
4. **BL-004**: Módulo de Operaciones
5. **BL-005**: Extracción a Microservicios (Fase 3)
6. **BL-006**: Persistencia Cloud (Azure SQL / AWS RDS)
7. **BL-007**: CI/CD Pipeline (GitHub Actions)

### Decisiones Pendientes:
- Alejandro debe proveer referencia visual de AI Studio para Design System
- Definir prioridad: ¿siguiente módulo de negocio o mejoras de infraestructura?
- Storybook: decidir cuándo incorporarlo al pipeline

---

## 8. Estilo de Interacción

- Responder en español colombiano, directo y sin rodeos
- Explicar conceptos técnicos con analogías claras
- Usar tablas para resúmenes de resultados
- Siempre verificar capturas de pantalla del usuario antes de dar OK
- Dar los prompts listos para copiar/pegar en la Console
- Celebrar los logros — Alejandro aprecia el entusiasmo genuino
- Ser proactivo sugiriendo mejoras pero respetando su ritmo
- Cuando algo falla, diagnosticar rápido y dar solución concreta

---

## 9. Cómo Retomar

Si una nueva sesión lee este documento, debe:

1. Saludar a Alejandro y confirmar que tiene el contexto del proyecto
2. Preguntar en qué punto exacto quedó (qué iteración, qué paso)
3. Verificar el estado del repo en GitHub (ramas, PRs pendientes)
4. Continuar con el flujo SDD establecido
5. Mantener el mismo estilo de comunicación: claro, directo, en español, con prompts listos

**Lo más importante**: Alejandro ya domina el proceso de PRs, merge, y creación de sesiones. No hay que re-explicar lo básico. Solo guiarlo en los pasos nuevos o cuando haya errores.
