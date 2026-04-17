# GOP 360° — Backlog de Evoluciones

> Documento vivo. Actualizar conforme avance el proyecto.
> Última actualización: 2026-04-16

---

## Iteraciones Completadas

| Iter | Feature | Estado | Tests |
|------|---------|--------|-------|
| 0-1 | Backend Scaffold (Clean Architecture + .NET 10) | Completada | 11/11 |
| 2 | Auth API + Login UI (JWT + Mock Handlers) | Completada | 32/32 |
| 3 | Wells Catalog CRUD (Entidades + Endpoints + Listado) | Completada | 54/54 |
| 4 | Well Creation Form (Wizard + Cascadas + Preview) | Completada | 57/57 |
| 5 | State Machine + UWI + RBAC | En progreso | — |

---

## Backlog Priorizado

### P0 — Crítico (próximas iteraciones)

**BL-001: API Gateway (Preparación Fase 2 Microservicios)**
- Agregar YARP como reverse proxy frente al monolito
- Frontend apunta al gateway, no al backend directo
- Configurar rutas por módulo: /api/v1/auth/*, /api/v1/wells/*, etc.
- Health checks por módulo
- Rate limiting por endpoint
- Prepara la infraestructura para extracción futura de microservicios
- Prioridad: después de completar módulo Wells completo

**BL-002: Design System + Storybook**
- Definir tokens de diseño (colores, tipografía, espaciado, sombras)
- Componentes base: Button, Input, Select, Table, Card, Badge, Modal
- Storybook para documentación visual de componentes
- Referencia: AI Studio (pendiente que Alejandro provea assets)
- Integrar con PrimeNG 21 theme customization

**BL-003: Módulo de Producción**
- CRUD de registros de producción por pozo
- Reportes diarios/mensuales
- Volúmenes (petróleo, gas, agua)
- Vinculación con pozo fiscalizado

**BL-004: Módulo de Operaciones**
- Programación de actividades operativas
- Seguimiento de intervenciones
- Histórico de operaciones por pozo

### P1 — Importante (mediano plazo)

**BL-005: Extracción a Microservicios (Fase 3)**
- Extraer Auth Service (bounded context independiente)
- Extraer Wells Service (con su propia base de datos)
- Extraer Production Service
- Event Bus (RabbitMQ o Azure Service Bus) para comunicación entre servicios
- Database-per-service pattern
- Prerequisito: BL-001 completado

**BL-006: Persistencia Cloud**
- Fase actual: In-memory (mocks frontend) + SQL Server local (backend)
- Fase 2: Docker Compose con SQL Server para desarrollo
- Fase 3: Azure SQL Database o AWS RDS para producción
- Migrations con EF Core ya preparadas en Infrastructure

**BL-007: CI/CD Pipeline**
- GitHub Actions para build + test automático en cada PR
- Deploy automático a Netlify (frontend) — ya configurado
- Deploy backend a Azure App Service o AWS ECS
- Ambientes: dev, staging, production

**BL-008: Observabilidad**
- Serilog ya configurado (structured logging)
- Agregar Application Insights o equivalente
- Dashboard de métricas de negocio
- Alertas por errores críticos

### P2 — Mejoras (largo plazo)

**BL-009: Auditoría Completa**
- Registro de todas las acciones de usuario
- Quién hizo qué, cuándo, desde dónde
- Reporte de auditoría para ANH
- AuditableEntity ya existe en Domain (CreatedBy, ModifiedBy, etc.)

**BL-010: Exportación de Reportes**
- Export a Excel/PDF de listados
- Reportes regulatorios para ANH
- Dashboards con gráficas (recharts o similar)

**BL-011: Notificaciones**
- Notificaciones in-app cuando cambia el estado de un pozo
- Email notifications para aprobaciones pendientes
- WebSocket o SSE para tiempo real

**BL-012: Multi-idioma**
- Locale system ya implementado (locale.ts por feature)
- Agregar soporte inglés además de español
- Cambio de idioma en runtime

---

## Decisiones Arquitectónicas Registradas

| ID | Decisión | Razón | Fecha |
|----|----------|-------|-------|
| ADR-001 | Monolito modular (Clean Architecture) | Velocidad de desarrollo, equipo pequeño | 2026-04-16 |
| ADR-002 | Contract-First (SDD) | Frontend y backend en paralelo | 2026-04-16 |
| ADR-003 | Mocks en frontend (useMocks flag) | Demo sin backend desplegado | 2026-04-16 |
| ADR-004 | CQRS con MediatR | Preparación para microservicios | 2026-04-16 |
| ADR-005 | Signals sobre NgRx para estado local | Simplicidad, menos boilerplate | 2026-04-16 |
| ADR-006 | PrimeNG 21 como UI library | Componentes enterprise-ready, soporte Angular 21 | 2026-04-16 |
| ADR-007 | Netlify para frontend deploy | Auto-deploy desde GitHub, SSL gratis | 2026-04-16 |

---

## Evolución Arquitectónica Planificada

```
Fase 1 (actual)          Fase 2                    Fase 3
┌─────────────┐     ┌─────────────┐          ┌──────────────┐
│  Frontend   │     │  Frontend   │          │   Frontend   │
│  (Angular)  │     │  (Angular)  │          │   (Angular)  │
└──────┬──────┘     └──────┬──────┘          └──────┬───────┘
       │                   │                        │
       ▼                   ▼                        ▼
┌─────────────┐     ┌─────────────┐          ┌──────────────┐
│  Monolito   │     │ API Gateway │          │ API Gateway  │
│  .NET 10    │     │   (YARP)    │          │   (YARP)     │
│             │     └──────┬──────┘          └──────┬───────┘
│ - Auth      │            │                   ┌────┼────┐
│ - Wells     │            ▼                   ▼    ▼    ▼
│ - Prod.     │     ┌─────────────┐     ┌─────┐ ┌─────┐ ┌─────┐
│ - Ops.      │     │  Monolito   │     │Auth │ │Wells│ │Prod │
└─────────────┘     │  .NET 10    │     │ Svc │ │ Svc │ │ Svc │
                    └─────────────┘     └──┬──┘ └──┬──┘ └──┬──┘
                                           │       │       │
                                          DB1     DB2     DB3
```
