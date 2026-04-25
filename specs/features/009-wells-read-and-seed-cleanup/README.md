# 009 — Wells Read API + Seed Cleanup (Iter 9)

## Resumen

Cierra los huecos funcionales de Iter 8: fortalece los endpoints de lectura de pozos (`GET /wells`, `GET /wells/{id}`) con filtros adicionales y tests de tenant isolation, y garantiza los catálogos geográficos mínimos (3 departamentos + 5 municipios) vía migración EF Core idempotente.

## Scope

| IN | OUT (deuda documentada) |
|----|-------------------------|
| Filtros nuevos: `campoId`, `denominacion` en GET /wells | Lifecycle del pozo (transiciones de estado) |
| Migración SeedCatalogosGeo (3 dptos + 5 mpios, idempotente) | Datos de producción |
| Integration tests: tenant isolation, paginación, filtros | Forma 101 |
| Verificación ClusterId consistency (no hay rename) | UI/frontend nuevo |
| Test de idempotencia del DbSeeder | UPDATE/DELETE endpoints (ya existen) |

## Endpoints afectados

| Método | Ruta | Cambio en Iter 9 |
|--------|------|-------------------|
| `GET` | `/api/v1/wells` | +2 query params: `campoId`, `denominacion` |
| `GET` | `/api/v1/wells/{id}` | Sin cambios (solo tests nuevos) |

## Artefactos SDD

- [Spec funcional](./spec.md) — User stories con Given/When/Then
- [Contrato OpenAPI](./contract.yml) — Endpoints GET /wells y GET /wells/{id}
- [Plan técnico](./plan.md) — Arquitectura, trade-offs, archivos afectados
- [Tasks](./tasks.md) — Desglose atómico por fase
- [EMERGENT-DECISIONS](./EMERGENT-DECISIONS.md) — Discrepancias entre instrucciones y código actual

## Decisiones emergentes (resueltas)

Ver [EMERGENT-DECISIONS.md](./EMERGENT-DECISIONS.md) para detalle. Resumen:

| # | Tema | Decisión final |
|---|------|----------------|
| ED-08 | clusterUbicacionId no existe | No-op (ya es ClusterId) ✅ |
| ED-09 | IDs geográficos | Secuenciales 1-N (alineado con staging) ✅ |
| ED-10 | HasData vs migración manual | Migración SQL idempotente ✅ |
| ED-11 | Aguazul DANE erróneo | Corregido a 85010 + UPDATE staging ✅ |

## Dependencias

- ✅ Iter 8 (Wells persistence + UWI, commit `2c21c85`)
- ✅ Iter 7 (Users persistence, auth, tenant isolation)
