# Arquitetura do Sistema SIGH

## Visão Geral

O **SIGH (Sistema Inteligente de Gestão de Histórico Disciplinar)** adota a **Clean Architecture** alinhada aos princípios de **Domain-Driven Design (DDD)**.

```
                  ┌─────────────────────────────────────────┐
                  │                 SIGH.Api                │
                  │        (Controllers, Middleware)        │
                  └────────────────────┬────────────────────┘
                                       │
                  ┌────────────────────▼────────────────────┐
                  │            SIGH.Application             │
                  │     (DTOs, Mappings, Interfaces)        │
                  └───────┬─────────────────────────┬───────┘
                          │                         │
     ┌────────────────────▼────┐       ┌────────────▼────────────────┐
     │       SIGH.Domain       │       │      SIGH.Infrastructure    │
     │  (Entities, Interfaces) │       │   (JWT, Email, Services)    │
     └─────────────────────────┘       └─────────────────────────────┘
                                       │
                                       │
                               ┌───────▼─────────────────────┐
                               │       SIGH.Persistence      │
                               │  (EF Core DbContext, Repos) │
                               └─────────────────────────────┘
```

## Descrição das Camadas

1. **SIGH.Domain**: Camada central contendo entidades, objetos de valor, interfaces do repositório e exceções de domínio. Não possui dependências externas.
2. **SIGH.Application**: Regras de aplicação, DTOs, mapeamentos (AutoMapper), validações (FluentValidation) e orquestração. Depende apenas do Domain.
3. **SIGH.Infrastructure**: Implementação de serviços externos como autenticação JWT, envio de emails, integrações de comunicação. Depende do Application.
4. **SIGH.Persistence**: Acesso a dados com Entity Framework Core 9 e SQL Server 2022. Depende do Application e Domain.
5. **SIGH.Api**: Ponto de entrada ASP.NET Core Web API 9 com Swagger, Serilog, CORS, Health Checks e Injeção de Dependências.
