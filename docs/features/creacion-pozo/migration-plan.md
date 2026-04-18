# Plan de Migración — Módulo Wells (Pre-UWI → V2.0 PPDM)

**Decisión:** Opción B — Supersedir directo.

---

## 1. Justificación

Tras inspeccionar el repositorio:

- El módulo actual (`src/app/domains/wells/`) es un **prototipo funcional con mocks**. No hay datos de producción.
- El backend (`backend/`) aún está en construcción. No hay base de datos con pozos reales.
- El formato UWI actual (`CO-{dpto}-{mpio}-{denom}-{consec}-{tray}`) no es compatible con el formato PPDM V2.0.
- La máquina de 4 estados (BORRADOR → PENDING_UWI → READY_FISCAL → FISCALIZADO) se simplifica a 2 estados + flag.
- Mantener coexistencia temporal (Opción A) agrega complejidad sin beneficio, dado que no hay datos reales que migrar.

**Decisión: Opción B — Supersedir directo** el código existente, manteniendo compatibilidad estructural (misma carpeta `domains/wells/`, mismos path aliases).

---

## 2. Cambios por componente

### 2.1. Frontend

| Componente | Acción | Detalle |
|-----------|--------|---------|
| `features/well-form/` | **ELIMINAR** | Wizard 4 pasos → reemplazado por `well-create/` (scroll continuo) |
| `features/well-create/` | **CREAR** | Nuevo formulario de página única |
| `features/well-detail/components/transition-*` | **ELIMINAR** | Transiciones de estado no aplican en V2.0 |
| `features/well-detail/` | **MODIFICAR** | Quitar transiciones, mostrar UWI PPDM |
| `features/well-manage/` | **MODIFICAR** | Nuevas columnas, nuevos filtros |
| `models/well-enums.ts` | **MODIFICAR** | Nuevos enums/valores |
| `models/well*.ts` | **MODIFICAR** | Nuevo shape de datos |
| `models/well-transition*` | **ELIMINAR** | Ya no hay transiciones |
| `domain/` | **CREAR** | Nueva carpeta con lógica pura (UWI generator, validators) |
| `store/` | **MODIFICAR** | Simplificar (quitar transiciones, agregar UWI preview) |
| `mocks/` | **MODIFICAR** | Nuevos endpoints y formato de datos |

### 2.2. Backend

| Componente | Acción | Detalle |
|-----------|--------|---------|
| `Domain/Enums/WellStatus.cs` | **MODIFICAR** | Reducir a 2 valores |
| `Domain/Enums/SubClasificacion*.cs` | **CREAR** | Nuevo enum |
| `Domain/ValueObjects/Uwi.cs` | **CREAR** | VO con algoritmo PPDM |
| `Domain/Entities/Well.cs` | **MODIFICAR** | Refactorizar estructura completa |
| `Application/Features/Wells/Commands/TransitionWell/` | **ELIMINAR** | Ya no hay transiciones |
| `Application/Features/Wells/Queries/PreviewUwi/` | **CREAR** | Nuevo endpoint |
| `Infrastructure/Persistence/Migrations/` | **MIGRACIÓN** | Script de migración de esquema |
| `API/Controllers/WellsController.cs` | **MODIFICAR** | Quitar endpoint de transición, agregar preview-uwi |

### 2.3. Specs

| Spec | Acción |
|------|--------|
| `specs/features/005-wells-catalog-crud/` | **Mantener como referencia histórica** (no eliminar) |
| `specs/features/006-well-creation-form/` | **Mantener como referencia histórica** |
| `specs/features/007-well-state-machine/` | **Mantener como referencia histórica** |
| `docs/features/creacion-pozo/` | **NUEVO** — spec definitiva V2.0 |

---

## 3. Migración de datos (si existieran)

> **Para el estado actual del proyecto (dev/staging con mocks), no hay datos reales que migrar.**

Si en el futuro hubiera datos legacy:

### 3.1. Script de migración de estados

```sql
-- Migrar estados legacy al modelo V2.0
UPDATE Wells SET Estado = 'BORRADOR' WHERE Estado IN ('BORRADOR', 'PENDING_UWI');
UPDATE Wells SET Estado = 'CREADO' WHERE Estado IN ('READY_FISCAL', 'FISCALIZADO');
UPDATE Wells SET Forma101Radicada = 1 WHERE Estado = 'FISCALIZADO'; -- asumiendo Fiscalizado = bloqueado
```

### 3.2. Script de migración de UWI

```sql
-- Los UWIs legacy (formato CO-XX-XXXXX-DENOM-XX-TRAY) no son compatibles con PPDM
-- Se marcan como legacy y se regeneran al editar el pozo
UPDATE Wells SET Uwi = NULL, Estado = 'BORRADOR' WHERE Uwi LIKE 'CO-%';
-- Los pozos deberán re-finalizarse para obtener UWI PPDM
```

### 3.3. Script de datos seed nuevos

- Actualizar clusters con `Abreviatura` (2 chars)
- Agregar SubClasificación default a pozos exploratorios existentes

---

## 4. Orden de ejecución

```
1. Crear rama claude/iter-9-creacion-pozo-spec-20260417
2. Producir paquete SDD (este set de docs)
3. GOP-Backend ejecuta tasks.be.md (bloques 1-13)
   - Migración de esquema en bloque 9
   - Tests en bloques 12-13
4. GOP-Frontend ejecuta tasks.fe.md (bloques 1-9)
   - Limpieza de legacy en bloque 9
   - Build verification en cada bloque
5. QA valida criterios de aceptación CA-01 a CA-13
6. Merge a main
```

---

## 5. Riesgos y mitigaciones

| Riesgo | Probabilidad | Mitigación |
|--------|-------------|------------|
| Specs 005-007 tienen tasks marcados que referencian archivos legacy | Media | Mantener specs como referencia. tasks.fe.md bloque 9 limpia legacy. |
| Mock handlers legacy interfieren con nuevos | Baja | Reescribir mock handlers completo en T-FE-021. |
| Usuarios de staging tienen pozos con UWI legacy | Baja (dev only) | Script de migración limpia UWIs y resetea a BORRADOR. |
| Dependencia circular si well-detail importa componentes eliminados | Media | Bloque 9 de tasks.fe.md elimina antes de que routes se actualicen. Orden: actualizar componentes (bloque 8), luego limpiar (bloque 9). |
