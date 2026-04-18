# ADR-001: Principios Arquitectónicos Transversales

**Estado:** Aceptado
**Fecha:** 2026-04-18
**Contexto:** Iter 9 — Creación de Pozo Nuevo V2.0
**Aplica a:** Todo código nuevo desde esta iteración en adelante.

---

## Decisión

A partir de la Iteración 9, todo código nuevo en el repositorio sigue estos principios. El código existente (iters 001-008) se actualiza progresivamente conforme se toque.

---

## 1. Frontend

### 1.1. Signals First

- `signal()`, `computed()`, `effect()`, `toSignal()` son la herramienta por defecto para reactividad.
- RxJS se usa **solo** para streams genuinos: inputs del usuario con debounce, respuestas HTTP, WebSocket.
- Ver CONSTITUTION.md §4.

### 1.2. NgRx Pattern Canónico

```
Component → dispatch(action)
    → Effect(HTTP via ApiService)
        → *Success | *Failure
        → Effect({dispatch:false}) para side-effects (toast, navigate)
```

- Ningún componente importa `HttpClient`.
- Los servicios API (`WellsApiService`, etc.) son la **única puerta HTTP**.
- Navegación post-acción **exclusivamente** en Effects (CONSTITUTION.md §4.1).

### 1.3. API Services como Única Puerta HTTP

- Tipados desde OpenAPI (`contract.yml`).
- Solo transforman respuesta exitosa con `map(mapper)`.
- Nunca manejan errores HTTP (eso es del interceptor, CONSTITUTION.md §5).

### 1.4. Feature Folder Structure

```
domains/{dominio}/
├── data-access/       # ApiService, index.ts
├── domain/            # Lógica pura: validators, generators, state-machine
├── features/          # Smart components (feature-*)
│   └── feature-name/
│       ├── components/ # Dumb components internos
│       ├── locale.ts
│       └── feature-name.component.ts
├── models/            # DTOs, Models, Mappers, Enums
├── store/             # NgRx: actions, reducer, selectors, effects
├── ui/                # Dumb components compartidos del dominio
├── mocks/             # Mock handlers (dev only)
└── routes.ts
```

### 1.5. Validators Puros

- Funciones puras en `domain/*.validators.ts`.
- Sin dependencia de Angular, inyección, o side effects.
- Cobertura 100% con tests unitarios (`.spec.ts`).
- Usados tanto por componentes (validación de formulario) como por tests.

### 1.6. State Machine Client-Side

- `canTransition(from, action, role)` en `domain/*.state-machine.ts`.
- Determina qué acciones puede ver cada rol según el estado actual.
- **No aplica para Iter 9** (solo 2 estados sin transiciones), pero se establece el patrón para features futuras.

---

## 2. Backend

### 2.1. Clean Architecture

```
Domain → Application → Infrastructure → API
```

- Domain sin dependencias externas (CONSTITUTION.backend.md §2, §3).

### 2.2. MediatR 12

- Commands y Queries como únicos puntos de entrada a la lógica.
- Pipeline: Logging → Validation → Authorization → Handler.
- Handlers son `internal sealed` (CONSTITUTION.backend.md §4.4).

### 2.3. Specifications + FluentValidation

- FluentValidation para formato y presencia.
- Handler para reglas de negocio.
- Cada RN referenciada por ID en el código:

```csharp
// RN-15: ANH solo puede crear pozos Estratigráficos
if (isAnh && request.Clasificacion != Clasificacion.Estratigrafico)
    return Result.Failure<Guid>(DomainErrors.Well.InvalidClasificacionForAnh);
```

### 2.4. Domain Events

- Para efectos secundarios: notificaciones, auditoría, integraciones.
- `WellCreatedEvent`, `WellFinalizedEvent` (futuro).
- No aplican exhaustivamente en Iter 9, pero el patrón queda establecido.

### 2.5. Contract-First

- El OpenAPI (`contract.yml`) manda sobre los DTOs.
- Los DTOs son `sealed record` espejo del contrato.
- Si hay discrepancia, el contrato prevalece.

---

## Consecuencias

### Positivas

- Código predecible: mismo patrón en todas las features.
- Testeable: lógica pura en `domain/`, validators desacoplados.
- Mantenible: un desarrollador que conoce una feature puede trabajar en cualquier otra.
- Contract-first: FE y BE pueden desarrollar en paralelo con certeza de compatibilidad.

### Negativas

- Boilerplate inicial mayor (NgRx actions/reducer/effects/selectors por feature).
- Curva de aprendizaje para desarrolladores no familiarizados con Clean Architecture + CQRS.

---

## Referencias

- CONSTITUTION.md (frontend Angular)
- CONSTITUTION.backend.md (backend .NET)
- CONSTITUTION.contracts.md (contratos OpenAPI)
