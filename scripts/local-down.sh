#!/usr/bin/env bash
# Para o SIGH local. Os dados (banco e evidências) ficam guardados nos volumes do projeto.
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT="${SIGH_LOCAL_PROJECT:-sigh-local}"
# Sem .env.local (já apagado), os segredos não importam para parar/apagar: valores de preenchimento.
if [ -f .env.local ]; then ENV_ARGS=(--env-file .env.local); else
  export SIGH_SA_PASSWORD=unused SIGH_JWT_SECRET=unused SIGH_ADMIN_PASSWORD=unused SIGH_DEMO_PASSWORD=unused; ENV_ARGS=(); fi
docker compose -p "$PROJECT" -f compose.app.yml ${ENV_ARGS[@]+"${ENV_ARGS[@]}"} down
echo "SIGH parado. Os dados foram mantidos. Para subir de novo: scripts/local-up.sh"
