# Spec: Catálogos y CRUD Básico de Pozos

**Feature ID:** 005-wells-catalog-crud
**Dominio:** `/api/v1/wells`, `/api/v1/catalogs`
**Dependencias:** 003-backend-scaffold (implementado), 004-auth-api (implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Contexto y Alcance

Esta feature implementa la **primera entidad de negocio** del sistema: el Pozo (Well). Incluye los catálogos de soporte (Contrato, Campo, Departamento, Municipio, Cluster) con datos seed, y el CRUD básico de pozos en estado borrador.

**NO incluido (iteraciones posteriores):**
- Generación de UWI (Unique Well Identifier)
- Máquina de estados del pozo (transiciones borrador → pending_uwi → ready_fiscal → fiscalizado)
- Cascadas reactivas en el formulario frontend (seleccionar contrato → filtrar campos automáticamente)
- Wizard multi-paso de creación
- Validaciones cruzadas complejas (ej. consecutivo único por campo)

**Incluido:**
- 5 entidades de catálogo con seed data
- Entidad Well + WellLocation con todos los campos del modelo
- CRUD completo (crear, leer, actualizar, eliminar borradores)
- 5 endpoints de catálogo con filtrado jerárquico
- Listado paginado con filtros y ordenamiento
- RBAC básico por rol
- Primera migración EF Core real

---

## 2. Historias de Usuario

### HU-020: Listar Pozos con Filtros y Paginación

**Como** usuario autenticado del sistema,
**quiero** ver un listado de pozos con filtros, búsqueda y paginación,
**para** encontrar rápidamente los pozos que necesito gestionar.

#### Criterios de Aceptación

**Escenario 1 — Listado exitoso con paginación**
- **Dado** que existen pozos en el sistema
- **Cuando** se envía `GET /api/v1/wells?page=1&pageSize=20`
- **Entonces** el servidor responde con `200 OK` y un body paginado con `items`, `total`, `page`, `pageSize`

**Escenario 2 — Filtro por contrato**
- **Dado** que existen pozos de diferentes contratos
- **Cuando** se envía `GET /api/v1/wells?contratoId=1`
- **Entonces** solo se retornan pozos cuyo contrato coincida

**Escenario 3 — Búsqueda por texto libre**
- **Dado** que se envía `GET /api/v1/wells?search=alpha`
- **Cuando** el servidor procesa la búsqueda
- **Entonces** retorna pozos cuyo `nombrePozo` o `denominacion` contengan el texto (case-insensitive)

**Escenario 4 — RBAC: OPERADOR ve solo sus pozos (mismo tenant)**
- **Dado** que un usuario con rol `OPERADOR` y tenant `2` consulta los pozos
- **Cuando** se envía `GET /api/v1/wells`
- **Entonces** solo se retornan pozos cuyo `tenantId` sea `2` (filtro automático multi-tenant)

**Escenario 5 — RBAC: ADMIN ve todos los pozos**
- **Dado** que un usuario con rol `ADMIN` consulta los pozos
- **Cuando** se envía `GET /api/v1/wells`
- **Entonces** se retornan pozos de todos los tenants

**Escenario 6 — Listado vacío**
- **Dado** que no existen pozos que cumplan los filtros
- **Cuando** se ejecuta la consulta
- **Entonces** retorna `200 OK` con `items: []`, `total: 0`

---

### HU-021: Crear Pozo en Borrador

**Como** operador (OPERADOR o SUPERVISOR),
**quiero** crear un pozo con información básica en estado borrador,
**para** registrar la intención de perforación antes de solicitar el UWI.

#### Criterios de Aceptación

**Escenario 1 — Creación exitosa**
- **Dado** que el usuario tiene rol `OPERADOR` o `SUPERVISOR`
- **Cuando** envía `POST /api/v1/wells` con los campos requeridos
- **Entonces** el servidor responde con `201 Created` y el pozo creado con estado `borrador`
- **Y** el `tenantId` se asigna automáticamente del JWT, no del body
- **Y** la `operadora` se obtiene del `tenantName` del usuario

**Escenario 2 — Validación de campos requeridos**
- **Dado** que faltan campos obligatorios
- **Cuando** se envía el POST
- **Entonces** retorna `422` con ProblemDetails y errores por campo

**Escenario 3 — RBAC: AUDITOR no puede crear**
- **Dado** que el usuario tiene rol `AUDITOR`
- **Cuando** intenta `POST /api/v1/wells`
- **Entonces** retorna `403 Forbidden`

**Escenario 4 — nombre_pozo se calcula automáticamente**
- **Dado** que el usuario envía `denominacion`, `consecutivo` y `contratoId`
- **Cuando** se crea el pozo
- **Entonces** el `nombrePozo` se calcula como `{cuenca del contrato}-{denominacion}-{consecutivo}`

---

### HU-022: Obtener Detalle de un Pozo

**Como** usuario autenticado,
**quiero** consultar todos los datos de un pozo específico,
**para** revisar su información completa.

#### Criterios de Aceptación

**Escenario 1 — Detalle exitoso**
- **Dado** que el pozo existe y el usuario tiene acceso (mismo tenant o ADMIN)
- **Cuando** se envía `GET /api/v1/wells/{id}`
- **Entonces** retorna `200 OK` con todos los campos del pozo incluyendo ubicación y relaciones de catálogo

**Escenario 2 — Pozo no encontrado**
- **Dado** que el ID no existe
- **Cuando** se envía `GET /api/v1/wells/{id}`
- **Entonces** retorna `404` con ProblemDetails

**Escenario 3 — RBAC: OPERADOR de otro tenant**
- **Dado** que el pozo pertenece al tenant `1` y el usuario es del tenant `2` con rol `OPERADOR`
- **Cuando** se envía `GET /api/v1/wells/{id}`
- **Entonces** retorna `404` (el query filter multi-tenant oculta el pozo)

---

### HU-023: Actualizar Pozo en Borrador

**Como** operador,
**quiero** editar un pozo que esté en estado borrador,
**para** corregir o completar la información antes de solicitar el UWI.

#### Criterios de Aceptación

**Escenario 1 — Actualización exitosa**
- **Dado** que el pozo existe, está en estado `borrador` y pertenece al tenant del usuario
- **Cuando** se envía `PUT /api/v1/wells/{id}` con datos actualizados
- **Entonces** retorna `200 OK` con el pozo actualizado

**Escenario 2 — No se puede editar pozo en otro estado**
- **Dado** que el pozo NO está en estado `borrador`
- **Cuando** se intenta actualizar
- **Entonces** retorna `422` con `detail`: *"Solo se pueden editar pozos en estado borrador."*

**Escenario 3 — RBAC: AUDITOR no puede editar**
- **Dado** que el usuario tiene rol `AUDITOR`
- **Cuando** intenta `PUT /api/v1/wells/{id}`
- **Entonces** retorna `403 Forbidden`

---

### HU-024: Eliminar Pozo en Borrador

**Como** operador,
**quiero** eliminar un pozo en estado borrador que ya no necesito,
**para** mantener limpio mi listado de pozos.

#### Criterios de Aceptación

**Escenario 1 — Eliminación exitosa (soft delete)**
- **Dado** que el pozo existe, está en `borrador` y pertenece al tenant del usuario
- **Cuando** se envía `DELETE /api/v1/wells/{id}`
- **Entonces** retorna `204 No Content`
- **Y** el pozo se marca como `IsDeleted = true` (soft delete)

**Escenario 2 — No se puede eliminar pozo en otro estado**
- **Dado** que el pozo NO está en estado `borrador`
- **Cuando** se intenta eliminar
- **Entonces** retorna `422` con `detail`: *"Solo se pueden eliminar pozos en estado borrador."*

**Escenario 3 — RBAC: solo OPERADOR y SUPERVISOR del mismo tenant**
- **Dado** que el usuario no tiene rol autorizado
- **Cuando** intenta `DELETE /api/v1/wells/{id}`
- **Entonces** retorna `403 Forbidden`

---

### HU-025: Consultar Catálogos con Filtros Jerárquicos

**Como** usuario del formulario de pozos,
**quiero** obtener los catálogos filtrados jerárquicamente,
**para** seleccionar valores válidos según dependencias (contrato → campo, departamento → municipio, campo → cluster).

#### Criterios de Aceptación

**Escenario 1 — Listar contratos**
- **Dado** que se envía `GET /api/v1/catalogs/contratos`
- **Entonces** retorna todos los contratos disponibles

**Escenario 2 — Listar campos filtrados por contrato**
- **Dado** que se envía `GET /api/v1/catalogs/campos?contratoId=1`
- **Entonces** retorna solo los campos que pertenecen al contrato 1

**Escenario 3 — Listar departamentos**
- **Dado** que se envía `GET /api/v1/catalogs/departamentos`
- **Entonces** retorna todos los departamentos con su código DANE

**Escenario 4 — Listar municipios filtrados por departamento**
- **Dado** que se envía `GET /api/v1/catalogs/municipios?departamentoId=1`
- **Entonces** retorna solo los municipios del departamento indicado

**Escenario 5 — Listar clusters filtrados por campo**
- **Dado** que se envía `GET /api/v1/catalogs/clusters?campoId=1`
- **Entonces** retorna solo los clusters del campo indicado

**Escenario 6 — Filtro sin coincidencias**
- **Dado** que se envía un filtro con ID que no tiene hijos
- **Entonces** retorna `200 OK` con array vacío `[]`

---

## 3. Reglas de Negocio

| Regla | Descripción |
|---|---|
| RN-030 | El `nombrePozo` se calcula: `{cuenca}-{denominacion}-{consecutivo}`. No se envía en el request body. |
| RN-031 | La `operadora` se auto-llena con `tenantName` del JWT del usuario. No se envía en el body. |
| RN-032 | El `tenantId` se asigna automáticamente del JWT. Nunca del body. |
| RN-033 | Solo pozos en estado `borrador` pueden editarse o eliminarse. |
| RN-034 | Los catálogos son read-only. No hay endpoints de escritura para catálogos. |
| RN-035 | La `denominacion` solo acepta letras (sin números ni especiales), max 50 caracteres. |
| RN-036 | El `consecutivo` es numérico de exactamente 2 dígitos (01-99). |
| RN-037 | Los campos derivados (`tipo_contrato`, `cuenca`, `codigo_dane_dpto`, `codigo_dane_mpio`) se resuelven en el backend a partir de las FK. |
| RN-038 | El filtro multi-tenant aplica automáticamente: OPERADOR y SUPERVISOR ven solo pozos de su tenant. ADMIN ve todos. AUDITOR ve todos (solo lectura). |
| RN-039 | Los catálogos no tienen filtro multi-tenant — son datos maestros compartidos. |
| RN-040 | Eliminación es siempre soft delete (`IsDeleted = true`). |

---

## 4. Enums del Dominio

| Enum | Valores | Descripción |
|---|---|---|
| `WellStatus` | `Borrador`, `PendingUwi`, `ReadyFiscal`, `Fiscalizado` | Estado del ciclo de vida. Esta feature solo usa `Borrador`. |
| `TipoTrayectoria` | `ST`, `P`, `PR`, `ML`, `G`, `O` | Tipo de trayectoria del pozo |
| `Clasificacion` | `Exploratorio`, `Desarrollo`, `Estratigrafico` | Clasificación del pozo |
| `TipoUbicacion` | `Continental`, `CostaFuera` | Tipo de ubicación geográfica |
| `TipoAngulo` | `H`, `V`, `D` | Horizontal, Vertical, Direccional |
| `TipoObjetivo` | `PH`, `I`, `M`, `D` | Tipo de objetivo del pozo |
| `TipoTerminacion` | `CD`, `LC`, `LR`, `GP`, `CC`, `OH`, `O` | Tipo de terminación |

---

## 5. Datos Seed de Catálogos

### Contratos (3 registros mínimo)

| Id | Nombre | Tipo | Cuenca |
|---|---|---|---|
| 1 | Contrato E&P Llanos | E&P | Llanos Orientales |
| 2 | Contrato TEA Magdalena | TEA | Valle Medio del Magdalena |
| 3 | Contrato E&P Putumayo | E&P | Putumayo |

### Campos (6 registros mínimo, 2 por contrato)

| Id | Nombre | ContratoId |
|---|---|---|
| 1 | Campo Rubiales | 1 |
| 2 | Campo Castilla | 1 |
| 3 | Campo La Cira | 2 |
| 4 | Campo Infantas | 2 |
| 5 | Campo Orito | 3 |
| 6 | Campo San Miguel | 3 |

### Departamentos (3 registros mínimo)

| Id | Nombre | CódigoDane |
|---|---|---|
| 1 | Meta | 50 |
| 2 | Santander | 68 |
| 3 | Putumayo | 86 |

### Municipios (6 registros mínimo, 2 por departamento)

| Id | Nombre | DepartamentoId | CódigoDane |
|---|---|---|---|
| 1 | Puerto Gaitán | 1 | 50568 |
| 2 | Acacías | 1 | 50006 |
| 3 | Barrancabermeja | 2 | 68081 |
| 4 | San Vicente de Chucurí | 2 | 68689 |
| 5 | Orito | 3 | 86320 |
| 6 | Puerto Asís | 3 | 86568 |

### Clusters (4 registros mínimo)

| Id | Nombre | CampoId |
|---|---|---|
| 1 | Cluster Norte | 1 |
| 2 | Cluster Sur | 1 |
| 3 | Cluster Central | 3 |
| 4 | Cluster Occidental | 5 |

---

## 6. RBAC por Endpoint

| Endpoint | ADMIN | SUPERVISOR | OPERADOR | AUDITOR |
|---|---|---|---|---|
| `GET /wells` | ✅ todos | ✅ su tenant | ✅ su tenant | ✅ todos (RO) |
| `GET /wells/{id}` | ✅ todos | ✅ su tenant | ✅ su tenant | ✅ todos (RO) |
| `POST /wells` | ✅ | ✅ | ✅ | ❌ |
| `PUT /wells/{id}` | ✅ | ✅ su tenant | ✅ su tenant | ❌ |
| `DELETE /wells/{id}` | ✅ | ✅ su tenant | ✅ su tenant | ❌ |
| `GET /catalogs/*` | ✅ | ✅ | ✅ | ✅ |

---

## 7. Edge Cases

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-040 | Crear pozo con contratoId que no existe | 422 — validación de FK en handler |
| EC-041 | Crear pozo con campoId que no pertenece al contratoId | 422 — *"El campo seleccionado no pertenece al contrato."* |
| EC-042 | Denominación con números o caracteres especiales | 422 — *"La denominación solo acepta letras."* |
| EC-043 | Consecutivo con menos o más de 2 dígitos | 422 — *"El consecutivo debe ser numérico de 2 dígitos."* |
| EC-044 | PUT con ID en body diferente al ID del path | Se ignora el ID del body. El ID del path prevalece. |
| EC-045 | DELETE de pozo ya eliminado (soft-deleted) | 404 — el query filter excluye eliminados |
| EC-046 | GET catálogo con filtro ID = 0 | 200 con array vacío (sin coincidencias) |
| EC-047 | Campos derivados no coinciden con FK (ej. cuenca enviada manualmente) | Los campos derivados se recalculan en el backend. Se ignora lo que envíe el frontend. |

---

## 8. Checklist de Revisión

- [ ] Las 6 HUs cubren CRUD completo + catálogos con criterios Given/When/Then
- [ ] Cada HU tiene al menos: happy path, error de validación, error de autorización
- [ ] Los datos seed de catálogos son suficientes para demostrar filtros jerárquicos
- [ ] RBAC cubre los 4 roles del sistema con multi-tenant
- [ ] Los enums están alineados con el modelo de datos proporcionado
- [ ] Las reglas de negocio cubren los campos calculados y derivados
- [ ] Los edge cases cubren validaciones de FK cruzadas
- [ ] No se incluye UWI, máquina de estados ni wizard — solo CRUD básico
