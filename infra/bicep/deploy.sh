#!/usr/bin/env bash
# T-INFRA-09: GOP 360° Azure Staging — Deploy Script
# Prerequisites: az CLI, logged in, correct subscription set
# Usage: ./infra/bicep/deploy.sh [--what-if] [--dev-ip <IP>] [--tag <docker-tag>]

set -euo pipefail

# ── Configuration ─────────────────────────────────────────────────────────────
RESOURCE_GROUP="rg-gop360-staging"
LOCATION="eastus2"
SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"
DOCKER_TAG="${DOCKER_TAG:-latest}"
DEV_IP=""
WHAT_IF=false

# ── Parse args ────────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --what-if) WHAT_IF=true; shift ;;
    --dev-ip)  DEV_IP="$2"; shift 2 ;;
    --tag)     DOCKER_TAG="$2"; shift 2 ;;
    *) echo "Unknown arg: $1"; exit 1 ;;
  esac
done

# ── Validate ──────────────────────────────────────────────────────────────────
echo "▶ Validating Azure login..."
az account show --output none 2>/dev/null || { echo "❌ Not logged in. Run: az login"; exit 1; }

if [[ -n "$SUBSCRIPTION_ID" ]]; then
  az account set --subscription "$SUBSCRIPTION_ID"
fi

CURRENT_SUB=$(az account show --query id -o tsv)
echo "  Subscription: $CURRENT_SUB"

# ── Resource Group ────────────────────────────────────────────────────────────
echo "▶ Ensuring resource group: $RESOURCE_GROUP..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ── SQL Admin Password ────────────────────────────────────────────────────────
# Generate or use existing password stored in KV
SQL_PASSWORD="${SQL_ADMIN_PASSWORD:-}"
if [[ -z "$SQL_PASSWORD" ]]; then
  # Try to get from environment or generate
  SQL_PASSWORD=$(openssl rand -base64 20 | tr -dc 'a-zA-Z0-9!@#$%' | head -c 20)
  echo "  Generated SQL password (save to KV manually if needed)"
fi

# ── Deploy ────────────────────────────────────────────────────────────────────
BICEP_DIR="$(dirname "$0")"
DEPLOY_CMD="az deployment group"

if [[ "$WHAT_IF" == "true" ]]; then
  echo "▶ Running what-if (no changes applied)..."
  DEPLOY_CMD="az deployment group what-if"
fi

$DEPLOY_CMD \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$BICEP_DIR/main.bicep" \
  --parameters "$BICEP_DIR/main.parameters.staging.json" \
  --parameters sqlAdminPassword="$SQL_PASSWORD" \
  --parameters dockerImageTag="$DOCKER_TAG" \
  --parameters devIpAddress="$DEV_IP" \
  --name "gop360-staging-$(date +%Y%m%d%H%M%S)"

if [[ "$WHAT_IF" == "true" ]]; then
  echo "✅ What-if completed (no resources modified)"
  exit 0
fi

# ── Post-deploy: Store secrets in Key Vault ───────────────────────────────────
echo "▶ Storing secrets in Key Vault..."

KV_NAME="kv-gop360-staging"
SQL_SERVER_FQDN=$(az sql server show \
  --name "sql-gop360-staging" \
  --resource-group "$RESOURCE_GROUP" \
  --query "fullyQualifiedDomainName" -o tsv)

CONNECTION_STRING="Server=${SQL_SERVER_FQDN};Database=sqldb-gop360-staging;User Id=gopadmin;Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Store connection string
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "ConnectionStrings--DefaultConnection" \
  --value "$CONNECTION_STRING" \
  --output none

# Store SQL password
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "SqlAdminPassword" \
  --value "$SQL_PASSWORD" \
  --output none

# Generate & store JWT signing key (32 bytes base64)
JWT_KEY="${JWT_SIGNING_KEY:-$(openssl rand -base64 32)}"
az keyvault secret set \
  --vault-name "$KV_NAME" \
  --name "JwtSettings--SigningKey" \
  --value "$JWT_KEY" \
  --output none

echo "✅ Secrets stored in Key Vault: $KV_NAME"

# ── Outputs ───────────────────────────────────────────────────────────────────
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "  GOP 360° Staging Infrastructure — Deployed"
echo "═══════════════════════════════════════════════════════════"

APP_URL=$(az webapp show \
  --name "app-gop360-staging-api" \
  --resource-group "$RESOURCE_GROUP" \
  --query "defaultHostName" -o tsv)

ACR_SERVER=$(az acr show \
  --name "acrgop360staging" \
  --resource-group "$RESOURCE_GROUP" \
  --query "loginServer" -o tsv)

echo "  App Service:   https://$APP_URL"
echo "  ACR:           $ACR_SERVER"
echo "  Key Vault:     https://$KV_NAME.vault.azure.net"
echo "  Health check:  https://$APP_URL/health"
echo ""
echo "  Next steps:"
echo "  1. Push Docker image: docker push $ACR_SERVER/gop-api:latest"
echo "  2. Restart App Service: az webapp restart --name app-gop360-staging-api --resource-group $RESOURCE_GROUP"
echo "  3. Run smoke tests: ./.github/workflows/smoke-tests.sh https://$APP_URL"
echo "═══════════════════════════════════════════════════════════"
