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

## Rodar na sua máquina (Windows)

Este passo a passo é para quem quer **ver o SIGH funcionando** no próprio computador, sem ser desenvolvedor.
Tudo roda em containers, com a mesma estrutura de produção (um servidor web que entrega as telas e encaminha
`/api` para a API), e com **dados fictícios de demonstração**: todos os nomes terminam com "TESTE" e uma
faixa laranja "AMBIENTE DE DEMONSTRAÇÃO" aparece no topo de todas as telas.

### 1. Instalar o Rancher Desktop
1. Baixe e instale o **Rancher Desktop** (gratuito e de código aberto): https://rancherdesktop.io
   — não usamos o Docker Desktop porque a licença dele é paga para empresas do porte da USGA.
2. Se o Windows pedir, ative a virtualização/WSL2: abra o **PowerShell como administrador**, rode
   `wsl --install` e reinicie o computador. (Se o computador não tiver a virtualização ligada, veja "Problemas comuns".)
3. Abra o Rancher Desktop. Na primeira tela, em **Container Engine**, escolha **dockerd (moby)**.
   O Kubernetes pode ficar desligado.
4. Em **Preferences → Virtual Machine**, deixe pelo menos **4 GB de memória** (o banco usa cerca de 2 GB).
5. Confira num PowerShell comum: `docker compose version` deve mostrar a versão.

### 2. Baixar o SIGH
1. Instale o **Git para Windows**: https://git-scm.com/download/win (pode aceitar as opções padrão).
2. No PowerShell:
   ```powershell
   cd $HOME\Documents
   git clone https://github.com/Bobjr02/SIGH.git
   cd SIGH
   ```

### 3. Subir o SIGH
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass   # só se o Windows bloquear o script; vale só para esta janela
.\scripts\local-up.ps1
```
A primeira vez baixa as imagens e compila (alguns minutos). O script:
- cria o arquivo `.env.local` com senhas aleatórias e fortes, **só desta máquina** (ele está no `.gitignore`
  pela regra `.env.*` e nunca vai para o git);
- sobe banco, e-mail, API e web, espera tudo ficar saudável e cria os dados de demonstração;
- no final mostra o endereço, o e-mail e a senha do administrador, os usuários de demonstração e a senha deles.

O SIGH local só responde **neste computador** (http://localhost:8080 e http://localhost:8025): as portas são
publicadas só em 127.0.0.1, então outras máquinas da rede não conseguem abrir a demonstração.

As próximas vezes reaproveitam o `.env.local` e sobem em menos de um minuto.

### 4. Entrar e conferir cada perfil
Abra **http://localhost:8080** (Chrome ou Edge). A senha dos usuários abaixo é a que o script mostrou.

| Usuário | Perfil | O que deve aparecer |
|---|---|---|
| `auxiliar@demo.local` | RH – Registro | "Minhas medidas", com a rejeitada destacada; botão **Registrar medida** |
| `encarregado@demo.local` | RH – Aprovador | "Aguardando a sua aprovação" (2 medidas) e o contador em **Aprovações pendentes** |
| `juridico@demo.local` | Jurídico | Todas as medidas, de todas as gerências; **Auditoria** no menu |
| `gestor.industrial@demo.local` | Gestor da USGA131 | Só as medidas e funcionários da Industrial (USGA131) |
| `gestor.agricola@demo.local` | Gestor da USGA137 e USGA141 | Só as medidas e funcionários das gerências agrícolas |
| `preposto@demo.local` | RH – Registro + RH – Aprovador | A fila de aprovação e as próprias medidas (não aprova as que ele mesmo registrar) |
| `admin@demo.local` | Administrador | Pede a troca de senha no primeiro acesso; **Usuários**, **Gerências**, importação de funcionários e auditoria. Não registra nem aprova medidas |

Os dados de demonstração têm 40 funcionários fictícios, o **histórico disciplinar fictício** importado da
planilha de penalidades (14 registros, com o selo "Histórico importado") e 8 medidas do SIGH em todos os estados
(aprovadas, uma suspensão, pendentes, uma rejeitada aguardando correção, uma cancelada e uma com carta em PDF
anexada). No funcionário 50090007, o quadro de apoio à decisão mostra o histórico separado das medidas do SIGH.

### 5. Ver os e-mails
Nada sai para a internet: todos os e-mails (conta criada, medida registrada, aprovada, rejeitada...) ficam
no **Mailpit**: http://localhost:8025

### 6. Parar, reiniciar e zerar
```powershell
.\scripts\local-down.ps1    # para; os dados ficam guardados
.\scripts\local-up.ps1      # sobe de novo, com os mesmos dados
.\scripts\local-reset.ps1   # APAGA os dados do SIGH local (pede para digitar APAGAR); o próximo local-up recria tudo
```
O `local-reset` apaga só os volumes deste SIGH local (projeto compose `sigh-local`); não mexe em mais nada.

### 7. Problemas comuns
- **"A porta 8080 (ou 8025) já está em uso"**: outro programa usa a porta. Feche-o, ou troque `SIGH_WEB_PORT`
  (ou `SIGH_MAILPIT_PORT`) no arquivo `.env.local` e rode o `local-up` de novo.
- **Fica lento, o banco não sobe ou cai**: falta memória. O SQL Server precisa de cerca de **2 GB livres**;
  aumente a memória em Rancher Desktop → Preferences → Virtual Machine (4 GB ou mais) e feche programas pesados.
- **Rancher Desktop não inicia / erro de WSL ou virtualização**: a virtualização está desligada. Ative
  "Intel VT-x" ou "AMD-V/SVM" na BIOS/UEFI do computador (procure o modelo + "ativar virtualização") e rode
  `wsl --install` como administrador.
- **"docker" não é reconhecido**: o Rancher Desktop não está aberto, ou o motor escolhido não é o dockerd (moby).
- **O script não roda ("execução de scripts foi desabilitada")**: use a linha `Set-ExecutionPolicy -Scope Process ...` acima.
- **Ver o que deu errado**: `docker compose -p sigh-local -f compose.app.yml logs api`.

> Para desenvolvedores: a stack é `compose.app.yml` (separado do `docker-compose.yml` de desenvolvimento, que
> continua igual para `dotnet run` + `npm run dev`). No Linux/macOS use `scripts/local-up.sh`. O teste de ponta a
> ponta contra a stack em containers é `npx playwright test -c playwright.local.config.ts` (em `SIGH/frontend`).

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

#### Empresa única
Esta é uma instalação de **empresa única**: todos os funcionários pertencem à empresa padrão do seed,
"Usina Serra Grande S.A." (`Seed:DefaultCompanyName`; o nome antigo "Empresa Padrão" é renomeado no próximo start).
As 9 gerências (Chave Organização do SAP) vêm de `Seed:ManagementUnits` no `appsettings.json`; o seed só cria as que
faltam e nunca sobrescreve nomes, que continuam editáveis no banco.

#### Importação de funcionários (exportação do SAP)
Fluxo em duas etapas (permissão `Employees.Import`):
1. `POST /api/v1/employees/import/preview` (multipart, campo `file`, .xlsx): *dry-run*. Nada é gravado em funcionários.
   Cria um **lote** (`EmployeeImportBatches`) com o SHA-256 do arquivo, a impressão digital do plano calculado e a validade
   (`Import:PreviewValidityMinutes`, 30 min), e devolve o resumo: novos, alterados (campo, valor antigo e novo; CPF mascarado),
   a desligar (ausentes do arquivo), reativados, sem mudança, erros e avisos.
2. `POST /api/v1/employees/import/{batchId}/confirm` (multipart: `file` + `confirmMassTermination`): o arquivo **não é guardado**,
   então o mesmo arquivo é reenviado. O servidor confere o hash, a validade e se o plano recalculado é idêntico ao pré-visualizado
   (se a base mudou no meio do caminho, 409) e aplica tudo numa transação. Se os desligamentos por ausência passarem de
   `Import:MaxTerminationPercent` (10%) dos ativos, exige `confirmMassTermination=true` (422 sem ele).
3. `GET /api/v1/employees/import/batches`: histórico (contagens e erros; sem CPF nem colunas sensíveis).

Regras: matrícula nova cria; existente com dados diferentes atualiza (a auditoria registra os campos); status de desligado no
arquivo desliga; matrícula ativa ausente do arquivo é desligada com a data da importação (**nunca apagada**); desligada que volta
ativa é reativada. Importar o mesmo arquivo de novo não altera nada. Linhas com erro são rejeitadas (identificadas por linha e
matrícula, nunca pelo CPF) e **não** contam como ausentes. CPF de outra matrícula desligada é aviso de possível readmissão.
CPF repetido no arquivo em linhas não desligadas: se uma delas é a matrícula que já tem esse CPF no banco, ela é mantida e as
outras são rejeitadas; se nenhuma é dona, todas são rejeitadas.

Leitura: só as colunas da lista fechada em `HrImportColumns` (Application/Employees/Import), conferida contra a tabela
"Colunas que o SIGH importa" do `SIGH/docs/referencias/LEIAME.md` e contra os arquivos fictícios do pacote: `Matrícula`,
`Nome`, `CPF`, `Status`, `Grupo_Emp`, `Cargo`, `Descr.Cargo`, `Uni.Org`, `Descr.Unid.Org.`, `Data admissão`, `Data demissão`,
`Descrição Turma` e `Chave Organização`. As demais (inclusive `Turma`, que é só o código, e as duas `Estado`) nunca são lidas,
gravadas, logadas nem citadas em mensagens. Cabeçalhos são comparados sem acento, maiúsculas, espaços ou pontuação.
Coluna obrigatória ausente bloqueia o arquivo com o nome dela; coluna opcional ausente (`Grupo_Emp`, `Descrição Turma`,
`Data demissão`) gera um aviso no topo da pré-visualização ("o campo X ficará vazio para todos").

Os arquivos fictícios do pacote (`SIGH/docs/referencias/importacao-funcionarios`) também estão em
`SIGH/backend/tests/SIGH.IntegrationTests/Fixtures/Referencias`, onde o teste `LeiameReferenceImportTests` confere a tabela
de resultados esperados do LEIAME linha a linha.

#### Funcionários e apoio à decisão
- `/employees`: lista com busca por nome ou matrícula e filtros (situação, grupo, gerência, setor); CPF sempre mascarado
  (`***.982.247-**`). `/employees/:id`: dados com CPF completo, histórico disciplinar e o quadro de apoio à decisão.
- `GET /api/v1/employees/{id}/decision-support` (`Employees.View` + `Disciplinary.Reports.ViewEmployeeHistory`): medidas
  aplicadas por tipo e por motivo, advertências, suspensões e dias de suspensão (dias corridos, contando início e fim), última
  medida e medidas nos últimos 6 e 12 meses. Só informativo. O cálculo soma todas as `IEmployeeMeasureSource` (hoje, as
  medidas do SIGH; o histórico importado entra como outra fonte).

#### Medidas disciplinares (fluxo do RH)
O RH não usa o processo em etapas. O fluxo real é: o auxiliar de escritório **registra** a medida já decidida e
o encarregado do RH **aprova** ou **rejeita**. A API (`/api/v1/disciplinary-records`) é uma fachada sobre o domínio
existente: cada operação executa, numa única transação, as etapas reais do `DisciplinaryCase` (criar, abrir, vincular
o acusado, ocorrências, investigação, decisão submetida; na aprovação: aprovar, criar e aplicar a medida e concluir).
- Tipos aceitos: advertência verbal, advertência escrita, suspensão (início e fim; os dias contam os dois extremos)
  e demissão por justa causa. `TerminationRecommendation`, `Guidance` e `Other` não aparecem no registro.
- Quem registra (ou corrige) nunca aprova a mesma medida; rejeição exige motivo e devolve a medida ao registrante,
  que corrige (`PUT`) e reenvia, ou cancela com motivo.
- Funcionário desligado não recebe medida; suspensão sobreposta a outra pendente/aprovada é bloqueada; mesmo
  funcionário + tipo + data de ocorrência pendente/aprovada devolve `409 POSSIBLE_DUPLICATE` até ser confirmada.
- **Justa causa não altera o status do funcionário**: o SAP é a fonte da verdade e a próxima importação marca o
  desligamento.
- Anexos (carta assinada, escaneada ou fotografada) usam as evidências: com a medida pendente ou aprovada, nunca cancelada.
- Avisos no sistema e por e-mail (`Records:NotifyByEmail`, padrão `true`); pendências há mais de
  `Records:PendingApprovalAlertDays` (padrão 3) dias geram um aviso por dia para os aprovadores.
- As permissões do processo em etapas continuam no código, mas nenhum perfil semeado as recebe (exceto as de
  evidência); as telas dele saíram do menu e das rotas.

#### Perfis (fixos no seed) e validade
| Permissão | Administrador | RH – Registro | RH – Aprovador | Jurídico | Gestor |
|---|:-:|:-:|:-:|:-:|:-:|
| Users.View / Create / ChangeStatus / Unlock / ManageRoles | ✔ | | | | |
| ManagementUnits.Manage | ✔ | | | | |
| Disciplinary.InfractionTypes.* | ✔ | | | | |
| Employees.View | ✔ | ✔ | ✔ | ✔ | ✔ |
| Employees.Import | ✔ | | | | |
| Disciplinary.Records.View | ✔ | ✔ | ✔ | ✔ | ✔ (só as gerências dele) |
| Disciplinary.Records.ViewAll (escopo total) | ✔ | ✔ | ✔ | ✔ | |
| Disciplinary.Records.Register | | ✔ | | | |
| Disciplinary.Records.Approve | | | ✔ | | |
| Disciplinary.Cases.AddEvidence | | ✔ | ✔ | | |
| Disciplinary.Cases.RemoveEvidence | | | ✔ | | |
| Disciplinary.Reports.* (5) | ✔ | ✔ | ✔ | ✔ | ✔ |
| Notifications.View / Read | ✔ | ✔ | ✔ | | |
| Audit.View | ✔ | | | ✔ | |

As permissões de cada perfil ficam em `SystemRoles` e o seed as sincroniza a cada inicialização (o que sobra é
retirado logicamente); não há tela para editá-las. Perfis e gerências de um usuário têm validade opcional
(`ValidFrom` inclusivo, `ValidUntil` exclusivo; na tela, "até" é o último dia): só vínculos vigentes contam.
Ninguém altera os próprios perfis, gerências ou status; o último Administrador ativo não pode ser retirado nem
desativado; o perfil Gestor exige pelo menos uma gerência. Toda atribuição/retirada é auditada
(`Security.RoleAssigned/RoleRevoked/ManagementUnitAssigned/ManagementUnitRevoked`).

#### Escopo por gerência (no servidor)
`IAccessScope`: escopo total com `Disciplinary.Records.ViewAll`; senão, as gerências vigentes do usuário. Aplicado
em funcionários, medidas, processos, relatórios, dashboard, CSV, download de evidências, notificações e trilha de
auditoria. Fora do escopo: listas sem o registro e detalhe `404` (nunca `403`). Uma medida é visível se o funcionário
pertence **hoje** a uma gerência do escopo (o histórico acompanha a pessoa); a gerência da época fica em
`DisciplinaryCaseEmployee.ManagementUnitIdAtTime`. `RouteScopeCoverageTests` falha se surgir uma rota GET sem
classificação, e `ScopeEnforcementTests` testa cada rota com escopo (gestor da USGA132 × funcionário da USGA131).

#### Perda de acesso imediata
O access token carrega o `SecurityStamp` do usuário e o próximo início/fim de vigência de perfil ou gerência.
`SecurityStampMiddleware` responde `401` (`SESSION_REVOKED` ou `ACCESS_CHANGED`) se o stamp mudou (desativação,
bloqueio, troca/redefinição de senha, perfis, gerências), se o usuário está inativo ou se o instante passou. O cache
do stamp dura no máximo 30 s (`Auth:SecurityStampCacheSeconds`). A desativação também revoga os refresh tokens; o
refresh é negado para usuário inativo. O front renova o token e recarrega as permissões.

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
