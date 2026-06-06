# Feature: Página de Planejamento

Documentação técnica completa da feature de Planejamento Estratégico — construída do zero, com explicações para quem está aprendendo .NET junto com o projeto.

---

## O que foi construído

Uma página de planejamento estratégico que:

1. **Exibe** um dashboard com 9 seções (overview, DRE, plano de 12 semanas, etc.)
2. **Persiste** as metas do negócio no banco de dados por tenant
3. **Permite editar** qualquer meta via um painel lateral (offcanvas/drawer)
4. **Recalcula** o DRE automaticamente quando os dados financeiros mudam

Foram criados arquivos no **backend** (C# / .NET) e no **frontend** (Angular).

---

## Parte 1 — Entidades de Domínio

### O que é uma "entidade" no .NET?

Em .NET com Entity Framework Core, uma **entidade** é uma classe C# comum que representa uma tabela no banco de dados. Cada propriedade da classe vira uma coluna. Simples assim.

```csharp
// Esta classe vira a tabela "PlanningGoals" no PostgreSQL
public class PlanningGoal : BaseEntity
{
    public string Key { get; set; }        // coluna: Key (text)
    public string Name { get; set; }       // coluna: Name (text)
    public decimal TargetValue { get; set; } // coluna: TargetValue (numeric)
    // ...
}
```

### A BaseEntity — herança para não repetir código

Toda entidade do sistema herda de `BaseEntity`:

```csharp
// src/VBBSManager.Domain/Entities/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

**Por que usar herança aqui?**

Sem herança, toda entidade precisaria repetir `Id`, `TenantId`, `CreatedAt` e `UpdatedAt`. Com herança, você declara uma vez e herda em todas.

**O que cada propriedade faz:**

| Propriedade | Tipo | Por quê existe |
|---|---|---|
| `Id` | `Guid` | Chave primária única no banco. UUID ao invés de int porque facilita multi-tenancy e evita colisões entre tenants. |
| `TenantId` | `Guid` | **Pilar do multi-tenancy.** Garante que cada registro pertence a um tenant específico. Sem isso, tenant A poderia ver dados do tenant B. |
| `CreatedAt` | `DateTime` | Auditoria — quando foi criado. |
| `UpdatedAt` | `DateTime?` | O `?` significa **nullable** em C# — pode ser null se o registro nunca foi editado. |

**Por que `Guid` ao invés de `int` para o Id?**

Com `int`, os IDs seriam `1, 2, 3...` — sequenciais e previsíveis. Um usuário mal-intencionado poderia tentar acessar `GET /api/goals/1`, `GET /api/goals/2` etc. Com `Guid` (UUID), o ID seria algo como `3f2504e0-4f89-11d3-9a0c-0305e82c3301` — imprevisível.

### A entidade PlanningGoal

```csharp
// src/VBBSManager.Domain/Entities/PlanningGoal.cs
public class PlanningGoal : BaseEntity
{
    public string Key { get; set; }                   // "cpa_general", "weekly_revenue"...
    public string Name { get; set; }                  // "CPA geral (todas campanhas)"
    public string? Description { get; set; }          // Opcional
    public decimal TargetValue { get; set; }          // 42 (meta: CPA ≤ R$42)
    public decimal? CurrentValue { get; set; }        // Valor atual (vem de syncs de dados)
    public string Unit { get; set; }                  // "BRL", "percent", "count"...
    public PlanningGoalCategory Category { get; set; }
    public PlanningGoalComparison ComparisonType { get; set; }
    public string? ActionIfFailed { get; set; }       // O que fazer se não bater a meta
    public int SortOrder { get; set; }                // Ordem de exibição
}
```

**O que o `?` significa em `decimal?`?**

Em C#, por padrão, tipos de valor como `int`, `decimal`, `bool` não podem ser `null`. O `?` os torna **nullable** — permite que o valor seja null. Usamos `decimal?` para `CurrentValue` porque o valor atual só existe quando o sistema já sincronizou dados reais. No início, é `null`.

**O que são os Enums?**

```csharp
// src/VBBSManager.Domain/Enums/PlanningEnums.cs
public enum PlanningGoalCategory
{
    WeeklyAlert,      // Verificar toda sexta
    DailyTraffic,     // Verificar todo dia
    WeeklyFinancial,  // Verificar toda semana
    MonthlyGrowth     // Verificar todo mês
}

public enum PlanningGoalComparison
{
    GreaterThan,  // Meta é "maior que X" (ex: ROAS ≥ 2.0x)
    LessThan      // Meta é "menor que X" (ex: CPA ≤ R$42)
}
```

Um **enum** é uma lista de constantes nomeadas. Ao invés de salvar o texto `"WeeklyAlert"` no banco (propenso a typos), salvamos um número inteiro (`0`, `1`, `2`, `3`) que o C# mapeia automaticamente para o nome. Mais seguro e eficiente.

No nosso caso, configuramos para salvar **como string** (`"WeeklyAlert"`) porque facilita debugging direto no banco. Isso é feito na configuração do EF (ver Parte 3).

### A entidade FinancialConfig

```csharp
// src/VBBSManager.Domain/Entities/FinancialConfig.cs
public class FinancialConfig : BaseEntity
{
    public decimal MonthlyGrossRevenue { get; set; }      // Faturamento bruto/mês
    public decimal MonthlyAdSpend { get; set; }           // Verba mensal de tráfego
    public decimal HotmartFeePercent { get; set; }        // Ex: 0.09 = 9%
    public decimal InstallmentFeePercent { get; set; }    // 0.0219 = 2,19%
    public decimal InstallmentSalesPercent { get; set; }  // 0.33 = 33% das vendas no cartão
    public decimal FederalTaxPercent { get; set; }        // 0.06 = 6%
    public decimal RefundRatePercent { get; set; }        // 0.01 = 1%
    public decimal MetaAdsTaxPercent { get; set; }        // 0.10 = 10%
    public decimal AccountingCost { get; set; }           // R$400/mês
    public decimal InvoicingCost { get; set; }            // R$250/mês
    public decimal ManychatCost { get; set; }             // R$448/mês
    public decimal HotmartPlayerCost { get; set; }        // R$69/mês
}
```

Essa entidade guarda os **inputs do DRE**. Com esses valores, o frontend pode calcular em tempo real:
- Receita líquida = Faturamento − taxas − impostos − reembolsos
- Margem após tráfego = Receita líquida − (AdSpend × (1 + MetaAdsTax))
- Lucro operacional = Margem após tráfego − custos fixos

**Por que os percentuais estão como decimais (0.09) e não como inteiros (9)?**

Por convenção matemática: multiplicar por `0.09` é mais direto do que fazer `valor * 9 / 100`. Ao exibir no frontend, o Angular faz a conversão para exibir `9%`.

---

## Parte 2 — Entity Framework Core e Migrations

### O que é o Entity Framework Core (EF Core)?

O EF Core é um **ORM** (Object-Relational Mapper). Ele resolve o problema de tradução entre dois mundos:

- **Mundo C#**: você trabalha com classes, objetos e LINQ
- **Mundo SQL**: o banco de dados entende apenas tabelas, linhas e SQL

Sem EF Core, você escreveria:
```sql
INSERT INTO planning_goals (id, tenant_id, key, name, target_value, ...)
VALUES (@id, @tenantId, @key, @name, @targetValue, ...);
```

Com EF Core, você escreve:
```csharp
db.PlanningGoals.Add(new PlanningGoal { Key = "cpa_general", TargetValue = 42 });
await db.SaveChangesAsync();
```

O EF Core converte automaticamente o objeto C# para o SQL correto.

### O que são Migrations?

Uma **migration** é um arquivo C# que descreve **como o banco de dados deve mudar**.

Pense assim: o banco começa vazio. Cada migration é um passo que adiciona, remove ou altera tabelas e colunas. O EF Core mantém um histórico de quais migrations já foram aplicadas na tabela `__EFMigrationsHistory`.

```
Migration 1: InitialCreate → cria as tabelas Tenants, Users, RefreshTokens, Integrations, Alerts, PlanningGoals, FinancialConfigs
Migration 2: AddIndexes     → adiciona índices extras (hipotético)
Migration 3: ...
```

### Como foi criada a migration neste projeto

O comando normal é:
```bash
dotnet ef migrations add InitialCreate \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Esse comando **lê** todas as entidades e configurações do EF e **gera automaticamente** os arquivos de migration. Como o `dotnet` CLI não estava disponível no ambiente durante o desenvolvimento, os arquivos foram criados **manualmente**.

### Anatomia de uma Migration

A migration `20260605120000_InitialCreate.cs` tem dois métodos:

```csharp
public partial class InitialCreate : Migration
{
    // Roda quando você aplica a migration (vai para frente)
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PlanningGoals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TargetValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                // ... todas as outras colunas
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlanningGoals", x => x.Id);
            });

        // Cria índice único: (TenantId, Key)
        migrationBuilder.CreateIndex(
            name: "IX_PlanningGoals_TenantId_Key",
            table: "PlanningGoals",
            columns: new[] { "TenantId", "Key" },
            unique: true);
    }

    // Roda quando você desfaz a migration (vai para trás)
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PlanningGoals");
        // ...
    }
}
```

**Por que o tipo `numeric(18,4)` para decimais?**

`numeric(18, 4)` significa: até 18 dígitos no total, com 4 casas decimais. Isso garante precisão para valores monetários como `12.370,0000` sem perda de arredondamento (problema comum com `float`/`double`).

**Por que o índice único em `(TenantId, Key)`?**

Cada meta tem uma `Key` (ex: `"cpa_general"`). Não faz sentido ter duas metas `cpa_general` para o mesmo tenant. O índice único no banco **garante** isso, independente de qualquer validação na aplicação. É uma segunda camada de segurança.

### Os outros arquivos de migration

Além do arquivo principal, existem dois outros:

**`20260605120000_InitialCreate.Designer.cs`**

É o "passaporte" da migration. Contém um snapshot do estado do modelo no momento em que a migration foi criada. O EF Core usa isso para saber o que já existe antes de gerar a próxima migration.

**`AppDbContextModelSnapshot.cs`**

É o "estado atual" do modelo segundo o EF Core. Sempre reflete a migration mais recente aplicada. Quando você roda `dotnet ef migrations add`, o EF compara o snapshot com as entidades atuais e gera apenas o diff.

### Como aplicar a migration no banco

Com o banco rodando (via Docker Compose):

```bash
dotnet ef database update \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Isso cria as tabelas no PostgreSQL. Você pode verificar no pgAdmin em `localhost:8080`.

---

## Parte 3 — Configurações do EF Core (Fluent API)

### O que é Fluent API?

Por padrão, o EF Core infere as configurações das entidades pelo nome das propriedades. Mas às vezes você precisa ser mais específico: definir tamanho máximo de texto, precisão de decimais, converter enums para string, etc.

A **Fluent API** é o jeito "fluente" (encadeado) de configurar isso em C#. Fica em classes separadas que implementam `IEntityTypeConfiguration<T>`.

### PlanningGoalConfiguration

```csharp
// src/VBBSManager.Infrastructure/Persistence/Configurations/PlanningGoalConfiguration.cs
public class PlanningGoalConfiguration : IEntityTypeConfiguration<PlanningGoal>
{
    public void Configure(EntityTypeBuilder<PlanningGoal> builder)
    {
        // Chave primária
        builder.HasKey(g => g.Id);

        // Limitar tamanho de strings (evita salvar textos enormes acidentalmente)
        builder.Property(g => g.Key).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(500);

        // Precisão dos decimais
        builder.Property(g => g.TargetValue).HasPrecision(18, 4);
        builder.Property(g => g.CurrentValue).HasPrecision(18, 4);

        // Salvar enums como string no banco
        // Ao invés de salvar 0, 1, 2... salva "WeeklyAlert", "DailyTraffic"...
        builder.Property(g => g.Category).HasConversion<string>();
        builder.Property(g => g.ComparisonType).HasConversion<string>();

        // Índices
        builder.HasIndex(g => new { g.TenantId, g.Key }).IsUnique(); // Não permite duplicar Key por tenant
        builder.HasIndex(g => g.TenantId);                           // Acelera queries por tenant
    }
}
```

**Por que `HasConversion<string>()` para os enums?**

Sem essa configuração, o EF salvaria `0`, `1`, `2` no banco. Se você precisar olhar o banco diretamente para debug, veria números sem contexto. Com `HasConversion<string>()`, vê `"WeeklyAlert"` — autoexplicativo.

**Por que separar a configuração em classes distintas?**

Por organização. Se todas as configurações ficassem no `OnModelCreating` do `AppDbContext`, esse método teria centenas de linhas. Uma classe por entidade é mais limpo e fácil de navegar.

O `AppDbContext` usa este método para descobrir todas as configurações automaticamente:
```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```
Isso varre a assembly inteira em busca de classes que implementam `IEntityTypeConfiguration<T>` e aplica todas elas. Você não precisa registrar manualmente.

---

## Parte 4 — O AppDbContext

### O que é o DbContext?

O `AppDbContext` é o **coração** da camada de dados. Ele representa a "sessão de banco de dados" — uma conexão ativa com o PostgreSQL que você usa para consultar, inserir, atualizar e deletar dados.

```csharp
// src/VBBSManager.Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Cada DbSet = uma tabela que você pode consultar
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<PlanningGoal> PlanningGoals => Set<PlanningGoal>();        // NOVO
    public DbSet<FinancialConfig> FinancialConfigs => Set<FinancialConfig>(); // NOVO

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### O que são os DbSets?

Um `DbSet<T>` é uma "janela" para uma tabela do banco. Quando você escreve:

```csharp
var goals = await db.PlanningGoals
    .Where(g => g.TenantId == tenantId)
    .ToListAsync();
```

O EF Core traduz isso para:
```sql
SELECT * FROM "PlanningGoals" WHERE "TenantId" = @tenantId
```

O `DbSet` expõe métodos como `.Where()`, `.FirstOrDefault()`, `.Add()`, `.Remove()` — todos do LINQ, a linguagem de queries do C#.

### Primary constructor — `(DbContextOptions<AppDbContext> options)`

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
```

Isso é um **primary constructor** do C# 12. É equivalente a:
```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
```

O `options` contém as configurações de conexão (string de conexão, provedor PostgreSQL, etc.) que são injetadas pelo sistema de DI do .NET.

---

## Parte 5 — Vertical Slice Architecture: a Feature Planning

### O que é Vertical Slice?

A arquitetura tradicional organiza por **camada técnica**:
```
Controllers/
  GoalsController.cs
  FinancialController.cs
Services/
  GoalsService.cs
  FinancialService.cs
DTOs/
  GoalResponse.cs
  FinancialRequest.cs
```

O problema: para entender o fluxo "GET /api/planning/goals", você navega entre 3 pastas diferentes.

**Vertical Slice** organiza por **funcionalidade** (feature):
```
Features/
  Planning/
    Goals/
      Get/
        GetPlanningGoalsController.cs   ← recebe o HTTP
        GetPlanningGoalsService.cs      ← lógica de negócio
        PlanningGoalResponse.cs         ← DTO de resposta
      Update/
        UpdatePlanningGoalsController.cs
        UpdatePlanningGoalsService.cs
        UpdatePlanningGoalRequest.cs
```

Para entender o fluxo de "GET goals", tudo está na mesma pasta. Menos navegação, mais foco.

### DTOs — Data Transfer Objects

Um **DTO** é um objeto que define exatamente o que vai entrar ou sair da API. **Nunca expose a entidade diretamente** — isso quebraria o encapsulamento e exporia campos internos.

**Por que usar `record` ao invés de `class`?**

```csharp
// DTO de resposta
public record PlanningGoalResponse(
    Guid Id,
    string Key,
    string Name,
    decimal TargetValue,
    decimal? CurrentValue,
    string Unit,
    string Category,
    string ComparisonType,
    string? ActionIfFailed,
    int SortOrder
);
```

`record` é um tipo C# imutável por padrão. Uma vez criado, não pode ser alterado. Para DTOs isso é perfeito — você cria, serializa para JSON e manda embora. Não precisa mutar. Além disso, `record` tem implementação automática de `Equals`, `ToString` e `GetHashCode` com base nos valores.

### O Controller

```csharp
// Features/Planning/Goals/Get/GetPlanningGoalsController.cs
[ApiController]
[Route("api/planning")]
[Authorize]              // ← Requer autenticação JWT
public class GetPlanningGoalsController(IGetPlanningGoalsService service) : ControllerBase
{
    [HttpGet("goals")]
    [ProducesResponseType(typeof(PlanningGoalsListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;  // ← vem do TenantMiddleware
        var result = await service.ExecuteAsync(tenantId, ct);
        if (!result.IsSuccess)
            return StatusCode(500, new { error = result.Error });
        return Ok(result.Value);
    }
}
```

**Responsabilidade do Controller: APENAS 3 coisas**
1. Receber a requisição HTTP
2. Delegar para o Service
3. Retornar a resposta

Nenhuma lógica de negócio fica aqui.

**O que é `[Authorize]`?**
Diz ao ASP.NET que este endpoint só pode ser acessado com um JWT válido no header `Authorization: Bearer <token>`. Sem isso, qualquer pessoa poderia acessar.

**O que é `CancellationToken ct`?**
Permite cancelar operações assíncronas. Se o usuário fechar o browser no meio de uma requisição longa, o .NET sinaliza o token e o banco cancela a query em andamento. Economiza recursos do servidor.

**Por que `IGetPlanningGoalsService` (interface) e não `GetPlanningGoalsService` (implementação)?**

Porque o Controller **não deve saber como** a lógica é implementada — só precisa saber **que** existe uma forma de buscar goals. Isso facilita testes unitários (você pode injetar um mock da interface) e troca de implementação sem alterar o Controller.

### O Service

```csharp
// Features/Planning/Goals/Get/GetPlanningGoalsService.cs
public class GetPlanningGoalsService(AppDbContext db, ILogger<GetPlanningGoalsService> logger)
    : IGetPlanningGoalsService
{
    public async Task<Result<PlanningGoalsListResponse>> ExecuteAsync(Guid tenantId, CancellationToken ct)
    {
        var goals = await db.PlanningGoals
            .Where(g => g.TenantId == tenantId)       // Filtra SEMPRE por tenant
            .OrderBy(g => g.Category)
            .ThenBy(g => g.SortOrder)
            .ToListAsync(ct);

        if (goals.Count == 0)
        {
            goals = BuildDefaults(tenantId);    // ← Lazy seeding (ver Parte 6)
            db.PlanningGoals.AddRange(goals);
            await db.SaveChangesAsync(ct);
        }

        var response = new PlanningGoalsListResponse(
            goals.Select(g => new PlanningGoalResponse(...)).ToList()
        );

        return Result<PlanningGoalsListResponse>.Ok(response);
    }
}
```

**O que é `async/await`?**

Operações de banco de dados são **I/O-bound** — o programa fica esperando o disco/rede responder. Com `async/await`, enquanto espera, a thread é liberada para atender outras requisições. Sem async, cada requisição prenderia uma thread e o servidor travaria com poucas conexões simultâneas.

**O que é `.Select()` e `.ToList()`?**

- `.Where(g => g.TenantId == tenantId)` — filtra (como `WHERE` no SQL)
- `.OrderBy(g => g.Category).ThenBy(g => g.SortOrder)` — ordena
- `.Select(g => new PlanningGoalResponse(...))` — transforma cada entidade em DTO (como `SELECT ... AS` no SQL)
- `.ToListAsync()` — executa a query e traz os resultados

O EF Core monta o SQL completo apenas quando `.ToListAsync()` é chamado — antes disso, é apenas uma declaração de intenção (chamado de **deferred execution**).

### O padrão Result\<T\>

```csharp
// O Service não lança exceção — retorna Result<T>
return Result<PlanningGoalsListResponse>.Ok(response);   // sucesso
return Result<PlanningGoalsListResponse>.Fail("Erro X");  // falha esperada
```

```csharp
// O Controller verifica o resultado
var result = await service.ExecuteAsync(...);
if (!result.IsSuccess)
    return StatusCode(500, new { error = result.Error });
return Ok(result.Value);
```

**Por que não usar `throw new Exception()`?**

Exceções são para situações **inesperadas** (banco caiu, memória cheia). Para falhas de negócio esperadas ("não encontrado", "já existe"), é melhor retornar um `Result<T>` — é mais explícito, mais fácil de testar e não tem overhead de stack trace.

---

## Parte 6 — Lazy Seeding

### O que é "lazy seeding"?

Quando um novo tenant acessa a feature de planejamento pela primeira vez, o banco não tem dados ainda. Em vez de criar dados iniciais no momento da instalação do sistema, usamos **lazy seeding** — criamos os dados padrão na **primeira vez que o endpoint é chamado** pelo tenant.

```csharp
// Em GetPlanningGoalsService.ExecuteAsync:
var goals = await db.PlanningGoals
    .Where(g => g.TenantId == tenantId)
    .ToListAsync(ct);

if (goals.Count == 0)                    // ← Primeiro acesso
{
    goals = BuildDefaults(tenantId);     // ← Cria os 16 goals padrão
    db.PlanningGoals.AddRange(goals);    // ← Adiciona ao contexto EF
    await db.SaveChangesAsync(ct);       // ← Persiste no banco
    logger.LogInformation("Seeded...");
}
```

**Por que não usar `HasData()` no OnModelCreating?**

`HasData()` é outra forma de seed, mas tem uma limitação crítica: os dados precisam ter IDs fixos hardcodados no código. Em um sistema multi-tenant onde cada tenant precisa de seus próprios registros (com TenantId diferente), isso é inviável. O lazy seeding resolve isso elegantemente.

**Os 16 goals padrão**

O método `BuildDefaults()` cria os valores iniciais baseados no planejamento do negócio:

```csharp
private static List<PlanningGoal> BuildDefaults(Guid tenantId)
{
    return [
        // WeeklyAlert
        Goal(tenantId, "weekly_revenue", "Faturamento semanal", 2500, "BRL",
             WeeklyAlert, GreaterThan, "Se < R$2.500: revisar criativos", 1),
        Goal(tenantId, "cpa_general", "CPA geral", 42, "BRL",
             WeeklyAlert, LessThan, "Se > R$50: matar criativos fracos", 3),
        // ... mais 14 goals
    ];
}
```

A sintaxe `[...]` é uma **collection expression** do C# 12 — forma moderna de criar listas.

---

## Parte 7 — Registro de Services (Injeção de Dependência)

### O que é Injeção de Dependência?

Injeção de Dependência (DI) é um padrão onde você **não cria** objetos diretamente com `new`. Em vez disso, você **declara que precisa** de um objeto, e o framework o fornece automaticamente.

**Sem DI (problemático):**
```csharp
public class GetPlanningGoalsController : ControllerBase
{
    private readonly AppDbContext _db = new AppDbContext(...); // ← ruim: acoplado
    private readonly GetPlanningGoalsService _service = new GetPlanningGoalsService(_db); // ← ruim
}
```

**Com DI (correto):**
```csharp
public class GetPlanningGoalsController(IGetPlanningGoalsService service) : ControllerBase
{
    // O .NET cria e injeta o service automaticamente. Você só declara que precisa dele.
}
```

### Onde registrar

```csharp
// src/VBBSManager.Api/Common/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddFeatureServices(this IServiceCollection services)
{
    // ... services existentes ...

    // Planning — adicionados nesta feature
    services.AddScoped<IGetPlanningGoalsService, GetPlanningGoalsService>();
    services.AddScoped<IUpdatePlanningGoalsService, UpdatePlanningGoalsService>();
    services.AddScoped<IGetFinancialConfigService, GetFinancialConfigService>();
    services.AddScoped<IUpdateFinancialConfigService, UpdateFinancialConfigService>();

    return services;
}
```

Isso diz ao .NET: "quando alguém pedir um `IGetPlanningGoalsService`, crie e entregue um `GetPlanningGoalsService`".

### AddScoped vs AddTransient vs AddSingleton

| Lifetime | Quando usar | Duração |
|---|---|---|
| `AddScoped` | **Services de negócio com acesso ao banco** — uma instância por requisição HTTP. O `AppDbContext` é scoped, então o service que o usa também deve ser. | Por requisição |
| `AddTransient` | Objetos leves sem estado, como validadores simples. | Nova instância toda vez que pedido |
| `AddSingleton` | Configurações, caches, clientes HTTP (cuidado com thread safety). | Uma instância para toda a aplicação |

**Por que services de negócio são `Scoped`?**

O `AppDbContext` é `Scoped` — uma instância por requisição. Se o seu service fosse `Singleton`, ele sobreviveria entre requisições e poderia tentar usar um `DbContext` já descartado (disposed). `Scoped` garante que o service e o contexto têm o mesmo ciclo de vida.

---

## Parte 8 — O Frontend Angular

### A estrutura do componente

```
features/planning/
  planning.component.ts    ← lógica e dados
  planning.component.html  ← template (HTML)
  planning.component.scss  ← estilos
```

Registrado nas rotas em `app.routes.ts`:
```typescript
{
  path: 'planning',
  loadComponent: () =>
    import('./features/planning/planning.component')
      .then(m => m.PlanningComponent)
}
```

O `loadComponent` com `import()` dinâmico cria um **lazy chunk** — o arquivo do componente só é baixado pelo browser quando o usuário navegar para `/planning`. Isso acelera o carregamento inicial da aplicação.

### Signals — o estado reativo do Angular 17+

```typescript
// Em vez de variáveis comuns:
goals: PlanningGoal[] = [];       // ← não é reativo

// Usamos Signals:
readonly goals = signal<PlanningGoal[]>([]);  // ← reativo
```

Um **Signal** é um container de valor que **notifica** o Angular quando muda. O Angular sabe exatamente quais partes do template precisam ser re-renderizadas, sem precisar verificar tudo.

**Como ler e escrever um Signal:**
```typescript
// Ler (no TypeScript)
const lista = this.goals();          // chama a função

// Ler (no template HTML)
{{ goals() }}                        // também chama

// Escrever
this.goals.set([...dados]);         // substitui o valor
this.goals.update(g => [...g, novo]); // transforma o valor atual
```

### Computed — valores derivados

```typescript
readonly dre = computed<DreResult | null>(() => {
    const cfg = this.financialConfig();  // lê o signal
    if (!cfg) return null;
    return this.computeDre(cfg);         // calcula o DRE
});
```

`computed()` cria um Signal **derivado** — seu valor é calculado automaticamente sempre que os Signals que ele lê mudam. Se `financialConfig` mudar, `dre` é recalculado automaticamente, e qualquer parte do template que usa `dre()` é atualizada.

Isso elimina a necessidade de chamar manualmente "recalcule o DRE quando o config mudar".

### O cálculo do DRE no frontend

```typescript
private computeDre(cfg: FinancialConfig): DreResult {
    const gross = cfg.monthlyGrossRevenue;

    const hotmartFee      = gross * cfg.hotmartFeePercent;              // 9%
    const installmentFee  = gross * cfg.installmentSalesPercent         // 33% no cartão
                                  * cfg.installmentFeePercent;          // × 2,19%
    const federalTax      = gross * cfg.federalTaxPercent;              // 6%
    const refundCost      = gross * cfg.refundRatePercent;              // 1%

    const netRevenue      = gross - hotmartFee - installmentFee
                                  - federalTax - refundCost;

    const adSpendWithTax  = cfg.monthlyAdSpend * (1 + cfg.metaAdsTaxPercent); // 10% Meta

    const marginAfterTraffic = netRevenue - adSpendWithTax;

    const fixedCosts      = cfg.accountingCost + cfg.invoicingCost
                          + cfg.manychatCost + cfg.hotmartPlayerCost;

    const operationalProfit = marginAfterTraffic - fixedCosts;
    const marginPercent     = (operationalProfit / gross) * 100;

    return { grossRevenue: gross, hotmartFee, ... };
}
```

O DRE é calculado **100% no frontend**, sem necessidade de requisição ao backend. Os dados já estão no signal `financialConfig`. Quando o usuário edita qualquer campo no Drawer e salva, o signal é atualizado e o DRE re-renderiza instantaneamente.

### O Drawer (offcanvas)

```html
<p-drawer
  [visible]="drawerVisible()"
  (visibleChange)="drawerVisible.set($event)"
  header="Editar Metas do Negócio"
  position="right"
>
  <!-- formulário dentro -->
</p-drawer>
```

**Por que `[visible]="drawerVisible()"` e não `[(visible)]="drawerVisible"`?**

`drawerVisible` é um Signal — uma função. `[(visible)]="drawerVisible"` passaria a *função* para o input, não o *valor* (que é sempre truthy). O padrão correto com Signals é:
- `[visible]="drawerVisible()"` — lê o valor atual do Signal
- `(visibleChange)="drawerVisible.set($event)"` — atualiza o Signal quando o Drawer fecha

### O `p-inputnumber` no formulário do Drawer

```html
<p-inputnumber
  [ngModel]="getGoalEditItem(goal.id)?.targetValue"
  (ngModelChange)="updateGoalTarget(goal.id, $event)"
  [prefix]="goal.unit === 'BRL' ? 'R$ ' : ''"
  [suffix]="goal.unit === 'percent' ? '%' : ''"
/>
```

- `[ngModel]` — lê o valor atual (one-way binding: dado → campo)
- `(ngModelChange)` — quando o usuário digita, chama a função para atualizar o Signal
- `prefix/suffix` — formatação visual (`R$ 42,00` ou `42%`)

Por que não usar `[(ngModel)]` (two-way binding)? Porque os dados ficam num Signal (imutável por natureza), não numa variável diretamente mutável. Precisamos chamar `.update()` explicitamente.

---

## Parte 9 — Fluxo completo de uma requisição

Para fixar, veja o caminho de **"usuário abre a página de planejamento"**:

```
1. Browser acessa /planning
   └── Angular carrega PlanningComponent (lazy)

2. ngOnInit() chama planningService.getGoals()
   └── PlanningService faz GET /api/planning/goals
       com header: Authorization: Bearer <token>

3. No backend:
   a. ExceptionMiddleware envolve tudo em try/catch
   b. JwtBearer valida o token → extrai claims
   c. TenantMiddleware lê o tenant_id do token
      → salva em HttpContext.Items["TenantId"]
   d. GetPlanningGoalsController.GetGoals() é chamado
   e. Pega tenantId do HttpContext.Items
   f. Chama service.ExecuteAsync(tenantId, ct)
   g. Service faz query: SELECT * FROM PlanningGoals WHERE TenantId = @tenantId
   h. Se vazio → insere 16 defaults → salva no banco
   i. Retorna Result<PlanningGoalsListResponse>.Ok(response)
   j. Controller retorna HTTP 200 com JSON

4. Angular recebe o JSON
   └── goals.set(response.goals)  → Signal atualizado

5. Angular detecta mudança no Signal
   └── Re-renderiza o template com os dados reais
       (tab Métricas exibe as 16 metas com status verde/vermelho)

6. O computed dre() recalcula automaticamente
   └── Tab DRE exibe o demonstrativo atualizado
```

---

## Parte 10 — Como rodar e testar

### 1. Subir o banco de dados

```bash
cd "VBBS Manager/API"
docker compose up -d postgres
```

Aguardar o healthcheck ficar verde (o banco aceita conexões).

### 2. Aplicar a migration

Abrir um terminal **com dotnet disponível** (terminal do Rider/VS Code):

```bash
dotnet ef database update \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Isso cria as 7 tabelas no PostgreSQL. Você pode verificar no pgAdmin:
`localhost:8080` → Database `vbbs_manager_dev` → Tables

### 3. Subir a API

```bash
dotnet run --project src/VBBSManager.Api
```

API sobe em `http://localhost:5000`.

### 4. Testar os endpoints

**Fazer login** para obter um token JWT:
```bash
POST http://localhost:5000/api/auth/login
{ "email": "...", "password": "..." }
```

**Buscar goals** (substitua o token):
```bash
GET http://localhost:5000/api/planning/goals
Authorization: Bearer eyJ...
```

Primeira chamada: insere 16 goals padrão e retorna.

**Atualizar uma meta:**
```bash
PUT http://localhost:5000/api/planning/goals
Authorization: Bearer eyJ...
{
  "goals": [
    { "id": "...", "targetValue": 38, "currentValue": 45 }
  ]
}
```

### 5. Subir o frontend

```bash
cd "VBBS Manager/WEB"
npm start
```

Acessar `http://localhost:4200` → navegar para "Planejamento" no menu lateral.

---

## Resumo dos arquivos criados

### Backend (`/API`)

| Arquivo | O que faz |
|---|---|
| `Domain/Entities/PlanningGoal.cs` | Entidade que mapeia para a tabela `PlanningGoals` |
| `Domain/Entities/FinancialConfig.cs` | Entidade que mapeia para `FinancialConfigs` |
| `Domain/Enums/PlanningEnums.cs` | Enums de categoria e tipo de comparação |
| `Infrastructure/Configurations/PlanningGoalConfiguration.cs` | Fluent API: tamanhos, precisões, índices |
| `Infrastructure/Configurations/FinancialConfigConfiguration.cs` | Fluent API: precisões, índice único por tenant |
| `Infrastructure/Migrations/20260605120000_InitialCreate.cs` | Migration: cria todas as 7 tabelas |
| `Infrastructure/Migrations/..Designer.cs` | Snapshot da migration para o EF Core |
| `Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Estado atual do modelo (usado para gerar próximas migrations) |
| `Features/Planning/Goals/Get/*` | GET /api/planning/goals |
| `Features/Planning/Goals/Update/*` | PUT /api/planning/goals |
| `Features/Planning/Financial/Get/*` | GET /api/planning/financial-config |
| `Features/Planning/Financial/Update/*` | PUT /api/planning/financial-config |

### Frontend (`/WEB`)

| Arquivo | O que faz |
|---|---|
| `shared/models/planning.models.ts` | Interfaces TypeScript espelhando os DTOs do backend |
| `core/services/planning.service.ts` | Calls HTTP para a API de planejamento |
| `features/planning/planning.component.ts` | Lógica: Signals, computed DRE, abertura do Drawer, save |
| `features/planning/planning.component.html` | Template: 9 tabs + Drawer com formulário |
| `features/planning/planning.component.scss` | Estilos |
| `app.routes.ts` | Rota `/planning` adicionada (lazy loaded) |
| `layout/shell/shell.component.ts` | Item "Planejamento" adicionado ao menu lateral |
