# Documentación Funcional Base (Blueprint) - GOP 360°

## 1. Contexto de la Aplicación
**GOP 360° (Gestión Operativa de Pozos)** es una plataforma integral diseñada para administrar el ciclo de vida completo de los pozos de hidrocarburos, desde su creación y perforación hasta su producción y fiscalización.

**Naturaleza del Proyecto Actual:** 
Este proyecto es un **Prototipo Funcional (Maqueta)**. Su objetivo principal es validar la experiencia de usuario (UX), la interfaz de usuario (UI) y los flujos de navegación complejos antes de la integración con los servicios backend reales. 
* Toda la lógica de enrutamiento (Routing), protección de rutas (Guards) y gestión de estado de la UI es **real y funcional**.
* Los datos presentados son **mocks** (datos simulados) aislados en la capa de servicios, garantizando que la capa de presentación no tenga datos "quemados".

---

## 2. Estructura de Layouts (Contenedores Principales)

La aplicación utiliza dos layouts principales que nunca coexisten:

### Auth Layout (`AuthLayoutComponent`)
* **Rutas:** `/login`, `/forgot-password`
* **Guard:** `noAuthGuard` — redirige a `/` si ya hay sesión activa
* **Descripción:** Vista minimalista split-screen (1/3 marca + 2/3 contenido). Sin menú lateral ni barra superior.

### Main Layout (`MainLayoutComponent`)
* **Rutas:** Todas las rutas autenticadas (`/`, `/wells/**`, `/operations/**`, `/production/**`, `/admin/**`)
* **Guard:** `authGuard` — redirige a `/login` si no hay sesión
* **Descripción:** Contenedor principal de la aplicación. Incluye:
  * **Sidebar** — menú lateral con links de navegación filtrados por RBAC
  * **Top Header** — información del usuario, tenant, botón de logout
  * **`<router-outlet>`** — área de contenido donde se cargan los dominios
* **Composición:** El `MainLayoutComponent` es el shell padre en `app.routes.ts`. Los dominios son `children` cargados con lazy loading. Sidebar y TopHeader persisten durante toda la navegación.

### Patrón RBAC en navegación

Los items del Sidebar se renderizan condicionalmente según el rol del usuario autenticado. Cada item de navegación declara qué roles pueden verlo. Los roles específicos y permisos de cada dominio se definen en la especificación funcional (`spec.md`) de cada feature.

**Defensa en profundidad:**
1. El Sidebar **oculta** links no autorizados (UX)
2. El `roleGuard` en la ruta **bloquea** acceso por URL directa (seguridad)

Ver `CONSTITUTION.md` §16-18 para los patrones arquitectónicos detallados.

---

## 3. Mapa de Navegación (Sitemap / Routing)

El enrutamiento está estructurado siguiendo el patrón de *Domain-Driven Design* (Lazy Loading por dominios).

### 🔐 Módulo de Autenticación
* `/login` ➔ Pantalla de inicio de sesión.
* `/forgot-password` ➔ Flujo de recuperación de credenciales.

### 📊 Dashboard Principal
* `/dashboard` ➔ Panel de control general. Resumen de pozos activos, alertas de operaciones y métricas de producción.

### 🛢️ Dominio: Gestión de Pozos (`/wells`)
* `/wells` ➔ Redirecciona a `/wells/manage`.
* `/wells/manage` ➔ **Explorador Jerárquico:** Bandeja principal con tabla/grid de pozos, filtros avanzados y búsqueda.
* `/wells/create` ➔ **Wizard de Creación (Forma 101):** Flujo paso a paso para la radicación de un nuevo pozo (Información General, Datos Técnicos, Trayectoria).
* `/wells/:id/info` ➔ **Infografía del Pozo:** Vista detallada de un pozo específico (Dashboard individual, historial de estados, diagrama esquemático).

### ⚙️ Dominio: Operaciones (`/operations`)
* `/operations` ➔ Redirecciona a `/operations/forms-100`.
* `/operations/forms-100` ➔ **Bandeja de Formas 100:** Listado de reportes operativos, estados de aprobación y filtros por operadora.
* `/operations/idop` ➔ **Informe Diario de Perforación (IDOP):** Formulario complejo para el reporte diario de actividades de taladros/equipos de perforación.

### 📈 Dominio: Producción (`/production`)
* `/production` ➔ Redirecciona a `/production/fiscalization`.
* `/production/fiscalization` ➔ **Fiscalización Volumétrica (Formas 200):** Módulo para el reporte, validación y conciliación de volúmenes de producción.

### 🛡️ Dominio: Administración (`/admin`)
* `/admin` ➔ Redirecciona a `/admin/users`.
* `/admin/users` ➔ **Gestión de Usuarios:** CRUD de usuarios, asignación de roles y permisos (RBAC).
* `/admin/audit-logs` ➔ **Trazabilidad:** Visor de logs de auditoría (quién hizo qué y cuándo).

## 4. Motor de Flujos y Máquina de Estados (Workflows)

La aplicación implementa un concepto de **Máquina de Estados** para la gestión de los procesos operativos. La radicación de cualquier forma (ej. Forma 101, Forma 100, IDOP) no solo almacena datos, sino que **dispara una instancia de flujo de trabajo (Workflow)**.

### Características del Motor de Flujos:
* **Definición de Flujos:** Cada tipo de forma tiene asociado un flujo de trabajo predefinido que dicta los estados posibles, las transiciones permitidas y los actores involucrados.
* **Generación de Tareas:** Al cambiar de estado, la máquina de estados genera y asigna tareas específicas a diferentes roles (ej. "Revisar Forma 101" asignado al rol *Supervisor*, "Aprobar Forma 101" asignado al rol *Gerente*).
* **Transiciones de Estado:** Las acciones del usuario en la interfaz (Aprobar, Rechazar, Solicitar Cambios) actúan como eventos que mueven la instancia del flujo de un estado a otro (ej. `Borrador` ➔ `En Revisión` ➔ `Aprobado`).

### Impacto en la Interfaz de Usuario (UI):
* **Bandeja de Tareas (Task Inbox):** Los usuarios tendrán una vista centralizada de las tareas pendientes que requieren su acción, derivadas de las instancias de flujo activas.
* **Renderizado Condicional:** Las vistas de detalle de las formas reaccionan al estado actual del flujo. Por ejemplo:
  * Si el estado es `Aprobado`, el formulario se renderiza en modo *Sólo Lectura* (Read-Only).
  * Si el usuario actual tiene una tarea pendiente sobre esa forma, se habilitan los botones de acción (Aprobar/Rechazar).
* **Línea de Tiempo (Timeline):** Cada forma incluye un componente visual de trazabilidad que muestra el historial de estados, quién ejecutó cada transición y en qué fecha.
