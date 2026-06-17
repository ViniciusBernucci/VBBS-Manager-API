# EF Core Avançado — AsNoTracking, Bulk Delete e Transações

> **Ordem de leitura:** documento **18** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [17-typed-http-clients-polly.md](./17-typed-http-clients-polly.md). Pré-requisito: [doc 04](./04-aula-dotnet-conceitos.md), seção 6 (EF Core básico).

---

## Contexto

No [doc 04](./04-aula-dotnet-conceitos.md), você aprendeu os conceitos básicos do EF Core: DbContext, DbSet, LINQ queries, CRUD. Aqui vamos para três recursos avançados que aparecem no projeto — especialmente no sync de dados da Meta Ads.

---

## 1. AsNoTracking — Queries de Leitura

### O que é Change Tracking?

Por padrão, quando você faz uma query com EF Core, ele **rastreia** os objetos retornados:

```csharp
var sales = await db.DailySalesSummaries
    .Where(x => x.TenantId == tenantId)
    .ToListAsync(ct);
```

O EF Core tira uma "foto" do estado original de cada `DailySalesSummary` retornado. Se você modificar algum deles e chamar `SaveChangesAsync()`, ele detecta as mudanças e gera os `UPDATE` correspondentes.

Isso é útil para operações de escrita. Mas se você só quer **ler** os dados — sem intenção de modificá-los — esse rastreamento é desperdício de memória e CPU.

### AsNoTracking() — desativa o rastreamento

```csharp
// src/VBBSManager.Api/Features/Financial/DRE/DreService.cs
var sales = await db.DailySalesSummaries
    .Where(x => x.TenantId == tenantId && x.Date >= monthStart && x.Date <= monthEnd)
    .AsNoTracking()   // ← diz ao EF: "não rastreie esses objetos"
    .ToListAsync(ct);

var metaSpendSummaries = await db.DailyAdSpendSummaries
    .Where(x => x.TenantId == tenantId && x.Date >= monthStart && x.Date <= monthEnd)
    .AsNoTracking()
    .ToListAsync(ct);
```

**Quando usar `AsNoTracking()`:**
- Queries de leitura para retornar dados ao frontend (GET endpoints)
- Queries dentro de cálculos (DRE, dashboard overview)
- Qualquer lugar onde você **não vai chamar** `SaveChangesAsync()` com esses objetos

**Quando NÃO usar `AsNoTracking()`:**
- Quando você vai modificar o objeto retornado e salvar de volta
- Quando você precisa do navigation property loaded (relacionamentos)

**Ganho de performance:**

```csharp
// Com tracking (padrão) — EF rastreia todos os 1000 registros
var all = await db.MetaCampaignDailyInsights.ToListAsync(ct);
// → aloca snapshots de estado para 1000 objetos

// Com AsNoTracking — sem overhead de rastreamento
var all = await db.MetaCampaignDailyInsights.AsNoTracking().ToListAsync(ct);
// → carrega os dados, sem estado extra
```

Para queries que retornam muitos registros (como os insights diários da Meta), `AsNoTracking()` pode reduzir o uso de memória significativamente.

---

## 2. ExecuteDeleteAsync — Bulk Delete

### O problema com o Remove tradicional

O EF Core tradicional para deletar exige:
1. Carregar os registros do banco (SELECT)
2. Chamar `Remove` em cada um
3. Chamar `SaveChangesAsync` (DELETE por ID)

```csharp
// ❌ Ineficiente para muitos registros
var registros = await db.MetaCampaignDailyInsights
    .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
    .ToListAsync(ct);  // ← SELECT no banco — carrega tudo para memória

foreach (var r in registros)
    db.MetaCampaignDailyInsights.Remove(r);  // marca para deletar

await db.SaveChangesAsync(ct);  // executa um DELETE por registro
```

Para 500 registros, isso gera 500 comandos SQL separados.

### ExecuteDeleteAsync — delete direto no banco (EF Core 7+)

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsMonthSyncService.cs
await db.MetaCampaignDailyInsights
    .Where(x => x.TenantId == tenantId
        && x.Date >= since && x.Date <= until)
    .ExecuteDeleteAsync(ct);
// SQL: DELETE FROM "MetaCampaignDailyInsights"
//      WHERE "TenantId" = @p0 AND "Date" >= @p1 AND "Date" <= @p2
```

**`ExecuteDeleteAsync`** traduz o `Where` diretamente para um `DELETE WHERE` no banco — um único comando SQL, sem carregar nada para memória.

**Comparação:**

| Método | SQL executado | Registros carregados para memória |
|---|---|---|
| `Remove` (loop) | N DELETEs (1 por registro) | Sim — todos os registros |
| `ExecuteDeleteAsync` | 1 DELETE com WHERE | Não — zero |

**Limitação:** `ExecuteDeleteAsync` não aciona o Change Tracking nem os eventos do EF Core. Se você tiver algum comportamento custom no `SaveChangesAsync`, ele não será executado. No projeto, isso não é problema.

---

## 3. Transações de Banco — BeginTransactionAsync

### O que é uma transação?

Uma transação garante **atomicidade**: ou todas as operações acontecem, ou nenhuma. Se algo falhar no meio, tudo é revertido (rollback).

O cenário clássico é "apagar e reinserir": se você apagou os dados antigos mas o processo morreu antes de inserir os novos, você ficaria com o banco vazio. Uma transação evita isso.

### Como usar no EF Core

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsMonthSyncService.cs
public async Task<MetaSyncSummary> SyncMonthAsync(
    Guid tenantId, int year, int month, CancellationToken ct = default)
{
    // 1. Busca dados da API (fora da transação — não envolve o banco)
    var (campaignData, adSetData, adData) = await FetchAllLevelsAsync(since, until, ct);

    // 2. Abre transação — tudo abaixo é atômico
    await using var transaction = await db.Database.BeginTransactionAsync(ct);

    // 3. Apaga dados antigos do período
    await db.MetaCampaignDailyInsights
        .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
        .ExecuteDeleteAsync(ct);

    await db.MetaAdSetDailyInsights
        .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
        .ExecuteDeleteAsync(ct);

    await db.MetaAdDailyInsights
        .Where(x => x.TenantId == tenantId && x.Date >= since && x.Date <= until)
        .ExecuteDeleteAsync(ct);

    // 4. Insere dados novos
    db.MetaCampaignDailyInsights.AddRange(campaignEntities);
    db.MetaAdSetDailyInsights.AddRange(adSetData.Select(d => MapAdSet(d, tenantId, now)));
    db.MetaAdDailyInsights.AddRange(adData.Select(d => MapAd(d, tenantId, now)));
    db.DailyAdSpendSummaries.AddRange(dailySummaries);

    await db.SaveChangesAsync(ct);

    // 5. Confirma a transação
    await transaction.CommitAsync(ct);

    // Se qualquer passo acima lançar exceção, a transação é revertida automaticamente
    // graças ao "await using" — o Dispose chama Rollback se não houve Commit
}
```

### `await using` — dispose automático

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(ct);
```

O `await using` garante que quando o bloco terminar (com sucesso ou exceção), `transaction.DisposeAsync()` é chamado. Se você não chamou `CommitAsync`, o dispose faz o **rollback** automaticamente.

Em outras palavras:
- **Tudo deu certo** → você chama `CommitAsync` → commit
- **Exceção foi lançada** → `await using` chama dispose → rollback automático

### Quando usar transações?

Use transações quando você precisa garantir que múltiplas operações de banco aconteçam todas ou nenhuma:

| Cenário | Usar transação? |
|---|---|
| Apagar e reinserir dados de sync | ✅ Sim — atomicidade obrigatória |
| Criar entidade + relacionamento | Geralmente não — EF Core faz isso atomicamente em `SaveChangesAsync` |
| Múltiplos `SaveChangesAsync` independentes | ✅ Sim — se precisar que todos aconteçam juntos |
| Um único `SaveChangesAsync` | Não — o próprio `SaveChanges` já é atômico |

---

## 4. EF Core não suporta queries paralelas no mesmo contexto

Você encontrará esse comentário em `DreService`:

```csharp
// ── Fontes de dados (sequencial — EF Core não suporta queries paralelas no mesmo contexto) ──
var sales = await db.DailySalesSummaries.Where(...).AsNoTracking().ToListAsync(ct);
var metaSpend = await db.DailyAdSpendSummaries.Where(...).AsNoTracking().ToListAsync(ct);
var transactions = await db.CashFlowTransactions.Where(...).AsNoTracking().ToListAsync(ct);
```

Por que não fazer as três queries em paralelo com `Task.WhenAll`?

```csharp
// ❌ ERRO — DbContext não é thread-safe
var t1 = db.DailySalesSummaries.ToListAsync(ct);
var t2 = db.CashFlowTransactions.ToListAsync(ct);
await Task.WhenAll(t1, t2);  // → InvalidOperationException
```

O `AppDbContext` é **Scoped** (uma instância por requisição). Ele não é thread-safe — não pode ser usado por duas queries ao mesmo tempo. Se você precisar de paralelismo real, precisa de instâncias separadas do DbContext.

Na prática, para o volume de dados deste projeto, executar sequencialmente é rápido o suficiente.

---

## Resumo

| Conceito | Quando usar |
|---|---|
| `AsNoTracking()` | Toda query de leitura que não vai chamar `SaveChangesAsync` com os objetos |
| `ExecuteDeleteAsync()` | Delete em massa — não carrega dados para memória |
| `BeginTransactionAsync()` | Múltiplas operações que precisam ser atômicas (tudo ou nada) |
| `await using var tx` | Garante rollback automático se exceção for lançada antes do `CommitAsync` |
| Não paralelize no mesmo DbContext | Execute queries sequencialmente — DbContext não é thread-safe |

---

*Próximo: [19-linq-avancado.md](./19-linq-avancado.md) — GroupBy, projeções, dicionários e parsing de dados externos.*
