# E2E local (Playwright)

Roda só localmente (ainda fora do CI). Specs:

| Spec | O que cobre |
| --- | --- |
| `auth-flow.spec.ts` | login do admin recém-semeado → troca obrigatória de senha → lista de medidas → reload mantém a sessão → logout; cookie `sigh_rt` HttpOnly/Secure/SameSite=Strict; refresh token fora de corpo e de storage |
| `two-tabs.spec.ts` | duas abas recarregando juntas não derrubam a sessão (com Web Locks e sem eles, exercitando a janela de tolerância do backend); logout numa aba leva a outra para `/login` |
| `evidence.spec.ts` | medida registrada + anexo pela tela da medida; upload de PDF real; `.exe` renomeado para `.pdf` rejeitado; download com SHA-256 igual ao gravado; registros de auditoria (criação, alteração, download) sem dados sensíveis |
| `password-reset.spec.ts` | "esqueci a senha": o e-mail chega ao Mailpit e o link redefine a senha (uso único) |
| `employees.spec.ts` | importação SAP (carga + atualização), apoio à decisão com uma suspensão registrada e aprovada pelo fluxo de medidas, foto JPG anexada à medida. Roda antes dos specs abaixo, que usam os funcionários importados |
| `records.spec.ts` | fluxo completo do RH: o admin cria auxiliar, encarregado e gestor da USGA131 (senha pelo link do e-mail); registro → rejeição → correção → aprovação; gestor da USGA131 vê, da USGA132 não (lista vazia, 404); admin sem botão de aprovar e API 403; quem tem os dois perfis não aprova a própria medida; gestor desativado vai para `/login` na requisição seguinte ao TTL (`Auth__SecurityStampCacheSeconds=2` no e2e) |

## Como funciona

- O `playwright.config.ts` gera um banco novo `SIGH_E2E_<timestamp>`, sobe a API com
  `dotnet run` apontando para ele (as migrations e o seed criam tudo) e sobe o Vite.
- O `globalSetup` garante o Mailpit (`docker compose up -d mailpit`).
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
