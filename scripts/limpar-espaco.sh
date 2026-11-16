#!/usr/bin/env bash
# Libera espaço em disco no Codespace/máquina de dev. Seguro e idempotente: pode rodar quantas vezes quiser.
#   scripts/limpar-espaco.sh               # limpeza leve (+ pesada só se o disco estiver cheio); mostra antes/depois
#   scripts/limpar-espaco.sh --completo    # tudo, leve e pesada, independentemente do espaço livre
#   scripts/limpar-espaco.sh --silencioso  # só uma linha de resumo (usado no postStartCommand do .devcontainer)
#   (as opções podem ser combinadas: --completo --silencioso)
#
# Limpeza leve (SEMPRE):
#   - Docker: cache de build, imagens órfãs (dangling) e containers parados;
#   - bancos SIGH_E2E_* que sobraram nos SQL Servers em execução (só esse padrão de nome).
# Limpeza pesada (com --completo, OU quando o espaço livre em /workspaces estiver abaixo de
# LIMPEZA_LIMITE_GB, padrão 6):
#   - caches do npm, do NuGet e dos navegadores do Playwright;
#   - pastas bin/ e obj/ do backend que NÃO estão no git.
# NUNCA apaga: volumes Docker, imagens em uso ou com nome, o .env/.env.local, nem arquivos versionados.
# Depois de rodar, se precisar: "cd SIGH/backend && dotnet tool restore" (dotnet-ef) e
# "cd SIGH/frontend && npx playwright install chromium" (e2e). O node_modules não é apagado.
set -uo pipefail

cd "$(dirname "$0")/.."
ROOT="$PWD"
QUIET=0
FULL=0
for arg in "$@"; do
  case "$arg" in
    --silencioso) QUIET=1 ;;
    --completo) FULL=1 ;;
    *) echo "Opção desconhecida: $arg (use --completo e/ou --silencioso)" >&2; exit 2 ;;
  esac
done
LIMIT_GB="${LIMPEZA_LIMITE_GB:-6}"
if ! [[ "$LIMIT_GB" =~ ^[0-9]+$ ]]; then
  echo "LIMPEZA_LIMITE_GB deve ser um número inteiro de GB (recebido: $LIMIT_GB)." >&2
  exit 2
fi
# Espaço medido em /workspaces (Codespace); fora dele, no disco do repositório.
DISK="/workspaces"
[ -d "$DISK" ] || DISK="$ROOT"

say() { [ "$QUIET" = 1 ] || echo "$@"; }
run() { if [ "$QUIET" = 1 ]; then "$@" >/dev/null 2>&1; else "$@"; fi; }
free_kb() { df -Pk "$DISK" | awk 'NR==2 {print $4}'; }
human() { awk -v kb="$1" 'BEGIN { printf "%.1f GB", kb / 1024 / 1024 }'; }

before=$(free_kb)
if [ "$QUIET" = 0 ]; then
  echo "== Espaço ANTES =="
  df -h "$DISK" /tmp 2>/dev/null
  echo
fi

if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  # Bancos SIGH_E2E_* em qualquer SQL Server em execução (dev, stack local ou e2e).
  # A senha do sa vem do próprio container (MSSQL_SA_PASSWORD), sem ler arquivos.
  for c in $(docker ps -q --filter ancestor=mcr.microsoft.com/mssql/server:2022-latest); do
    name=$(docker inspect -f '{{.Name}}' "$c" | sed 's#^/##')
    # shellcheck disable=SC2016
    dbs=$(docker exec "$c" bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name LIKE '"'"'SIGH[_]E2E[_]%'"'"'"' 2>/dev/null \
      | tr -d '\r' | grep -E '^SIGH_E2E_[0-9]{8,17}$' || true)
    for db in $dbs; do
      say "Apagando o banco $db (container $name)..."
      # shellcheck disable=SC2016
      docker exec -e DB="$db" "$c" bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "ALTER DATABASE [$DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DB];"' >/dev/null 2>&1 \
        || say "  aviso: não consegui apagar $db."
    done
  done

  say "Docker: containers parados..."
  run docker container prune -f
  say "Docker: imagens órfãs (sem nome)..."
  run docker image prune -f
  say "Docker: cache de build..."
  run docker builder prune -af
  # Sem "docker volume prune" e sem "docker system prune --volumes": volumes nunca são apagados.
else
  say "Docker indisponível: etapa do Docker pulada."
fi

limit_kb=$(( LIMIT_GB * 1024 * 1024 ))
now_kb=$(free_kb)
if [ "$FULL" = 1 ]; then
  heavy=1; reason="--completo"
elif [ "$now_kb" -lt "$limit_kb" ]; then
  heavy=1; reason="livre $(human "$now_kb") < limite ${LIMIT_GB} GB"
else
  heavy=0; reason="livre $(human "$now_kb") >= limite ${LIMIT_GB} GB"
fi

if [ "$heavy" = 1 ]; then
  say "Limpeza pesada ($reason)."
  if command -v npm >/dev/null 2>&1; then
    say "Cache do npm..."
    run npm cache clean --force
  fi
  if command -v dotnet >/dev/null 2>&1; then
    say "Cache do NuGet..."
    run dotnet nuget locals all --clear
  fi
  say "Navegadores do Playwright (~/.cache/ms-playwright)..."
  rm -rf "${PLAYWRIGHT_BROWSERS_PATH:-$HOME/.cache/ms-playwright}"

  say "Pastas bin/ e obj/ do backend (só as que não estão no git)..."
  while IFS= read -r -d '' dir; do
    if [ -z "$(git -C "$ROOT" ls-files -- "$dir" | head -1)" ]; then
      rm -rf "$dir"
    else
      say "  mantida (tem arquivos versionados): $dir"
    fi
  done < <(find "$ROOT/SIGH" -type d -name node_modules -prune -o -type d \( -name bin -o -name obj \) -print0 -prune)
else
  say "Limpeza pesada pulada ($reason): caches do npm/NuGet/Playwright e bin/obj mantidos. Use --completo para forçar."
fi

after=$(free_kb)
if [ "$QUIET" = 0 ]; then
  echo
  echo "== Espaço DEPOIS =="
  df -h "$DISK" /tmp 2>/dev/null
  echo
fi
mode=$([ "$heavy" = 1 ] && echo "leve + pesada" || echo "leve")
echo "limpar-espaco ($mode; $reason): livre antes $(human "$before"), depois $(human "$after") (liberado $(human $(( after > before ? after - before : 0 ))))."
