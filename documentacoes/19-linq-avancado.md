# LINQ Avançado — GroupBy, Projeções, Dicionários e Parsing

> **Ordem de leitura:** documento **19** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [18-ef-core-avancado.md](./18-ef-core-avancado.md). Pré-requisito: [doc 04](./04-aula-dotnet-conceitos.md), seção 6 (LINQ básico).

---

## Contexto

O [doc 04](./04-aula-dotnet-conceitos.md) cobriu o LINQ básico: `Where`, `OrderBy`, `FirstOrDefaultAsync`, `ToListAsync`, `AnyAsync`. Neste documento, vamos para padrões mais complexos que aparecem no sync de dados da Meta Ads e no serviço de DRE.

---

## 1. Select — Projeção (transformar uma coleção em outra)

`Select` transforma cada item de uma coleção em outro item. É o equivalente ao `map` em JavaScript ou ao `array_map` no PHP.

### Transformando entidade em DTO

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsMonthSyncService.cs
var campaignEntities = campaignData.Select(d => MapCampaign(d, tenantId, now)).ToList();
//                     ↑ para cada MetaInsightData d, chama MapCampaign e retorna MetaCampaignDailyInsight
```

O `Select` recebe uma função que transforma `MetaInsightData` → `MetaCampaignDailyInsight`. O resultado é `IEnumerable<MetaCampaignDailyInsight>`. Chamando `.ToList()` materializa em `List<T>`.

### Select inline (sem método separado)

```csharp
db.MetaAdSetDailyInsights.AddRange(
    adSetData.Select(d => MapAdSet(d, tenantId, now)));
//            ↑ projeta cada item chamando MapAdSet — sem .ToList() porque AddRange aceita IEnumerable
```

### Select com índice

O `DashboardOverviewService` usa `Select` com índice para gerar "Sem. 01", "Sem. 02":

```csharp
// src/VBBSManager.Api/Features/Dashboard/Overview/DashboardOverviewService.cs
var weeklyPoints = dre.WeeklyEvolution
    .Select((w, i) => new DashboardWeekPoint(
        $"Sem. {(i + 1):D2}",   // i começa em 0, então +1; :D2 = 2 dígitos (01, 02...)
        w.Revenue,
        w.AdSpend,
        w.Margin))
    .ToList();
```

`Select((item, index) => ...)` — a segunda sobrecarrega recebe o índice atual.

---

## 2. GroupBy — Agrupando dados

`GroupBy` agrupa itens por uma chave comum. É o equivalente ao `GROUP BY` do SQL.

### Agrupando insights por data

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsMonthSyncService.cs
var dailySummaries = campaignEntities
    .GroupBy(c => c.Date)   // agrupa todos os insights pelo campo Date
    .Select(g => new DailyAdSpendSummary
    {
        TenantId         = tenantId,
        Date             = g.Key,            // g.Key = o valor pelo qual agrupou (DateOnly)
        TotalSpend       = g.Sum(c => c.Spend),        // soma de todos do grupo
        TotalImpressions = g.Sum(c => c.Impressions),
        TotalClicks      = g.Sum(c => c.Clicks),
        TotalConversions = g.Sum(c => c.Conversions),
        TotalRevenue     = g.Sum(c => c.Revenue),
        LastSyncedAt     = now,
    });
```

**O que acontece passo a passo:**

```
campaignEntities = [
    { Date: 2026-06-01, CampaignName: "Campanha A", Spend: 100, ... },
    { Date: 2026-06-01, CampaignName: "Campanha B", Spend: 200, ... },
    { Date: 2026-06-02, CampaignName: "Campanha A", Spend: 150, ... },
    { Date: 2026-06-02, CampaignName: "Campanha B", Spend: 250, ... },
]

.GroupBy(c => c.Date) → 2 grupos:
    grupo[2026-06-01] = [Campanha A, Campanha B]
    grupo[2026-06-02] = [Campanha A, Campanha B]

.Select(g => new DailyAdSpendSummary { TotalSpend = g.Sum(c => c.Spend) })
→ DailyAdSpendSummary{ Date: 2026-06-01, TotalSpend: 300 }
→ DailyAdSpendSummary{ Date: 2026-06-02, TotalSpend: 400 }
```

Cada campanha tem seus dados por dia — ao agrupar por `Date`, você agrega todas as campanhas em um único número diário.

---

## 3. Dictionary\<TKey, TValue\> — Mapa chave→valor

Dicionários são estruturas de dados que associam uma chave a um valor. Em C#:

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Hotmart/HotmartSalesService.cs
var revenueByCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
var feeByCurrency     = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
```

`Dictionary<string, decimal>` = mapa de "código da moeda" → "valor total".

`StringComparer.OrdinalIgnoreCase` = as chaves são comparadas sem diferenciar maiúsculas de minúsculas. "BRL" == "brl" == "Brl".

### Lendo e acumulando valores

```csharp
foreach (var item in items)
{
    var currency = fee.CurrencyCode;  // ex: "BRL"
    var grossAmount = item.Purchase?.Price?.Value ?? 0m;

    // GetValueOrDefault retorna 0 se a chave não existe (sem exception)
    revenueByCurrency[currency] = revenueByCurrency.GetValueOrDefault(currency) + grossAmount;
    //                 ↑ atualiza ou cria a entrada para "BRL"
    feeByCurrency[currency]     = feeByCurrency.GetValueOrDefault(currency)     + fee.Total;
}
```

**`GetValueOrDefault(key)`** — retorna o valor se a chave existe, ou o valor padrão do tipo (`0` para `decimal`) se não existe. Evita a exception `KeyNotFoundException` que `dict[key]` lançaria se a chave não existir.

### Iterando o resultado

```csharp
var totalRevenue = revenueByCurrency.Values.Sum();
var totalFees    = feeByCurrency.Values.Sum();

// Criando o relatório consolidado
return new SalesConsolidatedReport(totalSales, revenueByCurrency, feeByCurrency);
```

---

## 4. Spread Operator em Collections (C# 12)

O HotmartSalesService usa uma sintaxe nova para criar lista a partir de LINQ:

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Hotmart/HotmartSalesService.cs
return [.. byDay
    .Select(kv => new DailySaleData(kv.Key, kv.Value.sales, kv.Value.revenue, kv.Value.fee))
    .OrderBy(x => x.Date)];
```

O `[.. expression]` é o **spread operator** de collection expressions. Ele materializa o `IEnumerable` em uma lista. É equivalente a `.ToList()`, mas mais compacto quando está no return.

```csharp
// Equivalente mais verboso
return byDay
    .Select(kv => new DailySaleData(...))
    .OrderBy(x => x.Date)
    .ToList();
```

---

## 5. Parsing com TryParse e CultureInfo.InvariantCulture

Dados de APIs externas chegam como strings. Precisamos convertê-los para tipos numéricos com segurança.

### O problema com Parse direto

```csharp
// ❌ Lança exceção se o valor for null, vazio ou mal formatado
var spend = decimal.Parse(d.Spend);
```

### TryParse — conversão segura

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsMonthSyncService.cs
private static long ParseLong(string? v) =>
    long.TryParse(v, out var r) ? r : 0;
```

`TryParse` tenta converter e retorna `true/false`. Se falhar, você usa o valor padrão (`0`) em vez de lançar exceção.

### O problema com decimais e localização

Diferentes culturas escrevem decimais de formas diferentes:
- Brasil: `1.234,56` (ponto para milhar, vírgula para decimal)
- EUA: `1,234.56` (vírgula para milhar, ponto para decimal)
- APIs internacionais: sempre `1234.56` (sem milhar, ponto para decimal)

```csharp
// ❌ Errado — usa cultura do sistema operacional
decimal.Parse("1234.56");  // pode falhar se SO estiver em pt-BR

// ✅ Correto — força cultura invariante (ponto como separador decimal)
private static decimal ParseDecimal(string? v) =>
    decimal.TryParse(v,
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture,
        out var r) ? r : 0m;
```

`NumberStyles.Any` aceita qualquer formato numérico válido (com ou sem sinal, notação científica, etc.).

`CultureInfo.InvariantCulture` força o parser a usar ponto como separador decimal — padrão de APIs internacionais como Meta e Hotmart.

### Nullable decimal com TryParse

Para valores que podem legitimamente não existir (como `Cpc` — custo por clique pode ser null):

```csharp
private static decimal? ParseNullable(string? v) =>
    string.IsNullOrWhiteSpace(v) ? null : ParseDecimal(v);
```

Se a string for vazia ou null → retorna `null`. Caso contrário, converte para decimal.

---

## 6. LINQ com Where + Sum em memória vs no banco

No `DreService`, há uma função helper que opera em memória (não no banco):

```csharp
// src/VBBSManager.Api/Features/Financial/DRE/DreService.cs
private static decimal SumTx(
    IEnumerable<CashFlowTransaction> txs,
    TransactionType type,
    CashFlowCategory category)
    => txs.Where(t => t.Type == type && t.Category == category).Sum(t => t.Amount);
```

Esta função é chamada assim:

```csharp
var adSpend = SumTx(transactions, TransactionType.Expense, CashFlowCategory.MetaAds);
//                   ↑ já está em memória — não acessa o banco
```

Antes desta linha, `transactions` já foi carregado com `ToListAsync()`. Então o `Where` e `Sum` aqui são LINQ-to-Objects (em memória), não LINQ-to-SQL (no banco).

**Como diferenciar:** se a coleção é `IQueryable<T>` (DbSet, resultado de query não materializada), o LINQ vai para o banco. Se é `IEnumerable<T>` (resultado de `ToListAsync`, arrays, listas), o LINQ opera em memória.

---

## 7. DateTimeOffset e timestamps Unix

A Hotmart retorna datas como timestamps Unix em milissegundos:

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Hotmart/HotmartSalesService.cs
var dateMs = purchase.ApprovedDate ?? purchase.OrderDate ?? purchase.PurchaseDate;
// dateMs pode ser: 1748563200000 (milissegundos desde 01/01/1970 UTC)

if (dateMs is long ms)
{
    // Converte timestamp para DateOnly no fuso de São Paulo (UTC-3)
    var spDt = DateTimeOffset.FromUnixTimeMilliseconds(ms)
        .ToOffset(TimeSpan.FromHours(-3))  // converte para UTC-3 (Brasília)
        .Date;                              // extrai apenas a data (sem horário)
    day = DateOnly.FromDateTime(spDt);
}
```

**Por que usar horário de São Paulo?**

Se uma venda aconteceu às 23h UTC (20h BRT), ela deve aparecer no dia correto para o empresário — que opera no horário de Brasília. Sem a conversão, vendas noturnas apareceriam no dia seguinte.

**`DateTimeOffset.FromUnixTimeMilliseconds`** converte o número para um instante no tempo (com fuso UTC). `.ToOffset(TimeSpan.FromHours(-3))` converte para o fuso UTC-3.

---

## 8. Método estático vs de instância

No `DreService`, você verá muitos métodos `private static`:

```csharp
// ✅ static — não acessa nenhuma dependência da instância
private static decimal SumTx(IEnumerable<CashFlowTransaction> txs, ...) => ...
private static decimal ComputeProjection(int year, ...) => ...
private static List<DreDataPoint> BuildWeeklyEvolution(...) => ...
private static List<DreLineDto> BuildLines(...) => ...
private static DreLineDto Line(...) => ...
```

**Regra:** se o método não usa `this` (não acessa `db`, `logger`, nem outras propriedades da instância), declare-o `static`. Isso comunica claramente que o método é uma função pura — dado o mesmo input, sempre retorna o mesmo output.

O compilador C# também avisa se você tentar adicionar `static` em método que usa `this`, o que ajuda a identificar onde há acoplamento desnecessário.

---

## Resumo

| Conceito | Uso no projeto |
|---|---|
| `Select(item => ...)` | Transforma coleções — entidade → DTO, dados externos → entidade |
| `Select((item, index) => ...)` | Quando precisa do índice durante a transformação |
| `GroupBy(item => item.Key)` | Agrupa por campo; `.Key` acessa o valor agrupador |
| `.Sum(item => item.Field)` | Soma dentro de grupos ou coleções |
| `Dictionary<K,V>` | Mapa chave→valor; `GetValueOrDefault` evita exception |
| `[.. collection]` | Spread operator — materializa IEnumerable em lista |
| `decimal.TryParse` + `InvariantCulture` | Parsing seguro de decimais de APIs internacionais |
| `DateTimeOffset.FromUnixTimeMilliseconds` | Converte timestamp Unix para data no fuso correto |
| `private static` | Funções puras que não dependem de estado da instância |

---

*Próximo: [20-excecoes-customizadas.md](./20-excecoes-customizadas.md) — quando criar exceções próprias e como organizá-las.*
