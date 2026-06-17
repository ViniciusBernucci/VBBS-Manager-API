# Integração Meta Ads — Estudo de Caso Completo

> **Ordem de leitura:** documento **21** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [20-excecoes-customizadas.md](./20-excecoes-customizadas.md). Pré-requisitos: docs 15, 16, 17, 18, 19 e 20.

---

## Visão geral da integração

A integração com o Meta Ads sincroniza métricas de campanhas publicitárias do Facebook/Instagram para o banco de dados local. Isso permite calcular KPIs como CPA, ROAS e gasto total sem depender de acesso online à API do Meta.

**Arquivos envolvidos:**

```
src/VBBSManager.Infrastructure/ExternalClients/Meta/
    MetaSettings.cs           ← configuração (IOptions<T>)
    MetaDtos.cs               ← DTOs de request/response da API
    MetaExceptions.cs         ← hierarquia de exceções
    MetaAdsClient.cs          ← client HTTP (Typed Client + Polly)
    MetaAdsMonthSyncService.cs ← orquestração do sync
src/VBBSManager.Infrastructure/Jobs/
    MetaAdsSyncJob.cs         ← job Hangfire agendado
src/VBBSManager.Api/Features/Traffic/
    Sync/TrafficSyncController.cs  ← endpoint de sync manual
    Sync/TrafficSyncService.cs     ← service que chama o sync
    Overview/TrafficOverviewService.cs
```

---

## Parte 1 — A Estrutura da API do Meta Ads

### A hierarquia Campaign → AdSet → Ad

O Meta Ads organiza as campanhas em 3 níveis hierárquicos:

```
Campanha (Campaign)
    → Conjunto de Anúncios (Ad Set)
        → Anúncio (Ad)
```

- **Campaign** — objetivo de marketing, orçamento total, período
- **AdSet** — segmentação de público, posicionamentos, bid
- **Ad** — criativo (imagem/vídeo), copy, chamada para ação

A API do Meta retorna insights por nível. Para saber o gasto total da campanha, você consulta no nível `campaign`. Para ver qual criativo específico teve melhor CPA, você consulta no nível `ad`.

### A constante MetaInsightLevel

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsClient.cs
public static class MetaInsightLevel
{
    public const string Campaign = "campaign";
    public const string AdSet    = "adset";
    public const string Ad       = "ad";
}
```

`public static class` sem instância — é apenas um contêiner de constantes. `const string` são constantes compiladas — o compilador substitui o uso da constante pelo valor literal em compile time.

Usar constantes em vez de strings literais evita typos: se você digitar `"campaing"` (com typo), o compilador não avisa. Se você usar `MetaInsightLevel.Campaign`, o compilador avisa imediatamente.

### Campos por nível

```csharp
private const string BaseFields =
    "campaign_id,campaign_name,impressions,clicks,reach,spend,cpc,cpm,ctr,frequency,actions,action_values";

private static readonly Dictionary<string, string> LevelFields = new()
{
    [MetaInsightLevel.Campaign] = BaseFields,
    [MetaInsightLevel.AdSet]    = $"adset_id,adset_name,{BaseFields}",
    [MetaInsightLevel.Ad]       = $"adset_id,adset_name,ad_id,ad_name,{BaseFields}",
};
```

O nível `campaign` retorna apenas campos de campanha. O nível `ad` retorna todos os campos (campanha + conjunto + anúncio). Cada chamada especifica quais campos quer — a API só retorna o que foi pedido.

`static readonly Dictionary` — diferente de `const` (que só aceita tipos primitivos), `readonly` é para tipos complexos. `static readonly` significa que o dicionário é criado uma vez, na inicialização da classe, e nunca substituído.

---

## Parte 2 — Os DTOs (MetaDtos.cs)

Os DTOs descrevem a estrutura JSON que a API retorna. Usamos `record` com `[JsonPropertyName]` para mapear os nomes snake_case da API para PascalCase do C#:

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaDtos.cs
public record MetaInsightData(
    [property: JsonPropertyName("campaign_id")]   string? CampaignId,
    [property: JsonPropertyName("campaign_name")] string? CampaignName,
    [property: JsonPropertyName("adset_id")]      string? AdSetId,
    // ...
    [property: JsonPropertyName("impressions")]   string? Impressions,  // ← string, não int!
    [property: JsonPropertyName("spend")]         string? Spend,        // ← string, não decimal!
    [property: JsonPropertyName("actions")]       List<MetaAction>? Actions,
    [property: JsonPropertyName("date_start")]    string DateStart);
```

**Por que `string?` para números?**

A API do Meta retorna métricas numéricas como strings JSON: `"impressions": "12345"` em vez de `"impressions": 12345`. Isso é uma peculiaridade da API. Por isso, toda conversão para `long`, `int` ou `decimal` acontece em `MetaAdsMonthSyncService` usando os métodos `ParseLong`, `ParseDecimal` e `ParseNullable` (explicados no [doc 19](./19-linq-avancado.md)).

**`[property: JsonPropertyName(...)]`** em records — quando o `JsonPropertyName` é aplicado em record, precisa do prefixo `property:` para indicar que o atributo é para a propriedade gerada, não para o parâmetro do construtor.

### Paginação com cursores

```csharp
public record MetaInsightsResponse(
    [property: JsonPropertyName("data")]   List<MetaInsightData>? Data,
    [property: JsonPropertyName("paging")] MetaPaging? Paging);

public record MetaPaging(
    [property: JsonPropertyName("cursors")] MetaCursors? Cursors,
    [property: JsonPropertyName("next")]    string? Next);

public record MetaCursors(
    [property: JsonPropertyName("before")] string? Before,
    [property: JsonPropertyName("after")]  string? After);
```

A API do Meta usa **cursor-based pagination** — diferente da paginação por página/offset tradicional.

---

## Parte 3 — Cursor-Based Pagination

### Paginação tradicional (offset) vs paginação por cursor

**Paginação offset (tradicional):**
```
GET /api/items?page=1&limit=100
GET /api/items?page=2&limit=100
GET /api/items?page=3&limit=100
```
Problema: se um novo item é inserido entre a página 1 e a página 2, os itens "deslizam" — você pode pular um item ou receber duplicatas.

**Paginação por cursor:**
```
GET /api/items?limit=100
→ retorna { data: [...], paging: { cursors: { after: "abc123" }, next: "url" } }

GET /api/items?limit=100&after=abc123
→ retorna { data: [...], paging: { cursors: { after: "def456" } } }

GET /api/items?limit=100&after=def456
→ retorna { data: [...], paging: {} }  ← sem "next" = última página
```

O cursor `after` aponta para o último item retornado. A próxima chamada pega "tudo depois desse item" — consistente mesmo com inserções concorrentes.

### Implementação no MetaAdsClient

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsClient.cs
private async Task<List<MetaInsightData>> FetchAllAsync(
    string level, string timeRange, CancellationToken ct)
{
    var all = new List<MetaInsightData>();
    string? afterCursor = null;     // começa sem cursor (primeira página)

    while (true)
    {
        var query = BuildQuery(level, timeRange, afterCursor);
        var url = $"{_settings.AdAccountId}/insights?{query}";

        var response = await httpClient.GetAsync(url, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            ThrowApiError(content, (int)response.StatusCode);

        var page = JsonSerializer.Deserialize<MetaInsightsResponse>(content, JsonOptions)
            ?? throw new InvalidOperationException("Resposta Meta Ads inválida.");

        all.AddRange(page.Data ?? []);  // acumula os dados desta página

        // Extrai o cursor para a próxima página
        afterCursor = page.Paging?.Next is not null
            ? page.Paging.Cursors?.After   // se tem "next", pega o cursor "after"
            : null;                         // se não tem "next", era a última página

        if (afterCursor is null) break;    // sai do loop
    }

    return all;
}
```

**Fluxo visual:**

```
Iteração 1: afterCursor = null
    → GET /act_123/insights?...
    → { data: [100 itens], paging: { cursors: { after: "abc" }, next: "url" } }
    all = [100 itens], afterCursor = "abc"

Iteração 2: afterCursor = "abc"
    → GET /act_123/insights?...&after=abc
    → { data: [100 itens], paging: { cursors: { after: "def" }, next: "url" } }
    all = [200 itens], afterCursor = "def"

Iteração 3: afterCursor = "def"
    → GET /act_123/insights?...&after=def
    → { data: [50 itens], paging: { cursors: { before: "..." } } }  ← sem "next"
    all = [250 itens], afterCursor = null

break → retorna all (250 itens)
```

`all.AddRange(page.Data ?? [])` — o `?? []` garante que se `Data` for null (resposta vazia), não lança exceção — apenas adiciona lista vazia.

---

## Parte 4 — MetaAdsMonthSyncService (Orquestração)

O `MetaAdsMonthSyncService` é o coração da integração — orquestra a busca de dados em 3 níveis e persiste de forma atômica.

### Busca sequencial por nível

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsMonthSyncService.cs
private async Task<(List<MetaInsightData> campaigns, ...)>
    FetchAllLevelsAsync(DateOnly since, DateOnly until, CancellationToken ct)
{
    // Sequencial para não sobrecarregar rate limit da API
    var campaigns = await client.GetDailyInsightsByLevelAsync(MetaInsightLevel.Campaign, since, until, ct);
    var adSets    = await client.GetDailyInsightsByLevelAsync(MetaInsightLevel.AdSet,    since, until, ct);
    var ads       = await client.GetDailyInsightsByLevelAsync(MetaInsightLevel.Ad,       since, until, ct);
    return (campaigns, adSets, ads);
}
```

Por que sequencial e não paralelo (Task.WhenAll)?

1. Rate limit da API — o Meta limita chamadas por minuto. Disparar 3 ao mesmo tempo triplicaria o consumo.
2. `AppDbContext` não é thread-safe — mas aqui não é o motivo, pois essas chamadas são para API externa, não para o banco.

### Tupla de retorno

```csharp
Task<(List<MetaInsightData> campaigns, List<MetaInsightData> adSets, List<MetaInsightData> ads)>
```

**Value Tuple** — C# permite retornar múltiplos valores sem criar uma classe:

```csharp
// Na declaração:
private async Task<(List<MetaInsightData> campaigns, List<MetaInsightData> adSets, List<MetaInsightData> ads)>
    FetchAllLevelsAsync(...) { ... }

// No uso (destructuring):
var (campaignData, adSetData, adData) = await FetchAllLevelsAsync(since, until, ct);
// campaignData, adSetData, adData são variáveis separadas
```

Tuplas são úteis quando um método precisa retornar 2-3 valores relacionados e criar uma classe dedicada seria overhead excessivo.

### O padrão "apaga e reinsere" com transação

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(ct);

// Apaga o mês atual — 3 tabelas de insights + resumo diário
await db.MetaCampaignDailyInsights.Where(...).ExecuteDeleteAsync(ct);
await db.MetaAdSetDailyInsights.Where(...).ExecuteDeleteAsync(ct);
await db.MetaAdDailyInsights.Where(...).ExecuteDeleteAsync(ct);
await db.DailyAdSpendSummaries.Where(...).ExecuteDeleteAsync(ct);

// Insere os dados novos
db.MetaCampaignDailyInsights.AddRange(campaignEntities);
db.MetaAdSetDailyInsights.AddRange(adSetData.Select(d => MapAdSet(d, tenantId, now)));
db.MetaAdDailyInsights.AddRange(adData.Select(d => MapAd(d, tenantId, now)));
db.DailyAdSpendSummaries.AddRange(dailySummaries);

await db.SaveChangesAsync(ct);
await transaction.CommitAsync(ct);
```

**Por que apagar e reinserir em vez de atualizar?**

Porque a Meta pode atualizar retroativamente dados de dias anteriores (atribuição de conversões). Ao sincronizar um mês inteiro, você quer os dados mais recentes para cada dia — mais simples garantir isso apagando e reinserindo do que tentando fazer um upsert.

### O campo omni_purchase

```csharp
// omni_purchase = métrica canônica do Meta para "Resultados" em campanhas de compra.
// Cobre offsite (pixel) + onsite (loja) sem duplicar
private const string PurchaseActionType = "omni_purchase";

private static int SumResults(List<MetaAction>? actions)
{
    if (actions is null) return 0;
    return actions
        .Where(a => a.ActionType == PurchaseActionType)
        .Sum(a => int.TryParse(a.Value, out var v) ? v : 0);
}
```

A API do Meta retorna um array `actions` com vários tipos de conversão: cliques, comentários, curtidas, compras, etc. Filtramos por `omni_purchase` porque é a métrica que representa "compras" sem duplicar contagem entre pixel offsite e loja onsite.

---

## Parte 5 — O Job Hangfire (MetaAdsSyncJob)

```csharp
// src/VBBSManager.Infrastructure/Jobs/MetaAdsSyncJob.cs
public class MetaAdsSyncJob(
    IMetaAdsMonthSyncService syncService,
    ILogger<MetaAdsSyncJob> logger)
{
    public const string JobId = "meta-ads-sync";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [300, 900, 1800])]
    public async Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
    {
        // ...
    }
}
```

**`[AutomaticRetry(Attempts = 3, DelaysInSeconds = [300, 900, 1800])]`** — atributo do Hangfire que configura retry automático:
- 3 tentativas após a falha inicial
- Delays: 5min (300s) → 15min (900s) → 30min (1800s)

Diferente do retry do Polly (que trata erros de rede no nível do HTTP), o retry do Hangfire trata falhas do job inteiro — inclusive errors de negócio como token expirado.

---

## Parte 6 — O Endpoint de Sync Manual

```csharp
// src/VBBSManager.Api/Features/Traffic/Sync/TrafficSyncController.cs
[ApiController]
[Route("api/traffic")]
[Authorize]
public class TrafficSyncController(ITrafficSyncService service) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] TrafficSyncRequest request,
        CancellationToken ct)
    {
        var tenantId = (Guid)HttpContext.Items["TenantId"]!;
        var result = await service.ExecuteAsync(tenantId, request.Year, request.Month, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
```

O endpoint permite que o usuário dispare o sync manualmente (via frontend ou Swagger) sem precisar esperar o job agendado. O `TrafficSyncService` captura as exceções de API e as converte em `Result.Fail` — o frontend recebe uma mensagem de erro legível.

---

## Resumo do Fluxo Completo

```
Usuário clica "Sincronizar" no frontend
    ↓
POST /api/traffic/sync { year: 2026, month: 6 }
    ↓
TrafficSyncController → TrafficSyncService
    ↓
MetaAdsMonthSyncService.SyncMonthAsync(tenantId, 2026, 6)
    ↓
FetchAllLevelsAsync(2026-06-01, 2026-06-30)
    ├── MetaAdsClient.GetDailyInsightsByLevelAsync("campaign", ...)
    │       → paginação por cursor
    │       → retorna 180 linhas (30 dias × 6 campanhas)
    ├── MetaAdsClient.GetDailyInsightsByLevelAsync("adset", ...)
    └── MetaAdsClient.GetDailyInsightsByLevelAsync("ad", ...)
    ↓
BeginTransactionAsync
    ↓
ExecuteDeleteAsync × 4 tabelas (apaga dados antigos)
    ↓
AddRange × 4 tabelas (insere dados novos)
    ↓
SaveChangesAsync → CommitAsync
    ↓
TrafficSyncService retorna Result.Ok(TrafficSyncResponse)
    ↓
Controller retorna HTTP 200 com contagens
```

---

## Glossário Meta Ads

| Termo | Significado |
|---|---|
| Campaign | Campanha — objetivo e orçamento total |
| AdSet | Conjunto de anúncios — segmentação e bid |
| Ad | Anúncio — o criativo (imagem/vídeo + copy) |
| Insights | Métricas de performance (impressões, cliques, gasto, conversões) |
| Cursor | Ponteiro para a posição atual na paginação |
| `omni_purchase` | Métrica de compras sem duplicar contagem pixel/onsite |
| `time_increment=1` | Retorna um registro por dia (não agregado) |
| `action_report_time=conversion` | Atribui conversões à data da compra, não do clique |

---

*Próximo: você concluiu a trilha de aprendizado! Consulte [00-trilha-de-aprendizado.md](./00-trilha-de-aprendizado.md) para próximos passos.*
