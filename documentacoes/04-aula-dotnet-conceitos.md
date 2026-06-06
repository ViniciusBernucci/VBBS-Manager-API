# Aula de .NET — Conceitos Aplicados no Projeto VBBS Manager

> **Para quem é essa aula?**
> Para quem já programa (PHP/Laravel, Node.js ou outra linguagem) e está aprendendo .NET usando este projeto como laboratório. A ideia é explicar CADA conceito com o código real que você já escreveu — não exemplos genéricos.

> **Ordem de leitura:** este é o **documento 04** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia antes o [01-aula-fundamentos-web-api.md](./01-aula-fundamentos-web-api.md) e suba o ambiente com [03-ambiente-local.md](./03-ambiente-local.md).

---

## Sumário

1. [A Solution e os Projetos](#1-a-solution-e-os-projetos)
2. [Namespaces e a Estrutura de Pastas](#2-namespaces-e-a-estrutura-de-pastas)
3. [Vertical Slice Architecture](#3-vertical-slice-architecture)
4. [C# Moderno — Sintaxe que você vai ver em todo lugar](#4-c-moderno--sintaxe-que-você-vai-ver-em-todo-lugar)
5. [Entidades e o Domain Layer](#5-entidades-e-o-domain-layer)
6. [Entity Framework Core — O ORM do .NET](#6-entity-framework-core--o-orm-do-net)
7. [Migrations — Controle de Schema do Banco](#7-migrations--controle-de-schema-do-banco)
8. [Dependency Injection — O coração do ASP.NET Core](#8-dependency-injection--o-coração-do-aspnet-core)
9. [Controllers e Roteamento](#9-controllers-e-roteamento)
10. [Services e o Padrão Result](#10-services-e-o-padrão-result)
11. [Records — DTOs em C#](#11-records--dtos-em-c)
12. [Middleware — Interceptando Requisições](#12-middleware--interceptando-requisições)
13. [Autenticação JWT](#13-autenticação-jwt)
14. [Async/Await e CancellationToken](#14-asyncawait-e-cancellationtoken)
15. [Multi-tenancy — Isolamento de Dados](#15-multi-tenancy--isolamento-de-dados)
16. [Enums](#16-enums)
17. [Program.cs — O Ponto de Entrada da Aplicação](#17-programcs--o-ponto-de-entrada-da-aplicação)
18. [Juntando Tudo — Fluxo Completo de uma Requisição](#18-juntando-tudo--fluxo-completo-de-uma-requisição)

---

## 1. A Solution e os Projetos

No .NET, um projeto grande é organizado em uma **Solution** (`.sln`) que agrupa múltiplos **projetos** (`.csproj`). Pensa como um workspace que contém vários módulos independentes.

```
VBBSManager.sln
├── src/
│   ├── VBBSManager.Api/              ← Projeto Web (ASP.NET Core)
│   ├── VBBSManager.Domain/           ← Projeto de Domínio (C# puro)
│   └── VBBSManager.Infrastructure/   ← Projeto de Infraestrutura (EF Core, etc.)
└── tests/
    └── VBBSManager.Tests/
```

### Por que separar em projetos?

Cada projeto tem suas dependências independentes. O `Domain` não conhece nada de banco de dados — é C# puro com suas entidades e regras. Isso garante que as regras de negócio não ficam acopladas a detalhes técnicos.

**Analogia com Laravel:** é como separar seus Models/Entities, suas migrations e sua camada HTTP em pacotes diferentes que só se comunicam por interfaces.

### Como um projeto referencia outro?

No arquivo `.csproj`:

```xml
<!-- VBBSManager.Api.csproj -->
<ProjectReference Include="..\VBBSManager.Domain\VBBSManager.Domain.csproj" />
<ProjectReference Include="..\VBBSManager.Infrastructure\VBBSManager.Infrastructure.csproj" />
```

A `Api` conhece `Domain` e `Infrastructure`. Mas o `Domain` não conhece ninguém — ele é autocontido.

---

## 2. Namespaces e a Estrutura de Pastas

Em C#, o **namespace** é como o "endereço" de uma classe. Ele evita conflito de nomes e organiza o código.

```csharp
// Arquivo: src/VBBSManager.Domain/Entities/CashFlowTransaction.cs
namespace VBBSManager.Domain.Entities;

public class CashFlowTransaction : BaseEntity
{
    // ...
}
```

O namespace `VBBSManager.Domain.Entities` diz que esta classe mora no projeto `VBBSManager.Domain`, dentro da pasta `Entities`.

### Usando uma classe de outro namespace

```csharp
// No serviço da Api, preciso usar a entidade do Domain
using VBBSManager.Domain.Entities;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.CashFlow.GetCashFlow;

public class GetCashFlowService(AppDbContext db) : IGetCashFlowService
{
    // Agora posso usar CashFlowTransaction, TransactionType, AppDbContext...
}
```

**Analogia com PHP:** o `namespace` é o `namespace` do PHP. O `using` é o `use`. A diferença é que em C# o namespace PRECISA bater com a estrutura de pastas (convenção, não obrigação técnica, mas sempre siga).

---

## 3. Vertical Slice Architecture

Este projeto usa **Vertical Slice Architecture** — uma das decisões mais importantes que tomamos.

### Organização tradicional (por camada técnica)

```
Controllers/
  FinancialController.cs
  AuthController.cs
Services/
  FinancialService.cs
  AuthService.cs
Repositories/
  TransactionRepository.cs
```

### Organização por feature (Vertical Slice)

```
Features/
  Financial/
    CashFlow/
      GetCashFlow/
        GetCashFlowController.cs    ← tudo relacionado está junto
        GetCashFlowService.cs
        GetCashFlowResponse.cs
      CreateTransaction/
        CreateTransactionController.cs
        CreateTransactionService.cs
        CreateTransactionRequest.cs
  Auth/
    Login/
      LoginController.cs
      LoginService.cs
      LoginRequest.cs
      LoginResponse.cs
```

### Por que Vertical Slice?

Quando você precisa mudar o "Criar Lançamento", você abre **uma pasta** e todos os arquivos relevantes estão lá. Na organização por camada, você navegaria entre 3 pastas diferentes para entender um único fluxo.

Em um projeto solo (ou time pequeno), isso reduz drasticamente o tempo de orientação.

---

## 4. C# Moderno — Sintaxe que você vai ver em todo lugar

### 4.1 Primary Constructors (C# 12)

Em versões antigas do C#, para injetar dependências num serviço você escrevia:

```csharp
// Forma antiga
public class GetCashFlowService : IGetCashFlowService
{
    private readonly AppDbContext _db;

    public GetCashFlowService(AppDbContext db)
    {
        _db = db;
    }
}
```

Com **Primary Constructors** (C# 12, .NET 8):

```csharp
// Forma moderna — usada em todo o projeto
public class GetCashFlowService(AppDbContext db) : IGetCashFlowService
{
    // db está disponível como parâmetro em todos os métodos
    public async Task<Result<CashFlowResponse>> ExecuteAsync(...)
    {
        var config = await db.CashFlowConfigs.FirstOrDefaultAsync(...);
        // ^^ usa db diretamente
    }
}
```

O parâmetro `db` do construtor fica disponível em todo o corpo da classe automaticamente.

### 4.2 Records

Records são tipos imutáveis ideais para DTOs (objetos de transferência de dados):

```csharp
// Em vez de uma classe com propriedades:
public class CreateTransactionRequest
{
    public DateOnly Date { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
}

// Usa-se um record:
public record CreateTransactionRequest(
    DateOnly Date,
    string Description,
    decimal Amount,
    TransactionType Type,
    CashFlowCategory Category,
    Guid? FixedExpenseId = null   // parâmetro opcional com valor padrão
);
```

**Vantagens dos records:**
- Imutável por padrão (não dá para mudar após criar)
- Igualdade por valor (dois records com os mesmos dados são iguais)
- Geração automática de `ToString()` útil para debug
- Sintaxe compacta

### 4.3 Nullable Reference Types

O C# moderno tem o conceito de "nullabilidade explícita". Você declara quando algo **pode** ser null com `?`:

```csharp
public class FixedExpensePayment : BaseEntity
{
    public Guid? CashFlowTransactionId { get; set; }  // pode ser null
    public CashFlowTransaction? CashFlowTransaction { get; set; }  // pode ser null
    
    public FixedExpense FixedExpense { get; set; } = null!;  // nunca null (garantia sua)
}
```

- `Guid?` = Nullable Guid (pode ter valor ou ser null)
- `= null!` = "eu garanto que isso nunca será null em runtime, confia em mim compilador"

### 4.4 Pattern Matching com `switch expression`

```csharp
private static string GetCategoryLabel(CashFlowCategory category) => category switch
{
    CashFlowCategory.HotmartPix   => "Vendas Hotmart (Pix)",
    CashFlowCategory.HotmartCard  => "Vendas Hotmart (Cartão)",
    CashFlowCategory.MetaAds      => "Meta Ads",
    CashFlowCategory.Taxes        => "Impostos",
    CashFlowCategory.Tools        => "Ferramentas / SaaS",
    CashFlowCategory.OtherExpense => "Outras Saídas",
    _ => category.ToString()  // caso padrão (equivale ao default:)
};
```

Muito mais limpo que um `if/else if` ou `switch` tradicional.

### 4.5 Collection Expressions (C# 12)

```csharp
// Antes
var lista = new List<string>();
// ou
var lista = new List<string> { "a", "b" };

// Com collection expressions:
List<string> lista = [];         // lista vazia
List<string> lista = ["a", "b"]; // lista com itens
```

Veja no projeto:

```csharp
if (config is null)
    return Result<CashFlowResponse>.Ok(
        new CashFlowResponse(year, month, false, 0, 0, 0, 0, [], null)
        //                                                        ^^ lista vazia
    );
```

### 4.6 `var` — Inferência de Tipo

```csharp
// Sem var (verboso):
List<CashFlowTransaction> transactions = await db.CashFlowTransactions
    .Where(t => t.TenantId == tenantId)
    .ToListAsync(ct);

// Com var (o compilador infere o tipo):
var transactions = await db.CashFlowTransactions
    .Where(t => t.TenantId == tenantId)
    .ToListAsync(ct);
```

O `var` não é tipagem dinâmica — o tipo ainda é verificado em tempo de compilação. É apenas açúcar sintático.

### 4.7 `is not null` e `?.` (null-conditional)

```csharp
// Verificação de null moderna:
if (transaction is not null)
    db.CashFlowTransactions.Remove(transaction);

// Equivalente ao antigo:
if (transaction != null)
    db.CashFlowTransactions.Remove(transaction);

// Operador null-conditional ?.
var tenantId = context.User?.FindFirstValue("tenant_id");
//                         ^^ se User for null, retorna null em vez de lançar exceção
```

---

## 5. Entidades e o Domain Layer

### 5.1 BaseEntity

Toda entidade do projeto herda de `BaseEntity`:

```csharp
// src/VBBSManager.Domain/Entities/BaseEntity.cs
namespace VBBSManager.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

**Conceitos aqui:**

- `abstract class` — não pode ser instanciada diretamente. Só existe para ser herdada.
- `Guid` — identificador único universal (UUID no .NET). Diferente do int autoincrement, o Guid é gerado na aplicação, não no banco.
- `= Guid.NewGuid()` — valor padrão: ao criar qualquer entidade, o Id já é preenchido automaticamente.
- `DateTime.UtcNow` — sempre use UTC em banco de dados. Nunca horário local.
- `DateTime?` — UpdatedAt é nullable porque ao criar, ainda não houve atualização.

### 5.2 Uma Entidade Real

```csharp
// src/VBBSManager.Domain/Entities/CashFlowTransaction.cs
using VBBSManager.Domain.Enums;

namespace VBBSManager.Domain.Entities;

public class CashFlowTransaction : BaseEntity
{
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public CashFlowCategory Category { get; set; }
}
```

**Conceitos:**

- `: BaseEntity` — herança. `CashFlowTransaction` herda `Id`, `TenantId`, `CreatedAt`, `UpdatedAt`.
- `DateOnly` — tipo introduzido no .NET 6 para datas sem horário. Antes usávamos `DateTime` e ignorávamos a parte do horário — torpe e confuso. `DateOnly` mapeia para a coluna `date` no PostgreSQL.
- `string = string.Empty` — inicializa como string vazia para evitar null. Boa prática para strings.
- `decimal` — **nunca use `float` ou `double` para dinheiro**. `decimal` é aritmética de ponto fixo e não tem erros de arredondamento como ponto flutuante.

### 5.3 Relacionamentos entre Entidades

```csharp
// src/VBBSManager.Domain/Entities/FixedExpensePayment.cs
public class FixedExpensePayment : BaseEntity
{
    public Guid FixedExpenseId { get; set; }        // FK — armazena o ID
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? CashFlowTransactionId { get; set; } // FK nullable

    // Navigation properties — EF Core preenche automaticamente via JOIN
    public FixedExpense FixedExpense { get; set; } = null!;
    public CashFlowTransaction? CashFlowTransaction { get; set; }
}
```

**Navigation properties** são propriedades que o EF Core preenche automaticamente quando você usa `.Include()` na query. A propriedade `FixedExpenseId` é a chave estrangeira real no banco. A propriedade `FixedExpense` é só para navegação no código C# — não existe como coluna.

---

## 6. Entity Framework Core — O ORM do .NET

EF Core é o ORM oficial do .NET. Ele mapeia classes C# para tabelas e LINQ para SQL.

### 6.1 DbContext

O `AppDbContext` é o ponto central — é por onde você acessa o banco:

```csharp
// src/VBBSManager.Infrastructure/Persistence/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Cada DbSet<T> representa uma tabela
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CashFlowTransaction> CashFlowTransactions => Set<CashFlowTransaction>();
    public DbSet<CashFlowConfig> CashFlowConfigs => Set<CashFlowConfig>();
    public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();
    public DbSet<FixedExpensePayment> FixedExpensePayments => Set<FixedExpensePayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as configurações de entidade automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

**`DbSet<T>`** — é uma coleção que representa a tabela. Quando você faz `db.CashFlowTransactions`, está acessando a tabela `CashFlowTransactions`.

### 6.2 Entity Configurations

Em vez de colocar toda configuração na entidade, usamos classes de configuração separadas (padrão `IEntityTypeConfiguration<T>`):

```csharp
// src/VBBSManager.Infrastructure/Persistence/Configurations/CashFlowTransactionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence.Configurations;

public class CashFlowTransactionConfiguration : IEntityTypeConfiguration<CashFlowTransaction>
{
    public void Configure(EntityTypeBuilder<CashFlowTransaction> builder)
    {
        builder.HasKey(t => t.Id);                                    // chave primária
        builder.Property(t => t.Description).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2);          // numeric(18,2) no PostgreSQL
        builder.Property(t => t.Date).HasColumnType("date");          // tipo da coluna no banco
        builder.HasIndex(t => new { t.TenantId, t.Date });            // índice composto
    }
}
```

**Por que precisão no decimal?** O banco precisa saber quantas casas decimais reservar. `HasPrecision(18, 2)` = até 18 dígitos no total, 2 após a vírgula. Para dinheiro, sempre use 2 casas.

### 6.3 Queries com LINQ

LINQ (Language Integrated Query) é a linguagem de consulta do C#. O EF Core traduz LINQ para SQL:

```csharp
// Busca simples
var config = await db.CashFlowConfigs
    .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);
// SQL: SELECT * FROM "CashFlowConfigs" WHERE "TenantId" = @p0 LIMIT 1

// Filtro + ordenação
var transactions = await db.CashFlowTransactions
    .Where(t => t.TenantId == tenantId && t.Date >= monthStart && t.Date < monthEnd)
    .OrderBy(t => t.Date)
    .ThenBy(t => t.CreatedAt)
    .ToListAsync(ct);
// SQL: SELECT * FROM "CashFlowTransactions"
//      WHERE "TenantId" = @p0 AND "Date" >= @p1 AND "Date" < @p2
//      ORDER BY "Date", "CreatedAt"

// Sum
var totalIncome = transactions
    .Where(t => t.Type == TransactionType.Income)
    .Sum(t => t.Amount);
// Isso é LINQ-to-Objects (já em memória) — não gera SQL, processa em C#

// Any — verifica existência
var alreadyPaid = await db.FixedExpensePayments.AnyAsync(
    p => p.TenantId == tenantId
      && p.FixedExpenseId == fixedExpenseId
      && p.Year == year && p.Month == month, ct);
// SQL: SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END
```

**`ToListAsync` vs processamento em memória:** quando você chama `.ToListAsync()`, o EF executa a query no banco e traz os dados para a memória. Depois disso, qualquer `.Where()` ou `.Sum()` opera em memória (LINQ-to-Objects), não no banco.

### 6.4 Inserção e Atualização

```csharp
// INSERIR
var transaction = new CashFlowTransaction
{
    TenantId = tenantId,
    Date = request.Date,
    Description = request.Description,
    Amount = request.Amount,
    Type = request.Type,
    Category = request.Category
    // Id, CreatedAt são preenchidos automaticamente pelo BaseEntity
};

db.CashFlowTransactions.Add(transaction);  // rastreia o objeto
await db.SaveChangesAsync(ct);             // executa o INSERT no banco
```

```csharp
// ATUALIZAR — busca, modifica, salva
var expense = await db.FixedExpenses
    .FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId, ct);

expense.Name = request.Name;
expense.Amount = request.Amount;
expense.UpdatedAt = DateTime.UtcNow;

await db.SaveChangesAsync(ct);  // EF detecta as mudanças e executa UPDATE
```

O EF Core usa **Change Tracking** — ele rastreia automaticamente quais propriedades mudaram e gera o UPDATE mínimo necessário.

```csharp
// DELETAR
db.CashFlowTransactions.Remove(transaction);
await db.SaveChangesAsync(ct);  // executa DELETE
```

---

## 7. Migrations — Controle de Schema do Banco

Uma **migration** é um arquivo C# que descreve uma mudança no schema do banco de dados. É a forma do EF Core versionar o banco — equivale aos arquivos de migration do Laravel/Artisan.

### 7.1 Estrutura de uma Migration

```csharp
// src/VBBSManager.Infrastructure/Persistence/Migrations/20260605130000_AddCashFlow.cs
public partial class AddCashFlow : Migration
{
    // Up() = aplica a mudança
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CashFlowTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Description = table.Column<string>(type: "character varying(255)", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CashFlowTransactions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CashFlowTransactions_TenantId_Date",
            table: "CashFlowTransactions",
            columns: new[] { "TenantId", "Date" });
    }

    // Down() = reverte a mudança
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CashFlowTransactions");
    }
}
```

**`Up()`** executa quando você aplica a migration. **`Down()`** executa quando você reverte.

### 7.2 Fluxo normal de trabalho

```bash
# 1. Você adiciona uma nova entidade ou modifica uma existente

# 2. Gera a migration (o EF compara o modelo atual com o snapshot)
dotnet ef migrations add NomeDaMigration --project ../VBBSManager.Infrastructure

# 3. Aplica no banco
dotnet ef database update --project ../VBBSManager.Infrastructure
```

O arquivo `AppDbContextModelSnapshot.cs` é o "estado atual do modelo" que o EF usa para calcular o diff na próxima migration.

### 7.3 A tabela __EFMigrationsHistory

O EF Core mantém uma tabela especial no banco chamada `__EFMigrationsHistory` que registra quais migrations já foram aplicadas:

```sql
SELECT * FROM "__EFMigrationsHistory";
-- MigrationId                              | ProductVersion
-- 20260605120000_InitialCreate            | 8.0.11
-- 20260605130000_AddCashFlow              | 8.0.11
-- 20260605140000_AddFixedExpenses         | 8.0.11
-- 20260605150000_AddFixedExpensePayments  | 8.0.11
```

Cada migration só é executada uma vez — se o ID já está nessa tabela, o EF pula.

---

## 8. Dependency Injection — O coração do ASP.NET Core

**Dependency Injection (DI)** é um padrão onde as dependências de uma classe são "injetadas" por fora, em vez de a classe criá-las. O ASP.NET Core tem DI embutida.

### 8.1 Por que DI?

**Sem DI (ruim):**
```csharp
public class GetCashFlowService
{
    private readonly AppDbContext _db;

    public GetCashFlowService()
    {
        _db = new AppDbContext(); // acoplamento forte — impossível de testar
    }
}
```

**Com DI (correto):**
```csharp
public class GetCashFlowService(AppDbContext db) : IGetCashFlowService
{
    // AppDbContext é injetado pelo container — a classe não sabe como criar
}
```

### 8.2 Registrando serviços

No `ServiceCollectionExtensions.cs`, você registra todas as implementações:

```csharp
public static IServiceCollection AddFeatureServices(this IServiceCollection services)
{
    services.AddScoped<ILoginService, LoginService>();
    services.AddScoped<IGetCashFlowService, GetCashFlowService>();
    services.AddScoped<ICreateTransactionService, CreateTransactionService>();
    services.AddScoped<IPayFixedExpenseService, PayFixedExpenseService>();
    // ...
    return services;
}
```

`AddScoped<Interface, Implementação>` — diz ao container: "quando alguém pedir um `IGetCashFlowService`, entregue um `GetCashFlowService`".

### 8.3 Lifetimes (Tempo de vida)

O .NET tem três lifetimes para serviços:

| Lifetime | Quando usar | Duração |
|----------|-------------|---------|
| `AddScoped` | A maioria dos serviços (Services, DbContext) | Uma requisição HTTP |
| `AddTransient` | Serviços leves, sem estado | Cada vez que é pedido |
| `AddSingleton` | Cache, configurações, clientes HTTP | Toda a vida da aplicação |

**Por que DbContext é Scoped?** Porque ele rastreia mudanças durante uma requisição. Se fosse Singleton, todos os usuários compartilhariam o mesmo contexto — desastre de concorrência.

### 8.4 Extension Methods no IServiceCollection

Note que usamos **extension methods** para organizar o registro:

```csharp
// ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        // ...
        return services;
    }
    
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));
        return services;
    }
}

// Program.cs — uso limpo
builder.Services.AddFeatureServices();
builder.Services.AddDatabase(builder.Configuration);
```

O `this IServiceCollection services` no parâmetro é o que faz ser um **extension method** — você chama como se fosse um método nativo do `IServiceCollection`.

---

## 9. Controllers e Roteamento

### 9.1 Estrutura de um Controller

```csharp
// src/VBBSManager.Api/Features/Financial/CashFlow/GetCashFlow/GetCashFlowController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VBBSManager.Api.Features.Financial.CashFlow.GetCashFlow;

[ApiController]               // ativa validação automática + resposta JSON
[Route("api/financial/cash-flow")]  // prefixo da rota
[Authorize]                   // exige autenticação JWT
public class GetCashFlowController(IGetCashFlowService service) : ControllerBase
{
    [HttpGet]                 // responde a GET
    [ProducesResponseType(typeof(CashFlowResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int year,   // vem da query string: ?year=2026
        [FromQuery] int month,  // vem da query string: &month=6
        CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, year, month, ct);

        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);  // HTTP 200 com o objeto serializado como JSON
    }
}
```

### 9.2 Atributos de Roteamento

| Atributo | Método HTTP | Exemplo de URL |
|----------|-------------|----------------|
| `[HttpGet]` | GET | `GET /api/financial/cash-flow` |
| `[HttpPost]` | POST | `POST /api/financial/cash-flow/transactions` |
| `[HttpPut("{id:guid}")]` | PUT | `PUT /api/financial/cash-flow/transactions/abc-123` |
| `[HttpDelete("{id:guid}")]` | DELETE | `DELETE /api/financial/cash-flow/transactions/abc-123` |
| `[HttpPatch("{id:guid}/toggle")]` | PATCH | `PATCH /api/financial/fixed-expenses/abc-123/toggle` |

O `{id:guid}` é um **route constraint** — diz que o parâmetro `id` deve ser um GUID válido. Se não for, retorna 404 automaticamente.

### 9.3 De onde vêm os parâmetros?

```csharp
// Da query string: GET /endpoint?year=2026&month=6
[HttpGet]
public IActionResult Get([FromQuery] int year, [FromQuery] int month) { }

// Do corpo (JSON): POST /endpoint com body { "date": "2026-06-01", ... }
[HttpPost]
public IActionResult Create([FromBody] CreateTransactionRequest request) { }

// Da rota: PUT /endpoint/abc-def-123
[HttpPut("{id:guid}")]
public IActionResult Update(Guid id, [FromBody] UpdateTransactionRequest request) { }
```

### 9.4 Retornos HTTP

```csharp
return Ok(data);                           // 200 com dados
return NoContent();                        // 204 sem dados
return BadRequest(new { error = "msg" }); // 400
return NotFound(new { error = "msg" });   // 404
return StatusCode(500, new { error });    // 500 customizado
```

---

## 10. Services e o Padrão Result

### 10.1 Por que usar Interface + Implementação?

```csharp
// Interface — o CONTRATO
public interface IGetCashFlowService
{
    Task<Result<CashFlowResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

// Implementação — o CÓDIGO REAL
public class GetCashFlowService(AppDbContext db) : IGetCashFlowService
{
    public async Task<Result<CashFlowResponse>> ExecuteAsync(...)
    {
        // implementação
    }
}
```

**Por que isso?** Porque no `ServiceCollectionExtensions` você registra a interface apontando para a implementação. Se quiser trocar a implementação (para testes, por exemplo), só muda o registro — sem alterar o controller.

O Controller **nunca conhece** a implementação concreta, só a interface.

### 10.2 O Padrão Result

Em vez de lançar exceções para erros de negócio, usamos um objeto `Result`:

```csharp
// src/VBBSManager.Api/Common/Results/Result.cs
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }
    // ...
    public static Result<T> Ok(T value) => new(true, null, value);
    public static Result<T> Fail(string error) => new(false, error, default);
}
```

**Uso no serviço:**

```csharp
public async Task<Result<Guid>> PayAsync(Guid tenantId, Guid fixedExpenseId, ...)
{
    var expense = await db.FixedExpenses.FirstOrDefaultAsync(...);

    if (expense is null)
        return Result<Guid>.Fail("Gasto fixo não encontrado.");  // erro de negócio

    var alreadyPaid = await db.FixedExpensePayments.AnyAsync(...);

    if (alreadyPaid)
        return Result<Guid>.Fail("Este gasto já foi marcado como pago neste mês.");

    // sucesso
    db.FixedExpensePayments.Add(payment);
    await db.SaveChangesAsync(ct);
    return Result<Guid>.Ok(payment.Id);
}
```

**Uso no controller:**

```csharp
var result = await service.PayAsync(tenantId, id, request, ct);

if (!result.IsSuccess)
    return BadRequest(new { error = result.Error });  // passa o erro para o cliente

return Ok(new { paymentId = result.Value });
```

**Por que não lançar exception?** Exceptions são para situações inesperadas (banco fora do ar, erro de rede). "Gasto não encontrado" é um fluxo esperado — não é uma exceção, é um resultado possível do negócio. O padrão Result torna isso explícito.

---

## 11. Records — DTOs em C#

DTOs (Data Transfer Objects) são objetos usados apenas para transferir dados entre camadas. Em C# moderno, usamos `record` para isso.

### 11.1 Request (entrada)

```csharp
// O que o frontend envia no body do POST
public record PayFixedExpenseRequest(
    int Year,
    int Month,
    decimal Amount,
    DateOnly Date
);
```

O ASP.NET Core desserializa automaticamente o JSON para esse record.

### 11.2 Response (saída)

```csharp
// O que o backend retorna no corpo da resposta
public record CashFlowResponse(
    int Year,
    int Month,
    bool IsConfigured,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance,
    List<CashFlowTransactionDto> Transactions,
    CashFlowConfigDto? Config      // nullable — pode não existir
);

public record CashFlowTransactionDto(
    Guid Id,
    DateOnly Date,
    string Description,
    decimal Amount,
    string Type,
    string Category,
    string CategoryLabel,
    bool IsFixed,
    bool IsPaid,
    Guid? PaymentId
);
```

**Por que nunca retornar a entidade diretamente?**

```csharp
// ERRADO — expõe a entidade do banco diretamente
return Ok(transaction);  // expõe campos internos, acoplamento com o banco

// CORRETO — retorna um DTO
return Ok(new CashFlowTransactionDto(
    transaction.Id,
    transaction.Date,
    transaction.Description,
    transaction.Amount,
    transaction.Type.ToString(),
    transaction.Category.ToString(),
    GetCategoryLabel(transaction.Category),
    false, true, null
));
```

Se você mudar o banco (renomear coluna, adicionar campo interno), o DTO não precisa mudar — o contrato com o frontend permanece estável.

---

## 12. Middleware — Interceptando Requisições

Middleware são componentes que processam a requisição antes (e depois) de ela chegar ao controller. Pensa como "plugins" na pipeline da requisição.

### 12.1 TenantMiddleware

```csharp
// src/VBBSManager.Api/Common/Middleware/TenantMiddleware.cs
public class TenantMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private static readonly Guid DevTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task InvokeAsync(HttpContext context)
    {
        // Tenta extrair o tenant_id do token JWT
        var tenantClaim = context.User.FindFirstValue("tenant_id");

        if (tenantClaim is not null && Guid.TryParse(tenantClaim, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;  // adiciona ao contexto da requisição
        }
        else if (env.IsDevelopment())
        {
            context.Items["TenantId"] = DevTenantId;  // dev: usa tenant fixo
        }

        await next(context);  // passa para o próximo componente da pipeline
    }
}
```

**`RequestDelegate next`** = referência para o próximo middleware na chain. Sempre chame `await next(context)` para continuar a requisição. Se não chamar, a requisição para ali.

### 12.2 A Pipeline de Middleware

No `Program.cs`, a ordem IMPORTA:

```csharp
app.UseMiddleware<ExceptionMiddleware>();  // 1. Captura exceções não tratadas
app.UseCors();                             // 2. Adiciona headers CORS
app.UseAuthentication();                  // 3. Valida o JWT e popula User
app.UseMiddleware<TenantMiddleware>();     // 4. Extrai tenant_id do User (depende do 3)
app.UseAuthorization();                   // 5. Verifica [Authorize] (depende do 3)
app.MapControllers();                     // 6. Roteia para o controller correto
```

Se você colocar `TenantMiddleware` antes de `UseAuthentication()`, `context.User` ainda estará vazio e o tenant nunca será extraído do token.

### 12.3 Acessando dados do Middleware no Controller

O middleware salva o `TenantId` em `HttpContext.Items` — uma coleção de dados que dura a requisição inteira:

```csharp
// No middleware:
context.Items["TenantId"] = tenantId;

// No controller:
var tenantId = (Guid)HttpContext.Items["TenantId"]!;
```

---

## 13. Autenticação JWT

JWT (JSON Web Token) é um token assinado que carrega informações (claims) sobre o usuário.

### 13.1 Estrutura do JWT

Um JWT tem três partes separadas por `.`:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9   ← Header (algoritmo)
.eyJzdWIiOiJhYmMxMjMiLCJlbWFpbCI6InZp...  ← Payload (claims)
.wH9o1ANDL9Jc2vQH6DZ9CAeTW_AoAl0rHAld    ← Signature (verificação)
```

O payload decodificado contém:

```json
{
  "sub": "ce3dc58b-a522-4ebf-aca1-d875849ccf3e",
  "email": "viniciusbernucci@gmail.com",
  "tenant_id": "00000000-0000-0000-0000-000000000001",
  "jti": "f8040450-f1cb-4f80-a947-26906d157219",
  "exp": 1780682735,
  "iss": "vbbs-manager",
  "aud": "vbbs-manager-web"
}
```

### 13.2 Gerando o JWT no Login

```csharp
// src/VBBSManager.Api/Features/Auth/Login/LoginService.cs
private string GenerateJwt(UserEntity user, DateTime expiresAt)
{
    var secret = config["Jwt:Secret"]!;
    var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("tenant_id", user.TenantId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: expiresAt,
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### 13.3 Hash de Senha com PBKDF2

Nunca armazene senhas em texto puro. Usamos **PBKDF2** (algoritmo de derivação de chave):

```csharp
// Verificar senha no login:
private static bool VerifyPassword(string password, string storedHash)
{
    // storedHash = "base64(salt):base64(hash)"
    var parts = storedHash.Split(':');
    var salt = Convert.FromBase64String(parts[0]);
    var expectedHash = Convert.FromBase64String(parts[1]);

    var actualHash = KeyDerivation.Pbkdf2(
        password: password,         // senha que o usuário digitou
        salt: salt,                 // salt aleatório que foi salvo junto ao hash
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 100_000,    // 100 mil iterações — lento intencionalmente
        numBytesRequested: 32
    );

    // Comparação em tempo constante — evita timing attacks
    return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
}
```

**Por que 100 mil iterações?** Para tornar ataques de força bruta caros computacionalmente. Um atacante que roubasse o banco de dados levaria anos para testar senhas comuns.

**Por que `FixedTimeEquals`?** Comparações normais (`==`) retornam mais rápido quando o primeiro byte difere. Um atacante pode medir o tempo e deduzir bytes da senha. `FixedTimeEquals` sempre leva o mesmo tempo, independente de onde a diferença está.

### 13.4 Refresh Token

O access token dura apenas 15 minutos. O refresh token dura 7 dias e fica no banco:

```csharp
private async Task<RefreshTokenEntity> CreateRefreshToken(UserEntity user, CancellationToken ct)
{
    var token = new RefreshTokenEntity
    {
        UserId = user.Id,
        TenantId = user.TenantId,
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), // 64 bytes aleatórios
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    db.RefreshTokens.Add(token);
    await db.SaveChangesAsync(ct);
    return token;
}
```

O frontend guarda ambos. Quando o access token expira, usa o refresh token para obter um novo — sem precisar fazer login novamente.

---

## 14. Async/Await e CancellationToken

### 14.1 Por que Async?

Em aplicações web, cada requisição usa uma thread. Se a thread fica bloqueada esperando o banco de dados responder, ela não pode atender outras requisições.

Com `async/await`, enquanto o banco processa, a thread é **liberada** para atender outras requisições. Quando o banco responde, a thread retoma o trabalho.

```csharp
// SÍNCRONO — thread fica bloqueada esperando o banco
var config = db.CashFlowConfigs.FirstOrDefault(c => c.TenantId == tenantId);

// ASSÍNCRONO — thread é liberada enquanto espera
var config = await db.CashFlowConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);
```

### 14.2 Regra de ouro: async all the way

Se um método chama algo async, ele também deve ser async:

```csharp
// CORRETO — toda a chain é async
public async Task<Result<CashFlowResponse>> ExecuteAsync(Guid tenantId, ...)
{
    var config = await db.CashFlowConfigs.FirstOrDefaultAsync(...);
    var transactions = await db.CashFlowTransactions.ToListAsync(...);
    await db.SaveChangesAsync(ct);
    return Result<CashFlowResponse>.Ok(...);
}
```

### 14.3 CancellationToken

O `CancellationToken` é passado em toda operação async do projeto. Ele serve para **cancelar** operações quando o cliente desconecta:

```csharp
public async Task<IActionResult> Get(
    [FromQuery] int year,
    [FromQuery] int month,
    CancellationToken ct)   // ASP.NET injeta automaticamente
{
    var result = await service.ExecuteAsync(tenantId, year, month, ct);
    // Se o usuário fechar o browser, ct é cancelado
    // O banco para de processar a query — economiza recursos
}
```

O ASP.NET Core injeta o `CancellationToken` automaticamente nos parâmetros do action — você não precisa criar nem gerenciar.

---

## 15. Multi-tenancy — Isolamento de Dados

**Multi-tenancy** significa que um sistema serve múltiplos "inquilinos" (tenants) com isolamento de dados.

### 15.1 Isolamento por TenantId

Toda entidade tem `TenantId`. Toda query filtra por ele:

```csharp
// Nunca busque sem o tenantId
var expenses = await db.FixedExpenses
    .Where(f => f.TenantId == tenantId && f.IsActive)
    .ToListAsync(ct);

// O mesmo ao inserir — sempre defina o tenantId
var expense = new FixedExpense
{
    TenantId = tenantId,  // isolamento garantido na criação
    Name = request.Name,
    // ...
};
```

### 15.2 O TenantMiddleware garante o contexto

O `tenantId` vem do JWT (via TenantMiddleware) e é passado para todos os serviços. Isso garante que:

1. Usuário A nunca acessa dados do Usuário B (isolamento)
2. Cada requisição sabe de qual tenant vem (contexto)

### 15.3 Por que isso importa para SaaS?

Quando este sistema virar SaaS e tiver 100 clientes, você só precisa garantir que todo `WHERE` tenha o `TenantId`. A arquitetura já está preparada desde o início.

---

## 16. Enums

Enums definem um conjunto fixo de valores com nome:

```csharp
// src/VBBSManager.Domain/Enums/CashFlowEnums.cs
namespace VBBSManager.Domain.Enums;

public enum TransactionType
{
    Income = 1,   // valor numérico explícito — boa prática para persistência
    Expense = 2
}

public enum CashFlowCategory
{
    // Entradas
    HotmartPix  = 1,
    HotmartCard = 2,
    OtherIncome = 3,

    // Saídas
    MetaAds      = 10,  // gap proposital — facilita adicionar novos valores depois
    Taxes        = 11,
    Tools        = 12,
    OtherExpense = 13
}
```

### Como o EF salva enums?

Por padrão, o EF salva enums como inteiros no banco. `TransactionType.Income` vira `1`, `TransactionType.Expense` vira `2`.

```sql
-- No banco: 1 = Income, 2 = Expense
SELECT "Type" FROM "CashFlowTransactions";
-- Retorna: 1, 2, 1, 1, 2
```

### Como o JSON serializa enums?

Por padrão, o ASP.NET seria `1` e `2` no JSON. Configuramos para usar o nome:

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
```

Agora o JSON retorna `"Income"` e `"Expense"` — muito mais legível para o frontend.

---

## 17. Program.cs — O Ponto de Entrada da Aplicação

O `Program.cs` é o arquivo que inicializa a aplicação. No .NET 6+, é um único arquivo (sem `Startup.cs` separado):

```csharp
// src/VBBSManager.Api/Program.cs

// 1. Cria o builder (configura serviços)
var builder = WebApplication.CreateBuilder(args);

// 2. Registra serviços no Container de DI
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));

builder.Services.AddCorsPolicy(builder.Configuration);       // extensão customizada
builder.Services.AddDatabase(builder.Configuration);         // extensão customizada
builder.Services.AddJwtAuth(builder.Configuration);          // extensão customizada
builder.Services.AddHangfire(builder.Configuration);         // jobs agendados
builder.Services.AddFeatureServices();                       // todos os serviços de features
builder.Services.AddSwagger();                               // documentação da API

// 3. Constrói a aplicação
var app = builder.Build();

// 4. Configura o pipeline de middleware (ordem importa!)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // só expõe Swagger em desenvolvimento
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();       // descobre todos os [ApiController] automaticamente

// 5. Inicia o servidor
app.Run();
```

**Fase 1 (Services):** configura o que vai estar disponível.
**Fase 2 (Middleware):** configura como as requisições são processadas.
**Fase 3 (Run):** inicia o servidor e fica ouvindo requisições.

---

## 18. Juntando Tudo — Fluxo Completo de uma Requisição

Vamos seguir a requisição `GET /api/financial/cash-flow?year=2026&month=6` do início ao fim:

```
FRONTEND
  │  Authorization: Bearer eyJhbGc...
  │  GET /api/financial/cash-flow?year=2026&month=6
  ▼
KESTREL (servidor HTTP do .NET)
  │
  ▼
ExceptionMiddleware
  │  "se qualquer erro acontecer abaixo, eu capturo e retorno 500"
  ▼
CorsMiddleware
  │  "adiciona headers Access-Control-Allow-Origin"
  ▼
AuthenticationMiddleware
  │  "decodifico o JWT"
  │  "populo context.User com os claims: sub, email, tenant_id"
  ▼
TenantMiddleware
  │  "leio tenant_id do context.User"
  │  "salvo em context.Items['TenantId']"
  ▼
AuthorizationMiddleware
  │  "o controller tem [Authorize]?"
  │  "context.User está autenticado?"
  │  "sim → continua"
  ▼
ROUTER
  │  "qual controller responde GET /api/financial/cash-flow?"
  │  → GetCashFlowController
  ▼
GetCashFlowController.Get(year=2026, month=6, ct)
  │  var tenantId = (Guid)HttpContext.Items["TenantId"]!
  │  → "00000000-0000-0000-0000-000000000001"
  │
  │  await service.ExecuteAsync(tenantId, 2026, 6, ct)
  ▼
GetCashFlowService.ExecuteAsync(tenantId, 2026, 6, ct)
  │
  │  // 1. Busca config do tenant
  │  var config = await db.CashFlowConfigs
  │      .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct)
  │  → SQL: SELECT * FROM "CashFlowConfigs" WHERE "TenantId" = '000..001' LIMIT 1
  │
  │  // 2. Calcula saldo inicial
  │  // 3. Busca transações do mês
  │  // 4. Busca gastos fixos ativos
  │  // 5. Busca pagamentos do mês
  │  // 6. Monta os DTOs
  │
  │  return Result<CashFlowResponse>.Ok(new CashFlowResponse(...))
  ▼
GetCashFlowController.Get (volta)
  │  result.IsSuccess == true
  │  return Ok(result.Value)
  ▼
ASP.NET Core SERIALIZA o CashFlowResponse para JSON
  │  (enums viram strings, DateOnly vira "2026-06-01", etc.)
  ▼
RESPOSTA HTTP 200
  {
    "year": 2026,
    "month": 6,
    "isConfigured": true,
    "openingBalance": 5000.00,
    "totalIncome": 12000.00,
    "totalExpense": 9500.00,
    "closingBalance": 7500.00,
    "transactions": [...],
    "config": { "initialYear": 2026, "initialMonth": 1, "initialBalance": 0 }
  }
  ▼
FRONTEND (Angular) recebe e exibe
```

---

## Glossário Rápido

| Termo | Equivalente em PHP/Laravel | Explicação |
|-------|---------------------------|------------|
| `Solution` | Projeto Laravel | Agrupa múltiplos projetos |
| `Project (.csproj)` | Composer package | Unidade independente com suas dependências |
| `Namespace` | Namespace PHP | Organiza classes, evita conflitos |
| `DbContext` | Eloquent / Model base | Ponto de acesso ao banco |
| `DbSet<T>` | `Model::query()` | Representa uma tabela |
| `Migration` | Migration Laravel | Versiona o schema do banco |
| `[ApiController]` | Atributo de Route | Ativa funcionalidades de API REST |
| `[HttpGet]` | `Route::get()` | Define verbo HTTP |
| `[Authorize]` | `auth` middleware | Exige autenticação |
| `IServiceCollection` | Service Provider | Container de DI |
| `AddScoped` | bind no AppServiceProvider | Registra serviço com lifetime de request |
| `record` | DTO class | Tipo imutável ideal para transferência de dados |
| `async/await` | `async/await` JS | Operações não-bloqueantes |
| `CancellationToken` | AbortController (JS) | Cancela operações quando cliente desconecta |
| `Guid` | UUID | Identificador único |
| `DateOnly` | Carbon sem horário | Data sem horário (mapeado para `date` no PostgreSQL) |
| `decimal` | `DECIMAL` no MySQL | Ponto fixo — use para dinheiro |
| `Result<T>` | Não tem nativo | Padrão para retornar sucesso/erro sem exceptions |

---

## Próximos Conceitos para Estudar

Agora que você domina o que foi aplicado, os próximos passos naturais são:

1. **Row Level Security no PostgreSQL** — isolamento de tenant no nível do banco (Fase 5 do projeto)
2. **Hangfire Jobs** — como criar jobs agendados que rodam em background
3. **Polly** — retry policies para chamadas a APIs externas (já está no projeto para o HotmartClient)
4. **Testes unitários** — testar os Services isoladamente com mock do AppDbContext
5. **FluentValidation** — validação declarativa dos Requests
6. **SignalR** — WebSocket para atualização em tempo real do dashboard

---

*Gerado em Junho 2026 — Projeto VBBS Manager*
