# RESUMO EXECUTIVO - SPRINT 3.0 (SIGH - SISTEMA INTELIGENTE DE GESTÃO DE HISTÓRICO DISCIPLINAR)

## 📌 SPRINT
**Sprint 3.0** - Construção Completa da Camada HTTP, Segurança, Autenticação, Autorização Baseada em Permissões, Middlewares e Padronização REST/ProblemDetails.

---

## 🎯 OBJETIVO
Construir toda a camada HTTP da aplicação SIGH (ASP.NET Core 9 Web API) com arquitetura corporativa padronizada, Controllers com Attribute Routing `/api/v1/`, padronização das respostas com `Result<T>` e tratamento global de exceções convertido em RFC7807 `ProblemDetails`. Além disso, foram implementados mecanismos avançados de segurança: rate limiting de login, security headers, autorização baseada em permissões (`PermissionAttribute`), auditoria e suíte completa de testes unitários e de integração.

---

## 📁 ARQUIVOS CRIADOS

### Application Layer (`SIGH.Application`)
- `Common/Models/Result.cs` - Modelo genérico de resposta unificada `Result<T>`.
- `Common/Validators/PasswordComplexityValidator.cs` - Validador de complexidade de senha.
- `Interfaces/IPasswordHasher.cs` - Contrato para hash de senhas (BCrypt).
- `Interfaces/IJwtTokenGenerator.cs` - Contrato para geração de Access Token JWT.
- `Interfaces/IRefreshTokenGenerator.cs` - Contrato para geração de Refresh Tokens aleatórios.
- `Interfaces/ITokenHasher.cs` - Contrato para hashing SHA256 de Refresh Tokens.
- `Interfaces/IDateTimeProvider.cs` - Provider de data e hora UTC testável.
- `Interfaces/IRandomStringGenerator.cs` - Gerador de strings/tokens aleatórios seguros.
- `Interfaces/ISighAuthorizationService.cs` - Contrato para verificação dinâmica de permissões.
- `Options/PasswordOptions.cs` - Opções configuráveis de senha e lockout.
- `Options/TokenOptions.cs` - Opções configuráveis de expiração de refresh token.
- `Authentication/Login/*` (`LoginModels.cs`, `LoginRequestValidator.cs`, `ILoginService.cs`, `LoginService.cs`)
- `Authentication/Logout/*` (`LogoutModels.cs`, `ILogoutService.cs`, `LogoutService.cs`)
- `Authentication/RefreshToken/*` (`RefreshTokenModels.cs`, `IRefreshTokenService.cs`, `RefreshTokenService.cs`)
- `Authentication/ForgotPassword/*` (`ForgotPasswordModels.cs`, `ForgotPasswordRequestValidator.cs`, `IForgotPasswordService.cs`, `ForgotPasswordService.cs`)
- `Authentication/ResetPassword/*` (`ResetPasswordModels.cs`, `ResetPasswordRequestValidator.cs`, `IResetPasswordService.cs`, `ResetPasswordService.cs`)
- `Authentication/ChangePassword/*` (`ChangePasswordModels.cs`, `ChangePasswordRequestValidator.cs`, `IChangePasswordService.cs`, `ChangePasswordService.cs`)
- `Users/CreateUser/*` (`CreateUserModels.cs`, `CreateUserRequestValidator.cs`, `ICreateUserService.cs`, `CreateUserService.cs`)
- `Users/UnlockUser/*` (`UnlockUserModels.cs`, `UnlockUserRequestValidator.cs`, `IUnlockUserService.cs`, `UnlockUserService.cs`)
- `Users/ChangeStatus/*` (`ChangeStatusModels.cs`, `ChangeUserStatusRequestValidator.cs`, `IChangeUserStatusService.cs`, `ChangeUserStatusService.cs`)
- `Users/GetUsers/*` (`GetUsersModels.cs`, `IGetUsersService.cs`, `GetUsersService.cs`)
- `Users/GetUserById/*` (`GetUserByIdModels.cs`, `IGetUserByIdService.cs`, `GetUserByIdService.cs`)

### Infrastructure Layer (`SIGH.Infrastructure`)
- `Authentication/BCryptPasswordHasher.cs` - Implementação de hash com `BCrypt.Net-Next`.
- `Authentication/JwtTokenGenerator.cs` - Gerador de JWT com claims de usuário, perfil e permissões.
- `Authentication/Sha256TokenHasher.cs` - Hashing determinístico SHA256 para Refresh Tokens.
- `Authentication/RefreshTokenGenerator.cs` - Gerador seguro por `RandomNumberGenerator`.
- `Services/SystemDateTimeProvider.cs` - Provedor de relógio UTC do sistema.
- `Services/RandomStringGenerator.cs` - Gerador alfanumérico aleatório seguro.
- `Authorization/PermissionAttribute.cs` - Atributo declarativo de autorização por permissão.
- `Authorization/PermissionRequirement.cs` - Requisito de autorização do ASP.NET Core.
- `Authorization/PermissionHandler.cs` - Handler de autorização avaliando claims `"permission"`.
- `Authorization/PermissionPolicyProvider.cs` - Provedor dinâmico de políticas `Permission:*`.
- `Authorization/AuthorizationService.cs` - Serviço para validação programática de permissões.

### API Layer (`SIGH.Api`)
- `Controllers/BaseController.cs` - Controller base injetando helpers `Result<T>` e contexto do usuário.
- `Controllers/AuthController.cs` - Controller com endpoints do fluxo de autenticação e credenciais.
- `Controllers/UsersController.cs` - Controller para gestão de usuários, status e bloqueios.
- `Controllers/HealthController.cs` - Controller customizado para verificação de saúde da API e Banco de Dados.
- `Middlewares/SecurityHeadersMiddleware.cs` - Middleware para inclusão de cabeçalhos HTTP de segurança.
- `Middlewares/RequestLoggingMiddleware.cs` - Middleware de log estruturado de requisições via Serilog com CorrelationId.
- `Middlewares/GlobalExceptionMiddleware.cs` - Captura global de exceções e conversão em RFC7807 `ProblemDetails`.

### Unit & Integration Test Layers (`tests/`)
- `SIGH.UnitTests/PasswordHasherTests.cs`
- `SIGH.UnitTests/TokenHasherTests.cs`
- `SIGH.UnitTests/JwtTokenGeneratorTests.cs`
- `SIGH.UnitTests/LoginServiceTests.cs`
- `SIGH.UnitTests/RefreshTokenServiceTests.cs`
- `SIGH.UnitTests/PasswordHistoryTests.cs`
- `SIGH.IntegrationTests/CustomWebApplicationFactory.cs` - Factory de testes HTTP em memória.
- `SIGH.IntegrationTests/AuthEndpointsTests.cs`
- `SIGH.IntegrationTests/UsersEndpointsTests.cs`
- `SIGH.IntegrationTests/HealthEndpointTests.cs`

---

## 📝 ARQUIVOS ALTERADOS
- `src/SIGH.Infrastructure/SIGH.Infrastructure.csproj` (Adição do pacote `BCrypt.Net-Next`)
- `src/SIGH.Infrastructure/DependencyInjection.cs` (Registro dos serviços de segurança, JWT e autorização)
- `src/SIGH.Application/DependencyInjection.cs` (Registro dos serviços de aplicação e FluentValidation)
- `src/SIGH.Api/Program.cs` (Orquestração do pipeline HTTP, Swagger JWT, RateLimiting, HealthChecks, Middlewares)
- `tests/SIGH.UnitTests/SIGH.UnitTests.csproj` (Referências a EF Core In-Memory, Infrastructure e Persistence)
- `tests/SIGH.IntegrationTests/SIGH.IntegrationTests.csproj` (Referências ao projeto Api e EF Core In-Memory)

---

## 🎮 CONTROLLERS
1. `AuthController` (`/api/v1/auth`)
2. `UsersController` (`/api/v1/users`)
3. `HealthController` (`/api/v1/health`)
*Todos herdam de `BaseController` e utilizam o padrão unificado `Result<T>`.*

---

## 🛡️ MIDDLEWARES
Pipeline orquestrado no `Program.cs` na seguinte ordem de execução:
1. `CorrelationIdMiddleware` - Propaga ou gera `X-Correlation-ID`.
2. `GlobalExceptionMiddleware` - Captura exceções e gera RFC7807 `ProblemDetails`.
3. `SecurityHeadersMiddleware` - Aplica cabeçalhos corporativos de proteção contra ataques OWASP.
4. `RequestLoggingMiddleware` - Registra métricas de execução, status HTTP, usuário e IP.
5. `UseRateLimiter` - Proteção contra abuso de força bruta.
6. `UseResponseCompression` - Compactação Gzip de payloads.
7. `UseAuthentication` e `UseAuthorization` - Validação de JWT Bearer e autorização Granular.

---

## 🔐 AUTHORIZATION
- Sistema baseado em **Permissões Granulares (Claims-Based Authorization)**, superando restrições de simples Roles.
- Implementação customizada via `PermissionAttribute("Users.Create")`, `PermissionRequirement`, `PermissionHandler` e `PermissionPolicyProvider`.
- As permissões e perfis são extraídos do banco de dados e persistidos diretamente no payload de claims do JWT Bearer.

---

## 🌐 ENDPOINTS IMPLEMENTADOS
| Método | Endpoint | Descrição | Requer Autenticação | Permissão Necessária |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Realiza login e gera Access + Refresh Token | Não | N/A (Rate limited: 10/min) |
| `POST` | `/api/v1/auth/logout` | Revoga tokens e encerra a sessão ativa | Sim | Autenticado |
| `POST` | `/api/v1/auth/refresh` | Rotaciona Refresh Token e emite novo JWT | Não | N/A |
| `POST` | `/api/v1/auth/forgot-password` | Inicia fluxo de recuperação de senha por e-mail | Não | N/A |
| `POST` | `/api/v1/auth/reset-password` | Redefine senha utilizando token de reset | Não | N/A |
| `POST` | `/api/v1/auth/change-password` | Altera a senha do usuário autenticado | Sim | Autenticado |
| `POST` | `/api/v1/users` | Cadastra novo usuário no sistema | Sim | `Users.Create` |
| `PATCH` | `/api/v1/users/{id}/status` | Altera status do usuário (Ativo, Inativo, Suspenso) | Sim | `Users.ChangeStatus` |
| `POST` | `/api/v1/users/{id}/unlock` | Desbloqueia manualmente um usuário bloqueado | Sim | `Users.Unlock` |
| `GET` | `/api/v1/users` | Consulta paginada com filtros e pesquisa de usuários | Sim | `Users.View` |
| `GET` | `/api/v1/users/{id}` | Obtém detalhes completos de um usuário por ID | Sim | `Users.View` |
| `GET` | `/api/v1/health` | Diagnóstico completo de saúde da API e SQL Server | Não | N/A |

---

## 📚 SWAGGER
- Agrupamento limpo organizado por Tags (`Authentication`, `Users`, `Health`).
- Configuração do esquema de segurança `Bearer` com autenticação via cabeçalho HTTP `Authorization: Bearer {token}`.
- Documentação detalhada dos tipos de retorno de sucesso (`Result<T>`) e erros (`ProblemDetails`).

---

## 🏥 HEALTH CHECKS
- Implementado via serviço nativo do ASP.NET Core (`AddHealthChecks().AddDbContextCheck<SighDbContext>()`).
- Endpoint `/health` e `/api/v1/health` retornando o estado estruturado da aplicação e integridade da conexão com o banco de dados SQL Server.

---

## 🛑 RATE LIMITER
- Aplicado via `Microsoft.AspNetCore.RateLimiting` no endpoint `POST /api/v1/auth/login`.
- Política `LoginIpRateLimit`: Máximo de **10 requisições por minuto por IP**, retornando código `HTTP 429 Too Many Requests` quando excedido.

---

## 🛡️ SECURITY HEADERS
Injetados dinamicamente em todas as respostas HTTP pelo `SecurityHeadersMiddleware`:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy: default-src 'self'`

---

## 📦 RESULT<T>
Estrutura padronizada de resposta HTTP para sucesso e falha da aplicação:
```json
{
  "success": true,
  "data": { ... },
  "message": "Operação realizada com sucesso.",
  "errorCode": null,
  "validationErrors": null
}
```

---

## ⚠️ PROBLEMDETAILS (RFC 7807)
Mapeamento automático de exceções no `GlobalExceptionMiddleware`:
- `BusinessRuleValidationException` ➔ `400 Bad Request`
- `ValidationException` (FluentValidation) ➔ `400 Bad Request` (com lista de erros em `validationErrors`)
- `UnauthorizedAccessException` ➔ `401 Unauthorized`
- `SecurityException` ➔ `403 Forbidden`
- `KeyNotFoundException` ➔ `404 Not Found`
- `Exception` genérica ➔ `500 Internal Server Error`

---

## 🧪 TESTES
- **Testes Unitários (`SIGH.UnitTests`)**:
  - `PasswordHasherTests`: Verificação de criptografia BCrypt e verificação de hash.
  - `TokenHasherTests`: Testes de hash SHA256 determinístico de Refresh Tokens.
  - `JwtTokenGeneratorTests`: Validação da criação de tokens JWT e verificação das claims geradas.
  - `LoginServiceTests`: Sucesso, credenciais inválidas, incremento de tentativas incorretas e bloqueio automático por limite de tentativas.
  - `RefreshTokenServiceTests`: Rotação válida de Refresh Token e detecção de reuso com revogação total das sessões do usuário.
  - `PasswordHistoryTests`: Validação do histórico das últimas N senhas (impossibilitando reutilização).
- **Testes de Integração (`SIGH.IntegrationTests`)**:
  - `AuthEndpointsTests`: Teste end-to-end do fluxo de login e refresh via HTTP usando `CustomWebApplicationFactory`.
  - `UsersEndpointsTests`: Testes HTTP de criação de usuário, desbloqueio e alteração de status.
  - `HealthEndpointTests`: Teste do status de integridade e respostas do endpoint `/health`.

---

## ✅ RESULTADO
- Compilação do aplicativo (applet) finalizada com **SUCESSO** e sem nenhum erro.
- Linter executado e aprovado com 0 avisos/erros.
- Camada HTTP totalmente implementada, aderindo estritamente à arquitetura RESTful, princípios Clean Architecture e especificações da Sprint 3.0.

---

## 📋 PENDÊNCIAS
- Nenhuma pendência técnica para a Sprint 3.0. A camada HTTP do backend está 100% construída e integrada com a aplicação e persistência.

---

## ⚠️ RISCOS
- Necessidade de configuração adequada de variáveis de ambiente de produção (como `JwtOptions:SecretKey` forte e String de Conexão do SQL Server) na implantação final do servidor.

---

## 💡 SUGESTÕES
- Para a próxima Sprint (Sprint 4.0/Integração), implementar serviço de envio de e-mails real (SMTP / SendGrid) para substituição do `NullEmailService` atual no envio de tokens de recuperação de senha.
