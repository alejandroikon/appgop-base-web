# Spec: Módulo de Autenticación (Auth)

**Feature ID:** 001-auth
**Dominio:** `/auth` → Auth Layout (sin Sidebar, sin TopHeader)
**Estado:** Borrador — pendiente revisión del desarrollador

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
- **Entonces** el sistema almacena el token de sesión simulado, registra el rol del usuario y redirige a `/dashboard`

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
- **Cuando** el servicio mock lanza un error simulado (ej. timeout o error 503)
- **Entonces** se muestra un toast de error global (PrimeNG `MessageService`): *"Sin conexión. Verifica tu red e intenta de nuevo."*
- **Y** el formulario vuelve a estar habilitado para reintento

**Escenario 5 — Usuario ya autenticado**
- **Dado** que existe una sesión activa en el store (NgRx)
- **Cuando** el usuario navega a `/login` directamente (ej. URL manual)
- **Entonces** el Auth Guard redirige automáticamente a `/dashboard` sin mostrar el formulario de login

---

### HU-002: Cierre de Sesión

**Como** usuario autenticado,
**quiero** poder cerrar mi sesión de forma explícita,
**para** proteger mi acceso en equipos compartidos.

#### Criterios de Aceptación

**Escenario 1 — Logout exitoso**
- **Dado** que el usuario está autenticado y presiona la acción "Cerrar Sesión" (ubicada en el TopHeader)
- **Cuando** confirma la acción (si aplica dialog de confirmación)
- **Entonces** el store de NgRx limpia el estado de sesión, se elimina el token del storage y se redirige a `/login`

**Escenario 2 — Token expirado / sesión inválida**
- **Dado** que el usuario tiene una sesión simulada expirada
- **Cuando** intenta navegar a cualquier ruta protegida
- **Entonces** el Auth Guard detecta la expiración, limpia el estado y redirige a `/login` con el query param `?reason=session_expired`
- **Y** en `/login` se muestra un toast informativo: *"Tu sesión ha expirado. Por favor, inicia sesión nuevamente."*

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
- **Entonces** es redirigido a `/login` sin perder el estado del formulario de login (si había algo escrito)

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

El `AuthService` mock debe contener al menos los siguientes perfiles para validar el RBAC:

| Correo | Contraseña | Rol | Descripción |
|---|---|---|---|
| `admin@gop360.com` | `Admin123*` | `ADMIN` | Acceso total, gestión de usuarios |
| `supervisor@gop360.com` | `Super123*` | `SUPERVISOR` | Aprobación de formas operativas |
| `operador@gop360.com` | `Oper123*` | `OPERADOR` | Carga de formas y reportes diarios |
| `auditor@gop360.com` | `Audit123*` | `AUDITOR` | Solo lectura, acceso a audit logs |

---

## 5. Requisitos de UI / UX

- El layout de auth es minimalista: centrado en pantalla, sin navegación lateral
- El formulario de login debe tener el logo de GOP 360° como elemento identitario
- El botón "Iniciar Sesión" debe mostrar un spinner de carga mientras el servicio procesa
- Los mensajes de error inline deben aparecer debajo del campo correspondiente, sin desplazar el layout
- La pantalla debe ser completamente responsive (mobile-first)
- Accesibilidad: atributos `aria-label` en campos, manejo correcto de foco tras error

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