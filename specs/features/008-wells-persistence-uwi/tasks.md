# Tasks — Iter 8 · Creación de Pozo Nuevo (Persistencia + UWI)

**Feature ID:** `008-wells-persistence-uwi`
**Entrada:** `blueprint.md` + `plan.md` en esta misma carpeta.
**Audiencia:** GOP-Spec → GOP-Backend → GOP-Frontend → GOP-QA → GOP-Docs agents.

---

## Cómo usar este documento

Cada fase tiene un **owner agent**, **precondiciones** (qué debe estar listo antes de empezar) y **definición de hecho** (qué debe entregar para pasar al siguiente).

Las fases son **secuenciales entre sí** pero **dentro de cada fase** varias tareas corren en paralelo si no hay dependencia explícita.

**Regla dura:** si un agente descubre que necesita hacer algo que no está listado aquí, lo documenta en `EMERGENT-DECISIONS.md` y sigue. Si descubre que **no puede** completar una tarea como está descrita, para y genera un ticket/mensaje.

---

## Fase 0 — GOP-Spec: validación y setup

**Owner:** GOP-Spec agent
**Duración estimada:** 1-2h
**Rama:** `008-wells-persistence-uwi` creada desde `gop-base-web`

### T0.1 — Crear estructura de la feature
- [ ] Crear `specs/features/008-wells-persistence-uwi/` con:
  - `README.md` (resumen 1 página, enlaces a blueprint/plan/tasks)
  - `blueprint.md` (copia del entregado por Cowork)
  - `plan.md` (idem)
  - `tasks.md` (idem)
  - `EMERGENT-DECISIONS.md` (vacío, se llena durante ejecución)
- [ ] Crear rama `008-wells-persistence-uwi` desde `gop-base-web`.
- [ ] Crear borrador de PR con el template del repo y enlace al blueprint.

### T0.2 — Validación del catálogo existente
**Critical blocker de §2.4 del plan — resolver antes de pasar a backend.**
- [ ] Revisar seed de Iter 3 en `src/Gop.Infrastructure/Persistence/Seeds/CatalogSeeder.cs`:
  - ¿Los códigos de `WellObjective` son 2 chars o 4 chars?
  - ¿`AngleType` usa V/H/D/J o nombres completos?
  - ¿`CompletionType` usa CC/OH... o algo distinto?
- [ ] Comparar con los ejemplos de `api-contract.md` §4.1 y `spec-backend.md` §4.2 ejemplos.
- [ ] Si hay discrepancia:
  - Preparar cambio al seeder (no migración — los catálogos son seed-time).
  - Documentar decisión en `EMERGENT-DECISIONS.md`.
- [ ] Verificar existencia de:
  - Al menos 1 `Operator`
  - Al menos 1 `Contract` (DEVELOPMENT)
  - Al menos 1 `Field`
  - Al menos 1 `ClusterLocation`
  - 2 `Department` DANE (mínimo Meta=50, Casanare=85)
  - 6 `Municipality` DANE (mínimo 3 por depto)
- [ ] Si faltan catálogos, añadirlos al seeder.

### T0.3 — Validación de contrato JWT
- [ ] Revisar `src/Gop.Infrastructure/Authentication/JwtTokenGenerator.cs` (Iter 7).
- [ ] Confirmar que el JWT incluye claim `OperatorId` para OperatorAgent. Si no:
  - Añadir claim `operatorId` al GenerateToken().
  - Documentar en EMERGENT-DECISIONS.
- [ ] Confirmar que `ICurrentUserService` expone `OperatorId`. Si no, extender interface + implementación.

### T0.4 — Seed del usuario seed apuntando a un Operator real
- [ ] Revisar `UserSeeder` de Iter 7. Los dos seed users (`alejandro.gutierrez@interkont.co` y `admin@interkont.co`) deben tener un `OperatorId` poblado para que el global query filter funcione en staging.
- [ ] Si la tabla `Operators` tiene al menos 1 fila, asignar `OperatorId` del primer Operator al seed user OperatorAgent.
- [ ] Si el seed user es ANH y `OperatorId` debe ser `null`, documentar en EMERGENT-DECISIONS que el filter-bypass para ANH es deuda hacia Iter 9.

### Definición de hecho fase 0
- Rama creada, PR draft abierto, estructura de feature en repo.
- Catálogos validados y seed completados.
- JWT extendido con OperatorId.
- Ningún blocker pendiente para que backend-agent arranque.

---

## Fase 1 — GOP-Backend: Domain + Application + Infrastructure + API

**Owner:** GOP-Backend agent
**Duración estimada:** 6-10h
**Bloques paralelizables:** B1 (Domain) primero; B2/B3 (App + Infra) en paralelo; B4 (API) al final.

### Bloque B1 — Domain (secuencial, ~2h)

#### T1.1 — Value Objects
- [ ] Crear `src/Gop.Domain/Entities/Wells/ValueObjects/WellName.cs` con factory `Compose(denomination, consecutive, trajectory)`.
- [ ] Crear `src/Gop.Domain/Entities/Wells/ValueObjects/FiscalizedUwi.cs` con factory `FromComponents(UwiComponents)`.
- [ ] Crear `src/Gop.Domain/Entities/Wells/ValueObjects/UwiComponents.cs` con los 8 componentes como props read-only.
- [ ] Heredar de `ValueObject` base (o crear base si no existe).
- [ ] Tests en `tests/Gop.Domain.Tests/Wells/ValueObjects/` para igualdad estructural y factories.

#### T1.2 — Enums
- [ ] Crear `TrajectoryType`, `ClassificationType`, `WellStatus`, `AngleType`, `ObjectiveType`, `CompletionType` en `src/Gop.Domain/Entities/Wells/Enums/`.
- [ ] Cada enum incluye TODOS los valores del V2 spec (aunque Iter 8 solo use un subset).
- [ ] Método de extensión `ToCode()` para cada enum que mapea a código string (ej: `Original → "O"`, `Vertical → "V"`).

#### T1.3 — Extender Well entity
- [ ] En `src/Gop.Domain/Entities/Wells/Well.cs`:
  - Añadir propiedades según §2.1 del plan.
  - Añadir factory `CreateNew(...)` con validaciones RN-17, RN-18 de invariante.
  - Mantener `Name` (legacy) setteado a `WellName.Value` para no romper seeds/tests existentes.
- [ ] Tests en `tests/Gop.Domain.Tests/Wells/WellTests.cs`:
  - CreateNew happy path.
  - CreateNew con denominación con sigla reservada → `DomainException`.
  - CreateNew con consecutivo con letras → `DomainException`.

#### T1.4 — Domain Services
- [ ] Crear `IUwiGenerator` + `UwiGenerator` en `src/Gop.Domain/Services/`.
- [ ] Implementar lógica PPDM Original+Development según §2.4 del plan.
- [ ] Implementar helpers privados: `ComputeWellNameSigle`, `ComputeClusterCode`, `RemoveDiacritics`.
- [ ] Para trayectorias/clasificaciones no soportadas, lanzar `NotSupportedInIter8Exception`.
- [ ] Crear `IWellNameComposer` + `WellNameComposer`.
- [ ] Tests en `tests/Gop.Domain.Tests/Services/UwiGeneratorTests.cs`:
  - 5+ casos happy path con inputs reales.
  - Casos borde: denominación con acentos, <4 chars, con espacios múltiples.
  - Caso unsupported: trayectoria ST → excepción específica.

#### T1.5 — Domain Exceptions
- [ ] Crear `NotSupportedInIter8Exception : DomainException` con propiedad `IterTarget`.
- [ ] Crear `DuplicateWellNameException : DomainException`.
- [ ] Crear `DuplicateUwiException : DomainException`.
- [ ] `DomainErrorCodes` estáticas para cada code del API contract.

**Definición de hecho B1:** `dotnet test tests/Gop.Domain.Tests` pasa ≥85% cobertura en Wells.

### Bloque B2 — Application (paralelo con B3, ~2h)

#### T1.6 — DTOs
- [ ] `src/Gop.Application/Wells/Dtos/`:
  - `CreateWellRequest` (payload entrada)
  - `CreateWellResponse` (201 body)
  - `WellDetailResponse` (shape de `GET /wells/{id}` según spec §4.3)
  - `WellListItem` (item de `GET /wells`)
  - `PreviewUwiRequest` / `PreviewUwiResponse`
  - `PagedResult<T>` (si no existe desde Iter 3)

#### T1.7 — Commands + Handlers
- [ ] `CreateWellCommand` + `CreateWellCommandHandler` según §3.1 del plan.
- [ ] Handler orquesta: validación → resolver OperatorId → composición nombre → generación UWI → check unicidad → factory `Well.CreateNew` → `repo.Add` → `UnitOfWork.Save`.
- [ ] Mapear excepciones de dominio a resultados de handler (o dejar que suban al middleware).

#### T1.8 — Queries + Handlers
- [ ] `GetWellByIdQuery` + handler (mapper Well → WellDetailResponse).
- [ ] `ListWellsQuery` + handler (filtros, paginación, sort).
- [ ] `PreviewUwiQuery` + handler.

#### T1.9 — Validators (FluentValidation)
- [ ] `CreateWellValidator` según §3.3 del plan.
- [ ] `PreviewUwiValidator`.
- [ ] Configurar pipeline behavior que los invoque antes del handler.

#### T1.10 — Abstractions
- [ ] `IWellRepository` en `src/Gop.Application/Abstractions/`.
- [ ] `IUnitOfWork` ya debe existir desde Iter 7.

**Definición de hecho B2:** `dotnet test tests/Gop.Application.Tests` pasa ≥80% cobertura. Todos los commands/queries con tests happy+error.

### Bloque B3 — Infrastructure (paralelo con B2, ~2h)

#### T1.11 — EF Core Configuration
- [ ] Extender `src/Gop.Infrastructure/Persistence/Configurations/WellConfiguration.cs` según §4.1 del plan.
- [ ] `OwnsOne` para `WellName`, `FiscalizedUwi`, `UwiComponents`.
- [ ] Conversiones enum → int.
- [ ] Índices únicos y compuestos.
- [ ] Global query filter por `OperatorId` vía `ICurrentUserService`.

#### T1.12 — Migration
- [ ] Generar migration:
  ```bash
  dotnet ef migrations add AddWellPersistenceColumns \
    -c ApplicationDbContext \
    -p src/Gop.Infrastructure \
    -s src/Gop.Api
  ```
- [ ] Revisar SQL generado. Debe ser exclusivamente `ALTER TABLE` y `CREATE INDEX` — sin `DROP`.
- [ ] Aplicar local: `dotnet ef database update` + verificar schema.
- [ ] Si SQLite in-memory (para tests), asegurar que migrations aplican.

#### T1.13 — Repositorio
- [ ] `src/Gop.Infrastructure/Persistence/Repositories/WellRepository.cs` implementando `IWellRepository`.
- [ ] `ExistsByWellNameAsync` y `ExistsByUwiAsync` usan `IgnoreQueryFilters()` (ver §4.4 plan).
- [ ] Register DI en `InfrastructureServiceExtensions`.

#### T1.14 — JWT operator claim
- [ ] Extender `JwtTokenGenerator` para emitir claim `operatorId`.
- [ ] Extender `CurrentUserService` para leer claim `operatorId` de `HttpContext`.
- [ ] Si `OperatorId = Guid.Empty` (ej: rol ANH sin operador asignado), el global query filter devuelve 0 rows — documentar en EMERGENT-DECISIONS como deuda Iter 9.

**Definición de hecho B3:** migration generada y aplicable. Smoke test local (crear pozo vía Swagger) funciona.

### Bloque B4 — API (secuencial, depende de B1+B2+B3, ~2h)

#### T1.15 — Controller
- [ ] `src/Gop.Api/Controllers/WellsController.cs` con 4 endpoints implementados según §5.1 del plan.
- [ ] Authorize attributes según matriz de roles (§2.1 spec).
- [ ] `[FromBody]`, `[FromQuery]`, `[FromRoute]` correctos.

#### T1.16 — Exception handling
- [ ] Extender `ExceptionHandlingMiddleware` (Iter 2) con los nuevos mappings según §5.2 plan.
- [ ] Asegurar catch de `SqlException (2601/2627)` → `DUPLICATE_UWI` como fallback.
- [ ] Respuestas en formato RFC 7807.

#### T1.17 — Endpoints 501 para diferidos
- [ ] Registrar 4 endpoints placeholder (PUT, DELETE, finalize, parent-well-data) que responden `501 NOT_IMPLEMENTED` con body:
  ```json
  { "code": "NOT_IMPLEMENTED", "message": "Endpoint diferido a Iter X", "traceId": "..." }
  ```

#### T1.18 — Tests de integración
- [ ] `tests/Gop.Api.IntegrationTests/Wells/WellsControllerTests.cs` con los 5 tests de §9.2 plan.
- [ ] Test 3 (tenant isolation) es **obligatorio** — bloquea merge si no pasa.

**Definición de hecho B4:**
- `dotnet test` global pasa.
- Cobertura global ≥60%.
- Swagger expone los 4 endpoints con shapes correctos.
- Smoke local: login → POST pozo → GET lista lo muestra.
- Commit con push a la rama y CI verde.

### Definición de hecho Fase 1
- Todos los tests verdes en CI.
- Imagen publicada en ACR (via CI workflow backend-ci.yml).
- Migration generada, revisada, aplicable.
- PR descriptor actualizado con lista de endpoints + errores soportados.

---

## Fase 2 — GOP-Frontend: wire-up wizard + fix municipios + lista/detalle

**Owner:** GOP-Frontend agent
**Duración estimada:** 6-10h
**Bloques paralelizables:** F1 (service) primero; F2/F3/F4 en paralelo si bandwidth.
**Requiere:** Fase 1 completa con BE desplegado en staging (para testing contra real).

### T2.1 — Service de wells
- [ ] Crear/extender `src/app/features/wells/services/wells.service.ts` con los 4 métodos del §6.1 plan.
- [ ] DTOs TypeScript en `src/app/features/wells/models/`:
  - `create-well-request.model.ts`
  - `create-well-response.model.ts`
  - `well-detail.model.ts`
  - `well-list-item.model.ts`
  - `preview-uwi-request.model.ts`
  - `preview-uwi-response.model.ts`
- [ ] Opcional: generar desde `openapi.yaml` con `openapi-generator-cli`. Si el tiempo no alcanza, escribir manualmente.

### T2.2 — Wizard: wire-up preview-uwi
- [ ] En `well-creation-wizard.component.ts` paso 3:
  - Al entrar al paso, disparar `wellsService.previewUwi(payload)`.
  - Mostrar spinner PrimeNG mientras resuelve.
  - Mostrar `fiscalizedUwi` y `wellName` en card prominente.
  - Si error: toast `p-toast` + bloquear avanzar.
- [ ] Remover cualquier mock/dummy del UWI del Iter 4.

### T2.3 — Wizard: fix cascada municipios
- [ ] Diagnóstico: reproducir el bug local.
- [ ] Aplicar fix según §6.3 plan (store reactivo que persista el estado).
- [ ] Test manual: llenar depto+municipio → avanzar → volver → volver al depto → municipio debe seguir seleccionado.
- [ ] Si el fix toma más de 2h o destapa rediseño profundo: **parar, crear ticket, mostrar warning UI**.

### T2.4 — Wizard: submit real
- [ ] Paso 4 (confirmación): al darle *Crear*, llamar `wellsService.createWell(payload)`.
- [ ] On success: navegar a `/wells/{id}` + toast "Pozo creado".
- [ ] On error:
  - `409 DUPLICATE_WELL_NAME` → inline en paso 2 con botón "Ver pozo existente".
  - `409 DUPLICATE_UWI` → similar.
  - `422 FORBIDDEN_SIGLA_IN_NAME` → inline en paso 1.
  - `422 NOT_IMPLEMENTED_IN_ITER_8` → toast "Esta trayectoria/clasificación se habilitará próximamente".
  - Otros: toast genérico.

### T2.5 — Página lista de pozos
- [ ] `wells-list.component.ts` + template + styles.
- [ ] `<p-table>` con paginación server-side.
- [ ] Columnas: Nombre, UWI, Estado, Contrato, Campo, Departamento, Municipio, Creado.
- [ ] Click fila → `router.navigate(['/wells', id])`.
- [ ] Filtros básicos: por estado (dropdown) y búsqueda por nombre (input con debounce).

### T2.6 — Página detalle pozo
- [ ] `well-detail.component.ts` + template.
- [ ] Layout 2 columnas con cards PrimeNG:
  - Card "Identidad": wellName, fiscalizedUwi, status.
  - Card "Clasificación": trajectoryType, classification, subClassification.
  - Card "Ubicación": departamento, municipio, cluster, campo.
  - Card "UWI Components": los 8 componentes en grid 4x2.
  - Card "Auditoría": createdAt, createdBy, updatedAt, updatedBy.
- [ ] Read-only (sin edición en Iter 8).
- [ ] Back button → lista.

### T2.7 — Ruteo + guards
- [ ] `wells.routes.ts` con 3 rutas según §6.6 plan.
- [ ] Verificar que el menú principal incluya link a `/wells/list`.
- [ ] `roleGuard(['OperatorAgent'])` — si el ANH intenta entrar a `/wells/new`, redirige a `/wells/list` con toast "Tu rol solo puede consultar".
- [ ] `authGuard` ya existe desde Iter 2.

### T2.8 — Eliminar flag `useMocks` de wells
- [ ] Remover mocks de pozos y flag (o dejar mock solo para tests unitarios).
- [ ] Environment config de staging: `NG_APP_API_URL=https://app-gop-api-staging-westus3.azurewebsites.net`.
- [ ] Build Netlify con `--configuration=staging`.

### T2.9 — Tests E2E FE
- [ ] Tests unitarios de cada componente nuevo con `@testing-library/angular` o equivalente.
- [ ] Test del service mock-eando HttpClient.
- [ ] E2E (Playwright si ya configurado, si no, manual check pre-merge):
  - Login → crear pozo → ver en lista → abrir detalle.

### Definición de hecho Fase 2
- FE deploy en Netlify apuntando al BE Azure.
- Wizard funcionando punta a punta contra BE real.
- Cascada municipios fixeada.
- Lista y detalle operativos.
- CI FE verde.

---

## Fase 3 — GOP-QA: validación cross-módulo

**Owner:** GOP-QA agent
**Duración estimada:** 3-5h
**Requiere:** Fases 1 y 2 completas con staging desplegado.

### T3.1 — Plan de pruebas
- [ ] Crear `specs/features/008-wells-persistence-uwi/testing/test-plan.md` con:
  - Objetivos de calidad.
  - Matriz de casos (happy + edge + security).
  - Criterios de aprobación.

### T3.2 — Ejecución E2E
Ejecutar los 8 pasos de §9.3 del plan. Por cada paso:
- [ ] Screenshot del antes y después.
- [ ] Timing aprox.
- [ ] Observaciones.

Pruebas de seguridad adicionales:
- [ ] **ST-01:** Crear 2 pozos con 2 OperatorAgents distintos → cada uno solo ve el suyo (tenant isolation).
- [ ] **ST-02:** Modificar manualmente el `operatorId` del payload POST desde DevTools → BE ignora, usa el del JWT.
- [ ] **ST-03:** Inyección SQL en denominación (`"RUBI'; DROP TABLE Wells;--"`) → parametrizada, sin efecto.
- [ ] **ST-04:** POST con `trajectoryTypeCode = "ST"` → `422 NOT_IMPLEMENTED_IN_ITER_8`.
- [ ] **ST-05:** POST con `denomination = "POZO ST"` → `422 FORBIDDEN_SIGLA_IN_NAME`.

### T3.3 — Audit de reglas RN-17 a RN-39 (subset Iter 8)
- [ ] RN-17 (sigla reservada): ✅/❌ con evidencia.
- [ ] RN-18 (consecutivo dígitos): ✅/❌.
- [ ] RN-19 (nombre único): ✅/❌.
- [ ] RN-27 a RN-39 (UWI Original+Development): verificar UWI generado contra expected para 3 inputs distintos.
- [ ] RN-37 (UWI único): ✅/❌.

### T3.4 — Performance smoke
- [ ] 10 requests POST /wells → medir p95.
- [ ] Target §8.1 plan BE: p95 < 500ms. Aceptable hasta 800ms en staging Basic tier.
- [ ] Si excede, crear ticket de optimización (no bloquea merge).

### T3.5 — Reporte QA
- [ ] `specs/features/008-wells-persistence-uwi/testing/test-report.md` con:
  - Resumen ejecutivo (✅ apto / ❌ no apto).
  - Defectos encontrados con severidad.
  - Evidencias (screenshots, curl outputs).
  - Recomendación merge/no-merge.

### Definición de hecho Fase 3
- Test report firmado apto para merge.
- Defectos críticos resueltos o trackeados como hot-fix post-merge.
- Evidencias guardadas en repo.

---

## Fase 4 — GOP-Docs: ADR + READMEs

**Owner:** GOP-Docs agent
**Duración estimada:** 1-2h
**Requiere:** Fases 1+2+3. Puede solaparse con QA si tienen bandwidth.

### T4.1 — ADR
- [ ] Crear `docs/adr/ADR-008-wells-persistence.md` con:
  - Contexto (pivote desde mock → real).
  - 6 decisiones clave del §3 blueprint (single-shot, tabla única UWI, índice BD, preview aparte, global query filter, fix síntoma municipios).
  - Consecuencias de cada una.
  - Alternativas descartadas.

### T4.2 — README de feature
- [ ] `specs/features/008-wells-persistence-uwi/README.md` con:
  - Resumen 1 párrafo.
  - Enlaces a blueprint, plan, tasks, ADR, test-report.
  - Instrucciones de "cómo correr local".
  - Scope explícito con tabla IN/OUT.

### T4.3 — CLAUDE.md raíz
- [ ] Actualizar `CLAUDE.md` del repo:
  - Añadir patrón Value Object con ejemplo `WellName`/`FiscalizedUwi`.
  - Añadir patrón Global Query Filter + cuándo usar `IgnoreQueryFilters()`.
  - Añadir convención de `NotSupportedInIter8Exception` para marcar deuda intencional.

### T4.4 — EMERGENT-DECISIONS.md
- [ ] Consolidar decisiones emergentes que surgieron durante la ejecución (las 7 de §11 plan + cualquier extra).
- [ ] Cada entrada: descripción, por qué se tomó, impacto, iter objetivo, owner.

### T4.5 — Spec de iter status
- [ ] Actualizar el snapshot de estado iter (el mismo archivo que Cowork mantiene en `/sessions/.../project_gop_iter_status.md`):
  - Añadir Iter 8 a "Iteraciones cerradas".
  - Mover las deudas nuevas al bloque correspondiente.

### Definición de hecho Fase 4
- ADR publicado.
- README feature publicado.
- CLAUDE.md actualizado.
- EMERGENT-DECISIONS publicado.

---

## Fase 5 — Revisión humana + merge

**Owner:** Alejandro
**Duración estimada:** 2-4h

- [ ] Review del PR:
  - ¿Scope respetado? (cross-check contra blueprint §2.1/§2.2)
  - ¿Tests verdes en CI?
  - ¿Migration revisada?
  - ¿ADR convincente?
- [ ] Smoke test manual en staging (independiente del hecho por QA):
  - Login.
  - Crear pozo Original+Desarrollo.
  - Ver en lista.
  - Ver en detalle.
- [ ] Merge con squash commit, mensaje:
  ```
  feat(wells): persistencia + UWI para trayectoria Original/Desarrollo (Iter 8)
  
  - Well persistido en Azure SQL con VOs WellName y FiscalizedUwi
  - Algoritmo UWI PPDM server-side (Original+Development)
  - 4 endpoints: POST, GET list, GET id, POST preview-uwi
  - Global query filter por OperatorId (tenant isolation)
  - FE wizard wire-up + fix cascada municipios
  - 7 deudas técnicas documentadas → Iter 9/10
  
  Refs: specs/features/008-wells-persistence-uwi/
  ```

### Definición de hecho fase 5
- PR mergeado en `gop-base-web`.
- Staging redeployado con el cambio.
- Snapshot de estado actualizado.
- Alejandro notifica "Iter 8 cerrada" en Cowork.

---

## Resumen de secuencia

```
Fase 0 (Spec) ──→ Fase 1 (Backend) ──→ Fase 2 (Frontend) ──→ Fase 3 (QA) ──→ Fase 5 (Merge)
                                                                    │
                                                                    └──→ Fase 4 (Docs) ──┘
```

- Fases 0 → 1 → 2 → 3 son duras.
- Fase 4 (Docs) puede correr en paralelo con Fase 3 (QA).
- Fase 5 (Merge) es la última, humana, después de que todo esté verde.

**Wall-clock total estimado:** 22-32h (dentro del target ~24h del Iter). Si excede 40h, algo del OUT OF SCOPE se coló — revisar y recortar.

---

## Métricas de éxito de la iteración

Al cierre, el snapshot de iter-status debe reflejar:

- [ ] **Iter 8 mergeada** (squash commit con SHA).
- [ ] **7 deudas técnicas documentadas** (las 7 de §11 plan + las emergentes).
- [ ] **4 endpoints operativos** en staging.
- [ ] **First pozo real persistido** en Azure SQL.
- [ ] **Demo video de 3 min** (opcional): login → crear → ver en lista. Subir a share interno de IK LABS.
—
