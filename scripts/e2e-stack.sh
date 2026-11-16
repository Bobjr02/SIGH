#!/usr/bin/env bash
# Suíte e2e (SIGH/frontend/e2e) contra a stack EM CONTAINERS: compose.app.yml + compose.e2e.yml.
# É o mesmo caminho do CI (.github/workflows/e2e.yml).
#   scripts/e2e-stack.sh                       # suíte inteira
#   scripts/e2e-stack.sh e2e/records.spec.ts   # argumentos vão para o "playwright test"
#
# - Segredos aleatórios gerados AQUI, só em variáveis de ambiente (nenhum arquivo; o .env.local não é lido nem tocado).
# - Projeto compose próprio (sigh-e2e), imagens sigh-api:e2e / sigh-web:e2e e portas 18080/18025:
#   convive com a stack de demonstração (sigh-local) e não mexe nos volumes dela.
# - No final, passando ou falhando: remove os containers, a rede e as imagens que criou
#   (E2E_KEEP_STACK=1 mantém a stack de pé para investigar; E2E_KEEP_IMAGES=1 mantém as imagens).
#   A stack e2e não cria volumes (veja compose.e2e.yml).
# Pré-requisitos: Docker Compose >= 2.24, Node (npm ci em SIGH/frontend) e o Chromium do Playwright
#   (cd SIGH/frontend && npx playwright install --with-deps chromium).
set -euo pipefail

cd "$(dirname "$0")/.."
ROOT="$PWD"
PROJECT="${SIGH_E2E_PROJECT:-sigh-e2e}"
COMPOSE=(docker compose -p "$PROJECT" --project-directory "$ROOT" -f "$ROOT/compose.app.yml" -f "$ROOT/compose.e2e.yml")

random_secret() { printf '%s-Aa9' "$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c "$1" || true)"; }

export SIGH_LOCAL_PROJECT="$PROJECT"
export SIGH_SA_PASSWORD="$(random_secret 24)"
export SIGH_JWT_SECRET="$(LC_ALL=C tr -dc 'a-f0-9' </dev/urandom | head -c 64 || true)"
export SIGH_ADMIN_EMAIL="admin@e2e.local"
export SIGH_ADMIN_PASSWORD="$(random_secret 16)"
export SIGH_DEMO_PASSWORD="$(random_secret 12)"   # exigida pelo compose.app.yml; sem dados de demonstração no e2e
export SIGH_WEB_PORT="${SIGH_E2E_WEB_PORT:-18080}"
export SIGH_MAILPIT_PORT="${SIGH_E2E_MAILPIT_PORT:-18025}"
export SIGH_E2E_DB_NAME="SIGH_E2E_$(date -u +%Y%m%d%H%M%S)"

cleanup() {
  local status=$?
  set +e
  if [ "$status" -ne 0 ]; then
    mkdir -p "$ROOT/SIGH/frontend/test-results"
    "${COMPOSE[@]}" logs --no-color --timestamps >"$ROOT/SIGH/frontend/test-results/stack-logs.txt" 2>&1
    echo "Logs da stack em SIGH/frontend/test-results/stack-logs.txt"
  fi
  if [ "${E2E_KEEP_STACK:-}" = "1" ]; then
    echo "E2E_KEEP_STACK=1: stack \"$PROJECT\" mantida (web em http://localhost:$SIGH_WEB_PORT). Para remover: $0 --down"
    exit "$status"
  fi
  echo "Removendo containers e rede do projeto \"$PROJECT\"..."
  # Sem --volumes: a stack e2e não cria volumes, e nenhum volume existente é tocado.
  "${COMPOSE[@]}" down --remove-orphans --timeout 5 >/dev/null 2>&1 || echo "Aviso: \"docker compose down\" falhou; rode $0 --down." >&2
  if [ "${E2E_KEEP_IMAGES:-}" != "1" ]; then
    echo "Removendo as imagens temporárias sigh-api:e2e e sigh-web:e2e..."
    docker image rm sigh-api:e2e sigh-web:e2e >/dev/null 2>&1
    # Camadas intermediárias e cache de build que a compilação deixou.
    docker image prune -f >/dev/null 2>&1
    docker builder prune -f >/dev/null 2>&1
  fi
  exit "$status"
}

if [ "${1:-}" = "--down" ]; then
  trap - EXIT
  "${COMPOSE[@]}" down --remove-orphans --timeout 5
  docker image rm sigh-api:e2e sigh-web:e2e 2>/dev/null || true
  exit 0
fi

trap cleanup EXIT

for port in "$SIGH_WEB_PORT" "$SIGH_MAILPIT_PORT"; do
  if (exec 3<>"/dev/tcp/127.0.0.1/$port") 2>/dev/null; then
    echo "ERRO: a porta $port já está em uso. Rode \"$0 --down\" ou troque SIGH_E2E_WEB_PORT / SIGH_E2E_MAILPIT_PORT." >&2
    exit 1
  fi
done

echo "Compilando e subindo a stack e2e (projeto \"$PROJECT\", banco $SIGH_E2E_DB_NAME)..."
start=$(date +%s)
"${COMPOSE[@]}" up -d --build --wait --wait-timeout 900
echo "Stack saudável em $(( $(date +%s) - start ))s."

SQL_CONTAINER="$("${COMPOSE[@]}" ps -q sqlserver)"

cd SIGH/frontend
start=$(date +%s)
E2E_TARGET=stack \
E2E_BASE_URL="http://localhost:$SIGH_WEB_PORT" \
E2E_MAILPIT_URL="http://localhost:$SIGH_MAILPIT_PORT" \
E2E_ADMIN_EMAIL="$SIGH_ADMIN_EMAIL" \
E2E_ADMIN_PASSWORD="$SIGH_ADMIN_PASSWORD" \
E2E_DB_NAME="$SIGH_E2E_DB_NAME" \
E2E_SQL_CONTAINER="$SQL_CONTAINER" \
E2E_BASE_CONNECTION_STRING="Server=localhost;User Id=sa;Password=$SIGH_SA_PASSWORD;TrustServerCertificate=True;" \
  npx playwright test "$@"
echo "Suíte e2e concluída em $(( $(date +%s) - start ))s."
