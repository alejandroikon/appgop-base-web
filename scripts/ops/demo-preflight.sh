#!/usr/bin/env bash
#
# GOP 360° — Pre-flight del demo staging
#
# Hace en una sola corrida:
#   1. Verifica prerequisitos (az CLI, login, sub correcta, repo actualizado).
#   2. Carga los dos secretos SeedUsers en Key Vault kv-gop360-staging.
#   3. Rebuildea la imagen Docker del backend en ACR desde el código local.
#   4. Reinicia el App Service.
#   5. Hace polling de /health/ready hasta 200 Healthy (timeout 3 min).
#   6. Valida un login end-to-end contra /api/v1/auth/login.
#
# Uso:
#   # Desde la raíz del repo, rama gop-base-web, con az login hecho:
#   bash gop-demo-preflight.sh
#
#   # Sin prompt interactivo (para re-runs):
#   SEED_EXEC_PASSWORD='...' SEED_OP_PASSWORD='...' bash gop-demo-preflight.sh
#
# Requiere: az CLI, curl, bash 4+, python3. Funciona en Cloud Shell o laptop.

set -euo pipefail

# ---------- Config (ajustar si cambiaron los nombres de recursos) ----------
RESOURCE_GROUP="rg-gop360-staging"
APP_SERVICE="app-gop360-staging-api"
KEY_VAULT="kv-gop360-staging"
BACKEND_URL="https://app-gop360-staging-api.azurewebsites.net"
HEALTH_ENDPOINT="/health/ready"
LOGIN_ENDPOINT="/api/v1/auth/login"
EXEC_EMAIL="alejandro.gutierrez@interkont.co"
OP_EMAIL="admin@interkont.co"
IMAGE_TAGS=("gop-api:latest" "gop360-backend:latest")
# `az webapp restart` returns ANTES de que el contenedor realmente cicle
# (proceso async del lado de App Service). Si arrancamos a pollear /health/ready
# de una, pegamos contra el contenedor VIEJO y devolvemos falso positivo.
# Esta grace window le da tiempo a App Service para iniciar el tear-down real.
# B1 Linux container tarda ~30-45s en empezar a fallar healthcheck tras restart.
RESTART_GRACE_SEC=45
MAX_HEALTH_WAIT_SEC=240
POLL_INTERVAL_SEC=5
# Exigimos 2 respuestas 200 CONSECUTIVAS antes de declarar healthy, para que
# no pase que una respuesta 200 tardía del contenedor viejo nos haga creer
# que ya está arriba el nuevo.
HEALTH_CONSECUTIVE_OK=2
EXPECTED_BRANCH="gop-base-web"
# ---------------------------------------------------------------------------

BOLD=$(tput bold 2>/dev/null || true)
DIM=$(tput dim 2>/dev/null || true)
NC=$(tput sgr0 2>/dev/null || true)

log()  { echo "${BOLD}[$(date +%H:%M:%S)] $*${NC}"; }
info() { echo "${DIM}         $*${NC}"; }
fail() { echo "${BOLD}ERROR:${NC} $*" >&2; exit 1; }

# Helper: construye JSON body de login escapando password correctamente.
build_login_body() {
  python3 -c 'import json, sys; print(json.dumps({"email": sys.argv[1], "password": sys.argv[2]}))' "$1" "$2"
}

# 1. Prerequisitos
log "1/7 — Verificando prerequisitos..."
command -v az >/dev/null 2>&1      || fail "az CLI no encontrado. Instalá Azure CLI o corré esto en Cloud Shell."
command -v curl >/dev/null 2>&1    || fail "curl no encontrado."
command -v python3 >/dev/null 2>&1 || fail "python3 no encontrado (necesario para escapar el JSON del login)."

az account show >/dev/null 2>&1 || fail "No estás logueado en Azure. Corré: az login"
SUB_NAME=$(az account show --query name -o tsv)
info "Suscripción activa: ${SUB_NAME}"

# Validar que estamos sobre el commit correcto si estamos en un repo
if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
  if [[ "${CURRENT_BRANCH}" != "${EXPECTED_BRANCH}" ]]; then
    echo
    echo "ATENCIÓN: estás en la rama '${CURRENT_BRANCH}', no en '${EXPECTED_BRANCH}'."
    read -r -p "¿Seguir igual? [y/N] " answer
    [[ "${answer,,}" != "y" ]] && fail "Abortado. Cambiá de rama: git checkout ${EXPECTED_BRANCH} && git pull"
  else
    git fetch origin "${EXPECTED_BRANCH}" >/dev/null 2>&1 || true
    LOCAL_SHA=$(git rev-parse HEAD)
    REMOTE_SHA=$(git rev-parse "origin/${EXPECTED_BRANCH}" 2>/dev/null || echo "")
    if [[ -n "${REMOTE_SHA}" && "${LOCAL_SHA}" != "${REMOTE_SHA}" ]]; then
      echo
      echo "ATENCIÓN: tu HEAD local (${LOCAL_SHA:0:7}) no coincide con origin/${EXPECTED_BRANCH} (${REMOTE_SHA:0:7})."
      echo "         La imagen se construirá con lo que tengas local, NO con lo que está en GitHub."
      read -r -p "¿Seguir igual? [y/N] " answer
      [[ "${answer,,}" != "y" ]] && fail "Abortado. Corré: git pull origin ${EXPECTED_BRANCH}"
    fi
  fi
fi

# Localizar el contexto de Docker build
if [[ -f "./backend/Dockerfile" ]]; then
  BACKEND_CTX="./backend"
elif [[ -f "./Dockerfile" ]]; then
  BACKEND_CTX="."
else
  fail "No encontré Dockerfile. Corré este script desde la raíz del repo o desde backend/."
fi
info "Contexto Docker: ${BACKEND_CTX}"

# 2. Descubrir ACR
log "2/7 — Descubriendo ACR en ${RESOURCE_GROUP}..."
ACR_NAME=$(az acr list -g "${RESOURCE_GROUP}" --query "[0].name" -o tsv)
[[ -z "${ACR_NAME}" ]] && fail "No encontré ningún ACR en el resource group ${RESOURCE_GROUP}."
info "ACR: ${ACR_NAME}"

# 3. Solicitar passwords (a menos que vengan en env vars)
log "3/7 — Preparando passwords de seed users..."
if [[ -z "${SEED_EXEC_PASSWORD:-}" ]]; then
  read -r -s -p "  Password para ${EXEC_EMAIL}: " SEED_EXEC_PASSWORD
  echo
fi
if [[ -z "${SEED_OP_PASSWORD:-}" ]]; then
  read -r -s -p "  Password para ${OP_EMAIL}: " SEED_OP_PASSWORD
  echo
fi
[[ -z "${SEED_EXEC_PASSWORD}" || -z "${SEED_OP_PASSWORD}" ]] && fail "Ambos passwords son obligatorios."

# 4. Cargar secretos en KV
log "4/7 — Cargando secretos en ${KEY_VAULT}..."
az keyvault secret set \
  --vault-name "${KEY_VAULT}" \
  --name "SeedUsers--ExecAdmin--Password" \
  --value "${SEED_EXEC_PASSWORD}" \
  --output none
info "SeedUsers--ExecAdmin--Password cargado."

az keyvault secret set \
  --vault-name "${KEY_VAULT}" \
  --name "SeedUsers--OpAdmin--Password" \
  --value "${SEED_OP_PASSWORD}" \
  --output none
info "SeedUsers--OpAdmin--Password cargado."

# 5. Rebuild imagen Docker en ACR
log "5/7 — Rebuildeando imagen Docker en ACR (típicamente 2-3 min)..."
IMAGE_ARGS=()
for tag in "${IMAGE_TAGS[@]}"; do
  IMAGE_ARGS+=("--image" "${tag}")
done
az acr build \
  --registry "${ACR_NAME}" \
  "${IMAGE_ARGS[@]}" \
  "${BACKEND_CTX}"
info "Imagen construida y pusheada como: ${IMAGE_TAGS[*]}"

# 6. Restart App Service + polling /health/ready
log "6/7 — Reiniciando App Service y esperando a que el container NUEVO esté saludable..."
az webapp restart \
  --name "${APP_SERVICE}" \
  --resource-group "${RESOURCE_GROUP}" \
  --output none

# Grace window: `az webapp restart` retorna antes de que el tear-down del
# contenedor realmente arranque. Sin este sleep, el primer curl pega contra
# el contenedor VIEJO, vuelve 200, y declaramos falso "DEMO-READY".
info "Grace window post-restart: ${RESTART_GRACE_SEC}s (el contenedor aún no arrancó a ciclar)..."
sleep "${RESTART_GRACE_SEC}"

HEALTH_OK=""
elapsed=${RESTART_GRACE_SEC}
consecutive_ok=0
while [[ $elapsed -lt $MAX_HEALTH_WAIT_SEC ]]; do
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BACKEND_URL}${HEALTH_ENDPOINT}" || echo "000")
  if [[ "${HTTP_CODE}" == "200" ]]; then
    consecutive_ok=$((consecutive_ok + 1))
    info "  /health/ready = 200 (${consecutive_ok}/${HEALTH_CONSECUTIVE_OK} consecutivas, ${elapsed}s)"
    if [[ $consecutive_ok -ge $HEALTH_CONSECUTIVE_OK ]]; then
      info "Container nuevo healthy (${elapsed}s totales incluyendo grace window)."
      HEALTH_OK=1
      break
    fi
  else
    # Reset del contador — si había una racha de 200s del container viejo
    # y ahora vemos 503/502/000, estamos en el momento del cycle.
    if [[ $consecutive_ok -gt 0 ]]; then
      info "  (reset contador — container ciclando)"
    fi
    consecutive_ok=0
    info "  esperando... HTTP ${HTTP_CODE} (${elapsed}s)"
  fi
  sleep $POLL_INTERVAL_SEC
  elapsed=$((elapsed + POLL_INTERVAL_SEC))
done
if [[ -z "${HEALTH_OK}" ]]; then
  echo
  echo "El backend no respondió saludable en ${MAX_HEALTH_WAIT_SEC}s."
  echo "Revisá logs con:"
  echo "  az webapp log tail --name ${APP_SERVICE} -g ${RESOURCE_GROUP}"
  fail "Abortado."
fi

# 7. Smoke test del login
log "7/7 — Probando login end-to-end con ExecAdmin..."
LOGIN_BODY=$(build_login_body "${EXEC_EMAIL}" "${SEED_EXEC_PASSWORD}")
LOGIN_TMP=$(mktemp)
trap 'rm -f "${LOGIN_TMP}"' EXIT

LOGIN_CODE=$(curl -s -o "${LOGIN_TMP}" -w "%{http_code}" \
  -X POST "${BACKEND_URL}${LOGIN_ENDPOINT}" \
  -H "Content-Type: application/json" \
  -d "${LOGIN_BODY}")

if [[ "${LOGIN_CODE}" != "200" ]]; then
  echo
  echo "--- Respuesta del login ---"
  cat "${LOGIN_TMP}"
  echo
  echo "---------------------------"
  fail "Login devolvió HTTP ${LOGIN_CODE} (esperaba 200). Revisá el password o logs del App Service."
fi

# Validar que la respuesta trae accessToken
if ! python3 -c "import json, sys; d=json.load(open('${LOGIN_TMP}')); assert 'accessToken' in d, 'missing accessToken'" 2>/dev/null; then
  echo "--- Respuesta ---"
  cat "${LOGIN_TMP}"
  fail "Login 200 pero la respuesta no contiene accessToken. Algo raro."
fi

echo
echo "${BOLD}========================================${NC}"
echo "${BOLD} DEMO-READY${NC}"
echo "${BOLD}========================================${NC}"
echo
echo "Credenciales para el demo:"
echo "  ${EXEC_EMAIL}  -> el password que acabás de cargar"
echo "  ${OP_EMAIL}    -> el segundo password"
echo
echo "URLs:"
echo "  FE:     https://transcendent-dieffenbachia-cda87f.netlify.app"
echo "  Health: ${BACKEND_URL}${HEALTH_ENDPOINT}"
echo "  SQL:    Azure Portal > sqldb-gop360-staging > Query editor"
echo
echo "Tip: antes de llamar al equipo, entrá al FE con los dos admins para confirmar"
echo "     que ambos funcionan y para calentar el cache de Azure SQL serverless."
