# Iter 9.5 — Plan de Implementación

**Branch:** `claude/iter-9.5-infra-azure-staging-20260420`

---

## Fases

### Fase A — IaC Bicep (~1.5h)
Crear módulos Bicep para provisionar todos los recursos Azure.

**Archivos esperados:**
```
infra/bicep/
  main.bicep
  main.parameters.staging.json
  deploy.sh
  modules/
    appservice.bicep
    keyvault.bicep
    sql.bicep
    acr.bicep
    monitoring.bicep
    identity.bicep
    networking.bicep
```

### Fase B — Dockerfile + Configuración Azure (~1h)
Containerizar la API y configurarla para Azure.

**Archivos esperados:**
```
backend/
  Dockerfile
  .dockerignore
  src/GOP.API/
    appsettings.Staging.json
    Extensions/
      KeyVaultExtension.cs
      MigrationExtension.cs
```

### Fase C — Seed DANE Completo (~1h)
Expandir seed de 3 dptos/6 mpios a 33 dptos/1100+ mpios.

**Archivos modificados:**
```
backend/src/GOP.Infrastructure/Persistence/
  Configurations/DepartamentoConfiguration.cs  (33 dptos)
  Configurations/MunicipioConfiguration.cs     (1100+ mpios via DbSeeder)
  DbSeeder.cs                                  (nuevo: seed idempotente)
  DaneData/
    departamentos.json
    municipios.json
```

### Fase D — GitHub Actions CI/CD (~1h)
Pipeline completo: build → test → image → deploy.

**Archivos esperados:**
```
.github/workflows/
  ci-cd-staging.yml
  smoke-test.yml
```

### Fase E — FE Environment Staging (~0.5h)
Apuntar el FE Netlify al BE Azure.

**Archivos modificados:**
```
src/environments/
  environment.staging.ts
netlify.toml           (contexto staging)
```

### Fase F — Documentación (~0.5h)
Manual de despliegue reproducible.

**Archivos esperados:**
```
docs/deployment/
  azure-staging.md
README.md (sección Deployment añadida)
```

---

## Riesgos

| Riesgo | Mitigación |
|--------|-----------|
| DANE data no disponible online | Dataset completo embebido en JSON dentro del repo |
| Azure credentials no disponibles | Código listo; documentar pasos manuales |
| EF migration conflict | Migraciones idempotentes; nueva migración para seed data |
| ACR image push timeout | Retry logic en workflow |

---

## Stopping Conditions

1. `dotnet build` falla → corregir antes de continuar
2. `dotnet test` falla → corregir antes de continuar
3. Inconsistencia irreconciliable en spec → documentar como ADR y decidir
