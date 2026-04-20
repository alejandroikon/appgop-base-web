#!/usr/bin/env bash
# T-INFRA-25: Smoke Tests standalone script (also used in CI/CD workflow)
# Usage: ./smoke-tests.sh <BASE_URL> <AUTH_TOKEN>
# Example: ./smoke-tests.sh https://app-gop360-staging-api.azurewebsites.net eyJ...

set -euo pipefail

BASE_URL="${1:-https://app-gop360-staging-api.azurewebsites.net}"
AUTH_TOKEN="${2:-}"
PASS=0
FAIL=0

check() {
  local name="$1"
  local cmd="$2"
  local expected="$3"

  actual=$(eval "$cmd" 2>/dev/null || echo "ERROR")
  if [[ "$actual" == "$expected" ]] || [[ "$expected" == "GTE:$actual" ]]; then
    echo "✅ $name"
    ((PASS++)) || true
  else
    echo "❌ $name — Expected: $expected | Got: $actual"
    ((FAIL++)) || true
  fi
}

echo "═══════════════════════════════════════════"
echo "  GOP 360° Smoke Tests"
echo "  Base URL: $BASE_URL"
echo "═══════════════════════════════════════════"
echo ""

# CA-01: Health
check "CA-01: /health → 200" \
  "curl -s -o /dev/null -w '%{http_code}' $BASE_URL/health" \
  "200"

# CA-01b: Readiness  
check "CA-01b: /health/ready → 200" \
  "curl -s -o /dev/null -w '%{http_code}' $BASE_URL/health/ready" \
  "200"

if [[ -n "$AUTH_TOKEN" ]]; then
  # CA-02: 33 Departamentos
  COUNT=$(curl -s "$BASE_URL/api/v1/catalogs/departamentos" \
    -H "Authorization: Bearer $AUTH_TOKEN" | \
    python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d.get('data',d)))" 2>/dev/null || echo "0")
  if [[ "$COUNT" -ge 33 ]]; then
    echo "✅ CA-02: $COUNT departamentos (≥33)"
    ((PASS++)) || true
  else
    echo "❌ CA-02: $COUNT departamentos (expected ≥33)"
    ((FAIL++)) || true
  fi

  # CA-03: ≥80 Municipios Santander
  SANTANDER_ID=$(curl -s "$BASE_URL/api/v1/catalogs/departamentos" \
    -H "Authorization: Bearer $AUTH_TOKEN" | \
    python3 -c "
import sys,json
d=json.load(sys.stdin)
items=d.get('data',d) if isinstance(d,dict) else d
for item in items:
    if str(item.get('codigoDane','')).lstrip('0')=='68': print(item['id']); break
" 2>/dev/null || echo "")

  if [[ -n "$SANTANDER_ID" ]]; then
    MPIO_COUNT=$(curl -s "$BASE_URL/api/v1/catalogs/municipios?departamentoId=$SANTANDER_ID" \
      -H "Authorization: Bearer $AUTH_TOKEN" | \
      python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d.get('data',d)))" 2>/dev/null || echo "0")
    if [[ "$MPIO_COUNT" -ge 80 ]]; then
      echo "✅ CA-03: $MPIO_COUNT municipios Santander (≥80)"
      ((PASS++)) || true
    else
      echo "❌ CA-03: $MPIO_COUNT municipios Santander (expected ≥80)"
      ((FAIL++)) || true
    fi
  else
    echo "❌ CA-03: Could not find Santander department"
    ((FAIL++)) || true
  fi

  # CA-05: UWI Canónico #1
  UWI1=$(curl -s "$BASE_URL/api/v1/wells/preview-uwi?codigoDaneDpto=50&codigoDaneMpio=50568&denominacion=CURE&consecutivo=1&clusterNombre=LA&clusterNumero=0&tipoAngulo=V&tipoTrayectoria=O&trayectoriaConsecutivo=0&tipoObjetivo=PH&tipoTerminacion=OH&isAnh=false" \
    -H "Authorization: Bearer $AUTH_TOKEN" | \
    python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('uwi','') or d.get('uwi',''))" 2>/dev/null || echo "")
  check "CA-05a: UWI Canónico #1" "echo '$UWI1'" "50568CURE0001LA0000VPH-OH"

  UWI2=$(curl -s "$BASE_URL/api/v1/wells/preview-uwi?codigoDaneDpto=68&codigoDaneMpio=68081&denominacion=ALPH&consecutivo=42&clusterNombre=CN&clusterNumero=3&tipoAngulo=H&tipoTrayectoria=ST2&trayectoriaConsecutivo=2&tipoObjetivo=I&tipoTerminacion=CD&isAnh=false" \
    -H "Authorization: Bearer $AUTH_TOKEN" | \
    python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('uwi','') or d.get('uwi',''))" 2>/dev/null || echo "")
  check "CA-05b: UWI Canónico #2" "echo '$UWI2'" "68081ALPH0042CN0003HST2I-CD"

  UWI3=$(curl -s "$BASE_URL/api/v1/wells/preview-uwi?codigoDaneDpto=86&codigoDaneMpio=86320&denominacion=ANHE&consecutivo=1&tipoAngulo=V&tipoTrayectoria=O&trayectoriaConsecutivo=0&tipoObjetivo=G&tipoTerminacion=O&isAnh=true" \
    -H "Authorization: Bearer $AUTH_TOKEN" | \
    python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('data',{}).get('uwi','') or d.get('uwi',''))" 2>/dev/null || echo "")
  check "CA-05c: UWI Canónico #3" "echo '$UWI3'" "86320ANHE0001CX0000VGT-O"
fi

echo ""
echo "═══════════════════════════════════════════"
echo "  Results: $PASS passed, $FAIL failed"
echo "═══════════════════════════════════════════"

[[ "$FAIL" -eq 0 ]] || exit 1
