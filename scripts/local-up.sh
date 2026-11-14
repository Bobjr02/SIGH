#!/usr/bin/env bash
# Sobe o SIGH inteiro (banco, Mailpit, API e web) com dados FICTÍCIOS de demonstração.
# Primeira execução: gera o .env.local (fora do git) com segredos aleatórios. As seguintes o reaproveitam.
#   scripts/local-up.sh            (projeto compose: sigh-local; outro nome: SIGH_LOCAL_PROJECT=meu-nome)
set -euo pipefail

cd "$(dirname "$0")/.."
ENV_FILE=".env.local"
PROJECT="${SIGH_LOCAL_PROJECT:-sigh-local}"
COMPOSE=(docker compose -p "$PROJECT" -f compose.app.yml --env-file "$ENV_FILE")

if ! docker compose version >/dev/null 2>&1; then
  echo "ERRO: 'docker compose' não encontrado. Instale e abra o Rancher Desktop (motor dockerd/moby) e tente de novo." >&2
  exit 1
fi

# Letras e dígitos aleatórios + um sufixo que garante maiúscula, minúscula, dígito e símbolo.
# Sem $, #, aspas, ; ou =: o valor vai sem escape para o .env e para a connection string.
random_secret() { printf '%s-Aa9' "$(LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c "$1")"; }

if [ ! -f "$ENV_FILE" ]; then
  umask 077
  cat >"$ENV_FILE" <<ENV
# Gerado por scripts/local-up em $(date -u +%Y-%m-%dT%H:%M:%SZ). Fora do git (.gitignore: .env.*).
# Segredos SÓ desta máquina. Apague este arquivo e rode scripts/local-reset para gerar outros.
SIGH_SA_PASSWORD=$(random_secret 24)
SIGH_JWT_SECRET=$(LC_ALL=C tr -dc 'a-f0-9' </dev/urandom | head -c 64)
SIGH_ADMIN_EMAIL=admin@demo.local
SIGH_ADMIN_PASSWORD=$(random_secret 16)
SIGH_DEMO_PASSWORD=$(random_secret 12)
SIGH_WEB_PORT=8080
SIGH_MAILPIT_PORT=8025
ENV
  echo "Arquivo $ENV_FILE criado com segredos aleatórios."
fi

value() { grep -E "^$1=" "$ENV_FILE" | head -1 | cut -d= -f2-; }

# Portas ocupadas por outro programa (se a stack deste projeto já está de pé, as portas são dela).
if [ -z "$("${COMPOSE[@]}" ps -q web 2>/dev/null)" ]; then
  for var in SIGH_WEB_PORT SIGH_MAILPIT_PORT; do
    port="$(value "$var")"
    if [ -n "$port" ] && (exec 3<>"/dev/tcp/127.0.0.1/$port") 2>/dev/null; then
      echo "ERRO: a porta $port ($var) já está em uso por outro programa. Feche-o ou troque $var no $ENV_FILE." >&2
      exit 1
    fi
  done
fi

echo "Subindo o SIGH (projeto \"$PROJECT\"). A primeira vez baixa as imagens e compila: pode levar alguns minutos..."
start=$(date +%s)
if ! "${COMPOSE[@]}" up -d --build --wait --wait-timeout 1200; then
  echo "ERRO: a stack não ficou saudável. Veja: docker compose -p $PROJECT -f compose.app.yml logs api" >&2
  exit 1
fi
elapsed=$(( $(date +%s) - start ))

demo_status="$("${COMPOSE[@]}" logs api 2>/dev/null | grep -E 'Demo: (dados de demonstração prontos|falha)' | tail -1 || true)"

web_port="$(value SIGH_WEB_PORT)"; web_port="${web_port:-8080}"
mail_port="$(value SIGH_MAILPIT_PORT)"; mail_port="${mail_port:-8025}"

cat <<INFO

============================================================
 SIGH no ar em ${elapsed}s  —  AMBIENTE DE DEMONSTRAÇÃO (dados fictícios)
============================================================
 Abra:            http://localhost:${web_port}
 Administrador:   $(value SIGH_ADMIN_EMAIL)
 Senha do admin:  $(value SIGH_ADMIN_PASSWORD)
                  (no primeiro acesso o sistema pede para trocar a senha)

 Usuários de demonstração (todos com a senha: $(value SIGH_DEMO_PASSWORD))
   auxiliar@demo.local            RH – Registro
   encarregado@demo.local         RH – Aprovador
   juridico@demo.local            Jurídico
   gestor.industrial@demo.local   Gestor da USGA131
   gestor.agricola@demo.local     Gestor da USGA137 e USGA141
   preposto@demo.local            RH – Registro + RH – Aprovador

 E-mails (Mailpit): http://localhost:${mail_port}
 Parar:  scripts/local-down.sh     Zerar:  scripts/local-reset.sh
============================================================
INFO

if [ -z "$demo_status" ]; then
  echo "Aviso: não encontrei no log da API a confirmação dos dados de demonstração." >&2
elif echo "$demo_status" | grep -q "falha"; then
  echo "Aviso: os dados de demonstração falharam. Veja: docker compose -p $PROJECT -f compose.app.yml logs api" >&2
fi
