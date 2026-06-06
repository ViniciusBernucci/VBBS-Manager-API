# Arquitetura do Sistema

> **Ordem de leitura:** documento **05** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia após [04-aula-dotnet-conceitos.md](./04-aula-dotnet-conceitos.md).

## Visão geral

O sistema é composto por três projetos .NET dentro de uma única solution:

```
VBBSManager.sln
├── src/VBBSManager.Domain
├── src/VBBSManager.Infrastructure
└── src/VBBSManager.Api
```

E um projeto de testes separado:

```
tests/VBBSManager.Tests
```

---

## Responsabilidade de cada projeto

### VBBSManager.Domain

Núcleo do sistema. Não referencia nenhum outro projeto.

- Entidades do banco de dados (`Tenant`, `User`, `Alert`, etc.)
- Enums de domínio (`AlertType`, `AlertSeverity`, `IntegrationProvider`)
- Value Objects (a adicionar conforme necessidade)

**Regra:** nenhum pacote externo — só tipos primitivos do .NET.

---

### VBBSManager.Infrastructure

Implementações de infraestrutura. Referencia apenas o Domain.

- `AppDbContext` — contexto do Entity Framework Core com PostgreSQL
- `Configurations/` — mapeamento de entidades para tabelas (fluent API)
- `Migrations/` — geradas automaticamente pelo EF Core CLI
- `ExternalClients/` — clientes HTTP para APIs externas (Hotmart, Meta Ads, Brevo, Evolution)
- `Jobs/` — jobs Hangfire com política de retry

**Regra:** nenhuma classe de Infrastructure é injetada diretamente em um Controller. Tudo passa pelo Service.

---

### VBBSManager.Api

Ponto de entrada da aplicação. Referencia Domain e Infrastructure.

- `Program.cs` — bootstrap, DI, pipeline de middleware
- `Common/` — utilitários transversais (Result pattern, middlewares, extensions de DI)
- `Features/` — organizado por Vertical Slice (ver seção abaixo)

---

## Vertical Slice Architecture

Cada feature é uma pasta autocontida com seus próprios arquivos:

```
Features/
└── NomeDaFeature/
    └── NomeDoUseCase/
        ├── NomeDoUseCaseRequest.cs   ← DTO de entrada
        ├── NomeDoUseCaseResponse.cs  ← DTO de saída
        ├── NomeDoUseCaseService.cs   ← interface + implementação da lógica
        └── NomeDoUseCaseController.cs ← recebe, delega, retorna
```

**Por que isso?** Em desenvolvimento solo você mexe em uma feature por vez. Ter todos os arquivos do mesmo fluxo na mesma pasta elimina a navegação entre `Controllers/`, `Services/`, `Repositories/` separados.

---

## Fluxo de uma requisição

```
HTTP Request
    ↓
ExceptionMiddleware        ← captura qualquer exceção não tratada
    ↓
Authentication (JWT)       ← valida o token de acesso
    ↓
TenantMiddleware           ← extrai tenant_id do token e injeta no HttpContext
    ↓
Controller                 ← valida entrada, lê tenant_id do contexto, chama Service
    ↓
Service                    ← toda a lógica de negócio, sem conhecer HTTP
    ↓
AppDbContext / ExternalClient ← acesso ao banco ou API externa
    ↓
HTTP Response
```

---

## Multi-tenancy

Toda entidade que armazena dados de negócio herda de `BaseEntity`, que inclui `TenantId` obrigatório.

O `TenantMiddleware` extrai o `tenant_id` do claim do JWT e disponibiliza via `HttpContext.Items["TenantId"]`.

Cada Service recebe o `tenantId` como parâmetro explícito — nunca acessa o `HttpContext` diretamente.

O isolamento por tenant será reforçado com Row Level Security no PostgreSQL na Fase 5 (SaaS).

---

## Result Pattern

Todos os Services retornam `Result<T>` ou `Result` em vez de lançar exceções para erros esperados.

```csharp
// Sucesso
return Result<LoginResponse>.Ok(response);

// Falha esperada (credencial inválida, registro não encontrado, etc.)
return Result<LoginResponse>.Fail("Credenciais inválidas");
```

O Controller interpreta o resultado e decide o status HTTP adequado. Exceções não tratadas são capturadas pelo `ExceptionMiddleware`.
