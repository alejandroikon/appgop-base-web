# Azure Staging — Manual de Despliegue

**GOP 360° · Iter 9.5 · Infraestructura Lean**  
**Versión:** 1.0 · **Autor:** GOP-Backend Agent  
**Costo objetivo:** ≤ $25/mes

---

## Prerrequisitos

```bash
# 1. Azure CLI ≥ 2.60
az --version

# 2. Docker Desktop (para build de imagen)
docker --version

# 3. .NET 10 SDK
dotnet --version

# 4. Login a Azure
az login
az account set --subscription <SUBSCRIPTION_ID>
```

---

## Variables de Entorno Necesarias

Exporta antes de ejecutar cualquier script:

```bash
export AZURE_SUBSCRIPTION_ID="<tu-subscription-id>"
export AZURE_TENANT_ID="<tu-tenant-id>"

# SQL Admin (mínimo 8 chars, upper+lower+number+symbol)
export SQL_ADMIN_PASSWORD="Gop360_Stg_$(openssl rand -hex 4)!"

# JWT Signing Key (32+ bytes)
export JWT_SIGNING_KEY="$(openssl rand -base64 32)"
```

---

## Paso 1: Provisionar Infraestructura (Bicep)

```bash
# Desde la raíz del repositorio
chmod +x infra/bicep/deploy.sh

# What-if (revisar cambios antes de aplicar)
./infra/bicep/deploy.sh --what-if

# Deploy real (primer despliegue ~8-12 min)
./infra/bicep/deploy.sh

# Con IP de dev para acceso SQL desde máquina local
MY_IP=$(curl -s https://api.ipify.org)
./infra/bicep/deploy.sh --dev-ip $MY_IP
```

### Recursos creados:

| Recurso | Nombre | Costo/mes |
|---------|--------|-----------|
| Resource Group | `rg-gop360-staging` | - |
| Managed Identity | `id-gop360-staging` | $0 |
| Container Registry | `acrgop360staging` | ~$5 |
| Key Vault | `kv-gop360-staging` | ~$0 |
| SQL Server | `sql-gop360-staging` | - |
| SQL Database | `sqldb-gop360-staging` (Serverless) | ~$5-8 |
| App Insights | `appi-gop360-staging` | ~$0 |
| Log Analytics | `log-gop360-staging` | ~$0 |
| App Service Plan | `asp-gop360-staging` (B1) | ~$13 |
| App Service | `app-gop360-staging-api` | incl. |

**Total: ~$23-26/mes** ✓

---

## Paso 2: Build y Push de Imagen Docker

```bash
# Login al ACR
az acr login --name acrgop360staging

# Build (desde backend/)
cd backend
docker build -t acrgop360staging.azurecr.io/gop-api:latest .

# Push
docker push acrgop360staging.azurecr.io/gop-api:latest

# Con tag específico
SHA=$(git rev-parse --short HEAD)
docker build -t acrgop360staging.azurecr.io/gop-api:$SHA .
docker push acrgop360staging.azurecr.io/gop-api:$SHA
```

---

## Paso 3: Configurar GitHub Actions

### Crear GitHub Secrets (en Settings → Secrets → Actions):

```bash
# 1. AZURE_CREDENTIALS — Service Principal con Contributor en el RG
az ad sp create-for-rbac \
  --name "sp-gop360-staging-github" \
  --role Contributor \
  --scopes /subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/rg-gop360-staging \
  --sdk-auth
# Copiar el JSON completo como secret AZURE_CREDENTIALS

# 2. SQL_ADMIN_PASSWORD — El mismo valor de $SQL_ADMIN_PASSWORD
# 3. JWT_SIGNING_KEY — El mismo valor de $JWT_SIGNING_KEY
```

Una vez configurados, el push a `gop-base-web` dispara automáticamente el pipeline completo.

---

## Paso 4: Aplicar Migraciones (primera vez)

Las migraciones se aplican automáticamente en startup (`ApplyMigrationsAndSeedAsync`).  
Para forzarlo manualmente:

```bash
# Restart del App Service (dispara startup + migrate + seed)
az webapp restart \
  --name app-gop360-staging-api \
  --resource-group rg-gop360-staging

# Verificar logs
az webapp log tail \
  --name app-gop360-staging-api \
  --resource-group rg-gop360-staging
```

---

## Paso 5: Smoke Tests

```bash
# Obtener URL del App Service
APP_URL=$(az webapp show \
  --name app-gop360-staging-api \
  --resource-group rg-gop360-staging \
  --query defaultHostName -o tsv)

# Test rápido (sin auth)
curl https://$APP_URL/health

# Suite completa (con auth)
TOKEN=$(curl -s -X POST https://$APP_URL/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gop.co","password":"Admin123!"}' | \
  python3 -c "import sys,json; print(json.load(sys.stdin)['data']['accessToken'])")

./.github/workflows/smoke-tests.sh "https://$APP_URL" "$TOKEN"
```

---

## Criterios de Aceptación — Checklist

| CA | Descripción | Cómo Verificar |
|----|-------------|----------------|
| CA-01 | `curl /health` → 200 `{"status":"Healthy"}` | `curl https://$APP_URL/health` |
| CA-02 | `GET /catalogs/departamentos` → 33 dptos | Ver smoke tests |
| CA-03 | `GET /catalogs/municipios?departamentoId=X` → ≥80 mpios Santander | Ver smoke tests |
| CA-04 | FE Netlify carga sin errores CORS | Abrir Netlify preview en browser |
| CA-05 | UWI canónico #1,2,3 correctos | Ver smoke tests |
| CA-06 | GitHub Action dispara en push | Push a `gop-base-web` y verificar |
| CA-07 | Secrets solo en Key Vault | `az keyvault secret list --vault-name kv-gop360-staging` |
| CA-08 | App Insights registra requests | Portal Azure → appi-gop360-staging → Live Metrics |
| CA-09 | Reproducible en <2h | Seguir este documento |
| CA-10 | Costo ≤ $30/mes | Azure Cost Management → rg-gop360-staging |

---

## Troubleshooting

### App Service no arranca (HTTP 503)
```bash
# Ver logs de la imagen Docker
az webapp log tail --name app-gop360-staging-api --resource-group rg-gop360-staging

# Verificar que la imagen existe en ACR
az acr repository list --name acrgop360staging

# Verificar env vars del App Service
az webapp config appsettings list --name app-gop360-staging-api --resource-group rg-gop360-staging
```

### SQL no conecta
```bash
# Verificar que la DB no está en auto-pause (tarda 15-20s en despertar)
az sql db show \
  --name sqldb-gop360-staging \
  --server sql-gop360-staging \
  --resource-group rg-gop360-staging \
  --query status

# Verificar connection string en Key Vault
az keyvault secret show \
  --vault-name kv-gop360-staging \
  --name "ConnectionStrings--DefaultConnection" \
  --query "value" -o tsv
```

### Key Vault Access Denied
```bash
# Verificar que el Managed Identity tiene rol Key Vault Secrets User
az role assignment list \
  --scope $(az keyvault show --name kv-gop360-staging --resource-group rg-gop360-staging --query id -o tsv) \
  --query "[].{principalId:principalId, role:roleDefinitionName}"
```

### CORS errors en FE
Verificar que el origen de Netlify termina en `.netlify.app` y que el App Service tiene el CORS configurado:
```bash
az webapp cors show --name app-gop360-staging-api --resource-group rg-gop360-staging
```

---

## Limpieza (Teardown)

```bash
# ⚠️ IRREVERSIBLE — Elimina TODOS los recursos de staging
az group delete --name rg-gop360-staging --yes --no-wait
echo "Eliminación en progreso (puede tardar 5-10 min)"
```

---

## Estimación de Costos Detallada

| Servicio | SKU | Precio |
|----------|-----|--------|
| App Service Plan B1 Linux | 730h/mes | $13.14/mes |
| Azure SQL Serverless GP_S_Gen5_1 | ~200h activo/mes (auto-pause) | $5-8/mes |
| Azure Container Registry Basic | 1 ACR | $5.00/mes |
| Key Vault Standard | <10,000 ops/mes | $0.03/mes |
| Application Insights | <5GB/mes (free tier) | $0.00/mes |
| Log Analytics | <5GB/mes (free tier) | $0.00/mes |
| **Total estimado** | | **$23-26/mes** |

> **Nota:** El SQL Serverless en staging típicamente usa <30h activo/mes, reduciendo el costo a ~$3-5.

---

## Migración a Producción (AKS)

Cuando el proyecto escale a producción:

1. El `Dockerfile` no cambia (mismo contenedor)
2. Las variables de entorno (`KeyVaultUri`, `AZURE_CLIENT_ID`, `ASPNETCORE_ENVIRONMENT`) se pasan al pod vía ConfigMap/SecretProviderClass
3. Las migraciones siguen siendo idempotentes
4. Solo cambia el target de deploy (AKS Deployment en lugar de App Service)

Estimado de migración: 1-2 días de infraestructura, 0 días de código de negocio.
