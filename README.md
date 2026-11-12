# SIGH - Sistema Inteligente de Gestão de Histórico Disciplinar

## Descrição do Projeto

O **SIGH (Sistema Inteligente de Gestão de Histórico Disciplinar)** é uma plataforma corporativa desenvolvida para gerenciar e acompanhar históricos disciplinares com alto nível de integridade, auditoria e inteligência operacional. 

Esta versão marca a **Fundação da Arquitetura**, preparando toda a infraestrutura backend e frontend com suporte a containers Docker, sem implementar regras de negócio ou formulários de produção nesta etapa inicial.

---

## Tecnologias Utilizadas

### Frontend
- **React 19**
- **TypeScript**
- **Vite**
- **Material UI (MUI v6)**
- **React Router v7**
- **Axios**
- **React Query (@tanstack/react-query)**
- **React Hook Form**
- **Zod & @hookform/resolvers**
- **Recharts**
- **DayJS**

### Backend (.NET 9)
- **ASP.NET Core Web API (.NET 9)**
- **Entity Framework Core 9** (SqlServer, Design, Tools)
- **FluentValidation**
- **AutoMapper**
- **JWT Authentication** (Somente dependências configuradas)
- **Swagger / OpenAPI** (Swashbuckle com suporte a JWT)
- **Serilog** (Console & Arquivos)

### Banco de Dados & Infraestrutura
- **SQL Server 2022**
- **Docker & Docker Compose**

---

## Estrutura do Projeto

```
SIGH/
├── frontend/
│   └── src/
│       ├── assets/
│       ├── components/
│       ├── contexts/
│       ├── hooks/
│       ├── layouts/
│       ├── pages/
│       ├── routes/
│       ├── services/
│       ├── styles/
│       ├── types/
│       └── utils/
├── backend/
│   ├── src/
│   │   ├── SIGH.Api/
│   │   ├── SIGH.Application/
│   │   ├── SIGH.Domain/
│   │   ├── SIGH.Infrastructure/
│   │   └── SIGH.Persistence/
│   └── tests/
│       ├── SIGH.UnitTests/
│       └── SIGH.IntegrationTests/
├── database/
│   ├── migrations/
│   └── scripts/
├── docs/
│   └── architecture/
└── scripts/
```

---

## Como Executar

### 1. Pré-requisitos
- **Node.js** 24
- **.NET 9 SDK**
- **Docker & Docker Compose**

Ou abra o repositório num **Codespace / devcontainer** (`.devcontainer/devcontainer.json`): ele já traz
.NET 9, Node 24, docker-in-docker e GitHub CLI, encaminha as portas 5000 (API), 3000 (front) e 8025
(Mailpit) e, no `postCreateCommand` (`.devcontainer/post-create.sh`), instala o Claude Code pelo
instalador nativo (`curl -fsSL https://claude.ai/install.sh | bash`, em `~/.local/bin`, que entra no
PATH), roda `dotnet tool restore` em `SIGH/backend` e `npm ci` em `SIGH/frontend`.

### 2. Subindo o Banco de Dados (SQL Server)
```bash
cp .env.example .env   # troque MSSQL_SA_PASSWORD (o .env não vai para o git)
docker compose up -d   # aguarde o status "healthy" em: docker compose ps
```
O container roda como o usuário `mssql` (não root), com o volume nomeado montado em `/var/opt/mssql`.
O mesmo `docker compose up -d` sobe o **Mailpit** (SMTP em `localhost:1025`, interface web em `http://localhost:8025`),
que captura os e-mails de desenvolvimento.

API em container (opcional): `docker compose --profile api up -d --build`. Ela roda sem root e grava as
evidências no volume nomeado `sigh-evidences` (`/var/lib/sigh/evidences`).

### 3. Executando o Backend (.NET 9)
Segredos **não** ficam nos `appsettings*.json`. Sem configuração válida a API não sobe e lista o que falta.

| Chave | Obrigatória | Observação |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | sim | — |
| `JwtOptions:SecretKey` | sim | pelo menos 32 bytes (UTF-8) |
| `JwtOptions:Issuer` / `JwtOptions:Audience` | sim | já definidos no `appsettings.json` |
| `Seed:AdminEmail` / `Seed:AdminPassword` | para criar o admin inicial | o admin nasce com troca de senha obrigatória |

Em desenvolvimento, use user-secrets:
```bash
cd SIGH/backend/src/SIGH.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=SIGH_DB;User Id=sa;Password=<senha do .env>;TrustServerCertificate=True;"
dotnet user-secrets set "JwtOptions:SecretKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Seed:AdminPassword" "<senha inicial do admin>"
dotnet run
```
Em CI/produção, use variáveis de ambiente (`ConnectionStrings__DefaultConnection`, `JwtOptions__SecretKey`, …; veja `.env.example`).

O Swagger estará acessível em: `http://localhost:5000/swagger` ou `https://localhost:5001/swagger`.

#### Troca de senha no primeiro acesso
Usuários com troca de senha pendente recebem um access token com a claim `pwd_change_required: "true"`.
Com ele, qualquer endpoint autenticado responde **403** (ProblemDetails com `"code": "PASSWORD_CHANGE_REQUIRED"`),
exceto `POST /api/v1/auth/change-password`, `POST /api/v1/auth/logout`, `POST /api/v1/auth/refresh` e `GET /api/v1/auth/me`.

Depois de `change-password`, todos os refresh tokens anteriores do usuário são revogados; a resposta traz um novo
`accessToken` já sem a claim e o novo refresh token vai no cookie. O access token antigo continua restrito até expirar.

#### Refresh token em cookie
O refresh token **nunca** aparece no corpo das respostas: `login`, `refresh` e `change-password` o gravam no cookie
`sigh_rt` (`HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth`, com a expiração do refresh token).
`POST /auth/refresh` lê o cookie e ignora o corpo; `POST /auth/logout` revoga o token e apaga o cookie.
`GET /auth/me` devolve o usuário com perfis e permissões efetivas lidas do banco.

Origens de CORS: `Cors:AllowedOrigins` (lista). Em dev o front usa o proxy do Vite e não precisa de CORS.

#### Evidências (arquivos)
Fluxo em dois passos:
1. `POST /api/v1/disciplinary-cases/{id}/evidences` (JSON: tipo, título, descrição, data de coleta) cadastra a evidência;
2. `POST /api/v1/disciplinary-cases/{id}/evidences/{evidenceId}/file` (multipart, campo `file`) anexa o arquivo, **uma única vez**.

- Formatos: pdf, jpg/jpeg, png, webp, mp4, mp3, m4a, docx, xlsx, validados pela **assinatura** (magic bytes).
  O tipo gravado é o detectado; o SHA-256 é calculado no servidor.
- Tamanho máximo: `Storage:MaxUploadSizeBytes` (padrão 25 MB), aplicado também no Kestrel e no `FormOptions`.
- Arquivos em `Storage:Local:RootPath`, **fora** do diretório da aplicação (dev: `SIGH/backend/.data/evidences`, ignorado
  pelo git). Nome físico = GUID + extensão.
- Download: `GET .../evidences/{evidenceId}/file` (exige `Disciplinary.Cases.View`; registrado na auditoria).
- Remoção: `DELETE .../evidences/{evidenceId}` com `{ "reason": "..." }` (lógica; exige `Disciplinary.Cases.RemoveEvidence`).
- Processos Concluídos ou Cancelados não aceitam evidência, arquivo nem remoção.
- Antivírus: ainda não há (`IMalwareScanner` no-op é o ponto de extensão).

#### E-mail
Configuração em `Email` (`Enabled`, `From`, `FromName`, `SendDeadlineAlerts`, `Smtp:Host/Port/Username/Password/UseStartTls`)
e `App:PublicBaseUrl` (base dos links). A senha SMTP só via user-secrets ou variável de ambiente (`Email__Smtp__Password`).
Com `Email:Enabled=false` nada é enviado (aviso no startup). Os e-mails passam por uma outbox (tabela `EmailOutbox`),
enviada por um worker com até 5 tentativas; nenhuma requisição espera o SMTP.

Em desenvolvimento a API usa o Mailpit. Para ver os e-mails:
- local: `http://localhost:8025`;
- **Codespace**: aba *Ports* → porta **8025** → abrir no navegador. Para os links dos e-mails apontarem para o front do
  Codespace, defina a URL pública da porta 3000:
  `dotnet user-secrets --project SIGH/backend/src/SIGH.Api set "App:PublicBaseUrl" "https://<codespace>-3000.app.github.dev"`.

#### Senha inicial e "esqueci a senha"
- Ao criar um usuário (`POST /api/v1/users`), a senha inicial é **opcional e desaconselhada**: a pessoa recebe por e-mail
  um link de uso único (válido por `Auth:AccountSetupTokenHours`, 72 h) para **definir a própria senha**. O e-mail nunca
  contém senha. Se o administrador informar uma senha inicial, ela precisa ser passada por outro canal e a troca continua
  obrigatória no primeiro login.
- `POST /api/v1/auth/forgot-password` sempre responde a mesma mensagem (sem revelar se o e-mail existe), com rate limit
  próprio. O link (`/reset-password?token=...`) é de uso único, guardado como hash e vale `Auth:PasswordResetTokenMinutes`
  (30 min). Depois do reset, todas as sessões e refresh tokens do usuário são revogados.

#### Várias abas
O refresh é serializado entre abas (Web Locks + BroadcastChannel no front) e o backend tolera, por
`Auth:RefreshTokenReuseGraceSeconds` (30 s), um refresh token recém-rotacionado apresentado de novo, sem disparar a
detecção de reuso.

#### Auditoria
Toda alteração de processos (e subentidades), evidências, funcionários, usuários, perfis, permissões e tipos de infração
gera um registro em `AuditLogs` (campos alterados com valor antigo/novo, usuário, data UTC, IP, correlation id). Eventos de
segurança (login, falha, bloqueio, troca/redefinição de senha, logout, reuso de token, mudança de perfis/permissões,
download de evidência) são gravados explicitamente. Senhas, hashes de token, segredos, caminho do storage, CPF e conteúdo
de arquivo nunca são gravados (`AuditPolicy`). A trilha só recebe inserções. Consulta: `GET /api/v1/audit-logs`
(permissão `Audit.View`) e `GET /api/v1/disciplinary-cases/{id}/audit-trail`.

#### Testes
```bash
cd SIGH/backend
dotnet test tests/SIGH.UnitTests
dotnet test tests/SIGH.IntegrationTests   # requer Docker: sobe um SQL Server via Testcontainers
```

### 4. Executando o Frontend (React + Vite)
O frontend é um projeto independente em `SIGH/frontend` (o antigo scaffold do AI Studio na raiz foi removido).
```bash
cd SIGH/frontend
npm ci
npm run dev        # http://localhost:3000, com proxy de /api para a API em http://localhost:5000
```
Outros comandos: `npm run typecheck`, `npm test` (Vitest + Testing Library + msw), `npm run build`,
`npm run check:bundle` (falha se o chunk de entrada passar de 500 kB) e `npm run e2e` (Playwright, local; veja
`SIGH/frontend/e2e/README.md`).

O access token fica só em memória; ao recarregar a página a sessão é restaurada com `POST /auth/refresh` (cookie)
e `GET /auth/me`. As permissões do front (`src/auth/permissions.ts`) espelham `PermissionCodes.All` via
`src/auth/permission-codes.json`; um teste no backend e outro no front falham se as listas divergirem.

---

## Arquitetura e Boas Práticas

- **Clean Architecture & DDD Ready**: Separação rigorosa entre Domínio, Aplicação, Infraestrutura e Persistência.
- **SOLID & Clean Code**: Injeção de dependência modularizada, nomenclaturas claras em inglês e interfaces em português.
- **Resiliência e Observabilidade**: Logs estruturados via Serilog e monitoramento de saúde via Healthchecks.
