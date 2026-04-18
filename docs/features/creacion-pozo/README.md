# RQF_GOP_01 — Creación de Pozo Nuevo V2.0 (Paquete SDD)

**Iteración:** 9.1
**Feature:** Creación de Pozo Nuevo — UWI Fiscalizado PPDM
**Fuente oficial:** RQF_GOP_01_CreacionPozoNuevo_V2.0 (31-mar-2026)
**Estado:** Spec completo, pendiente revisión humana por artefacto

---

## Índice de artefactos

| # | Artefacto | Ruta | Contenido |
|---|-----------|------|-----------|
| 1 | **spec.md** | [`docs/features/creacion-pozo/spec.md`](spec.md) | Especificación funcional: 12 HU, 40 RN, 13 CA, 10 EC, 2 PA |
| 2 | **contract.yml** | [`docs/features/creacion-pozo/contract.yml`](contract.yml) | OpenAPI 3.1: 13 endpoints (7 wells + 6 catalogs) |
| 3 | **plan.fe.md** | [`docs/features/creacion-pozo/plan.fe.md`](plan.fe.md) | Plan frontend Angular 21: árbol archivos, estado, componentes |
| 4 | **plan.be.md** | [`docs/features/creacion-pozo/plan.be.md`](plan.be.md) | Plan backend .NET 10: entidades, commands, queries, tests |
| 5 | **tasks.fe.md** | [`docs/features/creacion-pozo/tasks.fe.md`](tasks.fe.md) | 47 tareas en 9 bloques compilables (`ng build`) |
| 6 | **tasks.be.md** | [`docs/features/creacion-pozo/tasks.be.md`](tasks.be.md) | 45 tareas en 13 bloques compilables (`dotnet build/test`) |
| 7 | **data-model.md** | [`docs/features/creacion-pozo/data-model.md`](data-model.md) | Entidades, VOs, enums con diferencias vs modelo anterior |
| 8 | **uwi-algorithm.md** | [`docs/features/creacion-pozo/uwi-algorithm.md`](uwi-algorithm.md) | Algoritmo PPDM completo: 9 segmentos, 3 ejemplos, edge cases |
| 9 | **migration-plan.md** | [`docs/features/creacion-pozo/migration-plan.md`](migration-plan.md) | Opción B (supersedir directo), scripts de migración |

## Artefactos complementarios

| Artefacto | Ruta | Contenido |
|-----------|------|-----------|
| **ADR-001** | [`docs/adr/ADR-001-architecture-principles.md`](../adr/ADR-001-architecture-principles.md) | Principios arquitectónicos transversales FE+BE |

---

## Métricas del paquete

| Métrica | Valor |
|---------|-------|
| Historias de usuario | 12 (HU-01 a HU-12) |
| Reglas de negocio | 40 (RN-01 a RN-40) |
| Criterios de aceptación | 13 (CA-01 a CA-13) |
| Edge cases | 10 (EC-01 a EC-10) |
| Puntos abiertos | 2 (PA-01: fuente datos maestros, PA-02: Costa Afuera Dpto/Mpio) |
| Endpoints API | 13 (7 wells + 6 catalogs) |
| Tareas frontend | 47 en 9 bloques |
| Tareas backend | 45 en 13 bloques |
| Tests backend estimados | ~25 (Domain + Application) |
| Tests frontend estimados | ~18 (domain logic + validators) |

---

## Supersede

Este paquete reemplaza las especificaciones previas para el módulo de pozos:

- `specs/features/005-wells-catalog-crud/` — CRUD básico
- `specs/features/006-well-creation-form/` — Wizard multi-paso
- `specs/features/007-well-state-machine/` — Máquina de 4 estados

Las specs anteriores se mantienen en el repo como referencia histórica pero no deben usarse para nuevas implementaciones.

---

## Siguiente paso

GOP-Frontend y GOP-Backend toman el control ejecutando `tasks.fe.md` y `tasks.be.md` respectivamente.
