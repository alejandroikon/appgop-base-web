# Spec: Layout Base — Sidebar + TopHeader

**Feature ID:** 002-layout
**Dominio:** `core/layout/` — App Shell (Sidebar, TopHeader, Main Content)
**Estado:** En revisión — pendiente de aprobación para generar `plan.md`

---

## 1. Contexto y Alcance

Este módulo define la estructura visual y funcional del **Shell principal** de la aplicación GOP 360°. El Shell envuelve todas las rutas autenticadas y se compone de dos elementos persistentes: el **Sidebar** (menú lateral de navegación) y el **TopHeader** (barra superior de contexto). Ambos persisten durante toda la navegación entre dominios sin destruirse ni re-renderizarse al cambiar de ruta.

Al ser un **prototipo funcional**, los módulos de destino (Dashboard, Pozos, Operaciones, Producción, Administración) no están implementados aún; las rutas del sidebar apuntarán a componentes placeholder. Los datos del usuario (nombre, rol, tenant) provienen del store de autenticación implementado en la Feature 001-auth.

**Componentes incluidos en este spec:**
- `MainLayoutComponent` — Shell padre (Smart container)
- `SidebarComponent` — Menú lateral con navegación multinivel (Dumb)
- `TopHeaderComponent` — Barra superior de contexto y acciones globales (Dumb)
- `AppNavLinkComponent` — Item de navegación reutilizable con filtro RBAC (Dumb) — **condicional**: se crea solo si la librería de menú no cubre el caso
- Componentes placeholder para rutas de destino aún no implementadas

**Fuera de alcance (features posteriores):**
- Implementación real de los módulos de destino (Dashboard, Wells, Operations, Production, Admin)
- Notificaciones push / tiempo real (campana de notificaciones es placeholder visual)
- Selectores de contexto global (filtros de Operadora, Contrato, Campo)
- Breadcrumbs dinámicos por ruta (se reserva el espacio visual)
- Cambio de tema visual (dark mode)
- Perfil de usuario / configuración personal

---

## 2. Historias de Usuario

### HU-004: Navegación Principal por Módulos

**Como** usuario autenticado del sistema GOP 360°,
**quiero** ver un menú lateral con los módulos a los que tengo acceso según mi rol,
**para** navegar de forma clara y directa a las funcionalidades del sistema.

#### Criterios de Aceptación

**Escenario 1 — Renderizado del sidebar según rol**
- **Dado** que el usuario está autenticado y accede a cualquier ruta protegida
- **Cuando** se carga el Main Layout
- **Entonces** el sidebar muestra únicamente los módulos cuyo array de roles incluya el rol del usuario actual
- **Y** los módulos no autorizados no se renderizan en el DOM (no solo ocultos con CSS)

**Escenario 2 — Navegación a un módulo**
- **Dado** que el sidebar está visible con los módulos del usuario
- **Cuando** el usuario hace clic en un módulo de primer nivel que tiene submódulos
- **Entonces** se expande el acordeón del módulo mostrando los submódulos disponibles
- **Y** los demás módulos expandidos se colapsan (comportamiento accordion: solo uno abierto a la vez)

**Escenario 3 — Navegación a un submódulo**
- **Dado** que un módulo está expandido mostrando sus submódulos
- **Cuando** el usuario hace clic en un submódulo
- **Entonces** el sistema navega a la ruta correspondiente
- **Y** el submódulo seleccionado se resalta visualmente como activo
- **Y** el módulo padre permanece expandido y resaltado con el color de acento

**Escenario 4 — Indicador de ruta activa**
- **Dado** que el usuario se encuentra en una ruta específica (ej. `/admin/users`)
- **Cuando** observa el sidebar
- **Entonces** el módulo padre ("Administración") está expandido y resaltado
- **Y** el submódulo activo ("Usuarios") tiene fondo diferenciado y texto en color de acento
- **Y** los demás módulos y submódulos se muestran en estado inactivo (tono neutro)

**Escenario 5 — Módulo sin submódulos**
- **Dado** que un módulo de primer nivel no tiene submódulos (ej. "Panel de Control")
- **Cuando** el usuario hace clic en él
- **Entonces** el sistema navega directamente a la ruta del módulo sin desplegar acordeón

**Escenario 6 — Acceso directo por URL a ruta no autorizada**
- **Dado** que un usuario con rol `OPERADOR` no tiene acceso al módulo "Administración"
- **Cuando** intenta navegar directamente a `/admin/users` escribiendo la URL
- **Entonces** el `roleGuard` bloquea el acceso y redirige a una vista de acceso denegado o a la ruta principal
- **Y** el sidebar nunca muestra el módulo "Administración" para ese rol

---

### HU-005: Colapso y Expansión del Sidebar

**Como** usuario autenticado,
**quiero** poder colapsar el menú lateral para maximizar mi área de trabajo,
**para** tener más espacio al consultar datos o llenar formularios.

#### Criterios de Aceptación

**Escenario 1 — Colapsar el sidebar**
- **Dado** que el sidebar está en estado expandido (mostrando iconos + texto)
- **Cuando** el usuario hace clic en el botón de colapso (botón circular con chevron)
- **Entonces** el sidebar se reduce a su ancho mínimo mostrando solo iconos
- **Y** las etiquetas de texto de los módulos se ocultan
- **Y** los submódulos expandidos se colapsan
- **Y** el logo institucional se simplifica o reduce su tamaño
- **Y** el área de contenido principal se expande para ocupar el espacio liberado
- **Y** la transición es suave y animada

**Escenario 2 — Expandir el sidebar**
- **Dado** que el sidebar está en estado colapsado (solo iconos)
- **Cuando** el usuario hace clic en el botón de expansión (mismo botón circular, chevron invertido)
- **Entonces** el sidebar vuelve a su ancho completo mostrando iconos + texto
- **Y** el módulo activo se expande automáticamente mostrando sus submódulos

**Escenario 3 — Tooltips en modo colapsado**
- **Dado** que el sidebar está colapsado
- **Cuando** el usuario posiciona el cursor sobre un icono de módulo
- **Entonces** aparece un tooltip a la derecha del icono con el nombre del módulo
- **Y** el tooltip desaparece al mover el cursor fuera del icono

**Escenario 4 — Indicador visual en modo colapsado**
- **Dado** que el sidebar está colapsado y el usuario se encuentra en una ruta activa
- **Cuando** observa los iconos del sidebar
- **Entonces** el icono del módulo activo se muestra con el color de acento
- **Y** el icono tiene un contenedor de resalte (highlight box) con fondo diferenciado
- **Y** opcionalmente un borde lateral de acento como indicador adicional

**Escenario 5 — Sección de perfil en modo colapsado**
- **Dado** que el sidebar está colapsado
- **Cuando** el usuario observa la parte inferior del sidebar
- **Entonces** solo se muestra el avatar del usuario (sin nombre ni rol)
- **Y** el botón de cerrar sesión se reduce a su icono

---

### HU-006: Navegación Responsiva (Mobile / Tablet)

**Como** usuario que accede desde un dispositivo móvil o tablet,
**quiero** poder acceder al menú de navegación mediante un botón de hamburguesa,
**para** navegar entre módulos sin sacrificar el espacio de la pantalla.

#### Criterios de Aceptación

**Escenario 1 — Sidebar oculto por defecto en mobile**
- **Dado** que el usuario accede desde una pantalla con ancho menor al breakpoint `lg` (1024px)
- **Cuando** se carga el Main Layout
- **Entonces** el sidebar no es visible
- **Y** en el TopHeader aparece un botón de hamburguesa a la izquierda

**Escenario 2 — Abrir sidebar como drawer en mobile**
- **Dado** que el sidebar está oculto en mobile
- **Cuando** el usuario presiona el botón de hamburguesa
- **Entonces** el sidebar se despliega desde la izquierda como un panel overlay (drawer)
- **Y** se muestra un fondo oscuro (overlay/backdrop) detrás del sidebar
- **Y** el sidebar muestra todos los elementos en su estado expandido (iconos + texto)

**Escenario 3 — Cerrar el drawer**
- **Dado** que el sidebar está abierto como drawer en mobile
- **Cuando** el usuario presiona el fondo oscuro (overlay), o navega a una ruta, o presiona Escape
- **Entonces** el sidebar se cierra con animación de deslizamiento
- **Y** el fondo oscuro desaparece

**Escenario 4 — Navegación desde el drawer cierra el menú**
- **Dado** que el sidebar drawer está abierto
- **Cuando** el usuario selecciona un submódulo y navega a una nueva ruta
- **Entonces** el drawer se cierra automáticamente después de la navegación

---

### HU-007: Barra Superior con Contexto del Usuario

**Como** usuario autenticado,
**quiero** ver mi información de sesión y acciones globales en una barra superior persistente,
**para** tener contexto de quién soy, a qué organización pertenezco, y poder cerrar sesión rápidamente.

#### Criterios de Aceptación

**Escenario 1 — Información del usuario visible**
- **Dado** que el usuario está autenticado
- **Cuando** observa el TopHeader
- **Entonces** se muestra un mensaje de bienvenida con el nombre del usuario (ej. "Bienvenido, Juan Pérez")
- **Y** se muestra el rol del usuario con un badge identificativo (ej. "Soporte Operador")
- **Y** se muestra un avatar circular con las iniciales del usuario
- **Y** se muestra el nombre de la organización/tenant (ej. "ECOPETROL S.A.")

**Escenario 2 — Cierre de sesión desde el TopHeader**
- **Dado** que el usuario visualiza el TopHeader
- **Cuando** hace clic en el botón o enlace de "Cerrar Sesión"
- **Entonces** el sistema despacha la acción de logout (comportamiento definido en Feature 001-auth)
- **Y** el usuario es redirigido a `/login`

**Escenario 3 — TopHeader responsivo**
- **Dado** que el usuario accede desde un dispositivo mobile
- **Cuando** observa el TopHeader
- **Entonces** se muestra el botón de hamburguesa a la izquierda
- **Y** los elementos menos críticos (nombre del tenant, badge de rol) pueden ocultarse o truncarse para priorizar el espacio
- **Y** el avatar y el botón de logout permanecen visibles

---

### HU-008: Perfil de Usuario en el Sidebar

**Como** usuario autenticado,
**quiero** ver mi avatar, nombre y rol en la parte inferior del sidebar,
**para** confirmar rápidamente con qué cuenta estoy trabajando.

#### Criterios de Aceptación

**Escenario 1 — Perfil visible en modo expandido**
- **Dado** que el sidebar está expandido
- **Cuando** el usuario observa la parte inferior del sidebar
- **Entonces** se muestra: avatar circular con iniciales generadas dinámicamente, nombre completo del usuario y rol
- **Y** se muestra un botón de "Cerrar Sesión" que invoca la función de logout

**Escenario 2 — Perfil reducido en modo colapsado**
- **Dado** que el sidebar está colapsado
- **Cuando** el usuario observa la parte inferior
- **Entonces** solo se muestra el avatar circular (sin nombre ni rol)
- **Y** el botón de cerrar sesión se muestra como solo icono

---

## 3. Mapa de Navegación del Sidebar

La siguiente tabla define los módulos y submódulos visibles en el sidebar. Las rutas de destino apuntan a componentes placeholder hasta que se implementen las features correspondientes.

| Nivel | Label | Icono | Ruta | Roles con acceso |
|---|---|---|---|---|
| **Módulo** | Panel de Control | `pi pi-objects-column` | `/dashboard` | Todos |
| **Módulo** | Seguridad & Administración | `pi pi-shield` | — (solo agrupa) | `ADMIN` |
| Sub | Gestión de Usuarios | — | `/admin/users` | `ADMIN` |
| Sub | Registros de Auditoría | — | `/admin/audit-logs` | `ADMIN`, `AUDITOR` |

> **Nota:** Los módulos de Pozos, Operaciones y Producción se agregarán al sidebar cuando se desarrollen sus features correspondientes. En esta fase del prototipo solo se incluyen los módulos que tienen al menos un componente de destino (placeholder o real). Los submódulos no llevan icono para evitar sobresaturación visual.

> **Referencia RBAC:** Los roles listados son los definidos en Feature 001-auth (`ADMIN`, `SUPERVISOR`, `OPERADOR`, `AUDITOR`). Las reglas RBAC específicas de cada dominio se definen en la especificación funcional de cada feature; esta tabla es la fuente de verdad únicamente para la navegación del sidebar en esta fase.

---

## 4. Reglas de Negocio

| Regla | Descripción |
|---|---|
| RN-010 | El sidebar filtra items por rol del usuario autenticado. Un item se muestra solo si el rol del usuario está incluido en su array de roles |
| RN-011 | El filtrado RBAC del sidebar es solo capa visual (UX). La seguridad real la provee el `roleGuard` en cada ruta — defensa en profundidad |
| RN-012 | El sidebar solo maneja dos niveles de jerarquía: Módulo (nivel 1) y Submódulo (nivel 2). No se soportan niveles adicionales |
| RN-013 | El comportamiento del acordeón es exclusivo: solo un módulo puede estar expandido a la vez. Al expandir uno, los demás se colapsan |
| RN-014 | El estado expanded/collapsed del sidebar es local a la sesión (no se persiste en storage). Al recargar la página, el sidebar inicia expandido en desktop y oculto en mobile |
| RN-015 | El avatar del usuario se genera dinámicamente a partir de las iniciales del nombre (ej. "Juan Pérez" → "JP"). No se usan imágenes de avatar |
| RN-016 | El cierre de sesión desde el sidebar o el TopHeader despacha la misma acción de logout definida en Feature 001-auth |
| RN-017 | El sidebar y el TopHeader nunca se destruyen ni re-renderizan al cambiar de ruta entre módulos. Solo cambia el contenido del `<router-outlet>` interno |
| RN-018 | Los textos de los módulos y submódulos del sidebar provienen del locale centralizado, nunca se hardcodean en templates ni en el array de items de navegación |
| RN-019 | El botón de hamburguesa solo es visible en resoluciones menores a `lg` (1024px). En desktop, la hamburguesa no existe; el toggle de colapso cumple esa función |

---

## 5. Requisitos de UI / UX

> **Imagen de referencia:** [`specs/features/002-layout/ref-layout.png`](ref-layout.png) — Prototipo visual aprobado del layout. Todos los componentes de UI deben respetar la estructura, proporciones y disposición de elementos mostrados en esta imagen.

### 5.1. Paleta de Colores del Layout

| Token | Valor | Uso |
|---|---|---|
| **Color Principal** | `#083075` | Fondo del sidebar |
| **Color Secundario** | `#0D66EA` | Fondo de resalte del submódulo activo, highlight box del icono activo en modo colapsado |
| **Color de Acento** | `#FFCB05` | Texto/icono del módulo activo, borde de selección, botón de toggle, indicadores |
| **Fondo Neutro** | Blanco / `#FFFFFF` | Fondo del TopHeader y área de contenido |

> Estos colores deben registrarse como Design Tokens (§14 de CONSTITUTION) para ser reutilizables en toda la aplicación.

### 5.2. Sidebar — Estado Expandido

**Estructura vertical (de arriba a abajo):**

1. **Cabecera / Branding:**
   - Logo institucional (ANH) alineado a la izquierda
   - Nombre de la aplicación "GOP 360°" al lado del logo
   - Fondo: Color Principal (`#083075`)

2. **Menú de Navegación (zona central, scrollable si excede):**
   - Cada módulo (nivel 1): icono representativo + texto del módulo en tono neutro claro sobre fondo principal
   - Módulo activo: icono y texto en Color de Acento (`#FFCB05`)
   - Al expandir un módulo, se despliegan los submódulos con indentación
   - Submódulo activo: fondo en variante del Color Secundario (`#0D66EA`) + texto en Color de Acento
   - Submódulos inactivos: texto en tono neutro claro, sin icono
   - Separación visual clara entre módulos

3. **Perfil de Usuario (zona inferior, fijada al fondo):**
   - Avatar circular con iniciales generadas dinámicamente
   - Nombre del usuario y rol en texto claro
   - Botón de "Cerrar Sesión" con icono

**Ancho:** Fijo, aproximadamente `w-72` (288px)

### 5.3. Sidebar — Estado Colapsado

**Estructura vertical (de arriba a abajo):**

1. **Cabecera / Branding:**
   - Logo simplificado o reducido en tamaño
   - Nombre de la aplicación oculto

2. **Menú de Navegación:**
   - Solo iconos centrados verticalmente
   - Icono inactivo: tono neutro claro (contorno)
   - Icono activo: Color de Acento (`#FFCB05`) con contenedor de resalte (highlight box) en fondo derivado del Color Secundario con esquinas redondeadas
   - Borde lateral de acento (`#FFCB05`) en el lado izquierdo del icono activo como ancla visual adicional
   - Tooltip al hacer hover con el nombre del módulo, posicionado a la derecha del icono

3. **Perfil de Usuario:**
   - Solo avatar circular (sin nombre ni rol)
   - Botón de cerrar sesión reducido a solo icono

**Ancho:** Reducido, aproximadamente `w-20` (80px)

### 5.4. Botón de Toggle (Colapso/Expansión)

- **Ubicación:** Botón flotante en el borde derecho del sidebar, centrado verticalmente respecto al borde
- **Estilo:** Círculo sólido con fondo en Color de Acento (`#FFCB05`) e icono de dirección (chevron)
- **Comportamiento del chevron:** Apunta hacia la izquierda cuando el sidebar está expandido (indica "colapsar"), apunta hacia la derecha cuando está colapsado (indica "expandir")
- **Visibilidad:** Solo visible en resoluciones desktop (>= `lg`). En mobile, la hamburguesa del TopHeader reemplaza esta función
- **Contraste:** El alto contraste del botón asegura que sea descubrible (affordance)

### 5.5. TopHeader — Estructura

Barra horizontal fija en la parte superior del área de contenido (a la derecha del sidebar). Fondo blanco/neutro.

**Distribución horizontal (izquierda a derecha):**

1. **Zona Izquierda (start):**
   - Botón de hamburguesa (solo visible en mobile, < `lg`)
   - Texto de bienvenida: "Bienvenido, {Nombre del Usuario}"

2. **Zona Central (center):**
   - Badge del rol del usuario (ej. "Soporte Operador")
   - *Espacio reservado para breadcrumbs futuros*

3. **Zona Derecha (end):**
   - Avatar circular del usuario con iniciales
   - Nombre del Operador/Tenant (ej. "ECOPETROL S.A.") como badge o texto destacado
   - *Espacio reservado para campana de notificaciones (futuro)*

### 5.6. Transiciones y Animaciones

- La transición entre estados expandido/colapsado del sidebar debe ser suave (CSS transition, duración ~200-300ms)
- El drawer mobile se desliza desde la izquierda con animación de slide-in
- El overlay del drawer aparece con fade-in
- Los acordeones del menú se expanden/colapsan con animación vertical suave

### 5.7. Accesibilidad

- Todos los items de navegación son accesibles via teclado (Tab, Enter, Arrow keys)
- Los tooltips en modo colapsado tienen roles ARIA apropiados
- El botón de toggle tiene etiqueta accesible dinámica ("Colapsar menú" / "Expandir menú")
- El botón de hamburguesa tiene etiqueta accesible ("Abrir menú de navegación")
- El drawer mobile gestiona el foco: al abrirse, el foco se mueve al drawer; al cerrarse, vuelve al trigger
- Los items de navegación ocultos por RBAC no existen en el DOM (no solo `display: none`)
- El indicador de ruta activa es perceptible no solo por color (usa también borde/fondo como refuerzo)
- Los controles interactivos (toggle, hamburguesa, logout) tienen área mínima de toque de 44x44px

---

## 6. Restricciones Técnicas

- El módulo de Layout vive en `core/layout/` — puede importar de `core/auth/`, `shared/models/`, `shared/locale/`, `shared/ui/`
- **No puede** importar de ningún `domains/` — el layout es agnóstico a los dominios de negocio
- Los datos del usuario (nombre, rol, tenant) se leen del store NgRx de autenticación (Feature 001-auth)
- El `MainLayoutComponent` es el único componente Smart — Sidebar y TopHeader son Dumb
- El estado collapsed/expanded es UI local gestionado con Signals, no con NgRx
- Los textos del sidebar y TopHeader provienen del locale centralizado (`shared/locale/locale.ts`)
- Las rutas del sidebar no están hardcodeadas en templates — provienen de un array tipado de configuración

---

## 7. Edge Cases Identificados

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-010 | Usuario con rol sin ningún módulo asignado | El sidebar muestra solo la cabecera y el perfil de usuario, sin items de navegación. El usuario ve el contenido del `<router-outlet>` (dashboard o placeholder) |
| EC-011 | Nombre del usuario muy largo (> 25 caracteres) | Se trunca con ellipsis en el sidebar y TopHeader |
| EC-012 | Nombre del tenant muy largo | Se trunca con ellipsis en el TopHeader |
| EC-013 | Redimensión de ventana de desktop a mobile | El sidebar se oculta automáticamente al cruzar el breakpoint `lg` hacia abajo; el botón de hamburguesa aparece en el TopHeader |
| EC-014 | Redimensión de ventana de mobile a desktop | El drawer se cierra si estaba abierto y el sidebar estático reaparece en su último estado (expandido o colapsado) |
| EC-015 | Navegación a ruta sin módulo definido en el sidebar | Ningún módulo se muestra como activo en el sidebar |
| EC-016 | Doble clic rápido en el botón de toggle | Se debe completar la transición antes de aceptar un nuevo toggle, o usar debounce para evitar estados intermedios |
| EC-017 | Logout mientras el drawer mobile está abierto | El drawer se cierra y el usuario es redirigido a `/login` |
| EC-018 | Refresh de la página (F5) en una ruta autenticada | El sidebar se renderiza en estado expandido (default), el módulo correspondiente a la ruta actual se expande y resalta automáticamente |

---

## 8. Datos Mock Disponibles (Feature 001-auth)

Los siguientes usuarios mock ya existen en el sistema y están disponibles para validar el RBAC del sidebar:

| Correo | Rol | Tenant | Módulos visibles esperados |
|---|---|---|---|
| `admin@gop360.com` | `ADMIN` | Agencia Nacional de Hidrocarburos | Panel de Control, Seguridad & Administración (todos los sub) |
| `supervisor@gop360.com` | `SUPERVISOR` | Ecopetrol S.A. | Panel de Control |
| `operador@gop360.com` | `OPERADOR` | Ecopetrol S.A. | Panel de Control |
| `auditor@gop360.com` | `AUDITOR` | Agencia Nacional de Hidrocarburos | Panel de Control, Seguridad & Administración > Registros de Auditoría |

> Los módulos de Pozos, Operaciones y Producción se agregarán a esta tabla cuando sus features se especifiquen.

---

## 9. Checklist de Revisión (Desarrollador)

Antes de aprobar este spec y generar el `plan.md`, el desarrollador debe validar:

- [ ] Las historias de usuario cubren todos los estados del sidebar (expandido, colapsado, drawer mobile)
- [ ] Los criterios de aceptación cubren la navegación con teclado y accesibilidad
- [ ] El mapa de navegación es coherente con los roles definidos en Feature 001-auth
- [ ] La paleta de colores coincide con la imagen de referencia
- [ ] Los edge cases cubren escenarios de responsive y cambio de resolución
- [ ] Las restricciones técnicas son compatibles con las reglas de dependencias del CONSTITUTION (§9)
- [ ] La defensa en profundidad (RBAC sidebar + roleGuard) está documentada como regla de negocio
- [ ] Los datos mock existentes son suficientes para validar todos los flujos RBAC del sidebar