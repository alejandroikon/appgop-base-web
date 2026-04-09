# GOP 360° - Contexto del Proyecto (Claude Code)

## Metodología de Trabajo: Spec-Driven Development (SDD)

Este proyecto sigue la metodología SDD de INTERKONT. **Reglas obligatorias:**

1. **Nunca escribir código sin artefactos aprobados.** El orden es: `spec.md` → `plan.md` → `tasks.md` → implementación.
2. **Antes de implementar cualquier tarea**, leer el `tasks.md` de la feature activa y confirmar cuál tarea se ejecuta.
3. **Marcar `[x]`** en `tasks.md` únicamente cuando la tarea cumpla los estándares de calidad definidos en `CONSTITUTION.md`.
4. **Las tareas deben ser atómicas**: máximo un archivo por tarea. Si una tarea abarca más de un archivo, dividirla.

### Ejecución por Bloques Compilables

Las tareas en `tasks.md` se agrupan en **bloques de implementación**. Cada bloque es una secuencia de tareas que, al completarse, produce una aplicación que:

1. **Compila** sin errores (`ng build` ✅)
2. **Tiene un resultado validable** en la UI o en herramientas de desarrollo (ej. NgRx DevTools)

**Reglas de ordenamiento dentro de cada bloque:**

- Las tareas marcadas `[P]` pueden ejecutarse en paralelo (archivos distintos, sin dependencias).
- Los **componentes se crean ANTES que las rutas** que los referencian, porque Angular resuelve los `loadComponent: () => import(...)` en compilación (AOT).
- Los **modelos y tipos** se crean antes que los servicios/stores que los consumen.
- Las **rutas** (`*.routes.ts`) solo se crean/modifican cuando todos los componentes que importan ya existen.
- No avanzar al siguiente bloque hasta que `ng build` pase en el bloque actual.

**Estructura de fases típica:**

```
Phase 1 (Setup)       → Modelos, tipos, configuración de entorno
Phase 2 (Foundational)→ Store, servicios, guards — BLOQUEA fases siguientes
Phase 3+ (US*)        → Una fase por historia de usuario, en orden de prioridad
Phase N (Polish)      → Validación cruzada, imports, textos
```

Cada fase puede contener uno o más bloques. Consultar la sección **"Implementation Blocks"** del `tasks.md` activo para la secuencia exacta de ejecución con `/speckit-implement`.

---

## Constitución y Arquitectura Global

Debes adherirte estrictamente a las reglas arquitectónicas definidas en:
**`CONSTITUTION.md`** (raíz del proyecto)

No sugieras patrones de diseño que violen esos principios. Si hay duda entre dos enfoques, el `CONSTITUTION.md` es la fuente de verdad.

El mapa funcional completo del sistema está en:
**`blueprint.md`** (raíz del proyecto)

---

## Feature Activa

> **Actualizar esta sección cada vez que se cambie de feature o módulo.**

- **Feature:** Autenticación - Login
- **Spec:** `specs/features/001-auth/spec.md`
- **Plan:** `specs/features/001-auth/plan.md`
- **Tareas:** `specs/features/001-auth/tasks.md`

---

## Stack Tecnológico

- **Framework:** Angular (Standalone Components, sin NgModules)
- **Estado global:** NgRx
- **Estado local:** Signals
- **UI:** PrimeNG + Tailwind CSS
- **Lenguaje:** TypeScript estricto

---

## Reglas Críticas de Implementación (Angular)

### Componentes
- Todos los componentes son **Standalone** (NO agregar `standalone: true`, está implícito por defecto)
- Siempre usar `changeDetection: ChangeDetectionStrategy.OnPush`
- Usar `input()` y `output()` en lugar de `@Input()` / `@Output()`
- Usar `inject()` en lugar de inyección por constructor

### Templates
- Control flow nativo: `@if`, `@for` (con `track`), `@switch` — **prohibido** `*ngIf`, `*ngFor`
- **Prohibido** `NgClass`/`[ngClass]` → usar `[class]`
- **Prohibido** `NgStyle`/`[ngStyle]` → usar `[style]`
- **Prohibido** `ng-template` / `ng-container` para control flow

### Estado
- **Signals** para estado local de componente/feature (carga, pasos, modales)
- **NgRx** para estado que cruza límites de feature (sesión, catálogos globales)

### Servicios
- `providedIn: 'root'` para singletons
- Los servicios de dominio **nunca** manejan errores HTTP (eso es exclusivo de `core/http/error.interceptor.ts`)
- Los servicios solo transforman la respuesta exitosa con `map(mapper)`

### Textos
- **Prohibido** hardcodear textos en HTML o TS
- Textos globales → `shared/locale/locale.ts`
- Textos de feature → `domains/.../features/.../locale.ts`

### Imports
- Siempre usar path aliases: `@core/*`, `@shared/*`, `@wells/*`, `@operations/*`, `@production/*`, `@admin/*`
- Solo se permiten rutas relativas dentro de la misma feature

### TypeScript
- Strict mode activo. **Prohibido** usar `any` — usar `unknown` con type guards si aplica
- Preferir inferencia de tipos cuando sea obvio

---

## Instrucciones Generales

1. Antes de escribir código, verifica si hay una tarea asignada en `tasks.md`.
2. Ejecuta `ng build` después de cada cambio para verificar errores de compilación.
3. Si encuentras un error que no puedes resolver, repórtalo con: mensaje exacto, ubicación y causa probable.
4. No inicies el servidor de desarrollo (`ng serve`), el entorno ya lo gestiona.
5. No crear archivos o carpetas que no estén especificados en el `plan.md` activo.