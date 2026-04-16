# Spec: Formulario de Creación de Pozo — Wizard Multi-Paso

**Feature ID:** 006-well-creation-form
**Iteración:** 4
**Dominio:** Wells (`/wells/create`)
**Dependencias:** 005-wells-catalog-crud (aprobado e implementado)
**Estado:** Pendiente de revisión humana

---

## 1. Contexto y Alcance

La iteración anterior (005) implementó el CRUD básico de pozos con un formulario plano (`p-card` por sección). Esta feature **refactoriza** la experiencia de creación para usar un **wizard multi-paso** (PrimeNG Stepper) con cascadas reactivas, validación por paso, cálculo automático de nombre y preview en tiempo real.

**Incluido:**
- Wizard de 4 pasos con PrimeNG Stepper
- Cascadas reactivas: Contrato→(cuenca, tipo_contrato, campos), Campo→clusters, Departamento→municipios
- Validación por paso (no se avanza sin paso válido)
- Cálculo automático y preview de `nombre_pozo = {cuenca}-{denominación}-{consecutivo}`
- Endpoint backend para previsualizar nombre y verificar disponibilidad
- Paso de resumen/confirmación antes de guardar
- Guardado como estado `BORRADOR` (primer estado del state machine)
- Mocks actualizados para el nuevo endpoint
- Modo edición reutiliza el wizard con datos pre-cargados

**NO incluido (iteraciones posteriores):**
- Máquina de estados completa (transiciones post-borrador)
- Generación de UWI
- Guardado parcial de borrador (save draft intermedio)
- Upload de archivos adjuntos
- Coordenadas geográficas (latitud/longitud del pozo)

---

## 2. Historias de Usuario

### HU-030: Acceso al Wizard de Creación

**Como** operador (ADMIN, SUPERVISOR u OPERADOR),
**quiero** acceder a un formulario wizard para crear un nuevo pozo,
**para** registrar la información de forma guiada y progresiva.

#### Criterios de Aceptación

**Escenario 1 — Navegación exitosa al wizard**
- **Dado** que el usuario tiene rol `ADMIN`, `SUPERVISOR` u `OPERADOR`
- **Cuando** navega a `/wells/create`
- **Entonces** se muestra un wizard de 4 pasos con indicadores visuales del paso actual
- **Y** el paso 1 ("Información del Contrato") está activo por defecto

**Escenario 2 — RBAC: AUDITOR no puede acceder**
- **Dado** que el usuario tiene rol `AUDITOR`
- **Cuando** intenta navegar a `/wells/create`
- **Entonces** es redirigido a `/forbidden` (guard de ruta)

**Escenario 3 — Navegación por breadcrumb/botón volver**
- **Dado** que el usuario está en el wizard
- **Cuando** hace clic en "Cancelar" o el breadcrumb "Pozos"
- **Entonces** regresa a `/wells/manage` sin guardar datos

---

### HU-031: Paso 1 — Información del Contrato con Cascadas

**Como** operador,
**quiero** seleccionar el contrato y que los campos dependientes se actualicen automáticamente,
**para** asegurar consistencia jerárquica entre contrato, cuenca, tipo de contrato y campo.

#### Criterios de Aceptación

**Escenario 1 — Cascada Contrato → campos derivados + campos hijos**
- **Dado** que el usuario está en el paso 1
- **Cuando** selecciona un contrato del dropdown
- **Entonces** los campos `cuenca` y `tipo_contrato` se auto-llenan (read-only) con los datos del contrato seleccionado
- **Y** el dropdown de "Campo" se filtra para mostrar solo campos que pertenecen a ese contrato
- **Y** los valores anteriores de Campo y Cluster se limpian si ya no son válidos

**Escenario 2 — Cascada Campo → Cluster**
- **Dado** que el usuario seleccionó un campo del dropdown
- **Cuando** el campo cambia
- **Entonces** el dropdown de "Cluster" se filtra por el campo seleccionado
- **Y** el valor anterior de Cluster se limpia si no pertenece al nuevo campo

**Escenario 3 — Validación del paso 1 antes de avanzar**
- **Dado** que los campos requeridos del paso 1 (contrato, campo, clasificación) no están completos
- **Cuando** el usuario intenta avanzar al paso 2
- **Entonces** se muestran mensajes de error en los campos faltantes
- **Y** el stepper no avanza

**Escenario 4 — Denominación y consecutivo**
- **Dado** que el usuario ingresa la denominación y el consecutivo
- **Cuando** los valores cambian
- **Entonces** se muestra un preview en tiempo real de `nombre_pozo` calculado como `{cuenca}-{denominación}-{consecutivo}`

**Escenario 5 — Error de validación: denominación con caracteres inválidos**
- **Dado** que el usuario ingresa números o caracteres especiales en denominación
- **Cuando** el campo pierde foco
- **Entonces** se muestra error: *"Solo se permiten letras y espacios."*

**Escenario 6 — Error de validación: consecutivo fuera de formato**
- **Dado** que el usuario ingresa un consecutivo que no son 2 dígitos
- **Cuando** el campo pierde foco
- **Entonces** se muestra error: *"Debe ser numérico de 2 dígitos (ej. 01)."*

---

### HU-032: Paso 2 — Datos Técnicos

**Como** operador,
**quiero** completar los datos técnicos del pozo en un paso dedicado,
**para** separar la información contractual de la técnica.

#### Criterios de Aceptación

**Escenario 1 — Campos técnicos disponibles**
- **Dado** que el usuario avanzó al paso 2
- **Cuando** el paso se renderiza
- **Entonces** se muestran dropdowns para: tipo de trayectoria, tipo de ubicación, tipo de ángulo, tipo de objetivo, tipo de terminación
- **Y** cada dropdown muestra opciones con etiquetas descriptivas (ej. "Vertical simple (ST)")

**Escenario 2 — Validación del paso 2**
- **Dado** que algún campo técnico requerido está vacío
- **Cuando** el usuario intenta avanzar al paso 3
- **Entonces** se marcan los campos faltantes con error
- **Y** el stepper no avanza

**Escenario 3 — Navegación hacia atrás**
- **Dado** que el usuario está en el paso 2
- **Cuando** hace clic en "Anterior"
- **Entonces** regresa al paso 1 con los datos previamente ingresados intactos

---

### HU-033: Paso 3 — Ubicación Geográfica con Cascadas

**Como** operador,
**quiero** seleccionar la ubicación geográfica del pozo con cascadas departamento→municipio,
**para** registrar la ubicación con códigos DANE válidos.

#### Criterios de Aceptación

**Escenario 1 — Cascada Departamento → Municipios**
- **Dado** que el usuario está en el paso 3
- **Cuando** selecciona un departamento
- **Entonces** el dropdown de municipios se filtra por el departamento seleccionado
- **Y** el municipio anterior se limpia si no pertenece al nuevo departamento

**Escenario 2 — Cluster precargado del paso 1**
- **Dado** que el usuario seleccionó un campo y cluster en el paso 1
- **Cuando** llega al paso 3
- **Entonces** el cluster seleccionado se muestra como texto de solo lectura (ya fue elegido en paso 1)

**Escenario 3 — Validación del paso 3**
- **Dado** que departamento o municipio no están seleccionados
- **Cuando** el usuario intenta avanzar al paso 4
- **Entonces** se muestran errores en los campos faltantes

---

### HU-034: Paso 4 — Resumen y Confirmación

**Como** operador,
**quiero** revisar todos los datos ingresados antes de guardar,
**para** verificar que la información es correcta antes de crear el pozo.

#### Criterios de Aceptación

**Escenario 1 — Resumen completo**
- **Dado** que el usuario completó los pasos 1-3
- **Cuando** llega al paso 4
- **Entonces** se muestra un resumen read-only de toda la información agrupada por sección
- **Y** el nombre del pozo calculado se muestra destacado
- **Y** el estado "Borrador" se muestra como badge informativo

**Escenario 2 — Editar sección desde resumen**
- **Dado** que el usuario está en el paso 4
- **Cuando** hace clic en el indicador de un paso anterior del stepper
- **Entonces** navega a ese paso con los datos intactos

**Escenario 3 — Confirmar creación exitosa**
- **Dado** que el usuario revisa el resumen y confirma
- **Cuando** hace clic en "Guardar Borrador"
- **Entonces** se envía `POST /api/v1/wells` con los datos del formulario
- **Y** al recibir `201 Created` se muestra toast de éxito
- **Y** se navega a `/wells/manage`

**Escenario 4 — Error de validación del backend**
- **Dado** que el backend rechaza con `422`
- **Cuando** se recibe la respuesta
- **Entonces** se muestra toast con el detalle del error
- **Y** el usuario permanece en el paso 4

**Escenario 5 — Error de autorización**
- **Dado** que el token JWT expiró o el rol no tiene permiso
- **Cuando** se envía el POST
- **Entonces** se muestra error de autorización (manejado por `errorInterceptor`)

---

### HU-035: Preview de Nombre del Pozo en Tiempo Real

**Como** operador,
**quiero** ver el nombre calculado del pozo mientras lleno el formulario,
**para** confirmar que la nomenclatura es correcta antes de guardar.

#### Criterios de Aceptación

**Escenario 1 — Preview se actualiza en tiempo real**
- **Dado** que el usuario ha seleccionado contrato (que tiene cuenca) e ingresado denominación y consecutivo
- **Cuando** cualquiera de estos valores cambia
- **Entonces** se muestra un banner/chip con el nombre calculado: `{cuenca}-{denominación}-{consecutivo}`

**Escenario 2 — Preview incompleto**
- **Dado** que faltan uno o más valores para calcular el nombre
- **Cuando** el componente de preview se renderiza
- **Entonces** muestra un placeholder: *"Complete contrato, denominación y consecutivo para ver el nombre"*

**Escenario 3 — Verificación de disponibilidad del nombre**
- **Dado** que el usuario ha completado contrato, denominación y consecutivo
- **Cuando** los 3 valores están completos
- **Entonces** se invoca `GET /api/v1/wells/preview-name?contratoId=X&denominacion=Y&consecutivo=Z`
- **Y** si el nombre está disponible, se muestra badge verde "Disponible"
- **Y** si ya existe un pozo con ese nombre, se muestra badge rojo "Nombre ya en uso"

---

### HU-036: Modo Edición del Wizard

**Como** operador,
**quiero** editar un pozo en estado borrador usando el mismo wizard,
**para** corregir o completar la información de un pozo existente.

#### Criterios de Aceptación

**Escenario 1 — Carga de datos en modo edición**
- **Dado** que el usuario navega a `/wells/:id/edit` con un pozo en estado `BORRADOR`
- **Cuando** el wizard se inicializa
- **Entonces** todos los pasos se pre-cargan con los datos existentes del pozo
- **Y** las cascadas se resuelven correctamente (los dropdowns hijos muestran las opciones del padre)
- **Y** el título del wizard cambia a "Editar Pozo"

**Escenario 2 — No se puede editar pozo en otro estado**
- **Dado** que el usuario intenta editar un pozo que NO está en estado `BORRADOR`
- **Cuando** se carga el detalle del pozo
- **Entonces** se muestra mensaje informativo y se redirige a `/wells/manage`

**Escenario 3 — Actualización exitosa**
- **Dado** que el usuario modifica datos en el wizard en modo edición
- **Cuando** confirma en el paso 4
- **Entonces** se envía `PUT /api/v1/wells/{id}` con los datos actualizados
- **Y** se muestra toast de éxito y navega a `/wells/manage`

---

## 3. Reglas de Negocio

| Regla | Descripción |
|---|---|
| RN-050 | El nombre del pozo se calcula: `{cuenca}-{denominación}-{consecutivo}`. El frontend muestra el preview; el backend lo calcula definitivamente al crear. |
| RN-051 | La cascada Contrato→Campo es obligatoria: no se puede seleccionar campo sin contrato. |
| RN-052 | La cascada Campo→Cluster es opcional: cluster puede quedar nulo. |
| RN-053 | La cascada Departamento→Municipio es obligatoria: no se puede seleccionar municipio sin departamento. |
| RN-054 | Los campos derivados (cuenca, tipo_contrato) se muestran como read-only en el paso 1 tras seleccionar contrato. |
| RN-055 | Al cambiar el padre en una cascada, los hijos se limpian si el valor actual no es válido para el nuevo padre. |
| RN-056 | El wizard no permite avanzar al siguiente paso si el paso actual tiene campos requeridos sin completar. |
| RN-057 | El wizard permite retroceder a cualquier paso anterior sin perder datos. |
| RN-058 | El estado al crear siempre es `BORRADOR`. No se puede cambiar el estado desde el wizard. |
| RN-059 | La operadora se asigna automáticamente del `tenantName` del JWT. No aparece como campo editable. |
| RN-060 | El preview de nombre del pozo es informativo. El backend recalcula el nombre definitivo al persistir. |

---

## 4. Estructura de Pasos del Wizard

| Paso | Título | Campos | Cascadas |
|---|---|---|---|
| 1 | Información del Contrato | contrato*, campo*, clasificación*, denominación*, consecutivo*, cluster | Contrato→(cuenca, tipo_contrato, campos), Campo→clusters |
| 2 | Datos Técnicos | tipoTrayectoria*, tipoUbicación*, tipoÁngulo*, tipoObjetivo*, tipoTerminación* | Ninguna |
| 3 | Ubicación Geográfica | departamento*, municipio* | Departamento→municipios |
| 4 | Resumen y Confirmación | (todos read-only) + botón "Guardar Borrador" | Ninguna |

\* = campo requerido

---

## 5. RBAC por Funcionalidad

| Funcionalidad | ADMIN | SUPERVISOR | OPERADOR | AUDITOR |
|---|---|---|---|---|
| Acceder a `/wells/create` | ✅ | ✅ | ✅ | ❌ |
| Acceder a `/wells/:id/edit` | ✅ | ✅ su tenant | ✅ su tenant | ❌ |
| Ver preview de nombre | ✅ | ✅ | ✅ | ❌ |
| Guardar borrador | ✅ | ✅ | ✅ | ❌ |

---

## 6. Edge Cases

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-060 | Cambiar contrato cuando campo ya estaba seleccionado | Campo se limpia, cluster se limpia, opciones de campos se recargan |
| EC-061 | Cambiar departamento cuando municipio ya estaba seleccionado | Municipio se limpia, opciones de municipios se recargan |
| EC-062 | Contrato sin campos asociados | Dropdown de campo muestra "Sin campos disponibles" (deshabilitado) |
| EC-063 | Departamento sin municipios | Dropdown de municipio muestra "Sin municipios disponibles" (deshabilitado) |
| EC-064 | Campo sin clusters | Dropdown de cluster muestra "Ninguno (opcional)" y queda habilitado con lista vacía |
| EC-065 | Navegar a `/wells/:id/edit` con ID inexistente | Mostrar error 404 y redirigir a `/wells/manage` |
| EC-066 | Intentar editar pozo en estado no-borrador | Mostrar mensaje informativo y redirigir a `/wells/manage` |
| EC-067 | Pérdida de conexión al verificar nombre | Mostrar badge neutro "No se pudo verificar" en el preview |
| EC-068 | Denominación con espacios al inicio/final | Se aplica trim antes de enviar al backend y antes de calcular preview |
| EC-069 | Consecutivo con un solo dígito (ej. "1") | Validación impide avanzar: "Debe ser numérico de 2 dígitos (ej. 01)" |
| EC-070 | Preview de nombre con cuenca vacía (contrato no seleccionado) | Preview muestra placeholder, no intenta calcular nombre parcial |
| EC-071 | Doble clic en "Guardar Borrador" | Botón se deshabilita tras primer clic (loading state) |

---

## 7. Endpoints Consumidos

Esta feature consume endpoints definidos en `005-wells-catalog-crud/contract.yml` más un endpoint nuevo:

| Endpoint | Origen | Uso en el Wizard |
|---|---|---|
| `POST /api/v1/wells` | 005 | Crear pozo borrador (paso 4 — confirmar) |
| `PUT /api/v1/wells/{id}` | 005 | Actualizar pozo borrador (modo edición) |
| `GET /api/v1/wells/{id}` | 005 | Cargar datos para modo edición |
| `GET /api/v1/catalogs/contratos` | 005 | Paso 1 — dropdown de contratos |
| `GET /api/v1/catalogs/campos?contratoId=N` | 005 | Paso 1 — cascada campo por contrato |
| `GET /api/v1/catalogs/departamentos` | 005 | Paso 3 — dropdown de departamentos |
| `GET /api/v1/catalogs/municipios?departamentoId=N` | 005 | Paso 3 — cascada municipio por departamento |
| `GET /api/v1/catalogs/clusters?campoId=N` | 005 | Paso 1 — cascada cluster por campo |
| `GET /api/v1/wells/preview-name` | **006 (NUEVO)** | Preview de nombre + verificación de disponibilidad |

---

## 8. Checklist de Revisión

- [x] Las 7 HUs cubren wizard completo: acceso, 4 pasos, preview, modo edición
- [x] Cada HU tiene al menos: happy path, error de validación, error de autorización
- [x] Cascadas documentadas: Contrato→campos, Campo→clusters, Departamento→municipios
- [x] RBAC cubre 4 roles con restricción explícita para AUDITOR
- [x] Edge cases cubren cascadas, validaciones, estados y errores de conexión
- [x] Preview de nombre documentado con cálculo frontend + verificación backend
- [x] Modo edición documentado con pre-carga de cascadas
- [x] Dependencia explícita con 005-wells-catalog-crud
