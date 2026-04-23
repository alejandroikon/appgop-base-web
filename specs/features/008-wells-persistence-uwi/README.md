# 008 — Wells Persistence + UWI (Iter 8)

## Resumen

Primer flujo vertical persistido de GOP 360°. Un OperatorAgent puede crear un pozo Original+Desarrollo desde el wizard FE, ver el UWI fiscalizado calculado server-side, persistirlo en Azure SQL, y consultarlo en la lista y detalle.

## Scope

| IN | OUT (deuda documentada) |
|---|---|
| Trayectoria Original (`O`) | ST/PR/G/P/ML |
| Clasificación Development | Exploratory, Stratigraphic |
| Rol OperatorAgent | OperatorCoordinator, AnhGopAdministrator |
| POST/GET list/GET detail/POST preview-uwi | PUT, DELETE, finalize, parent-well-data |
| UWI PPDM 8 componentes (Original+Development) | UWI para otras trayectorias |
| Tenant isolation vía TenantId | ANH cross-operadora |

## Endpoints operativos en Iter 8

| Método | Ruta | Estado |
|---|---|---|
| `POST` | `/api/v1/wells` | ✅ Implementado |
| `GET` | `/api/v1/wells` | ✅ Implementado |
| `GET` | `/api/v1/wells/{id}` | ✅ Implementado |
| `POST` | `/api/v1/wells/preview-uwi` | ✅ Implementado (GET con query params) |
| `PUT` | `/api/v1/wells/{id}` | ✅ Implementado (Iter previo) |
| `DELETE` | `/api/v1/wells/{id}` | ✅ Implementado (Iter previo) |

## Artefactos SDD

- [Blueprint](./blueprint.md) — Contexto, scope, trade-offs
- [Plan técnico](./plan.md) — Arquitectura por capa
- [Tasks](./tasks.md) — Desglose atómico por fase
- [EMERGENT-DECISIONS](./EMERGENT-DECISIONS.md) — Decisiones durante ejecución
- Inputs V2: [`inputs/`](./inputs/)

## Cómo correr local

### Backend
```bash
cd backend
dotnet ef database update -p src/GOP.Infrastructure -s src/GOP.API
dotnet run --project src/GOP.API
```

### Frontend
```bash
npm install
npm start
# Apunta a http://localhost:5000 por defecto
```

## Dependencias

- ✅ Iter 7 (Users persistidos, auth funcionando)
- ✅ Iter 4 (Wizard UI)
- ✅ Iter 3 (Well entity + 5 catálogos)
