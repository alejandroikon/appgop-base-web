# ADR-009: Infraestructura Azure Lean para Staging GOP 360°

**Status:** Aceptado  
**Fecha:** 2026-04-20  
**Autor:** GOP-Backend Agent (Iter 9.5)  
**Contexto:** Primer despliegue E2E de GOP 360° para validación de Iter 9.2

---

## Contexto

El proyecto GOP 360° tiene 8+ iteraciones de código implementado (Clean Architecture, 132 tests) pero nunca ha sido desplegado en un entorno accesible de forma E2E. El stack oficial define AKS + Azure SQL Managed Instance para producción, pero esto tiene un costo de $400-600+/mes, desproporcionado para staging de validación.

Se necesita un entorno staging que:
- Permita validación E2E del flujo "Creación de Pozo Nuevo V2.0"
- Costo ≤ $25/mes
- Sea migrable a producción sin cambios de código de negocio
- Se pueda provisionar en <2h por cualquier dev del equipo

---

## Decisiones

### ADR-009-1: App Service en lugar de AKS para staging

**Decisión:** Usar Azure App Service Plan B1 Linux en lugar de AKS.

**Justificación:**
- AKS (cluster mínimo con 2 nodos): ~$150-200/mes
- App Service B1: ~$13/mes
- Mismo contenedor Docker: sin cambios de código
- AKS agrega 0 valor en staging — la validación E2E es sobre la lógica de negocio, no sobre orquestación de contenedores
- Migración a AKS: cambiar el target de deploy en CI/CD, 0 cambios en aplicación

**Riesgos:** App Service no replica la configuración de producción exactamente. Aceptable para staging.

---

### ADR-009-2: GitHub Actions en lugar de GitLab CI/CD

**Decisión:** CI/CD en GitHub Actions.

**Justificación:** El repositorio está en GitHub (`alejandroikon/appgop-base-web`). GitLab requeriría mirroring del repo. El stack oficial menciona GitLab CI/CD para la organización, pero al no tener GitLab self-hosted, GitHub Actions es la opción nativa sin fricción.

**Reversibilidad:** El workflow YAML es estándar — migrar a GitLab CI/CD requiere adaptar la sintaxis, no el proceso.

---

### ADR-009-3: Netlify para FE en lugar de Azure Static Web Apps

**Decisión:** Mantener Netlify para el frontend Angular.

**Justificación:**
- El FE ya funciona en Netlify desde iter 1
- Azure Static Web Apps requeriría reconfigurar CI/CD del FE + CORS + dominio custom
- Zero-downtime en la migración: no hay valor en mover el FE en staging
- La única diferencia relevante es la URL de la API — manejada con `NG_APP_API_URL`

---

### ADR-009-4: Bicep como IaC en lugar de Terraform

**Decisión:** Usar Bicep para la infraestructura Azure.

**Justificación:**
- Bicep es nativo de Azure: sin instalación de provider, sin state remoto
- El stack oficial v1.1 lo menciona explícitamente
- Para staging no se necesita multi-cloud portabilidad

---

### ADR-009-5: SQL Serverless en lugar de Managed Instance

**Decisión:** Azure SQL Database Serverless (GP_S_Gen5_1, auto-pause 60min).

**Justificación:**
- SQL Managed Instance: ~$400-500/mes — impracticable para staging
- SQL Serverless: $0 cuando en pausa, ~$5-8/mes en uso real de staging
- Compatibilidad: ambos son SQL Server — sin diferencias para EF Core

**Tradeoff:** El primer request tras 60min de inactividad tiene ~15-20s de latencia por "despertar". Aceptable en staging.

---

### ADR-009-6: Migraciones EF Core en startup

**Decisión:** `await db.Database.MigrateAsync()` en startup, no en pipeline de CI/CD.

**Justificación:**
- Garantiza que el schema siempre coincide con el código desplegado
- Idempotente: EF Core no re-aplica migraciones ya aplicadas
- Simplifica el pipeline: no necesita acceso directo a SQL desde GitHub Actions

**Riesgo mitigado:** Si una migración falla, el health check reporta DB unhealthy antes de que el tráfico llegue.

---

### ADR-009-7: Datos DANE via DbSeeder en lugar de HasData

**Decisión:** 1123 municipios + 33 departamentos cargados vía `DbSeeder` (upsert en startup) desde archivos JSON, no vía `HasData` en migraciones.

**Justificación:**
- `HasData` con 1123 registros genera una migración de >5MB que degrada el rendimiento de EF
- El seeder es idempotente: detecta registros existentes y solo inserta los faltantes
- Los JSON son la fuente de verdad (DIVIPOLA): fácilmente actualizables sin nueva migración
- Fuente oficial: `proyecto26/colombia` basado en datos abiertos DANE/MinTIC

---

## Consecuencias Positivas

1. Stack completo Azure provistonable en <15min con `deploy.sh`
2. CI/CD automatizado en cada push a `gop-base-web`
3. Costo verificable en Azure Cost Management (RG: `rg-gop360-staging`)
4. Migración a producción (AKS) sin cambios de código de negocio

## Consecuencias Negativas

1. App Service no es idéntico a AKS — puede haber diferencias en headers, networking
2. SQL Serverless tiene latencia de cold-start — no válido para prod
3. GitHub Actions no es GitLab — requerirá adaptación al migrar

---

## Referencias

- Stack Tecnológico GOP 360° v1.1
- CONSTITUTION.backend.md §Infrastructure
- Iter 9.5 spec.md, plan.md, tasks.md
