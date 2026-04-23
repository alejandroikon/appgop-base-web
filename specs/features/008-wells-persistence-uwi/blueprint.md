# Blueprint — Iter 8 · Creación de Pozo Nuevo (Persistencia + UWI)

**Feature ID:** `008-wells-persistence-uwi`
**Rama objetivo:** `008-wells-persistence-uwi` (base: `gop-base-web`)
**Fecha:** 2026-04-23
**Autor:** Cowork (sesión de estrategia) a partir de V2 specs entregadas por Alejandro
**Estado:** Draft — pendiente ejecución por GOP-Spec agent

---

## 1. Contexto

### 1.1 De dónde venimos

Al cierre de Iter 7 (2026-04-22) tenemos:

- **Wizard FE** de 4 pasos (Iter 4) que captura 18 campos de un pozo nuevo y llama a un POST que **no persiste**.
- **Well entity** en Domain con `WellLocation` owned (Iter 3) y 5 catálogos sembrados.
- **Auth persistida** en Azure SQL (Iter 7) con `alejandro.gutierrez@interkont.co` logueado como ADMIN ANH tenant.
- **Bug conocido:** cascada departamento → municipio en el wizard falla al volver al paso 2.
- **Stack Azure funcionando end-to-end** (Path D validado 2026-04-22).

El formulario "anda" pero al darle *Guardar* no pasa nada real: el pozo no queda en BD, no se genera UWI, no hay retorno al usuario más allá del spinner. Es el último hueco entre "demo técnico" y "producto usable".

### 1.2 A dónde vamos

**Objetivo de Iter 8:** Cerrar el primer flujo vertical persistido en GOP 360°. Al final de esta iteración, un OperatorAgent debe poder:

1. Abrir el wizard en el FE Netlify.
2. Seleccionar departamento → municipio (cascada funcionando).
3. Llenar los 18 campos de un pozo tipo **Original + Desarrollo**.
4. Ver el **UWI fiscalizado calculado en tiempo real** en el paso de preview.
5. Darle *Crear* y recibir confirmación con el `id`, `wellName` y `fiscalizedUwi` del pozo persistido en Azure SQL.
6. Listar sus pozos y ver el detalle del recién creado.

Ese es el corte — ni un endpoint más, ni una trayectoria más, ni un rol más. Todo lo demás del V2 spec queda documentado como deuda explícita hacia iteraciones posteriores.

### 1.3 Material de entrada

| Documento | Origen | Uso en Iter 8 |
|-----------|--------|---------------|
| `spec-frontend.md` V1.0 | Alejandro, 2026-04-23 | Contrato UI — referencia de CAs, reusar lo aplicable al slice |
| `spec-backend.md` V1.0 | Alejandro, 2026-04-23 | Contrato BE — **40 RN son el "north star", implementamos subset** |
| `api-contract.md` V1.0 | Alejandro, 2026-04-23 | Shapes de request/response — **respetar nombres de campos tal cual** |
| `openapi.yaml` V1.0 | Alejandro, 2026-04-23 | Validación automática — generador de clientes FE (opcional) |

**Decisión arquitectónica clave:** los 4 documentos son el **contrato oficial V2**. Todo lo que se implemente en Iter 8 debe **cumplir el contrato para los casos que sí soporta**, y devolver un `NOT_IMPLEMENTED_IN_ITER_8` controlado para los que no. No se inventan shapes nuevos que luego haya que romper.

---

## 2. Scope — qué entra y qué no

### 2.1 IN SCOPE (Iter 8)

| Área | Lo que sí se hace |
|------|-------------------|
| **Trayectoria** | Solo `O` (Original). Sin ST/PR/G/P/ML. |
| **Clasificación** | Solo `DEVELOPMENT` (Desarrollo). Sin EXPLORATORY ni STRATIGRAPHIC. |
| **Subclasificación** | `null` en todos los casos (no aplica para Desarrollo). |
| **Rol** | `OperatorAgent` (usuario ya persistido en Iter 7). |
| **Tenant** | Single-operator — el OperatorAgent ve solo sus pozos (filtro por `User.OperatorId`). |
| **Endpoints BE** | `POST /api/v1/wells` · `GET /api/v1/wells` · `GET /api/v1/wells/{id}` · `POST /api/v1/wells/preview-uwi` |
| **Persistencia** | Well entity persistida en Azure SQL vía EF Core. Incluye `WellLocation` owned y columnas de UWI. |
| **UWI Server-side** | Algoritmo PPDM fiscalizado, 8 componentes. Unicidad garantizada por índice único en BD. |
| **Nombre Server-side** | Composición `{denominación} {consecutivo}{sufijoTrayectoria}` para trayectoria `O`. |
| **FE Wizard** | Paso 3 muestra preview UWI calculado por BE. Al darle *Crear* llama POST real. Cascada depto→municipio fixeada. |
| **FE Lista** | Nueva vista `/wells` con tabla paginada de los pozos del usuario (reusa el listado que ya existe en mock). |
| **FE Detalle** | Nueva vista `/wells/{id}` con los campos del pozo. |
| **Validaciones FE** | Las mínimas: campos obligatorios, formato consecutivo (solo dígitos — RN-18). |
| **Validaciones BE** | RN-17 (sigla reservada en denominación), RN-18 (consecutivo solo dígitos), RN-19 (nombre único), RN-27-39 (UWI — solo la rama Original/Desarrollo), RN-37 (UWI único). |
| **Fix municipios** | Cascada `PoliticalDivision.Department → Municipality` operativa tanto al llenar por primera vez como al volver al paso. |

### 2.2 OUT OF SCOPE — deuda explícita

Estas son decisiones conscientes de **no hacer** en Iter 8. Cada una tiene su owner y su iter objetivo.

| Área diferida | Por qué no ahora | Iter objetivo |
|---------------|------------------|---------------|
| Trayectorias ST/PR/G (pozo padre) | Requiere lookup de pozo padre + lógica RN-03 a RN-10 + endpoint `parent-well-data`. Triplica alcance. | Iter 10 |
| Trayectorias P/ML (perforación/multilateral) | Sufijos distintos, no aporta al demo. | Iter 10 |
| Clasificación EXPLORATORY (+ A3/A2a/A2b/A2c/A1) | RN-11/12 requiere validar F103 Lahee B3 → depende de Procedures que no existen. | Iter 11 |
| Clasificación STRATIGRAPHIC | RN-15 (ANH-only) + RN-30 (sigla ANH) + VAF → tres integraciones. | Iter 12 |
| Rol `OperatorCoordinator` | Solo consulta/edita, no aporta a demo creación. | Iter 9 (junto con RBAC). |
| Rol `AnhGopAdministrator` | Requiere RN-15 + multi-tenancy cross-operadora. | Iter 9. |
| RN-15 (ANH only Estratigráfico) | Depende del rol ANH + clasificación estratigráfica, ambos diferidos. | Iter 9/12. |
| RN-40 (bloqueo post-F101) | No existe F101 ni Procedures todavía. | Iter 11 (cuando exista F101). |
| `DRAFT → CREATED` + `/finalize` endpoint | En Iter 8 un POST crea directo en `CREATED`. Sin borradores parciales. | Iter 10 (cuando haya más campos opcionales que obliguen a guardar parcial). |
| `PUT /wells/{id}` (edición post-creación) | Requiere ETag + RN-40 check. Riesgo bajo si se difiere. | Iter 10. |
| `DELETE /wells/{id}` | Idem. | Iter 10. |
| `WellStateHistory` + state machine | Requiere transiciones formales. Iter 8 solo usa `currentStateCode = REGISTERED`. | Iter 10 |
| Integración Mapa de Tierras (RN-25/26) | `gop.integration` no existe. | Iter 13+ |
| Integración SOLAR/VCH/VPAA/VAF | Catálogos hoy se siembran manualmente. Suficiente para demo. | Iter 13+ |
| ETag / `If-Match` concurrency | Sin edición no hay conflicto. | Iter 10 |
| Rate limiting | El staging no tiene tráfico real que lo justifique. | Iter 13 (pre-producción). |
| Audit logging (Audit.NET post-commit) | `[Audit.NET] Log` a stdout es suficiente en Iter 8. | Iter 12 |
| OWASP Top 10 completo | Tenemos A01 (tenant isolation), A03 (parametrized queries vía EF Core) y A07 (JWT desde Iter 2). El resto se difiere. | Iter 13 (pre-producción). |
| `GET /wells/{id}/parent-well-data` | Solo aplica a ST/PR/G. | Iter 10 |

### 2.3 Qué se construye **pensando en** lo que viene

Aunque el scope es acotado, hay decisiones estructurales que deben hacerse "bien desde el principio" porque romperlas después duele:

1. **Enum de TrajectoryType** en Domain — aunque Iter 8 solo use `O`, el enum incluye los 6 valores. Evita un refactor de 20 archivos cuando entren ST/PR/G.
2. **Enum de ClassificationType** — idem, 3 valores. Sub-classification se modela como value object opcional.
3. **Interface `IUwiGenerator`** — el algoritmo PPDM va detrás de una interface, no en controller. Cuando entren más trayectorias y clasificaciones, se extiende el servicio sin tocar la API.
4. **`WellName` y `FiscalizedUwi` como Value Objects** — no strings sueltos. Encapsulan reglas de formato y unicidad.
5. **Columna `Status` en Well** — aunque Iter 8 solo use `CREATED`, el enum incluye `DRAFT` y `CREATED` desde ya. Migración futura = nueva fila, no ALTER TABLE.
6. **Columnas de `FiscalizedUwiComponent`** — los 8 componentes persisten en la misma tabla `Wells` (no entidad separada) para Iter 8. Si en Iter 10 se necesita historial de UWIs, se extrae a tabla aparte. **Riesgo aceptado.**

---

## 3. Trade-offs y decisiones

### 3.1 Single-shot create vs. DRAFT + finalize

**Decisión:** Single-shot. El POST crea directamente en `CREATED`.

**Por qué:**
- El wizard FE ya tiene un paso de preview — ahí el usuario "confirma". No hace falta una segunda llamada.
- Implementar DRAFT + finalize duplica endpoints (POST + PUT + finalize) y obliga a manejar estados parciales en BD.
- En Iter 10, cuando se metan campos opcionales que obliguen a guardar parcial, se añade `DRAFT` sin romper el POST actual (pasa a aceptar `status: "DRAFT" | "CREATED"`).

**Trade-off:** si el usuario cierra el navegador a mitad del wizard, pierde lo que llevaba. Aceptable en Iter 8 — el wizard tiene solo 4 pasos y tarda <2 min.

### 3.2 8 componentes UWI en misma tabla vs. tabla separada

**Decisión:** Columnas en la misma `Wells`.

**Por qué:**
- No hay requisito todavía de historial de UWIs por pozo.
- Un JOIN menos en cada GET.
- EF Core configuration más simple (owned entity, no tabla aparte).

**Trade-off:** si en Iter 10 aparece "auditoría de cambios de UWI", hay que migrar los datos a tabla aparte. **Riesgo aceptado** — son 8 columnas, migración trivial.

### 3.3 Validación de UWI único: índice BD vs. check en aplicación

**Decisión:** Índice único en BD (`UNIQUE INDEX` sobre `Wells.FiscalizedUwi`). El check en aplicación es "hint" pre-insert.

**Por qué:**
- El índice es la garantía real; cualquier check en app es race-condition-prone.
- Si la escritura colisiona, EF Core captura la `SqlException` (error 2627/2601) y el handler lo traduce a `409 DUPLICATE_UWI`.
- Evita dos roundtrips (check + insert) por una sola (insert con catch).

**Trade-off:** el mensaje de error llega después del intento de persistencia (en vez de antes). Para el usuario es indistinguible.

### 3.4 Preview UWI: endpoint aparte vs. campo calculado en POST

**Decisión:** Endpoint aparte `POST /wells/preview-uwi`.

**Por qué:**
- El wizard necesita mostrar el UWI en el paso 3 (confirmación) **antes** de que el usuario le dé "Crear". Sin endpoint aparte, se tendría que hacer POST con flag `dryRun` — no está en el contrato V2.
- El contrato V2 ya lo define. Respetamos el contrato.
- El endpoint es read-only, rate-limit alto, se puede cachear.

**Trade-off:** hay dos rutas al algoritmo UWI (preview y create). Se resuelve llamando al mismo `IUwiGenerator` desde ambos controladores.

### 3.5 Tenant isolation: filtro explícito vs. global query filter de EF

**Decisión:** Global query filter en `ApplicationDbContext`.

**Por qué:**
- Garantiza que **ningún** query pueda traer pozos de otra operadora, incluyendo queries de mantenimiento que olvidemos revisar.
- El OperatorId se inyecta desde `ICurrentUserService` (ya existe en Iter 7).
- Es el patrón idiomático de EF Core para multi-tenancy.

**Trade-off:** hay que acordarse de `IgnoreQueryFilters()` para operaciones admin futuras. Trade-off aceptable.

### 3.6 Fix municipios: solución del síntoma vs. rediseño de la cascada

**Decisión:** Fix del síntoma (el store o signal del wizard pierde `municipalityDaneCode` al volver al paso).

**Por qué:**
- El bug es un glitch de estado reactivo, no un problema de contrato.
- Un rediseño de la cascada (mover estado a store global, usar `RxJS` vs. Signals) se sale del scope.
- Una línea o dos de código bien colocadas resuelven el problema.

**Trade-off:** si aparecen más glitches de estado en el wizard, habrá que abordarlos en una iter de hardening FE.

---

## 4. Criterios de éxito

Para que Iter 8 se considere **cerrada y mergeable**, debe cumplir todo esto:

### 4.1 Funcional
- [ ] OperatorAgent puede crear un pozo Original+Desarrollo desde FE Netlify, persistido en Azure SQL de staging.
- [ ] El UWI generado por el BE es el mismo que se preview-ó en el wizard.
- [ ] La lista de pozos del OperatorAgent muestra el recién creado.
- [ ] El detalle de un pozo muestra los 18 campos + los 8 componentes UWI.
- [ ] La cascada depto→municipio funciona al llenar por primera vez y al volver al paso.
- [ ] Un segundo intento de crear un pozo con el mismo `denomination + consecutive + trayectoria` del mismo contrato falla con `409 DUPLICATE_WELL_NAME`.

### 4.2 Reglas de negocio
- [ ] RN-17 implementada: si `denomination = "POZO ST"` el BE rechaza con `422 FORBIDDEN_SIGLA_IN_NAME`.
- [ ] RN-18 implementada: si `consecutive = "12A"` el BE rechaza con `400 VALIDATION_ERROR`.
- [ ] RN-19 implementada: nombre único, índice en BD.
- [ ] RN-27 a RN-39 implementadas para el camino Original+Desarrollo: el UWI respeta el formato PPDM.
- [ ] RN-37 implementada: UWI único, índice en BD.

### 4.3 Técnico
- [ ] Clean Architecture respetada: Domain no conoce EF Core, Application no conoce ASP.NET.
- [ ] Cobertura mínima: 80% en `Application` y `Domain`, 60% global.
- [ ] Tests de integración que levantan WebApplicationFactory + SQLite in-memory y validan el happy path end-to-end.
- [ ] Migración EF Core generada y aplicada en staging sin errores.
- [ ] Global query filter por `OperatorId` operativo — un test de seguridad prueba que OperatorAgent A no ve pozos de Operator B.
- [ ] CI backend verde en main y en la rama de feature.
- [ ] FE deploy en Netlify con apuntando al backend Azure real — smoke test manual pasa.

### 4.4 Documentación
- [ ] ADR en `docs/adr/ADR-008-wells-persistence.md` con las 6 decisiones de trade-offs de §3.
- [ ] README de la feature en `specs/features/008-wells-persistence-uwi/README.md` enlazando a blueprint + plan + tasks.
- [ ] `CLAUDE.md` del repo actualizado con la nueva convención de Value Objects.
- [ ] `EMERGENT-DECISIONS.md` con cualquier decisión que haya surgido durante la ejecución y no estuviera en el blueprint.

---

## 5. Riesgos

| # | Riesgo | Probabilidad | Impacto | Mitigación |
|---|--------|--------------|---------|-----------|
| R1 | Algoritmo UWI mal implementado — el FE preview devuelve string distinto al BE create | Media | Alto | Test unitario que valida los 6 ejemplos de `api-contract.md` y `spec-backend.md`. Un único `IUwiGenerator`, ambos endpoints lo llaman. |
| R2 | Migración EF Core rompe datos existentes en staging | Baja | Alto | Staging tiene 0 datos reales. Si rompe, `az sql db delete` + recrear. Documentar en plan.md. |
| R3 | Sigla de denominación (primeras 4 letras) tiene ambigüedades (ej: denominación < 4 chars, con acentos) | Media | Medio | Tests unitarios con casos límite: `denomination = "EL"`, `"Múrtico"`, `"A1"`. Definir padding/normalización en plan.md. |
| R4 | Cluster code mapping (RU0000, LAXXXX, VOOPH...) no está definido en los catálogos sembrados | Alta | Medio | Antes de empezar BE, GOP-Spec revisa los 5 catálogos de Iter 3. Si falta tabla de mapping cluster→código, la crea como parte de Iter 8 (cambio de catálogo, no feature nueva). |
| R5 | El FE wizard requiere cambios más profundos de lo esperado para integrar preview-uwi real | Media | Medio | Budget: si al segundo día de FE el wire-up no está listo, fallback a mostrar UWI calculado por JS espejo en cliente (no es production-ready pero demuestra). |
| R6 | El fix de municipios se destapa y revela un problema mayor de estado reactivo | Baja | Alto | Si después de 2h de debug el fix no es trivial, crear ticket aparte y dejar warning visible en UI ("si vuelves al paso anterior, recarga"). |
| R7 | Deuda del enum TrajectoryType/ClassificationType no es suficientemente genérica y Iter 10 rompe el schema | Media | Bajo | Revisar antes de merge que los enums cubren los 6 / 3 valores. ADR documenta por qué. |

---

## 6. Cadencia esperada

Basado en las 7 iteraciones anteriores (promedio ~24h wall-clock cada una, rango 1h-95h según complejidad):

| Fase | Duración estimada | Owner principal |
|------|-------------------|-----------------|
| GOP-Spec: revisión de plan/tasks + validación de catálogos existentes | 1-2h | spec-agent |
| GOP-Backend: Domain + Application + Infrastructure + API | 6-10h | backend-agent |
| GOP-Frontend: preview-uwi wire-up + fix municipios + lista + detalle | 6-10h | frontend-agent |
| GOP-QA: tests de integración cross-módulo + smoke staging | 3-5h | qa-agent |
| GOP-Docs: ADR + READMEs + CLAUDE.md | 1-2h | docs-agent |
| Revisión humana + merge | 2-4h | Alejandro |
| **Total estimado** | **~24-33h wall-clock** | — |

Dentro del target de ~24h/iter. Si excede, probablemente indica que se coló algo del OUT OF SCOPE.

---

## 7. Dependencias

### 7.1 Dependencias hacia arriba (qué debe existir)
- ✅ Iter 7 (Users persistidos, auth funcionando)
- ✅ Iter 6 (Azure stack, KV, SQL, CI)
- ✅ Iter 3 (Well entity + 5 catálogos)
- ✅ Iter 4 (Wizard UI)

### 7.2 Dependencias hacia abajo (qué se desbloquea)
- Iter 9 (RBAC multi-tenant) puede usar los pozos ya persistidos para probar aislamiento cross-operadora.
- Iter 10 (state machine + edit/delete) opera sobre los pozos creados aquí.
- Iter 11 (F101) recibe como input un pozo en estado `CREATED`.

### 7.3 Acción previa fuera del alcance de Cowork
Antes de mergear Iter 8, Alejandro debe confirmar que los dos KV secrets de seed users siguen cargados en `kv-gop360-staging`. Si no, `UserSeeder` rompe y `/health/ready` pasa a `Unhealthy`. Reminder ya documentado en `project_gop_iter_status.md`.

---

## 8. Enlaces

- Spec V2 FE: `/mnt/uploads/spec-frontend.md`
- Spec V2 BE: `/mnt/uploads/spec-backend.md`
- API Contract V2: `/mnt/uploads/api-contract.md`
- OpenAPI V2: `/mnt/uploads/openapi.yaml`
- Plan técnico: `./plan.md`
- Breakdown de tareas: `./tasks.md`
- Feature previa (wizard FE): `specs/features/006-well-creation-form/`
- Feature previa (catálogos): `specs/features/005-wells-catalog-crud/`
- Feature previa (users persist): `specs/features/007-users-persistence/`

---

*Este documento es la entrada oficial del GOP-Spec agent para arrancar la iteración. Si algo no está aquí, no se hace en Iter 8.*
