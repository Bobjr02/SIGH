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
| `encarregado@demo.local` | RH – Aprovador | "Aguardando a sua aprovação" (2 medidas) e o contador em **Aprovações** no menu |
| `juridico@demo.local` | Jurídico | O **Painel** (indicadores de todas as gerências); **Auditoria** no menu |
| `gestor.industrial@demo.local` | Gestor da USGA131 | O **Painel**, as medidas e os funcionários só da Industrial (USGA131) |
| `gestor.agricola@demo.local` | Gestor da USGA137 e USGA141 | O **Painel**, as medidas e os funcionários só das gerências agrícolas |
| `preposto@demo.local` | RH – Registro + RH – Aprovador | A fila de aprovação e as próprias medidas (não aprova as que ele mesmo registrar) |
| `admin@demo.local` | Administrador | Pede a troca de senha no primeiro acesso; o **Painel** e, em **Administração**, Usuários, Gerências, Importar funcionários, Importar histórico e Motivos. Não registra nem aprova medidas |

Os dados de demonstração têm 40 funcionários fictícios, o **histórico disciplinar fictício** importado da
planilha de penalidades (14 registros, com o selo "Histórico importado") e 8 medidas do SIGH em todos os estados
(aprovadas, uma suspensão, pendentes, uma rejeitada aguardando correção, uma cancelada e uma com carta em PDF
anexada). No funcionário 50090007, a aba **Apoio à decisão** mostra o histórico separado das medidas do SIGH.
O tema escuro é o padrão; o botão de lua (no topo) troca para o claro. As capturas de todas as telas, nos dois
temas, estão em [`SIGH/docs/telas`](SIGH/docs/telas).

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

**"Lembrar-me"** (campo opcional `rememberMe` no `POST /auth/login`):
- desmarcado (`false`, o padrão da tela): o cookie `sigh_rt` é gravado **sem** `Expires` — cookie de sessão, que o
  navegador apaga ao ser fechado. O refresh token continua expirando no servidor no prazo normal;
- marcado (`true`) ou omitido (clientes antigos): comportamento anterior, o cookie expira junto com o refresh token.

A escolha fica em `RefreshTokens.IsPersistent` e passa para o token novo a cada `refresh` (inclusive na janela de
tolerância entre abas) e na `change-password` (que herda a escolha do cookie da sessão atual). Tokens emitidos antes
desta mudança continuam persistentes (a migration `RefreshTokenRememberMe` cria a coluna com `true`).
Testes: `RememberMeTests` (integração, os dois casos e o campo omitido).

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
Na tela, a busca é por número da medida (`reference=MED-…`, traz a medida e as subentidades) ou matrícula
(`reference=50090006`), e o usuário é escolhido pelo nome (`GET /api/v1/audit-logs/users?search=`). Campos e valores
aparecem em português (datas em dd/MM/aaaa HH:mm de Maceió, Sim/Não, "(oculto)" para `[redacted]`, "—" para vazio);
os IDs técnicos ficam em "Detalhes técnicos".

#### Português na tela
Todo valor de enum que chega ao front está em `SIGH/frontend/src/i18n/enum-values.json`, gerado a partir do backend
por `EnumValuesFrontendSyncTests` (para regenerar: `UPDATE_ENUM_VALUES=1 dotnet test tests/SIGH.UnitTests --filter
EnumValuesFrontendSyncTests`). Os rótulos ficam em `src/i18n/labels.ts`; `enumLabels.test.ts` falha se algum valor não
tiver rótulo. Números, percentuais e datas usam `src/utils/formatters.ts` (1.248; 12,5%; dd/MM/aaaa).
As mensagens padrão do ASP.NET (ProblemDetails, binding, JSON) e do FluentValidation saem em português.

#### Relatórios e painel
- `GET /api/v1/disciplinary-records/stats`: distribuição por situação e por tipo (mesmos filtros da lista);
- `GET /api/v1/disciplinary-records/export`: CSV (UTF-8 com BOM, `;`) com cabeçalhos em português;
- `GET /api/v1/disciplinary-records/overview?period=Last30Days|Last90Days|Last12Months|CurrentYear`: indicadores do
  período comparados ao período anterior de mesmo tamanho, evolução mensal e atividades recentes.

Todos respeitam o escopo por gerência. Os endpoints antigos de `/api/v1/disciplinary-reports/*` continuam existindo
(e testados no escopo), mas a tela usa só os novos.

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
`npm run check:bundle` (falha se o chunk de entrada passar de 500 kB) e `npm run e2e` (Playwright; veja
`SIGH/frontend/e2e/README.md`).

**E2E contra a stack em contêineres** (o mesmo que o CI roda no workflow `SIGH E2E` a cada PR para a `main`):
```bash
scripts/e2e-stack.sh                       # sobe compose.app.yml + compose.e2e.yml, roda a suíte e desmonta tudo
scripts/e2e-stack.sh e2e/records.spec.ts   # só um spec (as dependências entre projetos do Playwright rodam antes)
```
Projeto `sigh-e2e`, portas 18080/18025, segredos aleatórios só na memória do script e nenhum volume: não interfere
na stack de demonstração (`sigh-local`) nem no `.env.local`.

`SIGH_API_TARGET=http://localhost:8080 npm run dev` usa a stack em contêineres como API (o nginx repassa `/api`).

**Telas com os dados de demonstração** (`SIGH/frontend/e2e-demo`): `npx playwright test -c playwright.demo.config.ts`
sobe a API num banco descartável `SIGH_E2E_<data>` com o seed de demonstração e o apaga no final. Roda:
- `no-english.spec`: percorre todas as páginas do menu, os detalhes e as abas com cada perfil (administrador e os 6
  usuários de demonstração) e falha se achar um termo em inglês (os nomes de todos os enums e palavras como Pending,
  Loading, Save, Not found…; palavra inteira, sem diferenciar maiúsculas);
- `accessibility.spec`: axe-core (WCAG 2.1 AA) nas telas principais, nos temas escuro e claro;
- `screenshots.spec`: com `SIGH_SCREENSHOTS=1`, grava as capturas em `SIGH/docs/telas/`.

**Tema**: os tokens (cores, tipografia, raios, sombras) estão em `src/styles/tokens.ts` e viram variáveis CSS
(`--sigh-*`) usadas pelo tema do MUI e pelo Tailwind (`src/index.css`). Escuro é o padrão; o claro fica salvo no
navegador (`localStorage["sigh.theme"]`). Fonte Inter empacotada (`@fontsource-variable/inter`), sem CDN.

**Logo oficial**: `SIGH/docs/design/logo/` (ver `LOGO.md`). Os SVGs ficam em `src/assets/logo/` sem nenhuma alteração
(nem svgo) e são usados por `<SighLogo variant="horizontal|completo|simbolo" />` como `<img alt="SIGH">`: todos os
arquivos usam os mesmos ids internos nos degradês, que colidiriam se embutidos na mesma página, e assim não pesam no JS.
O horizontal e o completo trocam entre as versões para fundo escuro e claro conforme o tema. A cor de destaque do tema
(`primary`) é o azul do escudo, `#0062FE`. Ícones: `public/favicon.ico`, `sigh-32.png`, `sigh-180.png` e `sigh-512.png`
(no `manifest.webmanifest`).

O access token fica só em memória; ao recarregar a página a sessão é restaurada com `POST /auth/refresh` (cookie)
e `GET /auth/me`. As permissões do front (`src/auth/permissions.ts`) espelham `PermissionCodes.All` via
`src/auth/permission-codes.json`; um teste no backend e outro no front falham se as listas divergirem.

### Pouco espaço no Codespace
O Codespace tem 32 GB e as imagens do SQL Server e do SDK do .NET, o cache de build do Docker e os caches de pacotes
enchem o disco rápido. Para ver e liberar espaço:
```bash
df -h /workspaces
scripts/limpar-espaco.sh
```
O script mostra o espaço livre antes e depois e apaga **só o que se recria sozinho**: cache de build do Docker,
imagens órfãs, containers parados, bancos `SIGH_E2E_*` que sobraram, caches do npm, do NuGet e do Playwright, e as
pastas `bin/`/`obj/`. **Nunca** apaga volumes Docker (o banco de dev e o da demonstração continuam lá), o `.env`/
`.env.local` nem arquivos versionados. Pode rodar quantas vezes quiser.

- O `.devcontainer` já roda `scripts/limpar-espaco.sh --silencioso` a cada início do Codespace (`postStartCommand`).
- `scripts/local-up` apaga o cache de build depois de compilar as imagens (`SIGH_KEEP_BUILD_CACHE=1` mantém).
- `scripts/e2e-stack.sh` remove no final os containers e as imagens `sigh-*:e2e` que criou.
- Depois da limpeza, se for usar: `cd SIGH/backend && dotnet tool restore` (dotnet-ef) e
  `cd SIGH/frontend && npx playwright install chromium` (e2e).
- Ainda sem espaço? `docker system df -v` mostra o que ocupa mais. Volumes que você não usa mais só saem com
  `docker volume rm <nome>`, de propósito, um por um.

---

## Arquitetura e Boas Práticas

- **Clean Architecture & DDD Ready**: Separação rigorosa entre Domínio, Aplicação, Infraestrutura e Persistência.
- **SOLID & Clean Code**: Injeção de dependência modularizada, nomenclaturas claras em inglês e interfaces em português.
- **Resiliência e Observabilidade**: Logs estruturados via Serilog e monitoramento de saúde via Healthchecks.
