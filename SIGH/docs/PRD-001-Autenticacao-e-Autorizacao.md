# PRD-001: Módulo de Autenticação e Autorização (SIGH)

**Documento de Requisitos de Produto (Product Requirements Document)**  
**Projeto:** SIGH — Sistema Inteligente de Gestão de Histórico Disciplinar  
**Módulo:** Autenticação, Autorização e Gestão de Usuários  
**Versão:** 1.0.0  
**Data:** 23 de Julho de 2026  
**Status:** Aprovado para Planejamento de Sprints  
**Autor:** Analista de Sistemas Sênior & Arquiteto de Software  

---

## Sumário Executivo / Índice

1. [Visão Geral](#1-visão-geral)
   - 1.1 Objetivo do Módulo
   - 1.2 Problema que Resolve
   - 1.3 Benefícios Corporativos
   - 1.4 Escopo do Módulo
   - 1.5 Fora do Escopo
2. [Requisitos Funcionais (RF)](#2-requisitos-funcionais)
3. [Requisitos Não Funcionais (RNF)](#3-requisitos-não-funcionais)
4. [Perfis de Acesso (Roles)](#4-perfis-de-acesso)
5. [Matriz de Permissões (RBAC)](#5-matriz-de-permissões)
6. [Fluxos de Processo (Workflows)](#6-fluxos-de-processo)
7. [Casos de Uso (UC)](#7-casos-de-uso)
8. [Regras de Negócio (RN)](#8-regras-de-negócio)
9. [Critérios de Aceite](#9-critérios-de-aceite)
10. [Casos de Teste (QA)](#10-casos-de-teste)
11. [Especificação de Wireframes (UI/UX)](#11-especificação-de-wireframes)
12. [Especificação de API REST (Endpoints)](#12-especificação-de-api-rest)
13. [Modelo de Banco de Dados](#13-modelo-de-banco-de-dados)
14. [Plano de Auditoria e Rastreabilidade](#14-plano-de-auditoria-e-rastreabilidade)
15. [Arquitetura de Segurança](#15-arquitetura-de-segurança)
16. [Análise e Mapeamento de Riscos](#16-análise-e-mapeamento-de-riscos)
17. [Dependências Técnicas e Operacionais](#17-dependências-técnicas-e-operacionais)
18. [Checklist para Desenvolvimento (Dev)](#18-checklist-para-desenvolvimento)
19. [Checklist para Garantia de Qualidade (QA)](#19-checklist-para-garantia-de-qualidade)
20. [Roadmap de Implementação por Sprints](#20-roadmap-de-implementação-por-sprints)

---

## 1. Visão Geral

### 1.1 Objetivo do Módulo
Fornecer um subsistema corporativo robusto, seguro e altamente auditável para autenticação de usuários, controle de sessões e autorização baseada em papéis (Role-Based Access Control - RBAC) para o **SIGH (Sistema Inteligente de Gestão de Histórico Disciplinar)**.

### 1.2 Problema que Resolve
A ausência de controle centralizado e padronizado de acesso a dados disciplinares sensíveis gera vulnerabilidades de conformidade (LGPD/ISO 27001), risco de vazamento de informações confidenciais de funcionários e falta de rastreabilidade sobre quem acessou ou modificou relatórios disciplinares.

### 1.3 Benefícios Corporativos
- **Conformidade Legal (LGPD):** Garantia de que somente pessoal autorizado e devidamente identificado acesse históricos de infrações e medidas disciplinares.
- **Rastreabilidade Total:** Auditoria imutável de todas as tentativas de acesso, alterações de permissão e trocas de senha.
- **Segurança Passiva e Ativa:** Proteção contra ataques de força bruta, sequestro de sessão (session hijacking) e vazamento de credenciais.
- **Experiência do Usuário (UX):** Fluxo intuitivo para recuperação de senha self-service e primeiro acesso guiado.

### 1.4 Escopo do Módulo
- Autenticação por credenciais (E-mail/CPF e Senha) com hash seguro (BCrypt / Argon2id).
- Gestão do ciclo de vida de tokens JWT (Access Token de curta duração e Refresh Token seguro com rotação).
- Controle de Primeiro Acesso com troca obrigatória de senha temporária.
- Auto-serviço de Recuperação e Alteração de Senha via token temporal descartável.
- Mecanismo de bloqueio temporário/definitivo por excesso de tentativas incorretas.
- Gestão administrativa de usuários (Cadastro, Inativação, Ativação, Associação de Perfil).
- Matriz RBAC para 5 perfis distintos (Administrador, RH, Gerente, Supervisor, Consulta).

### 1.5 Fora do Escopo
- Integração Single Sign-On (SSO) via SAML2/OAuth2/Active Directory (planejada para fase futura).
- Autenticação Multifator (MFA/2FA) via App Authenticator ou SMS (planejada para o Módulo de Segurança Avançada).
- Gestão de dados cadastrais completos de colaboradores (pertencente ao Módulo de Cadastro de Funcionários).

---

## 2. Requisitos Funcionais

| Código | Requisito Funcional | Descrição Detalhada |
| :--- | :--- | :--- |
| **RF001** | **Login de Usuários** | O sistema deve autenticar usuários utilizando e-mail corporativo ou CPF e senha válida, retornando um par de tokens JWT (Access + Refresh Token) mediante sucesso. |
| **RF002** | **Logout de Usuários** | O sistema deve invalidar a sessão ativa no cliente e revogar/cancelar a validade do Refresh Token no banco de dados do servidor. |
| **RF003** | **Primeiro Acesso** | O sistema deve identificar no login se o usuário possui a flag `IsFirstAccess = true` e forçar o redirecionamento imediato para a tela de criação de nova senha, bloqueando navegação até a conclusão. |
| **RF004** | **Alteração de Senha** | Usuários autenticados devem conseguir alterar sua própria senha mediante validação prévia da senha atual e respeito à política de complexidade. |
| **RF005** | **Recuperação de Senha** | O sistema deve oferecer fluxo "Esqueci minha senha" onde um e-mail com token temporário e assinado (validade de 15 min) é enviado para o usuário redefinir sua credencial. |
| **RF006** | **Refresh Token** | O sistema deve renovar o Access Token (validade 15 min) de forma transparente através de um Refresh Token (validade 8 horas) armazenado em cookie `HttpOnly`, `Secure` e `SameSite=Strict` com rotação automática (token reuse detection). |
| **RF007** | **Expiração de Sessão** | O sistema deve encerrar proativamente a sessão do usuário no frontend após 15 minutos de inatividade ininterrupta, exigindo nova reautenticação. |
| **RF008** | **Bloqueio por Tentativas Inválidas** | O sistema deve bloquear temporariamente a conta de um usuário após 5 (cinco) tentativas consecutivas de login incorretas por um período de 15 minutos. |
| **RF009** | **Desbloqueio de Usuários** | Administradores devem possuir funcionalidade para desbloquear manualmente contas bloqueadas por tentativas incorretas antes do tempo de expiração do bloqueio. |
| **RF010** | **Cadastro de Usuários** | Somente usuários com perfil `Administrador` podem cadastrar novos usuários, gerando uma senha temporária aleatória e marcando a conta com `IsFirstAccess = true`. |
| **RF011** | **Alteração de Perfis** | O perfil de um usuário existente só pode ser alterado por um `Administrador`. A mudança passa a valer na emissão do próximo Access Token. |
| **RF012** | **Ativação / Inativação** | Administradores podem inativar ou reativar contas de usuários. Usuários inativos não podem autenticar e têm seus tokens revogados imediatamente. |

---

## 3. Requisitos Não Funcionais

| Categoria | Código | Requisito Não Funcional |
| :--- | :--- | :--- |
| **Segurança** | RNF001 | As senhas devem ser armazenadas com hash forte utilizando BCrypt (fator de custo mínimo 12) ou Argon2id. Nunca armazenar texto puro. |
| **Segurança** | RNF002 | Todos os endpoints do módulo devem trafegar obrigatoriamente sobre HTTPS (TLS 1.3/1.2). |
| **Segurança** | RNF003 | Mensagens de erro em falhas de login devem ser genéricas ("E-mail ou senha inválidos") para evitar enumeração de usuários. |
| **Performance** | RNF004 | O tempo de resposta do endpoint de validação de autenticação (`/api/v1/auth/login`) não deve exceder 300ms sob carga nominal de 100 requisições simultâneas. |
| **Escalabilidade** | RNF005 | O mecanismo de validação de JWT deve ser *stateless* na API, permitindo escalabilidade horizontal das instâncias sem compartilhamento de memória de sessão. |
| **Auditoria** | RNF006 | Todas as ações do módulo (logins, falhas, bloqueios, trocas de senha, alterações de perfil) devem gerar registros estruturados imutáveis na tabela de AuditLogs. |
| **Disponibilidade**| RNF007 | O serviço de autenticação deve apresentar taxa de disponibilidade mínima de 99,9% (SLA corporativo). |
| **Responsividade**| RNF008 | As interfaces de Login, Recuperação e Primeiro Acesso devem adaptar-se perfeitamente a dispositivos móveis e desktops (breakpoints MUI v6: 320px a 1920px+). |
| **Compatibilidade**| RNF009 | Suporte garantido aos navegadores modernos: Google Chrome (últimas 3 versões), Mozilla Firefox, Microsoft Edge e Apple Safari. |
| **Logs** | RNF010 | Todos os logs devem conter `CorrelationId` e `RequestId` integrados via Serilog sem expor dados sensíveis (senhas, hashes ou tokens truncados). |
| **Backup** | RNF011 | RPO de 15 minutos e RTO de 1 hora para o banco de dados de usuários e credenciais em ambiente SQL Server. |

---

## 4. Perfis de Acesso

1. **Administrador (`Admin`):**
   - **Responsabilidade:** Gestão total do sistema, configurações de segurança, cadastro de usuários, concessão e revogação de perfis, auditoria global e desbloqueio manual de contas.
2. **RH (`HumanResources`):**
   - **Responsabilidade:** Gestão operacional das medidas disciplinares, cadastro de advertências/suspensões, consulta a históricos completos de colaboradores e emissão de relatórios oficiais.
3. **Gerente (`Manager`):**
   - **Responsabilidade:** Aprovação/revisão de ocorrências registradas na sua diretoria/gerência e consulta detalhada ao histórico disciplinar da sua linha hierárquica.
4. **Supervisor (`Supervisor`):**
   - **Responsabilidade:** Registro inicial de incidentes disciplinares e consulta ao histórico de subordinados diretos.
5. **Consulta (`AuditorRead`):**
   - **Responsabilidade:** Perfil exclusivo para auditoria interna/externa ou órgãos reguladores. Acesso somente leitura a relatórios sem permissão de alteração ou cadastro.

---

## 5. Matriz de Permissões (RBAC)

| Funcionalidade / Recurso | Administrador | RH | Gerente | Supervisor | Consulta |
| :--- | :---: | :---: | :---: | :---: | :---: |
| Autenticar-se / Logout | **Sim** | **Sim** | **Sim** | **Sim** | **Sim** |
| Alterar Própria Senha | **Sim** | **Sim** | **Sim** | **Sim** | **Sim** |
| Solicitar Recuperação de Senha | **Sim** | **Sim** | **Sim** | **Sim** | **Sim** |
| Cadastrar Usuários (RF010) | **Sim** | Não | Não | Não | Não |
| Alterar Perfil de Usuários (RF011) | **Sim** | Não | Não | Não | Não |
| Ativar / Inativar Usuários (RF012) | **Sim** | Não | Não | Não | Não |
| Desbloquear Usuários (RF009) | **Sim** | Não | Não | Não | Não |
| Visualizar Logs de Auditoria de Acesso | **Sim** | **Sim** | Não | Não | **Sim** |
| Listar Usuários do Sistema | **Sim** | **Sim** | Não | Não | Não |

---

## 6. Fluxos de Processo

```
+-----------------------------------------------------------------------------------+
|                                  FLUXO DE LOGIN                                   |
+-----------------------------------------------------------------------------------+
[Usuário] ---> Informa E-mail/Senha ---> [API POST /auth/login]
                                                 |
                                     Credenciais Válidas?
                                      /                \
                                    NÃO                 SIM
                                    /                    \
                     Incremente Tentativas Falhas      Conta Ativa?
                                  |                     /        \
                        Tentativas >= 5?              NÃO        SIM
                         /            \                |          |
                       SIM            NÃO          Retorna     Verifica IsFirstAccess
                        |              |           403 Forbidden  /           \
                 Bloqueie Conta   Retorna 401                  SIM            NÃO
                 por 15 min       Unauthorized                  |              |
                        |              |                   Retorna Token    Gera Tokens
                     Retorna        Retorna                + Redireciona    Access/Refresh
                    423 Locked    401 Unauthorized         p/ Primeiro      Retorna 200 OK
                                                           Acesso
```

### Detalhamento dos Fluxos Principais

1. **Fluxo de Login (RF001):**
   - Usuário acessa `/login` no React.
   - Envia formulário. API intercepta, valida hash de senha, verifica bloqueio e flag `IsFirstAccess`.
   - Se válido, grava Refresh Token em Cookie `HttpOnly` e retorna Access Token no body JSON.

2. **Fluxo Primeiro Acesso (RF003):**
   - Se `IsFirstAccess == true`, o login retorna um token com escopo restrito (`Scope: FirstAccess`).
   - Frontend intercepta e força exibição da tela `/primeiro-acesso`.
   - Usuário define nova senha. API altera para `IsFirstAccess = false` e emite token de acesso pleno.

3. **Fluxo Recuperação de Senha (RF005):**
   - Usuário digita e-mail em `/recuperar-senha`.
   - Backend verifica e-mail. Se existir e estiver ativo, gera token aleatório assinado com hash e expiração de 15 min, disparando e-mail.
   - Link no e-mail direciona para `/redefinir-senha?token=XYZ`. Usuário define nova senha.

4. **Fluxo de Renovação com Refresh Token (RF006):**
   - Quando o Access Token expira (15 min), o Axios Interceptor do frontend recebe 401 e chama `POST /auth/refresh`.
   - API lê o cookie `HttpOnly` contendo o Refresh Token, valida no banco, revoga o token antigo (rotação) e emite novo par.

5. **Fluxo de Sessão Expirada por Inatividade (RF007):**
   - Listener de eventos de input (`mousemove`, `keydown`, `click`) no React reinicia um timer de 15 minutos.
   - Se estourar 15 min sem atividade, limpa o estado da aplicação, chama o endpoint de logout para revogar o refresh token e redireciona para `/login?reason=expired`.

---

## 7. Casos de Uso

### UC01 - Realizar Login no Sistema
- **Objetivo:** Autenticar o usuário na plataforma SIGH.
- **Atores:** Qualquer usuário cadastrado.
- **Pré-condições:** Conta cadastrada e ativa.
- **Fluxo Principal:**
  1. Usuário informa e-mail e senha na tela de login.
  2. Sistema valida o formato dos dados de entrada.
  3. Sistema consulta a conta pelo e-mail informado.
  4. Sistema valida a senha com o hash armazenado.
  5. Sistema verifica se a conta está ativa e não bloqueada.
  6. Sistema gera o Access Token JWT (validade 15 min) e o Refresh Token (validade 8 hrs).
  7. Sistema grava o Refresh Token no Cookie e retorna os dados do usuário + Access Token.
  8. Sistema redireciona o usuário para o Dashboard correspondente ao seu perfil.
- **Fluxo Alternativo A (Senha Incorreta):**
  4a. Senha não confere com o hash.
  4b. Sistema incrementa o contador de tentativas falhas.
  4c. Retorna mensagem genérica: "E-mail ou senha inválidos." (HTTP 401).
- **Fluxo Alternativo B (Excesso de Tentativas - Bloqueio):**
  4b1. Contador de tentativas atinge 5.
  4b2. Sistema altera o status da conta para Bloqueada até `DateTimeOffset.UtcNow.AddMinutes(15)`.
  4b3. Retorna mensagem: "Conta temporariamente bloqueada por excesso de tentativas." (HTTP 423).
- **Fluxo Alternativo C (Primeiro Acesso):**
  5a. Sistema detecta `IsFirstAccess == true`.
  5b. Retorna flag de redirecionamento obrigatório para `/primeiro-acesso`.
- **Pós-condições:** Sessão iniciada com sucesso e log de auditoria `UserLoggedIn` registrado.

### UC02 - Cadastrar Novo Usuário
- **Objetivo:** Permitir que o Administrador crie novas contas no SIGH.
- **Atores:** Administrador.
- **Pré-condições:** Administrador autenticado.
- **Fluxo Principal:**
  1. Administrador acessa a tela de Gestão de Usuários e clica em "Novo Usuário".
  2. Informa Nome Completo, E-mail, CPF e Seleciona o Perfil (Role).
  3. Sistema valida se o E-mail e o CPF já existem na base.
  4. Sistema gera uma senha temporária forte (ex: `Sigh@2026!Tmp`).
  5. Sistema salva o usuário com `IsActive = true`, `IsFirstAccess = true` e grava o hash da senha temporária.
  6. Sistema envia e-mail com as credenciais iniciais para o novo usuário.
  7. Retorna confirmação "Usuário cadastrado com sucesso" (HTTP 201).
- **Pós-condições:** Conta criada no estado pendente de primeiro acesso e log `UserCreatedByAdmin` gerado.

---

## 8. Regras de Negócio

| Código | Regra de Negócio |
| :--- | :--- |
| **RN001** | **Complexidade de Senha:** A senha deve ter no mínimo 8 e no máximo 32 caracteres, contendo obrigatoriamente: 1 letra maiúscula, 1 letra minúscula, 1 número e 1 caractere especial (`@$!%*?&`). |
| **RN002** | **Bloqueio por Força Bruta:** 5 tentativas incorretas consecutivas no intervalo de 30 minutos acarretam o bloqueio da conta por 15 minutos. |
| **RN003** | **Unicidade de Identificador:** E-mail e CPF são únicos na base de dados de usuários. Tentativas de cadastro duplicado devem ser rejeitadas. |
| **RN004** | **Invalidação de Refresh Token na Troca de Senha:** Qualquer alteração ou redefinição de senha revoga imediatamente todos os Refresh Tokens ativos daquela conta em todos os dispositivos. |
| **RN005** | **Tempo de Vida dos Tokens:** Access Token expira em 15 minutos. Refresh Token expira em 8 horas. Token de Recuperação de Senha expira em 15 minutos. |
| **RN006** | **NÃO Reutilização de Senha:** Ao alterar a senha, a nova credencial não pode ser idêntica às últimas 3 (três) senhas utilizadas pelo usuário. |
| **RN007** | **Imutabilidade do Perfil Administrador Inicial:** O administrador padrão gerado no *bootstrap* do sistema não pode ser inativado ou ter seu perfil alterado. |

---

## 9. Critérios de Aceite

1. **Garantia de Não-Enumeração:**
   - **Dado** que um usuário tente logar com um e-mail não cadastrado,
   - **Quando** submeter o formulário de login,
   - **Então** o sistema deve responder com HTTP 401 e a exata mesma mensagem de erro apresentada quando a senha está errada: "E-mail ou senha inválidos."

2. **Obrigatoriedade de Primeiro Acesso:**
   - **Dado** um novo usuário cadastrado pelo Administrador (`IsFirstAccess = true`),
   - **Quando** realizar o primeiro login com a senha temporária,
   - **Então** o sistema deve impedir o acesso a qualquer rota do Dashboard e forçar a redefinição de senha antes de liberar o sistema.

3. **Segurança de Cookie de Refresh Token:**
   - **Dado** que a API emita um Refresh Token no login,
   - **Quando** inspecionar os cabeçalhos de resposta HTTP (`Set-Cookie`),
   - **Então** o cookie deve obrigatoriamente possuir as diretivas `HttpOnly`, `Secure`, `SameSite=Strict` e `Path=/api/v1/auth`.

---

## 10. Casos de Teste

### 10.1 Testes Funcionais
| ID Teste | Cenário | Passos | Resultado Esperado |
| :--- | :--- | :--- | :--- |
| **CT-FUNC-01** | Login com sucesso | Inserir e-mail e senha corretos -> Clicar Entrar | HTTP 200, JWT retornado, redirecionado para `/dashboard`. |
| **CT-FUNC-02** | Primeiro acesso obriga troca | Logar com conta nova `IsFirstAccess=true` | Redirecionado para `/primeiro-acesso`, bloqueando navegação paralela. |
| **CT-FUNC-03** | Recuperação de senha | Inserir e-mail válido no esqueci a senha | E-mail recebido com link temporal assinado; redefinição efetuada. |

### 10.2 Testes Negativos
| ID Teste | Cenário | Passos | Resultado Esperado |
| :--- | :--- | :--- | :--- |
| **CT-NEG-01** | Senha com complexidade fraca | Tentar criar nova senha como "123456" | Erro de validação de formulário (Zod) e rejeição na API (HTTP 400). |
| **CT-NEG-02** | Tentativa de login em conta inativa | Tentar logar com usuário `IsActive=false` | HTTP 403 Forbidden com mensagem "Conta inativa. Contate o Administrador." |

### 10.3 Testes de Segurança
| ID Teste | Cenário | Passos | Resultado Esperado |
| :--- | :--- | :--- | :--- |
| **CT-SEC-01** | Bloqueio por força bruta | Errar a senha 5 vezes consecutivas | HTTP 423 Locked no 5º erro; conta bloqueada durante 15 minutos. |
| **CT-SEC-02** | Reutilização de Refresh Token revogado | Tentar utilizar Refresh Token antigo | API detecta roubo de token, revoga todos os tokens do usuário e exige novo login. |

---

## 11. Especificação de Wireframes

### Telas do Módulo (Descrição UI/UX):

1. **Tela 1: Login (`/login`)**
   - **Visual:** Layout centralizado, design limpo, card elevado em fundo neutro (`#f8fafc`), logotipo SIGH no topo acompanhado de um ícone corporativo de escudo.
   - **Componentes:**
     - Campo de texto para E-mail/CPF (MUI `TextField` com validação em tempo real).
     - Campo de texto para Senha (com botão de alternar visibilidade ícone olho).
     - Checkbox "Lembrar meu e-mail".
     - Link secundário "Esqueceu sua senha?".
     - Botão primário "Entrar no Sistema" (Full width, cor `#003366`, estado de carregamento com Spinner).

2. **Tela 2: Primeiro Acesso / Troca Obrigatória (`/primeiro-acesso`)**
   - **Visual:** Banner explicativo destacado ("Bem-vindo ao SIGH. Por motivos de segurança, crie sua nova senha pessoal antes de prosseguir.").
   - **Componentes:**
     - Campo "Senha Atual / Temporária".
     - Campo "Nova Senha" com indicador visual dinâmico de força da senha (fraca, média, forte).
     - Lista de requisitos de senha com marcação dinâmica (v/x): 8+ caracteres, maiúscula, número, caractere especial.
     - Campo "Confirmar Nova Senha".
     - Botão "Atualizar Senha e Acessar".

3. **Tela 3: Recuperação de Senha (`/recuperar-senha`)**
   - **Visual:** Tela simples contendo instrução: "Informe seu e-mail cadastrado para receber as instruções de redefinição."
   - **Componentes:** Campo E-mail, Botão "Enviar E-mail de Recuperação", Link "Voltar ao Login".

4. **Tela 4: Gestão Administrativa de Usuários (`/admin/usuarios`)**
   - **Visual:** Tabela corporativa MUI (`DataGrid`) responsiva com filtros superiores (Buscar por nome, e-mail, perfil, status).
   - **Componentes:**
     - Botão Superior "Novo Usuário" (abre Modal com formulário Zod/React Hook Form).
     - Colunas: Nome, E-mail, Perfil (Chip colorido), Status (Ativo/Inativo/Bloqueado), Último Acesso, Ações.
     - Ações por linha: Menu dropdown com "Editar Perfil", "Desbloquear Conta", "Inativar/Ativar", "Resetar Senha".

---

## 12. Especificação de API REST

### 12.1 Endpoints do Módulo

| Método | Endpoint | Descrição | Auth Requerida | Status Esperados |
| :--- | :--- | :--- | :---: | :--- |
| `POST` | `/api/v1/auth/login` | Autentica usuário e retorna JWT | Não | `200 OK`, `400`, `401`, `423` |
| `POST` | `/api/v1/auth/logout` | Revoga o Refresh Token ativo | Sim | `200 OK`, `401` |
| `POST` | `/api/v1/auth/refresh` | Renova Access Token via Refresh Token Cookie | Não (Cookie) | `200 OK`, `401` |
| `POST` | `/api/v1/auth/forgot-password` | Solicita e-mail de redefinição de senha | Não | `200 OK` (sempre genérico) |
| `POST` | `/api/v1/auth/reset-password` | Redefine senha com token temporal | Não | `200 OK`, `400` |
| `POST` | `/api/v1/auth/change-password` | Altera senha de usuário logado | Sim | `200 OK`, `400`, `401` |
| `POST` | `/api/v1/users` | Cadastra novo usuário | Sim (`Admin`) | `201 Created`, `400`, `403` |
| `PATCH`| `/api/v1/users/{id}/status` | Ativa ou inativa usuário | Sim (`Admin`) | `200 OK`, `404` |
| `POST` | `/api/v1/users/{id}/unlock` | Desbloqueia manualmente a conta | Sim (`Admin`) | `200 OK`, `404` |

### 12.2 Exemplos de Payloads

#### Request: `POST /api/v1/auth/login`
```json
{
  "email": "gerente.rh@empresa.com.br",
  "password": "SenhaComplexa@2026"
}
```

#### Response Válida: `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresInSeconds": 900,
  "tokenType": "Bearer",
  "user": {
    "id": "e3a81234-5678-4a1b-9c2d-3e4f5a6b7c8d",
    "name": "Maria Oliveira",
    "email": "gerente.rh@empresa.com.br",
    "role": "HumanResources",
    "isFirstAccess": false
  }
}
```

#### Response de Erro Trada: `423 Locked` (ProblemDetails)
```json
{
  "type": "https://sigh.empresa.com.br/errors/account-locked",
  "title": "Conta Temporariamente Bloqueada",
  "status": 423,
  "detail": "Sua conta foi bloqueada por excesso de tentativas de login incorretas. Tente novamente em 15 minutos.",
  "instance": "/api/v1/auth/login",
  "extensions": {
    "requestId": "0HN123456789A:00000001",
    "lockoutEnd": "2026-07-23T16:35:00Z"
  }
}
```

---

## 13. Modelo de Banco de Dados

### 13.1 Tabela `Users`
| Campo | Tipo SQL | Nulo? | Chave | Descrição |
| :--- | :--- | :---: | :---: | :--- |
| `Id` | `UNIQUEIDENTIFIER` | Não | PK | Chave Primária (Guid) |
| `Name` | `NVARCHAR(150)` | Não | - | Nome completo do usuário |
| `Email` | `NVARCHAR(150)` | Não | UK | E-mail corporativo único |
| `Cpf` | `NVARCHAR(11)` | Não | UK | CPF (somente dígitos) |
| `PasswordHash` | `NVARCHAR(256)` | Não | - | Hash BCrypt/Argon2 da senha |
| `Role` | `NVARCHAR(50)` | Não | - | Perfil de Acesso (`Admin`, `RH`, etc.) |
| `IsActive` | `BIT` | Não | - | Status da conta (1=Ativo, 0=Inativo) |
| `IsFirstAccess` | `BIT` | Não | - | Exige troca no primeiro login |
| `FailedLoginAttempts`| `INT` | Não | - | Contador de erros de senha |
| `LockoutEnd` | `DATETIMEOFFSET` | Sim | - | Data/hora do fim do bloqueio |
| `CreatedAt` | `DATETIMEOFFSET` | Não | - | Auditoria de criação |
| `CreatedBy` | `UNIQUEIDENTIFIER` | Sim | - | Usuário criador |
| `UpdatedAt` | `DATETIMEOFFSET` | Sim | - | Última atualização |
| `UpdatedBy` | `UNIQUEIDENTIFIER` | Sim | - | Usuário atualizador |
| `DeletedAt` | `DATETIMEOFFSET` | Sim | - | Data de remoção lógica |
| `DeletedBy` | `UNIQUEIDENTIFIER` | Sim | - | Usuário deletedor |
| `IsDeleted` | `BIT` | Não | - | Soft Delete (Default: 0) |

### 13.2 Tabela `RefreshTokens`
| Campo | Tipo SQL | Nulo? | Chave | Descrição |
| :--- | :--- | :---: | :---: | :--- |
| `Id` | `UNIQUEIDENTIFIER` | Não | PK | Chave Primária |
| `UserId` | `UNIQUEIDENTIFIER` | Não | FK | Referência para `Users(Id)` |
| `Token` | `NVARCHAR(256)` | Não | UK | Hash do Refresh Token aleatório |
| `ExpiresAt` | `DATETIMEOFFSET` | Não | - | Data/hora de expiração |
| `IsRevoked` | `BIT` | Não | - | Cancelado manualmente/rotação |
| `ReplacedByToken` | `NVARCHAR(256)` | Sim | - | Token que o substituiu |
| `CreatedAt` | `DATETIMEOFFSET` | Não | - | Data de emissão |

### 13.3 Índices Obrigatórios
- `IX_Users_Email` (`Email`) - Único
- `IX_Users_Cpf` (`Cpf`) - Único
- `IX_RefreshTokens_Token` (`Token`) - Único
- `IX_RefreshTokens_UserId` (`UserId`)

---

## 14. Plano de Auditoria e Rastreabilidade

Eventos gravados automaticamente na tabela de Auditoria com cabeçalhos HTTP enriquecidos (`IP`, `UserAgent`, `CorrelationId`):

1. `AUTH_LOGIN_SUCCESS` — Login realizado com sucesso.
2. `AUTH_LOGIN_FAILED` — Tentativa de login incorreta.
3. `AUTH_ACCOUNT_LOCKED` — Conta bloqueada por excesso de erros.
4. `AUTH_PASSWORD_CHANGED` — Senha alterada pelo usuário.
5. `AUTH_REFRESH_TOKEN_REVOKED` — Refresh token cancelado.
6. `USER_CREATED` — Novo usuário cadastrado.
7. `USER_ROLE_UPDATED` — Perfil de usuário alterado.
8. `USER_STATUS_TOGGLED` — Conta ativada/inativada.

---

## 15. Arquitetura de Segurança

1. **Assinatura de Tokens JWT:**
   - Algoritmo `HMAC-SHA256` (HS256) ou `RSA-SHA256` (RS256).
   - Secret key com tamanho mínimo de 256 bits configurada no cofre de segredos (`JwtOptions`).
2. **Proteção de Cookie:**
   - `HttpOnly = true` (Impede leitura via scripts maliciosos / XSS).
   - `Secure = true` (Exige conexão cifrada HTTPS).
   - `SameSite = Strict` (Previne ataques de CSRF).
3. **Detecção de Reutilização de Refresh Token (Token Reuse Detection):**
   - Se um Refresh Token já revogado for apresentado para a API, o sistema assume invasão, revoga IMEDIATAMENTE toda a árvore de tokens associada àquele usuário e obriga nova autenticação.

---

## 16. Análise e Mapeamento de Riscos

| Risco Técnico / Operacional | Impacto | Mitigação |
| :--- | :---: | :--- |
| Variação de tempo de resposta revelando se e-mail existe na base (Timing Attack) | Médio | Executar verificação de hash com tempo constante mesmo quando usuário não for localizado. |
| Perda de sincronia de relógio entre servidores web e banco (Clock Skew) | Alto | Utilizar obrigatoriamente `DateTimeOffset.UtcNow` e aplicar `ClockSkew = TimeSpan.FromSeconds(5)` na validação do JWT. |
| Interceptação de links de redefinição de senha | Alto | Validar e-mail + Token temporal assinado com HMAC e consumir o token no primeiro uso. |

---

## 17. Dependências Técnicas e Operacionais

- **Infraestrutura:** SQL Server 2022 rodando em container Docker com suporte a `DATETIMEOFFSET`.
- **Serviço de Email:** Servidor SMTP configurado (`EmailOptions`) para envio dos e-mails funcionais de recuperação e primeiro acesso.
- **Frontend:** Suporte a Cookies de Terceiros/Mesmo domínio no navegador do usuário para manipulação de Refresh Token.

---

## 18. Checklist para Desenvolvimento (Dev)

- [ ] Criar entidade `User` e `RefreshToken` herdando de `AuditableEntity`.
- [ ] Configurar Fluent API no `SIGH.Persistence` com `AuditableEntityConfiguration`.
- [ ] Implementar serviço de hash de senha (`IPasswordHasher`) na infraestrutura.
- [ ] Implementar gerador e validador de JWT (`IJwtTokenGenerator`).
- [ ] Criar rotas e controllers em `SIGH.Api` sob `/api/v1/auth` e `/api/v1/users`.
- [ ] Configurar interceptor Axios no React para renovação transparente do token.
- [ ] Implementar telas MUI conforme Wireframes (`/login`, `/primeiro-acesso`, `/recuperar-senha`).

---

## 19. Checklist para Garantia de Qualidade (QA)

- [ ] Validar mensagens de erro genéricas no login para e-mail incorreto e senha incorreta.
- [ ] Testar bloqueio automático de conta após 5 erros seguidos.
- [ ] Testar expiração de sessão por 15 minutos de inatividade no navegador.
- [ ] Verificar se Refresh Token não é acessível via `document.cookie` no console JavaScript.
- [ ] Validar criação de registros na tabela de AuditLogs para cada ação do módulo.
- [ ] Executar testes de carga no endpoint de login (meta < 300ms).

---

## 20. Roadmap de Implementação por Sprints

```
+---------------------------------------------------------------------------------+
|                         ROADMAP DA IMPLEMENTAÇÃO                                |
+---------------------------------------------------------------------------------+

  [Sprint 1: Core Backend Auth]
  ├── Modelo de dados Users & RefreshTokens (EF Core)
  ├── IPasswordHasher (BCrypt) e IJwtTokenGenerator
  └── Endpoints API: POST /auth/login e POST /auth/refresh

  [Sprint 2: Auto-Serviço & Gestão de Senhas]
  ├── Endpoints: forgot-password, reset-password, change-password
  ├── Serviço de envio de e-mails (SMTP)
  └── Fluxo de Primeiro Acesso com IsFirstAccess

  [Sprint 3: UI Frontend & Integração completa]
  ├── Telas React/MUI: /login, /primeiro-acesso, /recuperar-senha
  ├── Axios Interceptors para Refresh Token e Sessão Expirada
  └── Telas Administrativas: Gestão e Bloqueio de Usuários
```

---

### Informações Finais de Entrega

- **Quantidade de Páginas Estimadas (Impressão / PDF Document):** ~14 a 16 páginas.
- **Complexidade:** Alta (Envolve segurança corporativa, JWT, rotação de refresh tokens, cookies seguros, auditoria e RBAC).
- **Riscos Principais:** Vazamento de credenciais caso HTTPS não seja forçado em produção; ataques de enumeração de e-mails se as respostas de erro divergirem.
- **Sugestões ao Tech Lead:**
  1. Utilizar o pacote `BCrypt.Net-Next` ou a implementação nativa de `PasswordHasher<T>` do ASP.NET Core Identity no backend.
  2. Implementar biblioteca `Resilience/Polly` para o serviço de envio de e-mails SMTP na recuperação de senha.
  3. Aplicar *Rate Limiting* no ASP.NET Core Web API 9 para o endpoint `/api/v1/auth/login` (máximo 10 requisições por minuto por IP) como camada adicional de defesa antes do banco de dados.
