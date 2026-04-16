# Reglas de Contratos OpenAPI 3.1 — GOP 360°

## 1. Propósito y Alcance

El archivo `contract.yml` de cada feature es la **verdad compartida** entre frontend y backend. Define exactamente qué endpoints existen, qué reciben y qué retornan. Ambos equipos implementan contra este contrato — el frontend genera sus DTOs y servicios, el backend genera sus controllers y DTOs de respuesta.

**Ubicación:** `specs/features/NNN-nombre/contract.yml`

**Formato:** OpenAPI 3.1.0, YAML. Un archivo por feature.

**Inmutabilidad:** Una vez aprobado por el humano para una iteración, el contrato no se modifica sin aprobación explícita. Si un endpoint necesita cambios breaking, se crea una versión nueva.

---

## 2. Estructura Estándar de un contract.yml

Todo `contract.yml` sigue esta estructura raíz. Las secciones son obligatorias salvo indicación contraria.

```yaml
openapi: 3.1.0

info:
  title: "GOP 360° — {NNN} {Nombre Feature}"     # Ej: "GOP 360° — 003 Wells Management"
  version: "1.0.0"                                 # Versión del contrato, no de la API
  description: |
    Contrato OpenAPI para la feature {NNN-nombre}.
    Referencia: specs/features/{NNN-nombre}/spec.md

servers:
  - url: /api/v1
    description: Base path versionado — todos los endpoints son relativos a este prefijo

security:
  - bearerAuth: []           # Seguridad global: todo endpoint requiere JWT salvo override explícito

paths:
  # Endpoints de la feature — ver §3

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: "Token JWT obtenido en /api/v1/auth/login"

  schemas:
    # Schemas reutilizables — ver §6

  parameters:
    # Parámetros reutilizables (paginación, filtros comunes) — ver §4
```

### 2.1. Campos obligatorios en `info`

| Campo | Regla |
|---|---|
| `title` | Incluye el ID de feature y nombre legible |
| `version` | Versión semántica del contrato (`1.0.0` inicial). Se incrementa si el contrato cambia post-aprobación |
| `description` | Referencia cruzada al `spec.md` de la feature |

### 2.2. Server base path

Siempre `/api/v1`. Los paths en `paths:` son relativos a este prefijo. Ejemplo: `/wells` en el contrato se traduce a `GET /api/v1/wells` en la URL completa.

---

## 3. Convenciones de Paths y operationId

### 3.1. Paths en kebab-case

```yaml
# ✅ Correcto
paths:
  /wells:                          # Colección
  /wells/{wellId}:                 # Recurso individual
  /wells/{wellId}/daily-reports:   # Sub-recurso con kebab-case
  /admin/audit-logs:               # Multi-palabra en kebab-case

# ❌ Prohibido
  /Wells:                          # PascalCase
  /wells/{wellId}/dailyReports:    # camelCase en path
  /wells/{well_id}:                # snake_case en path param
```

### 3.2. Path parameters en camelCase

Los parámetros de ruta usan camelCase: `{wellId}`, `{operatorId}`, `{reportId}`.

### 3.3. operationId: verbo + recurso en camelCase

Cada operación tiene un `operationId` único que sigue el patrón `verboRecurso`:

| Operación HTTP | Patrón operationId | Ejemplo |
|---|---|---|
| `GET` colección | `list{Recursos}` | `listWells` |
| `GET` por ID | `get{Recurso}` | `getWell` |
| `POST` crear | `create{Recurso}` | `createWell` |
| `PUT` reemplazo total | `update{Recurso}` | `updateWell` |
| `PATCH` parcial | `patch{Recurso}` | `patchWellStatus` |
| `DELETE` | `delete{Recurso}` | `deleteWell` |

```yaml
paths:
  /wells:
    get:
      operationId: listWells
      summary: Listar pozos con paginación y filtros
    post:
      operationId: createWell
      summary: Crear un nuevo pozo
  /wells/{wellId}:
    get:
      operationId: getWell
      summary: Obtener detalle de un pozo por ID
    put:
      operationId: updateWell
      summary: Actualizar un pozo existente
    delete:
      operationId: deleteWell
      summary: Eliminar (soft delete) un pozo
```

### 3.4. Anotación de roles por endpoint

Cada operación declara los roles autorizados mediante la extensión `x-roles`:

```yaml
post:
  operationId: createWell
  x-roles: [ADMIN, SUPERVISOR]
  summary: Crear un nuevo pozo
  security:
    - bearerAuth: []
```

| Valor de `x-roles` | Significado |
|---|---|
| `[ADMIN, SUPERVISOR, OPERADOR, AUDITOR]` | Cualquier rol autenticado |
| `[ADMIN]` | Solo administradores |
| `[ADMIN, SUPERVISOR]` | Admin o Supervisor |
| Omitido + `security: []` | Endpoint público (solo login, health) |

**Regla:** `x-roles` es informativo para documentación y generación de guards. La seguridad real se implementa en el backend con `[Authorize(Roles = "...")]` y en el frontend con `roleGuard`. Ver CONSTITUTION.backend.md §8.5 y CONSTITUTION.md §10.A01.

---

## 4. Paginación Estándar

### 4.1. Parámetros de request (query params)

Todo endpoint de listado acepta estos parámetros reutilizables:

```yaml
components:
  parameters:
    PageParam:
      name: page
      in: query
      required: false
      schema:
        type: integer
        minimum: 1
        default: 1
      description: "Número de página (1-indexed)"

    PageSizeParam:
      name: pageSize
      in: query
      required: false
      schema:
        type: integer
        minimum: 1
        maximum: 100
        default: 20
      description: "Elementos por página (máximo 100)"

    SearchParam:
      name: search
      in: query
      required: false
      schema:
        type: string
        maxLength: 200
      description: "Texto libre de búsqueda"

    SortByParam:
      name: sortBy
      in: query
      required: false
      schema:
        type: string
      description: "Campo de ordenamiento (validado contra whitelist del recurso)"

    SortDirParam:
      name: sortDir
      in: query
      required: false
      schema:
        type: string
        enum: [asc, desc]
        default: asc
      description: "Dirección de ordenamiento"
```

### 4.2. Schema de respuesta paginada

```yaml
components:
  schemas:
    PagedResponse:
      type: object
      required: [items, total, page, pageSize]
      properties:
        items:
          type: array
          items: {}                     # El schema concreto se define con allOf al usar
          description: "Elementos de la página actual"
        total:
          type: integer
          minimum: 0
          description: "Total de elementos que cumplen los filtros"
          example: 150
        page:
          type: integer
          minimum: 1
          description: "Página actual (1-indexed)"
          example: 1
        pageSize:
          type: integer
          minimum: 1
          maximum: 100
          description: "Tamaño de página solicitado"
          example: 20
```

### 4.3. Uso en un endpoint concreto

```yaml
paths:
  /wells:
    get:
      operationId: listWells
      x-roles: [ADMIN, SUPERVISOR, OPERADOR, AUDITOR]
      summary: Listar pozos con paginación y filtros
      parameters:
        - $ref: '#/components/parameters/PageParam'
        - $ref: '#/components/parameters/PageSizeParam'
        - $ref: '#/components/parameters/SearchParam'
        - $ref: '#/components/parameters/SortByParam'
        - $ref: '#/components/parameters/SortDirParam'
        - name: status
          in: query
          required: false
          schema:
            type: string
            enum: [ACTIVE, INACTIVE, SUSPENDED, PLUGGED]
          description: "Filtrar por estado del pozo"
      responses:
        '200':
          description: Lista paginada de pozos
          content:
            application/json:
              schema:
                allOf:
                  - $ref: '#/components/schemas/PagedResponse'
                  - type: object
                    properties:
                      items:
                        type: array
                        items:
                          $ref: '#/components/schemas/WellListItem'
              example:
                items:
                  - id: "550e8400-e29b-41d4-a716-446655440000"
                    name: "Pozo Alpha"
                    uwi: "COL-001-ABCD"
                    status: "ACTIVE"
                    operatorName: "Ecopetrol S.A."
                total: 150
                page: 1
                pageSize: 20
```

---

## 5. ProblemDetails — RFC 7807

### 5.1. Schema único de error

Toda respuesta 4xx y 5xx usa `application/problem+json` con este schema:

```yaml
components:
  schemas:
    ProblemDetails:
      type: object
      required: [type, title, status, detail]
      properties:
        type:
          type: string
          format: uri
          description: "URI de referencia del tipo de error"
          example: "https://tools.ietf.org/html/rfc7807"
        title:
          type: string
          description: "Título corto y legible del error"
          example: "Validation Failed"
        status:
          type: integer
          description: "Código HTTP del error"
          example: 422
        detail:
          type: string
          description: "Descripción legible del error para el usuario"
          example: "Uno o más campos contienen errores de validación."
        instance:
          type: string
          description: "URI de la request que generó el error"
          example: "/api/v1/wells"
        errors:
          type: object
          additionalProperties:
            type: array
            items:
              type: string
          description: "Errores de validación por campo (solo en 422)"
          example:
            name: ["El nombre del pozo es requerido."]
            uwi: ["El UWI debe contener entre 10 y 20 caracteres."]
        traceId:
          type: string
          description: "ID de correlación para trazabilidad"
          example: "00-abc123def456-01"
```

### 5.2. Integración con el frontend

El `errorInterceptor` del frontend (ver CONSTITUTION.md §5) extrae el mensaje de error con esta prioridad:

```
error.error?.message ?? error.error?.detail ?? fallback
```

Por lo tanto, el campo `detail` de ProblemDetails es el que llega al toast del usuario. **Debe ser siempre legible y en español.**

### 5.3. Uso en respuestas de error

Toda response de error referencia el schema compartido:

```yaml
responses:
  '404':
    description: Recurso no encontrado
    content:
      application/problem+json:
        schema:
          $ref: '#/components/schemas/ProblemDetails'
        example:
          type: "https://tools.ietf.org/html/rfc7807"
          title: "Resource Not Found"
          status: 404
          detail: "No se encontró un pozo con el ID proporcionado."
          instance: "/api/v1/wells/550e8400-e29b-41d4-a716-446655440000"
  '422':
    description: Error de validación
    content:
      application/problem+json:
        schema:
          $ref: '#/components/schemas/ProblemDetails'
        example:
          type: "https://tools.ietf.org/html/rfc7807"
          title: "Validation Failed"
          status: 422
          detail: "Uno o más campos contienen errores de validación."
          instance: "/api/v1/wells"
          errors:
            name: ["El nombre del pozo es requerido."]
```

---

## 6. Tipos Compartidos

### 6.1. IDs como UUID

Todos los identificadores de entidad son UUID v4 en formato string:

```yaml
properties:
  id:
    type: string
    format: uuid
    example: "550e8400-e29b-41d4-a716-446655440000"
```

**Regla:** Nunca usar `integer` para IDs de entidad. Los IDs secuenciales se reservan para catálogos legacy. Ver CONSTITUTION.backend.md §7.3.

### 6.2. Fechas en ISO 8601

Toda fecha o timestamp usa `format: date-time` (ISO 8601 con timezone):

```yaml
properties:
  createdAt:
    type: string
    format: date-time
    example: "2024-11-15T14:30:00Z"
  reportDate:
    type: string
    format: date
    example: "2024-11-15"
```

| Caso | Format | Ejemplo |
|---|---|---|
| Timestamp con hora | `date-time` | `2024-11-15T14:30:00Z` |
| Solo fecha (sin hora) | `date` | `2024-11-15` |

**Regla:** Todos los timestamps se transmiten en UTC (`Z`). La conversión a zona horaria local (COT, UTC-5) es responsabilidad del frontend.

### 6.3. Enums como strings

Los enumerables se declaran como `string` con valores explícitos en `enum`:

```yaml
properties:
  status:
    type: string
    enum: [ACTIVE, INACTIVE, SUSPENDED, PLUGGED]
    example: "ACTIVE"
  role:
    type: string
    enum: [ADMIN, SUPERVISOR, OPERADOR, AUDITOR]
    example: "ADMIN"
```

**Regla:** Los valores de enum son `UPPER_SNAKE_CASE`. Nunca integers, nunca PascalCase. Alineado con CONSTITUTION.backend.md §7.3 (enums almacenados como `string`).

### 6.4. Booleanos explícitos

Los campos booleanos no usan `0`/`1` ni `"true"`/`"false"`. Siempre `type: boolean`:

```yaml
properties:
  isActive:
    type: boolean
    example: true
```

### 6.5. Nullable

Campos que pueden ser nulos usan la sintaxis de OpenAPI 3.1:

```yaml
properties:
  deletedAt:
    type: ["string", "null"]
    format: date-time
    example: null
```

---

## 7. Naming: Schemas, Properties, Paths

### 7.1. Tabla de convenciones

| Elemento | Convención | Ejemplo |
|---|---|---|
| Paths (URL segments) | kebab-case | `/wells`, `/audit-logs`, `/daily-reports` |
| Path parameters | camelCase | `{wellId}`, `{reportId}` |
| Query parameters | camelCase | `page`, `pageSize`, `sortBy`, `operatorId` |
| Schema names | PascalCase | `WellDetail`, `WellListItem`, `CreateWellRequest` |
| Schema properties | camelCase | `wellName`, `operatorId`, `createdAt` |
| operationId | camelCase, verbo+recurso | `listWells`, `createWell`, `getWell` |
| Enum values | UPPER_SNAKE_CASE | `ACTIVE`, `IN_PROGRESS`, `PLUGGED_AND_ABANDONED` |

### 7.2. Sufijos de schemas

| Tipo de schema | Sufijo | Ejemplo |
|---|---|---|
| Respuesta detalle (GET by ID) | `Detail` | `WellDetail` |
| Respuesta listado (GET colección) | `ListItem` | `WellListItem` |
| Request de creación (POST) | `CreateRequest` | `CreateWellRequest` |
| Request de actualización (PUT) | `UpdateRequest` | `UpdateWellRequest` |
| Request de patch parcial (PATCH) | `PatchRequest` | `PatchWellStatusRequest` |
| Respuesta paginada | `PagedResponse` (genérico) | — |
| Error | `ProblemDetails` (único) | — |

### 7.3. Relación wire format → stacks

El contrato define el formato **sobre el cable** (wire format). Cada stack transforma según su convención:

```
contract.yml (camelCase)  →  Backend C# (PascalCase via System.Text.Json)
     wellName             →       WellName (propiedad C#)
     operatorId           →       OperatorId

contract.yml (camelCase)  →  Frontend TS DTO (camelCase directo)
     wellName             →       wellName (propiedad TS)
     operatorId           →       operatorId
```

**Regla:** `System.Text.Json` se configura con `JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase` en el backend para que las propiedades PascalCase de C# se serialicen como camelCase, coincidiendo con el contrato.

El frontend define sus DTOs espejo al contrato. Los mappers (`well.mapper.ts`, ver CONSTITUTION.md §6.3) transforman del DTO al modelo de dominio frontend si la forma difiere.

---

## 8. Reglas de Request Body

### 8.1. POST — Creación

El request body incluye todos los campos requeridos para crear el recurso. No incluye `id` (lo genera el backend), ni campos de auditoría (`createdAt`, `createdBy`).

```yaml
CreateWellRequest:
  type: object
  required: [name, uwi, operatorId, wellType]
  properties:
    name:
      type: string
      maxLength: 200
      description: "Nombre del pozo"
      example: "Pozo Alpha"
    uwi:
      type: string
      pattern: "^[A-Z0-9\\-]{10,20}$"
      description: "Identificador Único de Pozo"
      example: "COL-001-ABCD"
    operatorId:
      type: integer
      description: "ID de la operadora"
      example: 5
    wellType:
      type: string
      enum: [EXPLORATORY, DEVELOPMENT, INJECTION]
      description: "Tipo de pozo"
      example: "EXPLORATORY"
```

### 8.2. PUT — Actualización completa

Incluye todos los campos editables. El `id` va en el path, no en el body.

```yaml
paths:
  /wells/{wellId}:
    put:
      operationId: updateWell
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/UpdateWellRequest'
```

### 8.3. PATCH — Actualización parcial

Solo incluye los campos que cambian. Cada campo es opcional:

```yaml
PatchWellStatusRequest:
  type: object
  properties:
    status:
      type: string
      enum: [ACTIVE, INACTIVE, SUSPENDED, PLUGGED]
      description: "Nuevo estado del pozo"
      example: "SUSPENDED"
  minProperties: 1       # Al menos un campo debe enviarse
```

### 8.4. Reglas de request body

| Regla | Descripción |
|---|---|
| `id` nunca en el body | El ID del recurso va en el path parameter, nunca duplicado en el body. |
| Campos de auditoría excluidos | `createdAt`, `createdBy`, `lastModifiedAt`, `lastModifiedBy` son de solo lectura — nunca en request bodies. |
| `tenantId` excluido | El tenant se resuelve del JWT. Nunca se envía en el body. |
| `required` explícito | Todo campo obligatorio debe estar en el array `required`. No depender de que "es obvio". |
| `maxLength` en todo string | Todo campo string declara `maxLength`. Sin strings sin límite. |
| `example` en todo campo | Cada propiedad tiene un `example` representativo. Ver §11. |

---

## 9. Respuestas HTTP Estándar

### 9.1. Tabla de status codes por operación

| Operación | Éxito | Content-Type éxito | Errores posibles |
|---|---|---|---|
| `GET` colección | `200 OK` | `application/json` | `401`, `403` |
| `GET` por ID | `200 OK` | `application/json` | `401`, `403`, `404` |
| `POST` crear | `201 Created` | `application/json` | `401`, `403`, `409`, `422` |
| `PUT` actualizar | `200 OK` | `application/json` | `401`, `403`, `404`, `409`, `422` |
| `PATCH` parcial | `200 OK` | `application/json` | `401`, `403`, `404`, `422` |
| `DELETE` eliminar | `204 No Content` | — | `401`, `403`, `404` |

### 9.2. Response de creación (201)

El `201 Created` retorna el recurso creado y un header `Location`:

```yaml
responses:
  '201':
    description: Recurso creado exitosamente
    headers:
      Location:
        schema:
          type: string
        description: "URI del recurso creado"
        example: "/api/v1/wells/550e8400-e29b-41d4-a716-446655440000"
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/WellDetail'
```

### 9.3. Errores comunes declarados en todo contrato

Estos errores aplican a todo endpoint autenticado y deben declararse:

```yaml
# Declarar como responses reutilizables
components:
  responses:
    Unauthorized:
      description: Token JWT ausente, expirado o inválido
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://tools.ietf.org/html/rfc7807"
            title: "Unauthorized"
            status: 401
            detail: "Token de autenticación requerido."

    Forbidden:
      description: El usuario no tiene el rol requerido
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://tools.ietf.org/html/rfc7807"
            title: "Forbidden"
            status: 403
            detail: "No tiene permisos para realizar esta acción."

    InternalServerError:
      description: Error interno del servidor
      content:
        application/problem+json:
          schema:
            $ref: '#/components/schemas/ProblemDetails'
          example:
            type: "https://tools.ietf.org/html/rfc7807"
            title: "Internal Server Error"
            status: 500
            detail: "Error interno del servidor."
```

Uso en endpoints:

```yaml
responses:
  '401':
    $ref: '#/components/responses/Unauthorized'
  '403':
    $ref: '#/components/responses/Forbidden'
  '500':
    $ref: '#/components/responses/InternalServerError'
```

---

## 10. Seguridad

### 10.1. Esquema global

Todo contrato declara `bearerAuth` como esquema de seguridad global:

```yaml
security:
  - bearerAuth: []

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
      description: "Token JWT obtenido en /api/v1/auth/login"
```

### 10.2. Endpoints públicos

Solo los endpoints explícitamente públicos anulan la seguridad global:

```yaml
paths:
  /auth/login:
    post:
      operationId: login
      security: []                  # Override: endpoint público
      x-roles: []                   # Sin restricción de rol
      summary: Autenticación de usuario
```

**Regla:** La lista de endpoints públicos está restringida a: `/auth/login` y `/health`. Todo lo demás requiere JWT. Ver CONSTITUTION.backend.md §8.6.

### 10.3. Roles del sistema

Los 4 roles son fijos y están alineados con las tres constituciones:

| Rol | Descripción | Referencia |
|---|---|---|
| `ADMIN` | Acceso total, gestión de usuarios | Definido en CONSTITUTION.backend.md §8.1 |
| `SUPERVISOR` | Aprobación de formas operativas | Usuarios mock en spec 001-auth §4 |
| `OPERADOR` | Carga de formas y reportes | Usuarios mock en spec 001-auth §4 |
| `AUDITOR` | Solo lectura, acceso a audit logs | Usuarios mock en spec 001-auth §4 |

---

## 11. Ejemplos Inline Obligatorios

### 11.1. Regla

**Todo schema, toda propiedad y toda response debe tener un `example` representativo.** Sin excepciones. Los ejemplos son la documentación más rápida para el desarrollador frontend y el generador de mocks.

### 11.2. Niveles de ejemplo

```yaml
# Nivel 1 — Ejemplo por propiedad (obligatorio)
properties:
  name:
    type: string
    example: "Pozo Alpha"

# Nivel 2 — Ejemplo completo del schema (obligatorio)
WellDetail:
  type: object
  properties:
    id:
      type: string
      format: uuid
      example: "550e8400-e29b-41d4-a716-446655440000"
    name:
      type: string
      example: "Pozo Alpha"
    status:
      type: string
      enum: [ACTIVE, INACTIVE, SUSPENDED, PLUGGED]
      example: "ACTIVE"
  example:
    id: "550e8400-e29b-41d4-a716-446655440000"
    name: "Pozo Alpha"
    status: "ACTIVE"

# Nivel 3 — Ejemplo en la response (obligatorio)
responses:
  '200':
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/WellDetail'
        example:
          id: "550e8400-e29b-41d4-a716-446655440000"
          name: "Pozo Alpha"
          status: "ACTIVE"
```

### 11.3. Datos de ejemplo consistentes

Los contratos de una misma feature deben usar datos de ejemplo consistentes entre sí. Si el `CreateWellRequest` usa `"Pozo Alpha"`, la response de `createWell` y el detalle de `getWell` deben reflejarlo.

Usar los siguientes datos semilla estándar como base:

| Entidad | ID ejemplo | Nombre ejemplo |
|---|---|---|
| Well | `550e8400-e29b-41d4-a716-446655440000` | Pozo Alpha |
| Operator | `1` (legacy int) | Ecopetrol S.A. |
| User | `660e8400-e29b-41d4-a716-446655440000` | Juan Pérez |

---

## 12. Versionado de API y Evolución

### 12.1. Reglas de versionado

| Regla | Descripción |
|---|---|
| Prefijo fijo | Todos los endpoints bajo `/api/v1/`. |
| Estrategia URL path | Versión en la URL, no en headers ni query params. |
| Cambio breaking → nueva versión | Si un endpoint publicado necesita un cambio breaking, se crea `/api/v2/{recurso}`. |
| Contrato inmutable por iteración | Una vez aprobado el `contract.yml`, es inmutable para esa iteración. |

### 12.2. Cambios NO breaking (permitidos sin nueva versión)

- Agregar un campo **opcional** a un request body
- Agregar un campo a un response body (el frontend ignora campos desconocidos)
- Agregar un nuevo endpoint
- Agregar un nuevo valor a un enum **si el frontend lo maneja con fallback**
- Agregar un parámetro de query **opcional**

### 12.3. Cambios breaking (requieren nueva versión o nuevo contrato)

- Eliminar o renombrar un campo de response
- Eliminar o renombrar un endpoint
- Cambiar el tipo de un campo existente
- Hacer obligatorio un campo que era opcional en el request
- Eliminar un valor de un enum existente
- Cambiar el significado semántico de un campo

### 12.4. Versionado del contrato vs. de la API

| Concepto | Ejemplo | Cuándo cambia |
|---|---|---|
| `info.version` del contrato | `1.0.0` → `1.1.0` | Cada vez que el contrato cambia post-aprobación |
| Versión del path de la API | `/api/v1/` → `/api/v2/` | Solo ante cambios breaking en endpoints ya en producción |

---

## 13. Relación con el Pipeline SDD

### 13.1. Posición en el flujo

```
spec.md ──(aprobado)──→ contract.yml ──(aprobado)──→ plan.fe.md + plan.be.md ──→ tasks.*.md
```

El `contract.yml` es el **puente** entre la especificación funcional (qué) y los planes de implementación (cómo). Se produce después del `spec.md` y antes de los planes.

### 13.2. Trazabilidad spec → contrato

Cada endpoint del contrato debe poder rastrearse a una historia de usuario del `spec.md`:

```yaml
paths:
  /wells:
    post:
      operationId: createWell
      description: |
        Crea un nuevo pozo en el sistema.
        Referencia: spec.md HU-010, Escenario 1 (Creación exitosa)
```

### 13.3. Consistencia contrato → planes

| Dirección | Regla |
|---|---|
| Contrato → `plan.be.md` | Cada endpoint tiene al menos un controller action + command/query en el plan backend |
| Contrato → `plan.fe.md` | Cada endpoint tiene al menos un método de servicio + DTO en el plan frontend |
| Contrato → `tasks.be.md` | Cada controller action es una tarea atómica |
| Contrato → `tasks.fe.md` | Cada service method es una tarea atómica |

### 13.4. Lo que el contrato NO define

| Aspecto | Dónde se define |
|---|---|
| Estructura de tablas/columnas de la DB | `plan.be.md` (EF Core configurations) |
| Componentes Angular o estado NgRx | `plan.fe.md` |
| Lógica interna de validación del handler | `plan.be.md` (FluentValidation rules) |
| Reglas de negocio complejas | `spec.md` (reglas de negocio) |
| Estructura de carpetas del proyecto | `plan.fe.md` / `plan.be.md` |

---

## 14. Validación y Checklist Pre-Aprobación

### 14.1. Validación técnica

El `contract.yml` debe pasar validación con herramientas estándar:

```bash
# Validar sintaxis OpenAPI 3.1
npx @redocly/cli lint specs/features/NNN-nombre/contract.yml
```

### 14.2. Checklist de revisión

Antes de aprobar un `contract.yml`, verificar:

**Estructura:**
- [ ] `openapi: 3.1.0` declarado
- [ ] `info.title` incluye ID de feature y nombre
- [ ] `servers` con base path `/api/v1`
- [ ] `security` global con `bearerAuth`
- [ ] `securitySchemes.bearerAuth` declarado en components

**Endpoints:**
- [ ] Paths en kebab-case
- [ ] Cada operación tiene `operationId` único (verbo + recurso, camelCase)
- [ ] Cada operación tiene `x-roles` declarado
- [ ] Cada operación tiene `summary` descriptivo
- [ ] Endpoints públicos tienen `security: []` explícito

**Schemas:**
- [ ] Nombres en PascalCase con sufijo correcto (`Detail`, `ListItem`, `CreateRequest`)
- [ ] Propiedades en camelCase
- [ ] IDs son `format: uuid`
- [ ] Fechas son `format: date-time` o `format: date`
- [ ] Enums son `string` con valores `UPPER_SNAKE_CASE`
- [ ] Strings tienen `maxLength`
- [ ] Campos obligatorios en array `required`
- [ ] Ejemplos en toda propiedad, schema y response

**Paginación:**
- [ ] Listados usan `PageParam` y `PageSizeParam`
- [ ] Response usa `PagedResponse` con `items`, `total`, `page`, `pageSize`

**Errores:**
- [ ] Toda response de error usa `application/problem+json`
- [ ] Schema `ProblemDetails` declarado
- [ ] `401`, `403` declarados en todo endpoint autenticado
- [ ] `404` declarado en todo endpoint con path param de ID
- [ ] `422` declarado en todo endpoint con request body
- [ ] Mensajes de `detail` en español

**Trazabilidad:**
- [ ] Cada endpoint referencia una HU del `spec.md`
- [ ] No hay endpoints huérfanos (sin HU correspondiente)
- [ ] No hay HUs del spec sin endpoint (salvo las que son solo frontend/UI)

---

## 15. Reglas Inmutables — Resumen Ejecutivo

1. **OpenAPI 3.1.0.** Sin excepciones.
2. **Un `contract.yml` por feature.** Ubicado en `specs/features/NNN-nombre/`.
3. **Paths en kebab-case.** Properties en camelCase. Schemas en PascalCase. Enums en UPPER_SNAKE_CASE.
4. **`operationId` = verbo + recurso** en camelCase. Único por contrato.
5. **Paginación estándar.** `page` (1-indexed), `pageSize` (max 100). Response: `items`, `total`, `page`, `pageSize`.
6. **ProblemDetails RFC 7807** para todo error. Content-Type `application/problem+json`. Mensajes en español.
7. **JWT Bearer global.** Solo login y health son públicos. `x-roles` en cada endpoint.
8. **IDs = UUID.** Fechas = ISO 8601 UTC. Enums = strings.
9. **Ejemplos obligatorios** en toda propiedad, schema y response.
10. **Contrato inmutable** una vez aprobado. Cambios breaking = versión nueva.
11. **Validación técnica** con linter OpenAPI antes de aprobar.
12. **Trazabilidad bidireccional** spec ↔ contrato ↔ planes ↔ tareas.
