#!/usr/bin/env bash
# Apaga SÓ os volumes deste projeto compose (banco e evidências do SIGH local), pedindo confirmação.
# Não toca em outros projetos, volumes ou no banco de desenvolvimento. O .env.local é mantido.
set -euo pipefail
cd "$(dirname "$0")/.."
PROJECT="${SIGH_LOCAL_PROJECT:-sigh-local}"
# Sem .env.local (já apagado), os segredos não importam para parar/apagar: valores de preenchimento.
if [ -f .env.local ]; then ENV_ARGS=(--env-file .env.local); else
  export SIGH_SA_PASSWORD=unused SIGH_JWT_SECRET=unused SIGH_ADMIN_PASSWORD=unused SIGH_DEMO_PASSWORD=unused; ENV_ARGS=(); fi

echo "Isto apaga TODOS os dados do SIGH local do projeto \"$PROJECT\" (volumes ${PROJECT}_db-data e ${PROJECT}_evidences)."
read -r -p "Digite APAGAR para confirmar: " answer
if [ "$answer" != "APAGAR" ]; then
  echo "Cancelado. Nada foi apagado."
  exit 0
fi

docker compose -p "$PROJECT" -f compose.app.yml ${ENV_ARGS[@]+"${ENV_ARGS[@]}"} down --volumes --remove-orphans
echo "Dados apagados. Na próxima vez, scripts/local-up.sh recria tudo do zero."
