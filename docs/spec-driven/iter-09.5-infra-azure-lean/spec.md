# Iter 9.5 — Infraestructura Azure Lean (Staging)

**Fecha:** 2026-04-20  
**Autor:** GOP-Backend Agent (Iter 9.5)  
**Estado:** Implementado

---

## Objetivo

Desplegar la infraestructura mínima en Azure que permita validación E2E del flujo  
"Creación de Pozo Nuevo V2.0" (Iter 9.2) con costo objetivo ≤ $25/mes.

---

## Criterios de Aceptación (CA)

| # | Criterio |
|---|---------|
| CA-01 | `curl https://app-gop360-api-staging.azurewebsites.net/health` → 200 `{"status":"Healthy"}` |
| CA-02 | `GET /api/v1/catalogs/departamentos` → 33 departamentos desde BD |
| CA-03 | `GET /api/v1/catalogs/municipios?departamentoId=<id>` → ≥80 mpios para Santander (dpto 68) |
| CA-04 | Preview Netlify carga Dpto → Mpio sin errores CORS |
| CA-05 | Crear pozo E2E → UWI coincide con los 3 casos canónicos del Iter 9.1 |
| CA-06 | GitHub Action auto-dispara en push a `gop-base-web` |
| CA-07 | Secrets solo en Key Vault, nunca en appsettings de producción |
| CA-08 | App Insights registra requests con CorrelationId |
| CA-09 | Documentación reproducible en <2h por tercero |
| CA-10 | Costo real 7 días ≤ $30/mes proyectado |

---

## RN-INFRA

| # | Requisito |
|---|---------|
| RN-INFRA-01 | Secretos (connection strings, signing keys) solo en Key Vault via Managed Identity |
| RN-INFRA-02 | SQL Serverless auto-pause ≤ 60 minutos |
| RN-INFRA-03 | Deploy automático solo en push a rama `gop-base-web` |
| RN-INFRA-04 | `/health/ready` valida conexión DB con `CanConnectAsync` |
| RN-INFRA-05 | Imágenes Docker en ACR etiquetadas con SHA corto del commit |
| RN-INFRA-06 | Migraciones EF Core idempotentes (aplicadas en startup) |
| RN-INFRA-07 | Seed DANE idempotente (upsert) — 33 departamentos + ≥1100 municipios |
| RN-INFRA-08 | FE lee API URL de variable de entorno `NG_APP_API_URL` |
| RN-INFRA-09 | Datos DANE oficiales DIVIPOLA (no inventados) |
| RN-INFRA-10 | Logs estructurados con CorrelationId via Application Insights |

---

## RN-SEC

| # | Requisito |
|---|---------|
| RN-SEC-01 | HTTPS only, TLS mínimo 1.2 en App Service |
| RN-SEC-02 | CORS: `*.netlify.app` + `localhost:4200` |
| RN-SEC-03 | Key Vault soft-delete 7 días |
| RN-SEC-04 | SQL firewall: solo Azure Services + IP dev explícita |

---

## RN-COSTO

| # | Requisito |
|---|---------|
| RN-COSTO-01 | App Service Plan B1 Linux (~$13/mes) |
| RN-COSTO-02 | Azure SQL Serverless GP_S_Gen5_1 auto-pause 60min (~$5-8/mes activo) |
| RN-COSTO-03 | ACR Basic (~$5/mes) |
| RN-COSTO-04 | Key Vault Standard (~$0/mes para secrets) |
| RN-COSTO-05 | App Insights + Log Analytics (~$0 con 5GB free tier) |

**Total proyectado: $23-26/mes** ✓

---

## Naming Convention

| Recurso | Nombre |
|---------|--------|
| Resource Group | `rg-gop360-staging` |
| App Service Plan | `asp-gop360-staging` |
| App Service | `app-gop360-api-staging` |
| SQL Server | `sql-gop360-staging` |
| SQL Database | `sqldb-gop360-staging` |
| ACR | `acrgop360staging` |
| Key Vault | `kv-gop360-staging` |
| App Insights | `appi-gop360-staging` |
| Log Analytics | `log-gop360-staging` |
| Managed Identity | `id-gop360-staging` |

**Región:** `eastus2`

---

## ADRs

| # | Decisión | Justificación |
|---|---------|--------------|
| ADR-INFRA-01 | App Service en lugar de AKS | AKS: ~$150+/mes; App Service: ~$13/mes. Mismo contenedor, migrable sin cambios de código. |
| ADR-INFRA-02 | GitHub Actions en lugar de GitLab CI/CD | Repo ya en GitHub; sin beneficio en migrar. |
| ADR-INFRA-03 | Netlify para FE en lugar de Azure Static Web Apps | FE ya funciona en Netlify; zero-downtime. |
| ADR-INFRA-04 | Bicep como IaC | Nativo Azure, sin dependencias externas. Recomendación del stack v1.1. |
| ADR-INFRA-05 | SQL Serverless vs Managed Instance | Managed Instance ~$400/mes; Serverless escala a 0 en inactividad. |
| ADR-INFRA-06 | Migraciones en startup vs pipeline | Startup garantiza coherencia en cada deploy; idempotentes via EF Core. |
| ADR-INFRA-07 | Managed Identity para Key Vault | Elimina credenciales hardcoded; best practice de seguridad Azure. |

---

## Casos de Validación UWI Canónicos

| # | Input | UWI Esperado |
|---|-------|-------------|
| 1 | Dpto 50568 · Curé · #0001 · Cluster LA · Vertical · Prod. Hidrocarburos · Pozo Huérfano | `50568CURE0001LA0000VPH-OH` |
| 2 | Dpto 68081 · Alpha · #0042 · Cluster CN · Horizontal ST2 · Inyector · Cierre Definitivo | `68081ALPH0042CN0003HST2I-CD` |
| 3 | Dpto 86320 · ANH-E · #0001 · Sin cluster · Vertical · Gas · Operativo | `86320ANHE0001CX0000VGT-O` |
