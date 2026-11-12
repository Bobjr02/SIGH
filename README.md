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
exceto `POST /api/v1/auth/change-password`, `POST /api/v1/auth/logout` e `POST /api/v1/auth/refresh`.

Depois de `change-password`, todos os refresh tokens anteriores do usuário são revogados e a resposta traz um
**novo par de tokens** (`accessToken`, `refreshToken`, `expiresInMinutes`) já sem a claim; o cliente deve trocar os
tokens que tinha pelos novos. O access token antigo continua restrito até expirar.

#### Testes
```bash
cd SIGH/backend
dotnet test tests/SIGH.UnitTests
dotnet test tests/SIGH.IntegrationTests   # requer Docker: sobe um SQL Server via Testcontainers
```

### 4. Executando o Frontend (React + Vite)
```bash
npm install
npm run dev
```
A aplicação web estará acessível em: `http://localhost:3000`.

---

## Arquitetura e Boas Práticas

- **Clean Architecture & DDD Ready**: Separação rigorosa entre Domínio, Aplicação, Infraestrutura e Persistência.
- **SOLID & Clean Code**: Injeção de dependência modularizada, nomenclaturas claras em inglês e interfaces em português.
- **Resiliência e Observabilidade**: Logs estruturados via Serilog e monitoramento de saúde via Healthchecks.
