# Iter 9.5 — Task Board

## Fase A: IaC Bicep

- [x] T-INFRA-01: `infra/bicep/modules/identity.bicep` — User-assigned Managed Identity
- [x] T-INFRA-02: `infra/bicep/modules/acr.bicep` — Azure Container Registry Basic
- [x] T-INFRA-03: `infra/bicep/modules/keyvault.bicep` — Key Vault Standard con RBAC
- [x] T-INFRA-04: `infra/bicep/modules/sql.bicep` — SQL Server + DB Serverless auto-pause 60min
- [x] T-INFRA-05: `infra/bicep/modules/monitoring.bicep` — Log Analytics + App Insights
- [x] T-INFRA-06: `infra/bicep/modules/appservice.bicep` — App Service Plan B1 + App Service
- [x] T-INFRA-07: `infra/bicep/main.bicep` — Orquestador con outputs
- [x] T-INFRA-08: `infra/bicep/main.parameters.staging.json` — Parámetros staging
- [x] T-INFRA-09: `infra/bicep/deploy.sh` — Script de despliegue con az CLI

## Fase B: Dockerfile + Configuración Azure

- [x] T-INFRA-10: `backend/Dockerfile` — Multi-stage build .NET 10 Linux
- [x] T-INFRA-11: `backend/.dockerignore` — Exclusiones para imagen limpia
- [x] T-INFRA-12: `backend/src/GOP.API/appsettings.Staging.json` — Config sin secretos
- [x] T-INFRA-13: `backend/src/GOP.API/Extensions/KeyVaultExtension.cs` — Key Vault + Managed Identity
- [x] T-INFRA-14: `backend/src/GOP.API/Extensions/MigrationExtension.cs` — Migrate on startup
- [x] T-INFRA-15: `backend/src/GOP.API/Program.cs` — Integrar KV + migrations + CORS staging + App Insights
- [x] T-INFRA-16: `backend/src/GOP.API/Controllers/HealthController.cs` — `/health/ready` con DB check

## Fase C: Seed DANE Completo

- [x] T-INFRA-17: `backend/src/GOP.Infrastructure/Persistence/DaneData/departamentos.json` — 33 dptos DANE
- [x] T-INFRA-18: `backend/src/GOP.Infrastructure/Persistence/DaneData/municipios.json` — 1122 mpios DANE
- [x] T-INFRA-19: `backend/src/GOP.Infrastructure/Persistence/DbSeeder.cs` — Seed idempotente vía upsert
- [x] T-INFRA-20: `backend/src/GOP.Infrastructure/Persistence/Configurations/DepartamentoConfiguration.cs` — Remover HasData mínimo
- [x] T-INFRA-21: `backend/src/GOP.Infrastructure/Persistence/Configurations/MunicipioConfiguration.cs` — Remover HasData mínimo
- [x] T-INFRA-22: `dotnet ef migrations add FullDaneSeed` — Nueva migración limpia (sin seed en migración)
- [x] T-INFRA-23: `backend/src/GOP.Infrastructure/DependencyInjection.cs` — Registrar DbSeeder

## Fase D: GitHub Actions

- [x] T-INFRA-24: `.github/workflows/ci-cd-staging.yml` — Pipeline completo 5 jobs
- [x] T-INFRA-25: `.github/workflows/smoke-tests.yml` — Smoke tests post-deploy UWI

## Fase E: FE Environment

- [x] T-INFRA-26: `src/environments/environment.staging.ts` — URL del BE Azure
- [x] T-INFRA-27: `netlify.toml` — Contexto staging con NG_APP_API_URL

## Fase F: Documentación

- [x] T-INFRA-28: `docs/deployment/azure-staging.md` — Manual reproducible
- [x] T-INFRA-29: `docs/adr/ADR-009-infra-azure-lean.md` — ADR de decisiones infra
- [x] T-INFRA-30: `README.md` — Sección Deployment añadida
