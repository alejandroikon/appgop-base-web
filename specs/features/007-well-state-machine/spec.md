# Spec: Máquina de Estados del Pozo — UWI + RBAC por Transición

**Feature ID:** 007-well-state-machine
**Iteración:** 5
**Dominio:** Wells (`/api/v1/wells/{id}/transition`, `/api/v1/wells/{id}/history`)
**Dependencias:** 005-wells-catalog-crud (implementado), 006-well-creation-form (implementado)
**Estado:** Generado por GOP-Spec

---

## 1. Contexto y Alcance

Las iteraciones anteriores crearon pozos exclusivamente en estado `BORRADOR`. Esta feature implementa la **máquina de estados completa** del ciclo de vida del pozo, la **generación automática de UWI** (Unique Well Identifier) con formato ANH Colombia, y el **control de acceso granular por transición** (RBAC).

**Incluido:**
- 4 estados: `BORRADOR` → `PENDING_UWI` → `READY_FISCAL` → `FISCALIZADO`
- 4 acciones de transición: ENVIAR, APROBAR_UWI, DEVOLVER, FISCALIZAR
- Generación automática de UWI al ejecutar ENVIAR
- RBAC por transición (qué rol puede ejecutar qué acción)
- Validaciones por transición (campos completos, UWI válido, comentario obligatorio)
- Historial inmutable de transiciones por pozo
- Frontend: badges de estado, botones de acción contextuales, diálogo de devolución con motivo, timeline de historial
- Backend: endpoint PATCH para transiciones, endpoint GET para historial, lógica de estado en Domain

**NO incluido (iteraciones posteriores):**
- Notificaciones por correo al cambiar de estado
- Aprobaciones multi-nivel (firma de varios supervisores)
- Bloqueo concurrente de transiciones (optimistic locking)
- Coordenadas geográficas del pozo
- Upload de documentos de soporte por transición

**Nota sobre roles:** El sistema define 4 roles fijos (ADMIN, SUPERVISOR, OPERADOR, AUDITOR) según CONSTITUTION.backend.md §8.1. La función de "Fiscalizador" mencionada en el alcance se mapea al rol SUPERVISOR, dado que en la ANH el supervisor tiene autoridad tanto para aprobar UWI como para fiscalizar. Si el negocio requiere separar estas funciones en roles distintos, se requiere un cambio constitucional.

---

## 2. Máquina de Estados

### 2.1. Diagrama de Transiciones

```
                          ENVIAR                    APROBAR_UWI                FISCALIZAR
  ┌──────────┐  ──────────────────►  ┌──────────────┐  ─────────────►  ┌──────────────┐  ──────────►  ┌──────────────┐
  │ BORRADOR │                       │ PENDING_UWI  │                  │ READY_FISCAL │               │ FISCALIZADO  │
  └──────────┘  ◄──────────────────  └──────────────┘  ◄─────────────  └──────────────┘               └──────────────┘
                      DEVOLVER                              DEVOLVER
```

### 2.2. Tabla de Transiciones Válidas

| Estado Actual    | Acción        | Estado Destino  | Roles Permitidos              | Requiere Comentario | Efecto Secundario             |
|------------------|---------------|-----------------|-------------------------------|---------------------|-------------------------------|
| `BORRADOR`       | `ENVIAR`      | `PENDING_UWI`   | ADMIN, SUPERVISOR, OPERADOR   | No                  | Genera UWI si no existe       |
| `PENDING_UWI`    | `APROBAR_UWI` | `READY_FISCAL`  | ADMIN, SUPERVISOR             | No                  | Ninguno                       |
| `PENDING_UWI`    | `DEVOLVER`    | `BORRADOR`      | ADMIN, SUPERVISOR             | Sí (motivo)         | Ninguno (UWI se preserva)     |
| `READY_FISCAL`   | `FISCALIZAR`  | `FISCALIZADO`   | ADMIN, SUPERVISOR             | No                  | Ninguno                       |
| `READY_FISCAL`   | `DEVOLVER`    | `BORRADOR`      | ADMIN, SUPERVISOR             | Sí (motivo)         | Ninguno (UWI se preserva)     |

### 2.3. Reglas del Estado Terminal

- `FISCALIZADO` es un estado **terminal**. No admite ninguna transición.
- Un pozo en estado `FISCALIZADO` no puede editarse ni eliminarse.

---

## 3. Formato UWI — ANH Colombia

### 3.1. Patrón

```
CO-{daneDpto}-{daneMpio}-{denominacion}-{consecutivo}-{trayectoria}
```

| Segmento        | Fuente                          | Ejemplo      |
|------------------|---------------------------------|--------------|
| `CO`             | Código país fijo                | `CO`         |
| `daneDpto`       | Código DANE del departamento    | `50`         |
| `daneMpio`       | Código DANE del municipio       | `50568`      |
| `denominacion`   | Denominación del pozo (UPPER)   | `ALPHA`      |
| `consecutivo`    | Consecutivo 2 dígitos           | `01`         |
| `trayectoria`    | Código tipo trayectoria         | `ST`         |

### 3.2. Ejemplo Completo

Pozo con denominación "ALPHA", consecutivo "01", tipo trayectoria "ST", ubicado en Puerto Gaitán (DANE municipio 50568), Meta (DANE departamento 50):

```
CO-50-50568-ALPHA-01-ST
```

### 3.3. Reglas de Generación

| Regla   | Descripción |
|---------|-------------|
| RN-070  | El UWI se genera automáticamente en el backend al ejecutar la transición ENVIAR. El frontend no envía UWI. |
| RN-071  | La denominación se convierte a UPPERCASE antes de incluirse en el UWI. |
| RN-072  | Si el pozo ya tiene UWI (fue devuelto y re-enviado), se preserva el UWI existente sin regenerar. |
| RN-073  | El UWI debe ser único en todo el sistema. Si existe duplicado, la transición falla con error 409. |
| RN-074  | MaxLength del campo UWI: 50 caracteres. |

---

## 4. Historias de Usuario

### HU-040: Enviar Pozo para Asignación de UWI

**Como** operador (OPERADOR, SUPERVISOR o ADMIN),
**quiero** enviar un pozo en estado borrador para que se le asigne un UWI automáticamente,
**para** iniciar el proceso de fiscalización del pozo.

#### Criterios de Aceptación

**Escenario 1 — Envío exitoso con generación de UWI**
- **Dado** que el pozo está en estado `BORRADOR` con todos los campos requeridos completos
- **Y** el usuario tiene rol `OPERADOR`, `SUPERVISOR` o `ADMIN`
- **Cuando** se envía `PATCH /api/v1/wells/{id}/transition` con `action: "ENVIAR"`
- **Entonces** el servidor responde con `200 OK`
- **Y** el estado cambia a `PENDING_UWI`
- **Y** se genera un UWI con formato `CO-{daneDpto}-{daneMpio}-{denominacion}-{consecutivo}-{trayectoria}`
- **Y** se crea un registro en el historial de transiciones

**Escenario 2 — Envío fallido: campos incompletos**
- **Dado** que el pozo está en `BORRADOR` pero le faltan campos requeridos (ej. municipioId es null)
- **Cuando** se intenta enviar
- **Entonces** retorna `422` con ProblemDetails indicando los campos faltantes
- **Y** el estado permanece en `BORRADOR`

**Escenario 3 — RBAC: AUDITOR no puede enviar**
- **Dado** que el usuario tiene rol `AUDITOR`
- **Cuando** intenta enviar un pozo
- **Entonces** retorna `403 Forbidden`

**Escenario 4 — Transición inválida: pozo no está en BORRADOR**
- **Dado** que el pozo está en estado `PENDING_UWI`
- **Cuando** se intenta ejecutar `ENVIAR`
- **Entonces** retorna `409 Conflict` con detalle: *"La acción ENVIAR no es válida desde el estado PENDING_UWI."*

**Escenario 5 — Re-envío de pozo devuelto (UWI ya existe)**
- **Dado** que el pozo fue devuelto a `BORRADOR` y ya tiene UWI asignado
- **Cuando** se envía nuevamente
- **Entonces** se transiciona a `PENDING_UWI` sin regenerar el UWI
- **Y** el UWI existente se preserva

**Escenario 6 — UWI duplicado**
- **Dado** que al generar el UWI ya existe otro pozo con el mismo identificador
- **Cuando** se intenta enviar
- **Entonces** retorna `409 Conflict` con detalle: *"Ya existe un pozo con el UWI generado."*

---

### HU-041: Aprobar UWI del Pozo

**Como** supervisor (SUPERVISOR o ADMIN),
**quiero** aprobar el UWI asignado a un pozo,
**para** avanzar el pozo al estado listo para fiscalización.

#### Criterios de Aceptación

**Escenario 1 — Aprobación exitosa**
- **Dado** que el pozo está en estado `PENDING_UWI` con UWI válido
- **Y** el usuario tiene rol `SUPERVISOR` o `ADMIN`
- **Cuando** se envía `PATCH /api/v1/wells/{id}/transition` con `action: "APROBAR_UWI"`
- **Entonces** el estado cambia a `READY_FISCAL`
- **Y** se registra en el historial de transiciones

**Escenario 2 — RBAC: OPERADOR no puede aprobar UWI**
- **Dado** que el usuario tiene rol `OPERADOR`
- **Cuando** intenta aprobar el UWI
- **Entonces** retorna `403 Forbidden`

**Escenario 3 — Transición inválida: estado no es PENDING_UWI**
- **Dado** que el pozo está en estado `BORRADOR`
- **Cuando** se intenta ejecutar `APROBAR_UWI`
- **Entonces** retorna `409 Conflict`

---

### HU-042: Fiscalizar Pozo

**Como** supervisor (SUPERVISOR o ADMIN),
**quiero** marcar un pozo como fiscalizado,
**para** completar el ciclo de vida del pozo en el sistema.

#### Criterios de Aceptación

**Escenario 1 — Fiscalización exitosa**
- **Dado** que el pozo está en estado `READY_FISCAL`
- **Y** el usuario tiene rol `SUPERVISOR` o `ADMIN`
- **Cuando** se envía `PATCH /api/v1/wells/{id}/transition` con `action: "FISCALIZAR"`
- **Entonces** el estado cambia a `FISCALIZADO`
- **Y** se registra en el historial

**Escenario 2 — RBAC: OPERADOR no puede fiscalizar**
- **Dado** que el usuario tiene rol `OPERADOR`
- **Cuando** intenta fiscalizar
- **Entonces** retorna `403 Forbidden`

**Escenario 3 — Transición inválida: estado no es READY_FISCAL**
- **Dado** que el pozo está en estado `PENDING_UWI`
- **Cuando** se intenta ejecutar `FISCALIZAR`
- **Entonces** retorna `409 Conflict`

---

### HU-043: Devolver Pozo a Borrador

**Como** supervisor (SUPERVISOR o ADMIN),
**quiero** devolver un pozo a estado borrador indicando el motivo,
**para** que el operador corrija la información antes de re-enviar.

#### Criterios de Aceptación

**Escenario 1 — Devolución exitosa desde PENDING_UWI**
- **Dado** que el pozo está en estado `PENDING_UWI`
- **Y** el usuario tiene rol `SUPERVISOR` o `ADMIN`
- **Cuando** se envía `PATCH /api/v1/wells/{id}/transition` con `action: "DEVOLVER"` y `comment: "Denominación incorrecta, verificar nomenclatura"`
- **Entonces** el estado cambia a `BORRADOR`
- **Y** el UWI se preserva (no se elimina)
- **Y** se registra en el historial con el comentario

**Escenario 2 — Devolución exitosa desde READY_FISCAL**
- **Dado** que el pozo está en estado `READY_FISCAL`
- **Cuando** se ejecuta DEVOLVER con comentario
- **Entonces** el estado cambia a `BORRADOR` y el comentario se registra

**Escenario 3 — Devolución fallida: sin comentario**
- **Dado** que se intenta devolver sin proporcionar comentario
- **Cuando** se envía la transición
- **Entonces** retorna `422` con error: *"El motivo de devolución es requerido."*

**Escenario 4 — RBAC: OPERADOR no puede devolver**
- **Dado** que el usuario tiene rol `OPERADOR`
- **Cuando** intenta devolver
- **Entonces** retorna `403 Forbidden`

**Escenario 5 — Devolución inválida desde BORRADOR**
- **Dado** que el pozo está en estado `BORRADOR`
- **Cuando** se intenta ejecutar `DEVOLVER`
- **Entonces** retorna `409 Conflict`

**Escenario 6 — Devolución inválida desde FISCALIZADO**
- **Dado** que el pozo está en estado `FISCALIZADO`
- **Cuando** se intenta devolver
- **Entonces** retorna `409 Conflict` con detalle: *"No se puede devolver un pozo fiscalizado."*

---

### HU-044: Consultar Historial de Transiciones

**Como** usuario autenticado (cualquier rol),
**quiero** consultar el historial de transiciones de un pozo,
**para** conocer quién hizo qué cambio, cuándo y por qué.

#### Criterios de Aceptación

**Escenario 1 — Historial con múltiples entradas**
- **Dado** que el pozo ha pasado por varias transiciones
- **Cuando** se envía `GET /api/v1/wells/{id}/history`
- **Entonces** retorna `200 OK` con un array de transiciones ordenadas por fecha descendente
- **Y** cada entrada incluye: estados from/to, acción, usuario, rol, comentario (si aplica), fecha

**Escenario 2 — Historial vacío (pozo nuevo en borrador)**
- **Dado** que el pozo acaba de crearse y no ha tenido transiciones
- **Cuando** se consulta el historial
- **Entonces** retorna `200 OK` con array vacío `[]`

**Escenario 3 — Pozo no encontrado**
- **Dado** que el ID no existe
- **Cuando** se consulta el historial
- **Entonces** retorna `404` con ProblemDetails

**Escenario 4 — RBAC: todos los roles pueden consultar**
- **Dado** que el usuario tiene cualquier rol autenticado (ADMIN, SUPERVISOR, OPERADOR, AUDITOR)
- **Cuando** consulta el historial de un pozo de su tenant (o de cualquier tenant si es ADMIN/AUDITOR)
- **Entonces** obtiene el historial completo

---

### HU-045: Visualizar Estado y Acciones Contextuales en el Frontend

**Como** usuario del sistema,
**quiero** ver el estado actual del pozo con un badge visual y los botones de acción disponibles según mi rol,
**para** saber qué acciones puedo realizar sobre el pozo.

#### Criterios de Aceptación

**Escenario 1 — Badge de estado con colores semánticos**
- **Dado** que un pozo se muestra en el listado o en la vista de detalle
- **Cuando** el componente se renderiza
- **Entonces** se muestra un badge (p-tag) con color según el estado:

| Estado         | Color    | Etiqueta        |
|----------------|----------|-----------------|
| BORRADOR       | Gris     | Borrador        |
| PENDING_UWI    | Naranja  | Pendiente UWI   |
| READY_FISCAL   | Azul     | Listo Fiscal    |
| FISCALIZADO    | Verde    | Fiscalizado     |

**Escenario 2 — Botones de acción para OPERADOR en BORRADOR**
- **Dado** que el usuario es OPERADOR y el pozo está en BORRADOR
- **Cuando** ve la vista de detalle del pozo
- **Entonces** se muestran los botones: "Enviar para UWI", "Editar" (enlace al wizard)
- **Y** NO se muestran: "Aprobar UWI", "Fiscalizar", "Devolver"

**Escenario 3 — Botones de acción para SUPERVISOR en PENDING_UWI**
- **Dado** que el usuario es SUPERVISOR y el pozo está en PENDING_UWI
- **Cuando** ve la vista de detalle
- **Entonces** se muestran: "Aprobar UWI", "Devolver a Borrador"

**Escenario 4 — Botones de acción para SUPERVISOR en READY_FISCAL**
- **Dado** que el usuario es SUPERVISOR y el pozo está en READY_FISCAL
- **Cuando** ve la vista de detalle
- **Entonces** se muestran: "Fiscalizar", "Devolver a Borrador"

**Escenario 5 — Sin acciones en FISCALIZADO**
- **Dado** que el pozo está en FISCALIZADO (cualquier rol)
- **Cuando** ve la vista de detalle
- **Entonces** NO se muestran botones de transición (solo el badge verde "Fiscalizado")

**Escenario 6 — AUDITOR: solo lectura**
- **Dado** que el usuario es AUDITOR
- **Cuando** ve cualquier pozo en cualquier estado
- **Entonces** NO se muestran botones de transición ni de edición
- **Y** SÍ puede ver el badge de estado y el historial

**Escenario 7 — Diálogo de devolución con motivo obligatorio**
- **Dado** que el usuario hace clic en "Devolver a Borrador"
- **Cuando** se abre el diálogo
- **Entonces** se muestra un textarea para el motivo de devolución
- **Y** el botón "Confirmar Devolución" está deshabilitado hasta que se ingrese texto (mínimo 10 caracteres)

**Escenario 8 — Confirmación de transiciones no-devolver**
- **Dado** que el usuario hace clic en "Enviar para UWI", "Aprobar UWI" o "Fiscalizar"
- **Cuando** el sistema solicita confirmación
- **Entonces** se muestra un diálogo simple: *"¿Está seguro de {acción}?"* con botones Confirmar/Cancelar

**Escenario 9 — Loading state durante transición**
- **Dado** que el usuario confirma una transición
- **Cuando** la petición está en curso
- **Entonces** el botón muestra spinner y se deshabilita
- **Y** los demás botones de acción se deshabilitan

---

### HU-046: Vista de Detalle del Pozo con Historial

**Como** usuario autenticado,
**quiero** ver el detalle completo de un pozo junto con su historial de transiciones,
**para** tener visibilidad del estado actual y la trazabilidad completa.

#### Criterios de Aceptación

**Escenario 1 — Vista de detalle completa**
- **Dado** que el usuario navega a `/wells/:id`
- **Cuando** el componente se carga
- **Entonces** se muestra:
  - Header: nombre del pozo + badge de estado + UWI (o "Sin UWI" si es borrador)
  - Sección de acciones: botones contextuales según rol y estado (HU-045)
  - Información del pozo: datos agrupados por sección (contrato, técnicos, ubicación)
  - Historial: timeline de transiciones con fecha, usuario, acción y comentario

**Escenario 2 — Pozo no encontrado**
- **Dado** que el ID no existe o no pertenece al tenant del usuario
- **Cuando** se carga la vista
- **Entonces** se muestra error y se redirige a `/wells/manage`

**Escenario 3 — Acceso desde el listado**
- **Dado** que el usuario está en el listado de pozos (`/wells/manage`)
- **Cuando** hace clic en el nombre de un pozo
- **Entonces** navega a `/wells/:id` con la vista de detalle

**Escenario 4 — Historial con motivos de devolución**
- **Dado** que el pozo fue devuelto y el historial tiene entradas con comentario
- **Cuando** se renderiza el timeline
- **Entonces** las entradas de devolución muestran el motivo en texto destacado (ej. cuadro informativo)

---

## 5. Reglas de Negocio

| Regla   | Descripción |
|---------|-------------|
| RN-070  | El UWI se genera automáticamente al ejecutar ENVIAR. El frontend no puede enviar ni modificar el UWI. |
| RN-071  | La denominación se convierte a UPPERCASE en el UWI. |
| RN-072  | Si el pozo ya tiene UWI (re-envío post-devolución), se preserva el existente. |
| RN-073  | El UWI debe ser único. Duplicado genera 409. |
| RN-074  | MaxLength del UWI: 50 caracteres. |
| RN-075  | DEVOLVER siempre requiere comentario (mínimo 10 caracteres, máximo 500). |
| RN-076  | Solo pozos en estado BORRADOR pueden editarse (PUT) o eliminarse (DELETE). Esto refuerza RN-033 de 005. |
| RN-077  | FISCALIZADO es terminal: no admite ninguna transición ni modificación. |
| RN-078  | Al ejecutar ENVIAR, todos los campos requeridos del pozo (de 005) deben estar completos. Si faltan, retorna 422 con los campos faltantes. |
| RN-079  | Cada transición genera un registro inmutable en el historial (nunca se elimina ni edita). |
| RN-080  | El historial registra: usuario, rol del usuario, fecha, estados from/to, acción y comentario. |
| RN-081  | El OPERADOR solo puede ejecutar ENVIAR sobre pozos de su propio tenant. Multi-tenant aplica. |
| RN-082  | El AUDITOR no puede ejecutar ninguna transición. Solo consulta historial y detalle. |

---

## 6. RBAC por Transición

| Endpoint / Acción                        | ADMIN | SUPERVISOR | OPERADOR | AUDITOR |
|------------------------------------------|-------|------------|----------|---------|
| `PATCH transition` — ENVIAR              | ✅    | ✅         | ✅ (su tenant) | ❌ |
| `PATCH transition` — APROBAR_UWI         | ✅    | ✅         | ❌       | ❌      |
| `PATCH transition` — DEVOLVER            | ✅    | ✅         | ❌       | ❌      |
| `PATCH transition` — FISCALIZAR          | ✅    | ✅         | ❌       | ❌      |
| `GET history`                            | ✅ todos | ✅ su tenant | ✅ su tenant | ✅ todos (RO) |
| Ver detalle `/wells/:id`                 | ✅ todos | ✅ su tenant | ✅ su tenant | ✅ todos (RO) |
| Botones de acción en UI                  | Todos | Según estado | Solo ENVIAR | Ninguno |

---

## 7. Edge Cases

| ID     | Escenario | Comportamiento Esperado |
|--------|-----------|-------------------------|
| EC-080 | Transición concurrente: dos usuarios intentan transicionar el mismo pozo al mismo tiempo | El primero en llegar ejecuta; el segundo recibe 409 (estado ya cambió) |
| EC-081 | DEVOLVER con comentario de solo espacios | 422 — se aplica trim, resultado vacío falla validación de mínimo 10 caracteres |
| EC-082 | ENVIAR pozo con UWI que colisiona con pozo soft-deleted | El UWI se valida contra todos los pozos (incluyendo soft-deleted). 409 si existe. |
| EC-083 | GET history de pozo de otro tenant (OPERADOR) | 404 — el query filter multi-tenant oculta el pozo |
| EC-084 | PATCH transition en pozo soft-deleted | 404 — el query filter excluye eliminados |
| EC-085 | PATCH transition con action no reconocida (ej. "CANCELAR") | 422 — acción no es un valor válido del enum |
| EC-086 | ENVIAR con denominación que tiene acentos → UWI | La denominación se normaliza: acentos se preservan en el campo `denominacion`, el UWI usa la versión sin acentos (ASCII). Ej: "NIÑO" → `CO-50-50568-NINO-01-ST` |
| EC-087 | Pozo devuelto→re-enviado→aprobado: ¿cambia UWI? | No. El UWI generado en el primer ENVIAR persiste en todo el ciclo (RN-072) |
| EC-088 | FISCALIZAR pozo con datos incompletos (campo fue editado en borrador post-devolución y quedó incompleto) | 422 — la validación de campos completos se re-ejecuta en FISCALIZAR |
| EC-089 | Historial de pozo en BORRADOR (recién creado, sin transiciones) | 200 con array vacío `[]` |

---

## 8. Checklist de Revisión

- [x] Las 7 HUs cubren: 4 transiciones, historial, UI contextual, vista de detalle
- [x] Cada HU tiene al menos: happy path, error de validación/transición, error de autorización
- [x] Máquina de estados documentada con diagrama y tabla de transiciones
- [x] Formato UWI definido con patrón, ejemplo y reglas de generación
- [x] RBAC cubre los 4 roles con permisos por transición (no solo por endpoint)
- [x] Edge cases cubren concurrencia, re-envío, UWI duplicado, soft-delete, datos incompletos
- [x] Dependencias explícitas con 005 (Well entity, enums, endpoints de detalle/edición)
- [x] Nota explícita sobre mapeo Fiscalizador → SUPERVISOR por restricción constitucional
