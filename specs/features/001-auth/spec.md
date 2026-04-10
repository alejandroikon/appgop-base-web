# Spec: Módulo de Autenticación (Auth)

**Feature ID:** 001-auth
**Dominio:** `/auth` → Auth Layout (sin Sidebar, sin TopHeader)
**Estado:** Aprobado — listo para generar `plan.md`

---

## 1. Contexto y Alcance

Este módulo cubre el ciclo de autenticación del usuario dentro de GOP 360°. Al ser un **prototipo funcional**, las credenciales son validadas contra un servicio mock (`AuthService`) que simula la lógica de un backend real sin realizar llamadas HTTP.

**Rutas incluidas en este spec:**
- `/login` → Pantalla de inicio de sesión
- `/forgot-password` → Flujo de recuperación de contraseña (pantalla + confirmación)

**Rutas NO incluidas (se especificarán en features posteriores):**
- `/reset-password/:token` (flujo de reset con token real)
- Integración con proveedor de identidad externo (OAuth / SSO)

---

## 2. Historias de Usuario

### HU-001: Inicio de Sesión con Credenciales

**Como** usuario registrado del sistema GOP 360°,
**quiero** ingresar mi correo electrónico y contraseña para autenticarme,
**para** acceder al panel de control y a las funcionalidades operativas según mi rol.

#### Criterios de Aceptación

**Escenario 1 — Inicio de sesión exitoso**
- **Dado** que el usuario se encuentra en `/login` con sesión no activa
- **Cuando** ingresa un correo y contraseña válidos (presentes en el mock de usuarios) y presiona "Iniciar Sesión"
- **Entonces** el sistema almacena el token de sesión simulado, registra el rol del usuario y redirige a la página principal del sistema (`/`)

**Escenario 2 — Credenciales incorrectas**
- **Dado** que el usuario se encuentra en `/login`
- **Cuando** ingresa un correo o contraseña que no corresponden a ningún usuario del mock
- **Entonces** el formulario muestra un mensaje de error inline: *"Correo o contraseña incorrectos. Verifique sus datos."*
- **Y** los campos no se limpian (el usuario puede corregir sin reescribir todo)
- **Y** el sistema NO redirige al usuario

**Escenario 3 — Campos vacíos o inválidos**
- **Dado** que el usuario se encuentra en `/login`
- **Cuando** intenta enviar el formulario con uno o ambos campos vacíos, o con un correo en formato inválido
- **Entonces** el formulario muestra mensajes de validación por campo:
  - Campo vacío: *"Este campo es requerido"*
  - Correo inválido: *"Ingrese un correo electrónico válido"*
- **Y** el botón de envío no dispara la llamada al servicio

**Escenario 4 — Error de red / servicio no disponible**
- **Dado** que el usuario envía credenciales válidas
- **Cuando** el servicio mock lanza un error simulado (ej. timeout o fallo del servidor)
- **Entonces** se muestra una notificación de error global: *"Sin conexión. Verifica tu red e intenta de nuevo."*
- **Y** el formulario vuelve a estar habilitado para reintento

**Escenario 5 — Usuario ya autenticado**
- **Dado** que existe una sesión activa
- **Cuando** el usuario navega a `/login` directamente (ej. URL manual)
- **Entonces** el guard redirige automáticamente a la página principal del sistema (`/`) sin mostrar el formulario de login

---

### HU-002: Cierre de Sesión

**Como** usuario autenticado,
**quiero** poder cerrar mi sesión de forma explícita,
**para** proteger mi acceso en equipos compartidos.

#### Criterios de Aceptación

**Escenario 1 — Logout exitoso**
- **Dado** que el usuario está autenticado y presiona la acción "Cerrar Sesión" (ubicada en el TopHeader)
- **Cuando** confirma la acción
- **Entonces** el sistema limpia el estado de sesión, elimina el token del almacenamiento y redirige a `/login`

**Escenario 2 — Token expirado / sesión inválida**
- **Dado** que el usuario tiene una sesión simulada expirada
- **Cuando** intenta navegar a cualquier ruta protegida
- **Entonces** el guard detecta la expiración, limpia el estado y redirige a `/login` con el parámetro `?reason=session_expired`
- **Y** en `/login` se muestra una notificación informativa: *"Tu sesión ha expirado. Por favor, inicia sesión nuevamente."*

---

### HU-003: Recuperación de Contraseña

**Como** usuario que olvidó su contraseña,
**quiero** solicitar un enlace de recuperación mediante mi correo registrado,
**para** poder restablecer mi acceso sin necesidad de contactar al administrador.

#### Criterios de Aceptación

**Escenario 1 — Solicitud enviada correctamente**
- **Dado** que el usuario se encuentra en `/forgot-password`
- **Cuando** ingresa un correo registrado en el mock y presiona "Enviar enlace"
- **Entonces** se muestra una pantalla de confirmación con el mensaje: *"Si el correo está registrado, recibirás un enlace de recuperación en los próximos minutos."*
- **Y** no se revela si el correo existe o no (seguridad anti-enumeración)

**Escenario 2 — Correo no registrado**
- **Dado** que el usuario ingresa un correo NO presente en el mock
- **Cuando** presiona "Enviar enlace"
- **Entonces** el sistema muestra la **misma** pantalla de confirmación que en el Escenario 1 (sin revelar que el correo no existe)

**Escenario 3 — Correo en formato inválido**
- **Dado** que el usuario escribe un correo con formato incorrecto (sin @, sin dominio)
- **Cuando** intenta enviar el formulario
- **Entonces** se muestra validación inline: *"Ingrese un correo electrónico válido"*
- **Y** el servicio no es invocado

**Escenario 4 — Navegación de vuelta al Login**
- **Dado** que el usuario se encuentra en `/forgot-password`
- **Cuando** presiona el enlace "Volver al inicio de sesión"
- **Entonces** es redirigido a `/login` con el formulario en estado inicial (reposo)

---

## 3. Reglas de Negocio

| Regla | Descripción |
|---|---|
| RN-001 | El correo electrónico es case-insensitive: `Admin@gop.com` y `admin@gop.com` son equivalentes |
| RN-002 | La contraseña es case-sensitive |
| RN-003 | No existe límite de intentos de login en el prototipo (se documentará para backend real) |
| RN-004 | El token de sesión mock tiene duración configurable (default: 8 horas) |
| RN-005 | El sistema de recuperación de contraseña no revela si un correo existe o no |
| RN-006 | El layout de autenticación (`/auth`) no debe mostrar Sidebar ni TopHeader bajo ninguna circunstancia |

---

## 4. Usuarios Mock (Datos Simulados)

El `AuthService` mock debe contener al menos los siguientes perfiles para validar el RBAC. Cada usuario pertenece a un operador (tenant) que será utilizado por módulos futuros para filtrar información por empresa:

| Correo | Contraseña | Rol | Operador (Tenant) | Descripción |
|---|---|---|---|---|
| `admin@gop360.com` | `Admin123*` | `ADMIN` | Agencia Nacional de Hidrocarburos | Acceso total, gestión de usuarios |
| `supervisor@gop360.com` | `Super123*` | `SUPERVISOR` | Ecopetrol S.A. | Aprobación de formas operativas |
| `operador@gop360.com` | `Oper123*` | `OPERADOR` | Ecopetrol S.A. | Carga de formas y reportes diarios |
| `auditor@gop360.com` | `Audit123*` | `AUDITOR` | Agencia Nacional de Hidrocarburos | Solo lectura, acceso a audit logs |

---

## 5. Requisitos de UI / UX

> **Imagen de referencia:** [`specs/features/001-auth/ref-login.png`](ref-login.png) — Prototipo visual aprobado del login. Todos los componentes de UI deben respetar la estructura, proporciones y disposición de elementos mostrados en esta imagen.

### 5.1. Estructura General del Layout de Auth

El layout de autenticación es exclusivo para las rutas `/login` y `/forgot-password`. No muestra Sidebar ni TopHeader bajo ninguna circunstancia.

El diseño es una **pantalla dividida (Split-Screen)** en proporción fija **1/3 — 2/3**:
- El panel izquierdo ocupa 1/3 del ancho total de la pantalla.
- El panel derecho ocupa 2/3 del ancho total de la pantalla.
- En dispositivos móviles, el panel izquierdo se oculta y el panel derecho ocupa el 100% de la pantalla.

Ambas pantallas (`/login` y `/forgot-password`) comparten exactamente el mismo layout; solo cambia el contenido del panel derecho.

---

### 5.2. Panel Izquierdo — Visual/Marca (1/3)

- **Fondo:** Imagen fotográfica representativa de la industria de hidrocarburos, configurable sin recompilar la aplicación. Sobre la imagen se aplica una capa de superposición (overlay) con color azul navy oscuro y opacidad parcial para garantizar contraste y legibilidad del texto.
- **Acento visual:** Línea horizontal corta en color amarillo/dorado, ubicada en la zona media-izquierda del panel, por encima del bloque de texto. Sirve como elemento de identidad de marca.
- **Título principal:** *"Bienvenido a GOP 360°"* — tipografía grande, peso bold, color blanco, en la zona inferior-izquierda del panel.
- **Subtexto descriptivo:** *"Accede a tu cuenta para gestionar todos los recursos de manera eficiente y segura."* — tipografía regular, color blanco con menor prominencia que el título, inmediatamente debajo del título.
- El panel no contiene logo ni ningún otro elemento interactivo.

---

### 5.3. Panel Derecho — Formulario (2/3)

Fondo blanco sólido. El contenido está distribuido verticalmente en tres zonas:

**Zona superior — Identidad institucional:**
- Logo institucional combinado (Ministerio de Energía + ANH) ubicado en la parte superior izquierda del panel.
- Archivo de imagen: `src/assets/icons/login-logo.png`.

**Zona central — Formulario (centrado verticalmente):**
- Título de bienvenida y subtítulo descriptivo (varía según la pantalla, ver §5.4 y §5.5).
- Campos del formulario con labels en mayúsculas pequeñas, marcados con asterisco (*) si son requeridos, e ícono de información (ⓘ) al lado del label.
- Cada campo tiene un ícono decorativo a la izquierda dentro del input (ícono de sobre para correo, ícono de candado para contraseña).
- El campo de contraseña tiene además un ícono de toggle (ojo) a la derecha para mostrar/ocultar el texto.
- Botón de acción principal de ancho completo, fondo azul navy (mismo tono del panel izquierdo), texto blanco en bold.

**Zona inferior — Pie del panel:**
- Texto de versión del sistema: *"GOP 360° v1.0.0"* en tipografía pequeña, color gris muted, alineado a la izquierda.

---

### 5.4. Contenido de la Pantalla Login (`/login`)

**Zona central:**
- Título: *"Bienvenido de nuevo"*
- Subtítulo: *"Ingresa a tu cuenta para continuar"*
- Campo **CORREO ELECTRÓNICO \*** — placeholder: `usuario@dominio.com`
- Campo **CONTRASEÑA \*** — con toggle de visibilidad
- Enlace *"¿Olvidó su contraseña?"* alineado a la derecha, que navega a `/forgot-password`
- Botón *"Iniciar Sesión"*

**Estados de la pantalla:**
- **Reposo:** Formulario habilitado, esperando interacción.
- **Enviando:** Botón deshabilitado con indicador de carga; texto cambia a *"Ingresando..."*. Campos no editables.
- **Error de validación:** Mensaje inline debajo del campo afectado, sin desplazar el layout.
- **Error de autenticación/red:** Notificación global de error en pantalla.
- **Éxito:** Redirección inmediata a la página principal del sistema (`/`); sin estado visual intermedio.

---

### 5.5. Contenido de la Pantalla Recuperación de Contraseña (`/forgot-password`)

Mismo layout que `/login`. Solo cambia la zona central del panel derecho:

- Título: *"Recuperar contraseña"*
- Subtítulo: *"Ingresa tu correo registrado y te enviaremos las instrucciones."*
- Campo **CORREO ELECTRÓNICO \***
- Botón *"Enviar enlace de recuperación"*
- Enlace *"Volver al inicio de sesión"* alineado debajo del botón

**Estados de la pantalla:**
- **Reposo:** Formulario habilitado.
- **Enviando:** Botón deshabilitado con indicador de carga.
- **Confirmación:** El formulario es reemplazado por un mensaje neutral (aplica tanto si el correo existe como si no): *"Si el correo está registrado, recibirás un enlace de recuperación en los próximos minutos."* y un botón *"Volver al inicio de sesión"*. No se vuelve a mostrar el formulario en esta misma visita.

---

### 5.6. Comportamiento del Toggle de Contraseña (Show/Hide)

- **Dado** que el campo de contraseña contiene texto ingresado
- **Cuando** el usuario presiona el ícono de ojo a la derecha del campo
- **Entonces** el texto de la contraseña alterna entre visible y oculto, y el ícono cambia para reflejar el estado actual
- El botón de toggle tiene una etiqueta accesible para lectores de pantalla que cambia dinámicamente: *"Mostrar contraseña"* / *"Ocultar contraseña"*

---

### 5.7. Accesibilidad

- Todos los campos tienen etiqueta visible asociada.
- Los íconos decorativos dentro de los campos son puramente visuales (no transmiten información al lector de pantalla por sí solos).
- El ícono de toggle de contraseña es el único ícono interactivo; tiene etiqueta accesible.
- El foco se posiciona automáticamente en el primer campo con error tras un intento de envío fallido.
- El contraste de texto sobre los fondos (blanco y azul navy) cumple nivel AA de accesibilidad.
- El formulario puede enviarse presionando Enter desde cualquier campo.

---

## 6. Restricciones Técnicas

- El módulo de Auth NO debe importar nada de `domains/` — solo puede depender de `core/` y `shared/`
- El `AuthService` en `core/auth/` es el único punto de verdad del estado de sesión
- El estado de sesión (usuario, rol, token) vive en NgRx (`core/` store o feature store global)
- Las rutas `/login` y `/forgot-password` son las **únicas rutas públicas** del sistema; todas las demás requieren autenticación
- El guard de autenticación debe operar sobre el estado NgRx, no consultando directamente el storage

---

## 7. Edge Cases Identificados

| ID | Escenario | Comportamiento Esperado |
|---|---|---|
| EC-001 | Usuario escribe espacios en blanco en los campos | Trim antes de validar; tratar como vacío si solo hay espacios |
| EC-002 | Usuario presiona Enter en el campo contraseña | Equivalente a presionar el botón de submit |
| EC-003 | Usuario copia/pega correo con espacios al inicio o final | Trim automático antes de enviar al servicio |
| EC-004 | Usuario desactiva JavaScript (escenario extremo) | Fuera de alcance del prototipo |
| EC-005 | Navegación directa a ruta protegida sin sesión | Guard redirige a `/login` y conserva la URL original como `returnUrl` |
| EC-006 | Usuario usa el botón atrás del navegador tras logout | Guard detecta sesión inválida y mantiene en `/login` |
| EC-007 | Doble clic en "Iniciar Sesión" | El botón se deshabilita al primer clic; se evita doble submit |
| EC-008 | Correo registrado con diferente capitalización | RN-001: normalizar a minúsculas antes de comparar en el mock |

---

## 8. Checklist de Revisión (Desarrollador)

Antes de aprobar este spec y generar el `plan.md`, el desarrollador debe validar:

- [ ] Los criterios de aceptación cubren todos los flujos identificados en `blueprint.md`
- [ ] Los usuarios mock son suficientes para probar todos los roles del sistema (RBAC)
- [ ] Los edge cases cubren los escenarios de navegación con el router de Angular
- [ ] La regla de no-revelación de correo en recuperación es aceptable para el prototipo
- [ ] El `returnUrl` tras redirección está contemplado en los casos de uso
- [ ] Se alineó con la sección de Auth Layout del `blueprint.md`