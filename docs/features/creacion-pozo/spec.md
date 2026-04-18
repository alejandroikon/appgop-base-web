# Spec: RQF_GOP_01 — Creación de Pozo Nuevo (V2.0 Definitivo)

**Feature ID:** RQF_GOP_01
**Versión:** 2.0 (31-mar-2026)
**Dominio:** Wells (`/wells/create`)
**Ruta SDD:** `docs/features/creacion-pozo/`
**Fuente oficial:** `docs/source/RQF_GOP_01_CreacionPozoNuevo_V2.0.pdf`
**Instructivo UWI:** `docs/source/INSTRUCTIVO_UWI_03032026.pdf`
**Supersede:** specs/features/005-wells-catalog-crud, 006-well-creation-form, 007-well-state-machine
**Estado:** PENDIENTE REVISIÓN HUMANA

---

## 1. Contexto y Alcance

### 1.1. Contexto regulatorio

La Agencia Nacional de Hidrocarburos (ANH) de Colombia exige que a partir del **1 de junio de 2026** la generación del UWI (Unique Well Identifier) Fiscalizado se realice **exclusivamente** a través del sistema GOP 360. El UWI sigue la metodología PPDM (Petroleum Public Data Model).

### 1.2. Alcance de esta iteración

| Incluido | Excluido |
|----------|----------|
| Formulario de creación de pozo nuevo (página única con scroll) | Creación de Pozo Antiguo/Perforado (RQF_GOP_02) |
| Algoritmo PPDM completo para UWI Fiscalizado | Aprobación ANH del UWI |
| Guardado de borrador y finalización de registro | Flujo de Forma 101 |
| 40 reglas de negocio consolidadas | Coordenadas geográficas (lat/lng) |
| Edición y eliminación condicional (pre-Forma 101) | Upload de archivos adjuntos |
| Integración con catálogos SOLAR/DANE | Integración real con Mapa de Tierras (PA-02) |
| Validación de unicidad de UWI y nombre | Notificaciones push |

### 1.3. Relación con módulo existente

El código actual en `src/app/domains/wells/` (iteraciones 005–007) es un prototipo **pre-UWI-fiscalizado** con:

- Formato UWI simplificado: `CO-{daneDpto}-{daneMpio}-{denominacion}-{consecutivo}-{trayectoria}`
- Wizard multi-paso (4 pasos) en lugar de formulario único con scroll
- Máquina de 4 estados (BORRADOR → PENDING_UWI → READY_FISCAL → FISCALIZADO)
- 4 tipos de objetivo (faltan C, GT, O)
- Sin sub-clasificaciones de Exploratorio

**Esta especificación reemplaza completamente** las specs 005, 006 y 007 para efectos de la nueva implementación. Ver `migration-plan.md`.

---

## 2. Roles y Permisos

### 2.1. Mapeo de roles V2.0 → Sistema

| Rol V2.0 | Origen | Rol sistema | Permisos en este módulo |
|-----------|--------|-------------|------------------------|
| Agente | Operadora | `OPERADOR` | Crear, Editar, Eliminar |
| Coordinador | Operadora | `SUPERVISOR` | Consultar, Editar |
| Admin GOP | ANH | `ADMIN` | Total (acceso cross-tenant) |
| — | ANH | `AUDITOR` | Consultar (solo lectura) |

### 2.2. Restricciones por rol

| Acción | OPERADOR | SUPERVISOR | ADMIN | AUDITOR |
|--------|----------|------------|-------|---------|
| Crear pozo | ✅ | ❌ | ✅ | ❌ |
| Editar pozo (pre-Forma101) | ✅ | ✅ | ✅ | ❌ |
| Eliminar pozo (pre-Forma101) | ✅ | ❌ | ✅ | ❌ |
| Consultar pozo | ✅ | ✅ | ✅ | ✅ |
| Listar pozos | ✅ (tenant) | ✅ (tenant) | ✅ (all) | ✅ (all) |

> **RN-15 (especial):** Cuando la operadora es ANH (el ADMIN actúa como operador), solo puede crear pozos con Clasificación = Estratigráfico.

---

## 3. Estructura del Formulario

**Layout:** Página única con scroll continuo (NO wizard). Dividido en 3 secciones + 1 panel inferior.

**Total campos:** 17 visibles + 1 calculado (UWI) = 18.

### 3.1. Sección 1 — Información General del Contrato

| # | Campo | Tipo | Editable | Obligatorio | Origen | Regla |
|---|-------|------|----------|-------------|--------|-------|
| 1 | Operadora / ANH | Texto | No | Auto | Sesión | RN-01 |
| 2 | Contrato | Dropdown | Sí | Sí | SOLAR / VPAA | RN-02 |
| 3 | Tipo de Contrato | Texto | No | Auto | Contrato sel. | RN-03 |
| 4 | Cuenca | Texto | No | Auto | Contrato sel. | RN-04 |

### 3.2. Sección 2 — Detalles Técnicos del Pozo

| # | Campo | Tipo | Editable | Obligatorio | Regla |
|---|-------|------|----------|-------------|-------|
| 5 | Tipo de Pozo por Trayectoria | Dropdown | Sí | Sí | RN-05 |
| 6 | Clasificación Inicial | Dropdown (2 niveles) | Sí | Sí | RN-06, RN-15 |
| 7 | Campo | Dropdown / Auto | Sí | Condicional | RN-11..RN-14 |
| 8 | Denominación del Pozo | Texto | Sí | Sí | RN-07 |
| 9 | Consecutivo | Numérico | Sí | Sí | RN-08 |
| 10 | Nombre del Pozo (generado) | Texto | No | Auto | RN-16..RN-22 |
| 11 | Tipo de Pozo por Ubicación | Auto / Dropdown | Condicional | Sí | RN-09 |
| 12 | Tipo de Pozo por Ángulo | Dropdown | Sí | Sí | RN-10 |
| 13 | Tipo de Pozo por Objetivo | Dropdown | Sí | Sí | RN-24 |
| 14 | Tipo de Pozo por Terminación | Dropdown | Sí | Sí | RN-23 |

### 3.3. Sección 3 — Ubicación Geográfica

| # | Campo | Tipo | Editable | Obligatorio | Regla |
|---|-------|------|----------|-------------|-------|
| 15 | Departamento | Dropdown | Sí | Sí | RN-25 |
| 16 | Código DANE Departamento | Numérico 2 díg | No | Auto | RN-28 |
| 17 | Municipio | Dropdown | Sí | Sí | RN-26 |
| 18 | Código DANE Municipio | Numérico 3 díg | No | Auto | RN-29 |
| 19 | Cluster-Locación | Dropdown + Crear | Sí | Sí | RN-32 |

### 3.4. Panel Inferior — UWI Fiscalizado

| # | Campo | Tipo | Editable | Regla |
|---|-------|------|----------|-------|
| 20 | UWI Fiscalizado | Texto calculado | No | RN-27..RN-38 |

**Requisito visual:** Panel destacado visualmente (fondo diferenciado, tipografía monoespaciada para el UWI). Se actualiza en tiempo real conforme el usuario llena campos.

### 3.5. Acciones del formulario

| Acción | Comportamiento | Validación |
|--------|---------------|------------|
| Guardar Borrador | Persiste estado parcial. Al menos 1 campo llenado. | Mínima (solo presencia de al menos 1 campo) |
| Finalizar Registro | Valida todos los campos, genera UWI definitivo, verifica unicidad. | Completa (todos los RN aplican) |

---

## 4. Reglas de Negocio (RN-01 a RN-40)

### Sección 1 — Información General

| ID | Regla | Validación |
|----|-------|------------|
| **RN-01** | **Operadora** se auto-llena con el nombre del tenant de la sesión del usuario. No es editable. | Backend: `tenantName` del JWT. |
| **RN-02** | **Contrato** se obtiene de SOLAR (operadoras regulares) o VPAA (ANH para contratos estratigráficos). Lista filtrada por operadora. | Backend: catálogo filtrado por `tenantId`. |
| **RN-03** | **Tipo de Contrato** se auto-completa al seleccionar Contrato. No editable. Valores: E&P, TEA, Convenio E&P, Asociación, etc. | Derivado de FK contrato. |
| **RN-04** | **Cuenca** se auto-completa al seleccionar Contrato. No editable. | Derivado de FK contrato. |

### Sección 2 — Detalles Técnicos

| ID | Regla | Validación |
|----|-------|------------|
| **RN-05** | **Tipo de Pozo por Trayectoria** es obligatorio. Valores: `ST` (Side Track), `P` (Piloto), `PR` (Profundización), `ML` (Multilateral), `G` (Gemelo), `O` (Original). | Enum cerrado. |
| **RN-06** | **Clasificación Inicial** es obligatoria. Estructura jerárquica: Exploratorio → sublista {A3, A2a, A2b, A2c, A1}; Desarrollo (sin sublista); Estratigráfico (sin sublista). | Valor principal + subclasificación si Exploratorio. |
| **RN-07** | **Denominación del Pozo**: solo letras (A-Z, a-z), espacios y guiones. Máximo 50 caracteres. Se convierte a MAYÚSCULAS al guardar. | Regex: `^[A-Za-z\s\-]{1,50}$`, normalizado a uppercase. |
| **RN-08** | **Consecutivo**: número entero positivo. Sin ceros a la izquierda en la entrada del usuario. Se aplica zero-padding a 4 dígitos para el UWI. | Rango: 1–9999. |
| **RN-09** | **Tipo de Pozo por Ubicación**: `CONTINENTAL` o `COSTA_FUERA`. Se pre-selecciona según el contrato (campo derivado de contrato). Editable si el contrato aplica a ambos. | Enum cerrado. |
| **RN-10** | **Tipo de Pozo por Ángulo** es obligatorio. Valores: `H` (Horizontal), `V` (Vertical), `D` (Desviado). | Enum cerrado. |
| **RN-11** | Si **Clasificación = Exploratorio**, el campo **Campo** es opcional (el pozo puede estar en área no delimitada). | Campo nullable. |
| **RN-12** | Si **Clasificación = Desarrollo**, el campo **Campo** es **obligatorio**. Se filtra por catálogo de campos del contrato seleccionado. | Not null + FK válida. |
| **RN-13** | Si **Clasificación = Estratigráfico**, el campo **Campo** es opcional. | Campo nullable. |
| **RN-14** | Cambiar **Clasificación** limpia el valor de **Campo** previamente seleccionado. | Frontend: reset del dropdown Campo. |
| **RN-15** | Si la **operadora es ANH**, solo puede seleccionar **Clasificación = Estratigráfico**. Las opciones Exploratorio y Desarrollo se deshabilitan. | Backend: validación + Frontend: filtrado visual. |
| **RN-16** | **Nombre del Pozo** se genera automáticamente: `{Campo o Área}-{Denominación}-{Consecutivo}`. | Calculado, no editable. |
| **RN-17** | Si no hay **Campo** seleccionado (Exploratorio sin campo), usar el nombre del **Contrato** como prefijo del Nombre del Pozo. | Fallback a nombre de contrato. |
| **RN-18** | **Nombre del Pozo** no es editable directamente por el usuario. | Campo read-only. |
| **RN-19** | **Nombre del Pozo** debe ser **único por operadora**. Duplicado bloquea el guardado. | Backend: unique constraint (tenantId, nombrePozo). |
| **RN-20** | **Denominación** se normaliza a **MAYÚSCULAS** tanto en el nombre del pozo como en el UWI. | Backend: `.ToUpperInvariant()`. |
| **RN-21** | **Consecutivo** se muestra sin ceros a la izquierda en el Nombre del Pozo pero con zero-padding a 4 dígitos en el UWI. | Formateo diferenciado por contexto. |
| **RN-22** | **Nombre del Pozo** se recalcula ante cambio de cualquier componente (Campo, Denominación, Consecutivo). | Frontend: `computed()` reactivo. |
| **RN-23** | Si **Tipo Terminación = OH** (Hoyo Abierto), mostrar **advertencia** visual no bloqueante: "La terminación Hoyo Abierto está sujeta a las disposiciones de la Resolución 40537, Artículo 21." | Frontend: p-message warning. No impide guardar. |
| **RN-24** | **Tipo de Pozo por Objetivo** es obligatorio. Valores: `PH` (Producción de Hidrocarburos), `I` (Inyección), `M` (Monitoreo), `D` (Disposición), `C` (Captación), `GT` (Geotérmico), `O` (Otro). | Enum cerrado. Nuevos en V2.0: C, GT, O. |

### Sección 3 — Ubicación Geográfica

| ID | Regla | Validación |
|----|-------|------------|
| **RN-25** | **Departamento** debe validarse contra el Mapa de Tierras (polígono del contrato). Solo departamentos dentro del área del contrato son válidos. | Backend: validación contra servicio Mapa de Tierras. **PA-01 pendiente.** |
| **RN-26** | **Municipio** se filtra por departamento seleccionado. Debe validarse contra el Mapa de Tierras. | Backend: cascada + validación geográfica. |

### Panel UWI — Algoritmo PPDM

| ID | Regla | Detalle |
|----|-------|---------|
| **RN-27** | **UWI Fiscalizado** se genera automáticamente a partir de los campos del formulario. Formato PPDM. | Ver `uwi-algorithm.md`. |
| **RN-28** | Segmento **Departamento**: código DANE de 2 dígitos. | Ejemplo: `50` (Meta). |
| **RN-29** | Segmento **Municipio**: código DANE de 3 dígitos (parte municipal, sin prefijo departamento). | Ejemplo: `568` (Puerto Gaitán). |
| **RN-30** | Segmento **Sigla**: 4 caracteres MAYÚSCULAS. Primeras 4 letras de la Denominación. Si Denominación es compuesta (2+ palabras), 2 letras de cada una de las 2 primeras palabras. **Excepción ANH:** prefijo `ANH` + 1 carácter. | Ejemplo: "Cusiana Renata" → `CURE`. |
| **RN-31** | Segmento **Número**: 4 caracteres numéricos con zero-padding. | Ejemplo: 1 → `0001`. |
| **RN-32** | Segmento **Cluster/Locación**: 2 caracteres alfabéticos (abreviatura) + 4 caracteres numéricos con zero-padding. **Excepción:** si cluster = pozo (sin locación agrupada), se indica con `C` + padding. | Ejemplo: "Locación A" → `LA0000`. Sin cluster → `CXXXXX`. |
| **RN-33** | Segmento **Ángulo**: 1 carácter. `H`, `V` o `D`. | Directo del campo Tipo Ángulo. |
| **RN-34** | Segmento **Trayectoria**: código variable. `ST`, `P`, `PR`, `ML`, `G`. Si Trayectoria = Original (`O`), el segmento queda vacío. Si aplica consecutivo de trayectoria (e.g., ST2), se incluye. | Ejemplo Original: vacío. Side Track 2: `ST2`. |
| **RN-35** | Segmento **Objetivo**: código directo. `PH`, `I`, `M`, `D`, `C`, `GT`, `O`. | Directo del campo Tipo Objetivo. |
| **RN-36** | Segmento **Terminación**: código precedido de **guión** (`-`). `CD`, `LC`, `LR`, `GP`, `CC`, `OH`, `O`. | Ejemplo: `-OH`, `-CD`. |
| **RN-37** | **UWI debe ser único** en todo el sistema (cross-tenant). Duplicado bloquea la finalización. | Backend: unique constraint global. |
| **RN-38** | **UWI es inmutable** tras la generación (Finalizar Registro). No puede editarse. | Backend: reject update si UWI cambia. |
| **RN-39** | A partir del **1 de junio de 2026**, la generación del UWI se realiza **exclusivamente en GOP**. Ningún sistema externo puede asignar UWIs. | Regla operativa/legal. |

### Flujo y Estado

| ID | Regla | Validación |
|----|-------|------------|
| **RN-40** | Un pozo con estado `CREADO` **no puede editarse ni eliminarse** si la **Forma 101 ha sido radicada**. | Backend: validación cruzada con módulo Forma 101. |

---

## 5. Historias de Usuario

### HU-01: Acceso al formulario de creación

**Como** OPERADOR (Agente),
**quiero** acceder al formulario de creación de pozo nuevo,
**para** registrar un nuevo pozo en el sistema GOP.

#### Escenarios

**E-01.1 — Happy path: acceso exitoso**
- **Dado** que el usuario tiene rol `OPERADOR` o `ADMIN`
- **Cuando** navega a `/wells/create`
- **Entonces** se muestra el formulario de creación con scroll continuo
- **Y** la Sección 1 muestra su Operadora auto-llenada (RN-01)
- **Y** el dropdown de Contrato lista los contratos de su operadora (RN-02)

**E-01.2 — Error de autorización: SUPERVISOR no puede crear**
- **Dado** que el usuario tiene rol `SUPERVISOR`
- **Cuando** navega a `/wells/create`
- **Entonces** es redirigido a `/forbidden` con mensaje "No tiene permisos para crear pozos"

**E-01.3 — Error de autorización: AUDITOR no puede crear**
- **Dado** que el usuario tiene rol `AUDITOR`
- **Cuando** navega a `/wells/create`
- **Entonces** es redirigido a `/forbidden`

---

### HU-02: Selección de contrato y cascadas

**Como** OPERADOR,
**quiero** seleccionar un contrato y que los campos derivados se completen automáticamente,
**para** asegurar consistencia entre contrato, cuenca y tipo de contrato.

#### Escenarios

**E-02.1 — Happy path: cascada contrato**
- **Dado** que el usuario está en el formulario de creación
- **Cuando** selecciona un Contrato del dropdown
- **Entonces** los campos Tipo de Contrato y Cuenca se auto-llenan (RN-03, RN-04)
- **Y** el dropdown de Campo se habilita con los campos del contrato

**E-02.2 — Cambio de contrato: limpieza de cascada**
- **Dado** que el usuario ya seleccionó un Contrato con campos derivados
- **Cuando** cambia a otro Contrato
- **Entonces** Tipo de Contrato y Cuenca se actualizan
- **Y** Campo, Cluster, Nombre del Pozo se limpian

**E-02.3 — ANH selecciona contrato: solo estratigráfico**
- **Dado** que el usuario es ADMIN de ANH (tenantId = ANH)
- **Cuando** selecciona un Contrato
- **Entonces** la lista de Clasificación solo muestra "Estratigráfico" (RN-15)

---

### HU-03: Configuración de clasificación y campo

**Como** OPERADOR,
**quiero** seleccionar la clasificación del pozo con las sub-clasificaciones correspondientes,
**para** que el sistema determine correctamente la obligatoriedad del Campo.

#### Escenarios

**E-03.1 — Exploratorio: campo opcional con sub-clasificación**
- **Dado** que el usuario seleccionó Clasificación = Exploratorio
- **Cuando** selecciona sub-clasificación (A3, A2a, A2b, A2c, A1)
- **Entonces** el campo Campo queda como **opcional** (RN-11)
- **Y** el dropdown de sub-clasificación se muestra con las opciones A3/A2a/A2b/A2c/A1

**E-03.2 — Desarrollo: campo obligatorio**
- **Dado** que el usuario seleccionó Clasificación = Desarrollo
- **Entonces** el campo Campo se marca como **obligatorio** (RN-12)
- **Y** no se muestra dropdown de sub-clasificación

**E-03.3 — Cambio de clasificación: limpieza**
- **Dado** que el usuario tenía Clasificación = Desarrollo con Campo = "Rubiales"
- **Cuando** cambia Clasificación a Exploratorio
- **Entonces** el valor de Campo se limpia (RN-14)

---

### HU-04: Denominación y nombre del pozo

**Como** OPERADOR,
**quiero** ingresar la denominación y consecutivo del pozo para que el nombre se genere automáticamente,
**para** asegurar nomenclatura uniforme según las reglas ANH.

#### Escenarios

**E-04.1 — Happy path: nombre generado**
- **Dado** que Campo = "Rubiales", Denominación = "Cusiana Renata", Consecutivo = 1
- **Cuando** el sistema calcula el Nombre del Pozo
- **Entonces** muestra "RUBIALES-CUSIANA RENATA-1" (RN-16, RN-20)

**E-04.2 — Sin campo (exploratorio): fallback a contrato**
- **Dado** que Clasificación = Exploratorio, Campo = vacío, Contrato = "E&P Llanos"
- **Cuando** el usuario ingresa Denominación = "Alpha" y Consecutivo = 5
- **Entonces** Nombre del Pozo = "E&P LLANOS-ALPHA-5" (RN-17)

**E-04.3 — Validación: denominación con caracteres inválidos**
- **Dado** que el usuario ingresa Denominación = "Pozo#123"
- **Entonces** se muestra error "Solo se permiten letras, espacios y guiones" (RN-07)
- **Y** el campo se marca como inválido

**E-04.4 — Validación: nombre duplicado**
- **Dado** que ya existe un pozo "RUBIALES-ALPHA-1" en la misma operadora
- **Cuando** el usuario configura campos que generan el mismo nombre
- **Entonces** se muestra error "Ya existe un pozo con este nombre" (RN-19)
- **Y** los botones Guardar Borrador y Finalizar se deshabilitan

---

### HU-05: Tipo de terminación con advertencia

**Como** OPERADOR,
**quiero** seleccionar el tipo de terminación del pozo y recibir advertencia si selecciono Hoyo Abierto,
**para** cumplir con la Resolución 40537 Art. 21.

#### Escenarios

**E-05.1 — Happy path: terminación sin advertencia**
- **Dado** que el usuario selecciona Tipo Terminación = CD (Casing)
- **Entonces** no se muestra ninguna advertencia

**E-05.2 — Advertencia para Hoyo Abierto**
- **Dado** que el usuario selecciona Tipo Terminación = OH (Hoyo Abierto)
- **Entonces** se muestra advertencia amarilla: "La terminación Hoyo Abierto está sujeta a las disposiciones de la Resolución 40537, Artículo 21" (RN-23)
- **Y** el usuario puede continuar sin acción adicional (no bloqueante)

---

### HU-06: Ubicación geográfica con validación

**Como** OPERADOR,
**quiero** seleccionar departamento y municipio validados contra el área del contrato,
**para** asegurar que la ubicación del pozo sea consistente con el polígono contractual.

#### Escenarios

**E-06.1 — Happy path: ubicación válida**
- **Dado** que Contrato = "E&P Llanos" (polígono en Meta)
- **Cuando** selecciona Departamento = Meta, Municipio = Puerto Gaitán
- **Entonces** códigos DANE se auto-llenan: Dpto = 50, Mpio = 568 (RN-28, RN-29)

**E-06.2 — Cascada departamento → municipio**
- **Dado** que seleccionó Departamento = Meta
- **Entonces** dropdown de Municipio se filtra para mostrar solo municipios de Meta (RN-26)

**E-06.3 — Departamento inválido para el contrato**
- **Dado** que Contrato = "E&P Llanos" (polígono en Meta)
- **Cuando** selecciona Departamento = Bogotá D.C.
- **Entonces** se muestra error "El departamento seleccionado no pertenece al área del contrato" (RN-25)

---

### HU-07: Generación y preview de UWI

**Como** OPERADOR,
**quiero** ver el UWI Fiscalizado generarse en tiempo real conforme lleno el formulario,
**para** verificar que el identificador es correcto antes de finalizar.

#### Escenarios

**E-07.1 — Happy path: UWI completo**
- **Dado** Dpto=Meta(50), Mpio=Puerto Gaitán(568), Denominación="Cusiana Renata", Consecutivo=1, Cluster="Locación A", Ángulo=V, Trayectoria=O(Original), Objetivo=PH, Terminación=OH
- **Entonces** UWI = `50568CURE0001LA0000VPH-OH` (sin trayectoria porque Original = vacío)

**E-07.2 — UWI parcial (campos incompletos)**
- **Dado** que solo se han llenado Dpto y Mpio
- **Entonces** UWI muestra los segmentos completados con placeholders para los pendientes: `50568____????______-__`

**E-07.3 — Validación: UWI duplicado**
- **Dado** que el UWI generado ya existe en el sistema
- **Cuando** el usuario intenta Finalizar Registro
- **Entonces** se muestra error "Ya existe un pozo con este UWI" (RN-37)
- **Y** la finalización se bloquea

---

### HU-08: Guardar borrador

**Como** OPERADOR,
**quiero** guardar un borrador parcial del pozo en cualquier momento,
**para** poder completar el registro en otro momento.

#### Escenarios

**E-08.1 — Happy path: borrador con datos parciales**
- **Dado** que el usuario ha llenado al menos 1 campo del formulario
- **Cuando** hace clic en "Guardar Borrador"
- **Entonces** el sistema persiste los datos parciales con estado = `BORRADOR`
- **Y** muestra toast de éxito "Borrador guardado"
- **Y** navega a `/wells/manage`

**E-08.2 — Error: formulario vacío**
- **Dado** que el usuario no ha llenado ningún campo
- **Cuando** hace clic en "Guardar Borrador"
- **Entonces** se muestra error "Debe completar al menos un campo para guardar borrador"

**E-08.3 — Borrador no genera UWI**
- **Dado** que el usuario guarda un borrador
- **Entonces** el campo UWI queda como `null` (no se genera UWI para borradores)

---

### HU-09: Finalizar registro

**Como** OPERADOR,
**quiero** finalizar el registro del pozo con todos los datos validados y UWI generado,
**para** que el pozo quede registrado oficialmente en GOP.

#### Escenarios

**E-09.1 — Happy path: finalización exitosa**
- **Dado** que todos los 17 campos obligatorios están completos y válidos
- **Y** el UWI generado es único
- **Y** el nombre del pozo es único
- **Cuando** hace clic en "Finalizar Registro"
- **Entonces** el sistema persiste el pozo con estado = `CREADO` y UWI definitivo
- **Y** muestra toast "Pozo registrado exitosamente"
- **Y** navega a `/wells/manage`

**E-09.2 — Error: campos incompletos**
- **Dado** que faltan campos obligatorios
- **Cuando** hace clic en "Finalizar Registro"
- **Entonces** se marcan los campos faltantes con error
- **Y** se muestra mensaje "Complete todos los campos obligatorios antes de finalizar"
- **Y** el scroll se posiciona en el primer campo con error

**E-09.3 — Error: UWI duplicado en finalización**
- **Dado** que todos los campos son válidos pero el UWI ya existe
- **Cuando** hace clic en "Finalizar Registro"
- **Entonces** se muestra error 409: "Ya existe un pozo con el UWI [UWI]" (RN-37)

---

### HU-10: Edición de pozo

**Como** OPERADOR o SUPERVISOR,
**quiero** editar un pozo previamente creado,
**para** corregir datos antes de la radicación de la Forma 101.

#### Escenarios

**E-10.1 — Happy path: edición de borrador**
- **Dado** que existe un pozo en estado `BORRADOR`
- **Cuando** el usuario navega a `/wells/{id}/edit`
- **Entonces** se carga el formulario con los datos existentes, todos editables

**E-10.2 — Edición de pozo creado (pre-Forma101)**
- **Dado** que existe un pozo en estado `CREADO` sin Forma 101 radicada
- **Cuando** el usuario navega a `/wells/{id}/edit`
- **Entonces** se carga el formulario con datos existentes
- **Y** el UWI se regenera si cambian los campos que lo componen

**E-10.3 — Error: pozo bloqueado por Forma 101**
- **Dado** que el pozo tiene Forma 101 radicada (RN-40)
- **Cuando** intenta editar
- **Entonces** se muestra mensaje "Este pozo no puede editarse porque tiene Forma 101 radicada"
- **Y** el formulario se muestra en modo solo lectura

---

### HU-11: Eliminación de pozo

**Como** OPERADOR o ADMIN,
**quiero** eliminar un pozo que ya no es necesario,
**para** mantener limpio el registro de pozos.

#### Escenarios

**E-11.1 — Happy path: eliminación de borrador**
- **Dado** que el pozo está en estado `BORRADOR`
- **Cuando** el usuario confirma la eliminación
- **Entonces** se ejecuta soft delete (RN-40)
- **Y** el pozo desaparece de la lista

**E-11.2 — Error: pozo con Forma 101 radicada**
- **Dado** que el pozo tiene Forma 101 radicada
- **Cuando** intenta eliminar
- **Entonces** se muestra error "No se puede eliminar un pozo con Forma 101 radicada"

**E-11.3 — Error de autorización: SUPERVISOR no puede eliminar**
- **Dado** que el usuario es SUPERVISOR
- **Cuando** intenta eliminar un pozo
- **Entonces** se muestra error 403 "No tiene permisos para eliminar pozos"

---

### HU-12: Listado y consulta de pozos

**Como** usuario de cualquier rol,
**quiero** ver la lista de pozos con filtros y paginación,
**para** localizar y consultar la información de los pozos.

#### Escenarios

**E-12.1 — Happy path: listado paginado**
- **Dado** que existen pozos en el sistema
- **Cuando** navega a `/wells/manage`
- **Entonces** se muestra tabla paginada con columnas: Nombre, Operadora, Contrato, UWI, Estado, Fecha

**E-12.2 — Filtro multi-tenant automático**
- **Dado** que el usuario es OPERADOR del tenant "Ecopetrol"
- **Entonces** solo ve pozos del tenant "Ecopetrol"
- **Y** el ADMIN ve pozos de todos los tenants

**E-12.3 — Detalle de pozo**
- **Dado** que el usuario hace clic en un pozo del listado
- **Cuando** navega a `/wells/{id}`
- **Entonces** se muestra el detalle completo del pozo con UWI y todos los campos

---

## 6. Criterios de Aceptación (CA-01 a CA-13)

| ID | Criterio | Given | When | Then |
|----|----------|-------|------|------|
| **CA-01** | Formulario de creación accesible | Usuario OPERADOR o ADMIN autenticado | Navega a `/wells/create` | Formulario de scroll continuo con 3 secciones + panel UWI |
| **CA-02** | Operadora auto-poblada | Usuario autenticado en sesión | Entra al formulario | Campo "Operadora" muestra nombre del tenant, no editable |
| **CA-03** | Cascada Contrato → derivados | Contrato seleccionado | Se selecciona Contrato | Tipo Contrato, Cuenca se auto-llenan. Campo se filtra. |
| **CA-04** | Clasificación con sub-tipo | Formulario visible | Selecciona Clasificación Exploratorio | Sublista A3/A2a/A2b/A2c/A1 se muestra. Campo queda opcional. |
| **CA-05** | Nombre de pozo auto-generado | Campo, Denominación y Consecutivo llenados | Cambio en cualquiera de los 3 | Nombre se recalcula: `{Campo}-{Denominación}-{Consecutivo}` |
| **CA-06** | Advertencia Hoyo Abierto | Formulario visible | Selecciona Terminación OH | Mensaje warning Res. 40537 Art. 21 visible, no bloqueante |
| **CA-07** | UWI preview en tiempo real | Panel UWI visible | Campos del formulario cambian | UWI se recalcula según algoritmo PPDM |
| **CA-08** | Guardar borrador parcial | Al menos 1 campo llenado | Clic en "Guardar Borrador" | Pozo persistido con estado BORRADOR, sin UWI |
| **CA-09** | Finalizar registro completo | Todos los campos válidos | Clic en "Finalizar Registro" | Pozo persistido con estado CREADO, UWI generado y único |
| **CA-10** | Unicidad de UWI | UWI ya existe en sistema | Intenta finalizar | Error 409: UWI duplicado |
| **CA-11** | Edición condicional | Pozo sin Forma 101 radicada | Navega a edición | Formulario editable, UWI se puede regenerar |
| **CA-12** | Bloqueo por Forma 101 | Pozo con Forma 101 radicada | Intenta editar o eliminar | Acción bloqueada con mensaje explicativo |
| **CA-13** | Restricción ANH | Usuario ADMIN de ANH | Selecciona Clasificación | Solo "Estratigráfico" disponible |

---

## 7. Edge Cases

| ID | Caso | Comportamiento esperado |
|----|------|------------------------|
| EC-01 | Denominación compuesta de 1 sola palabra de 3 letras ("Sol") | Sigla UWI: `SOLX` (padding con X a 4 caracteres) |
| EC-02 | Denominación compuesta de 3+ palabras ("Rio Magdalena Sur") | Sigla UWI: 2 primeras letras de palabra 1 + 2 primeras de palabra 2 = `RIMA` |
| EC-03 | Consecutivo = 9999 (máximo) | UWI Número = `9999`. Siguiente consecutivo requiere revisión de política. |
| EC-04 | Cluster no existe (usuario crea uno nuevo) | POST cluster → se asigna ID → UWI incluye nueva codificación cluster |
| EC-05 | Pozo Costa Afuera sin Departamento/Municipio | **PA-02 pendiente.** Comportamiento temporal: campos Dpto/Mpio requeridos. |
| EC-06 | Sesión expira durante llenado del formulario | Interceptor 401 despacha `sessionExpired()`. Datos no guardados se pierden. |
| EC-07 | Dos usuarios crean pozo con mismo UWI simultáneamente | Backend: constraint `UNIQUE` en DB. El segundo recibe 409. |
| EC-08 | ANH crea pozo y la Denominación comienza con "ANH" | Sigla UWI: `ANH` + 1er carácter de denominación (RN-30 excepción ANH) |
| EC-09 | Cambio de contrato invalida departamento ya seleccionado | Departamento y Municipio se limpian. Validación contra nuevo polígono. |
| EC-10 | Trayectoria = Side Track con consecutivo de trayectoria | Segmento trayectoria en UWI incluye número: `ST2`, `ST3` |

---

## 8. Integraciones Externas

| Sistema | Datos | Tipo | Dirección | Estado |
|---------|-------|------|-----------|--------|
| SOLAR | Contratos, Operadoras, Tipo Contrato, Cuenca | REST | GOP consume | **PA-01: fuente definitiva pendiente** |
| VCH | Contratos (fuente alternativa) | REST | GOP consume | PA-01 |
| VPAA | Contratos estratigráficos ANH | REST | GOP consume | PA-01 |
| VAF | Contratos ANH (fuente alternativa) | REST | GOP consume | PA-01 |
| Mapa de Tierras (VP Técnica) | Validación Dpto/Mpio vs polígono | REST | GOP consume | **PA-02: alcance pendiente** |
| DANE | Códigos departamento/municipio | Catálogo | Precarga referencial | Estable |

> **Nota:** Mientras PA-01 y PA-02 se resuelven, el backend expone catálogos propios con datos precargados del DANE y contratos seed. La integración real con SOLAR/Mapa de Tierras se implementará como feature separada.

---

## 9. Flujo de Estados

```
[inicio]
   │
   ├─[Guardar Borrador]──────▶ BORRADOR
   │                              │
   │                              ├─[Editar]──▶ BORRADOR (mismo estado)
   │                              ├─[Eliminar]──▶ ELIMINADO (soft delete)
   │                              └─[Finalizar Registro]──▶ CREADO
   │
   └─[Finalizar Registro]──────▶ CREADO
                                   │
                                   ├─[Forma 101 NO radicada]─▶ Editable / Eliminable
                                   │   ├─[Editar]──▶ CREADO (re-genera UWI si campos cambian)
                                   │   └─[Eliminar]──▶ ELIMINADO (soft delete)
                                   │
                                   └─[Forma 101 radicada]───▶ BLOQUEADO (read-only, RN-40)
```

---

## 10. Puntos Abiertos

| ID | Descripción | Responsable | Impacto |
|----|-------------|-------------|---------|
| **PA-01** | Fuente definitiva de datos maestros de Contratos. ¿SOLAR, VCH, VPAA o VAF? | ANH Vicepresidencia | Endpoint de catálogo de contratos puede cambiar. Se diseña con interface abstracta. |
| **PA-02** | Departamento/Municipio para pozos Costa Afuera. ¿Se deshabilita obligatoriedad? | ANH Equipo Funcional | Afecta validación de RN-25/RN-26 y segmentos DANE del UWI. |

---

## 11. Requisitos No Funcionales

| Requisito | Detalle |
|-----------|---------|
| **Rendimiento** | El preview de UWI debe actualizarse en ≤ 100ms (cómputo client-side). |
| **Accesibilidad** | Formulario navegable por teclado. Labels asociados a inputs. Errores anunciados por screen reader. |
| **Responsividad** | Formulario usable en tablet (≥ 768px). No se requiere soporte mobile. |
| **Auditoría** | Toda creación, edición y eliminación se registra con userId, timestamp y acción. |
| **Concurrencia** | UWI unique constraint a nivel de DB para manejar race conditions. |
