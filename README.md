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
- **Node.js** (v20 ou superior)
- **.NET 9 SDK**
- **Docker & Docker Compose**

### 2. Subindo o Banco de Dados (SQL Server)
```bash
cp .env.example .env   # troque MSSQL_SA_PASSWORD (o .env não vai para o git)
docker compose up -d   # aguarde o status "healthy" em: docker compose ps
```
O container roda como o usuário `mssql` (não root), com o volume nomeado montado em `/var/opt/mssql`.

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
