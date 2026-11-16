# E2E (Playwright)

Roda localmente e no CI (workflow `SIGH E2E`, `.github/workflows/e2e.yml`, em todo PR para a `main` que mexa em
`SIGH/**` ou no `compose.app.yml`). Specs:

| Spec | O que cobre |
| --- | --- |
| `auth-flow.spec.ts` | login do admin recém-semeado → troca obrigatória de senha → lista de medidas → reload mantém a sessão → logout; cookie `sigh_rt` HttpOnly/Secure/SameSite=Strict; refresh token fora de corpo e de storage |
| `two-tabs.spec.ts` | duas abas recarregando juntas não derrubam a sessão (com Web Locks e sem eles, exercitando a janela de tolerância do backend); logout numa aba leva a outra para `/login` |
| `evidence.spec.ts` | medida registrada + anexo pela tela da medida; upload de PDF real; `.exe` renomeado para `.pdf` rejeitado; download com SHA-256 igual ao gravado; registros de auditoria (criação, alteração, download) sem dados sensíveis |
| `password-reset.spec.ts` | "esqueci a senha": o e-mail chega ao Mailpit e o link redefine a senha (uso único) |
| `employees.spec.ts` | importação SAP (carga + atualização), apoio à decisão com uma suspensão registrada e aprovada pelo fluxo de medidas, foto JPG anexada à medida. Roda antes dos specs abaixo, que usam os funcionários importados |
| `records.spec.ts` | fluxo completo do RH: o admin cria auxiliar, encarregado e gestor da USGA131 (senha pelo link do e-mail); registro → rejeição → correção → aprovação; gestor da USGA131 vê, da USGA132 não (lista vazia, 404); admin sem botão de aprovar e API 403; quem tem os dois perfis não aprova a própria medida; gestor desativado vai para `/login` na requisição seguinte ao TTL (`Auth__SecurityStampCacheSeconds=2` no e2e) |

## Dois alvos

| Alvo | Como rodar | API e front | Banco |
| --- | --- | --- | --- |
| **stack** (o do CI) | `scripts/e2e-stack.sh` (na raiz) | containers do `compose.app.yml` + `compose.e2e.yml` (nginx em :18080, Mailpit em :18025) | SQL Server da própria stack, sem volume |
| **dev** | `npm run e2e` (em `SIGH/frontend`) | `dotnet run` + Vite (:5000 / :3000) | SQL Server do `docker-compose.yml` |

O `scripts/e2e-stack.sh` gera segredos aleatórios só para a execução, sobe a stack (`up --build --wait`), roda
`playwright test` com `E2E_TARGET=stack` e, passando ou falhando, remove containers, rede e as imagens
`sigh-api:e2e`/`sigh-web:e2e` (e o cache de build). A stack e2e não cria volumes. Argumentos vão para o Playwright
(`scripts/e2e-stack.sh e2e/records.spec.ts`); `E2E_KEEP_STACK=1` mantém a stack de pé para investigar e
`scripts/e2e-stack.sh --down` a remove depois. Quando falha, os logs da stack ficam em `test-results/stack-logs.txt`;
no CI, o relatório HTML do Playwright, os traces e esses logs viram o artifact `playwright-report`.

Seletores: por papel e texto acessível (`getByRole`, `getByLabel`); `data-testid` só onde não há alternativa estável
(e o `data-testid` fica no componente).

## Como funciona (alvo dev)

- O `playwright.config.ts` gera um banco novo `SIGH_E2E_<timestamp>`, sobe a API com
  `dotnet run` apontando para ele (as migrations e o seed criam tudo) e sobe o Vite.
- O `globalSetup` garante o Mailpit (`docker compose up -d mailpit`) e o `globalTeardown` remove o container se foi
  o setup que o criou (nunca volumes).
- O `globalTeardown` **apaga o banco desta execução ao final, mesmo se os testes falharem**
  (só nomes `SIGH_E2E_*`; `E2E_KEEP_DB=1` mantém o banco para investigar).
- A connection string base e a senha inicial do admin vêm dos user-secrets da API
  (`ConnectionStrings:DefaultConnection`, `Seed:AdminPassword`); nada é versionado.

## Pré-requisitos

1. SQL Server do `docker-compose.yml` rodando (container `sigh-sqlserver`).
2. User-secrets da API configurados (veja o README principal).
3. Portas 5000 e 3000 livres (a API do e2e não reaproveita uma API já de pé).
4. Navegador: `npx playwright install --with-deps chromium`.

## Rodar

```bash
cd SIGH/frontend
npm run e2e
```

Variáveis opcionais: `E2E_NEW_PASSWORD` (senha que o admin define; padrão aleatória),
`E2E_ADMIN_EMAIL`, `E2E_BASE_URL`, `E2E_MAILPIT_URL`, `E2E_BASE_CONNECTION_STRING`,
`E2E_SQL_CONTAINER`, `E2E_KEEP_DB=1`.
