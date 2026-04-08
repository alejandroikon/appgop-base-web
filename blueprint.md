# Documentación Funcional Base (Blueprint) - GOP 360°

## 1. Contexto de la Aplicación
**GOP 360° (Gestión Operativa de Pozos)** es una plataforma integral diseñada para administrar el ciclo de vida completo de los pozos de hidrocarburos, desde su creación y perforación hasta su producción y fiscalización.

**Naturaleza del Proyecto Actual:** 
Este proyecto es un **Prototipo Funcional (Maqueta)**. Su objetivo principal es validar la experiencia de usuario (UX), la interfaz de usuario (UI) y los flujos de navegación complejos antes de la integración con los servicios backend reales. 
* Toda la lógica de enrutamiento (Routing), protección de rutas (Guards) y gestión de estado de la UI es **real y funcional**.
* Los datos presentados son **mocks** (datos simulados) aislados en la capa de servicios, garantizando que la capa de presentación no tenga datos "quemados".

---

## 2. Estructura de Layouts (Contenedores Principales)

La aplicación utiliza dos layouts principales para manejar la experiencia del usuario:

1. **Auth Layout (`/auth`)**: 
   * **Descripción**: Vista minimalista, sin menú lateral ni barra superior de navegación.
   * **Uso**: Pantalla de inicio de sesión, recuperación de contraseña.
2. **Main Layout (`/`)**: 
   * **Descripción**: Contenedor principal de la aplicación. Incluye el **Sidebar** (menú lateral de navegación principal) y el **Top Header** (perfil de usuario, notificaciones, breadcrumbs).
   * **Uso**: Todas las vistas internas de los dominios de negocio.

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
