# Logging Estruturado com ILogger\<T\>

> **Ordem de leitura:** documento **15** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [13-jobs.md](./13-jobs.md).

---

## Por que não usar `Console.WriteLine`?

A primeira tentação de qualquer programador ao debugar é colocar um `Console.WriteLine`. Em desenvolvimento funciona, mas em produção é inútil porque:

1. A saída vai para o terminal e some quando o processo fecha
2. Não tem nível de severidade (não dá para filtrar por "só erros")
3. Não tem metadados (timestamp, qual requisição gerou, qual tenant)
4. Não integra com ferramentas de monitoramento (Datadog, Sentry, etc.)

O .NET tem um sistema de logging embutido — `ILogger<T>` — que resolve tudo isso.

---

## 1. Injetando o ILogger

O `ILogger<T>` é registrado automaticamente pelo ASP.NET Core. Você só precisa injetá-lo:

```csharp
// Qualquer classe pode receber ILogger via Primary Constructor
public class MetaAdsClient(
    HttpClient httpClient,
    IOptions<MetaSettings> options,
    ILogger<MetaAdsClient> logger) : IMetaAdsClient
{
    // logger está disponível em todos os métodos
}
```

O parâmetro genérico `<MetaAdsClient>` é o "nome da categoria" — aparece no log para indicar de qual classe veio a mensagem.

Veja nos jobs:

```csharp
// src/VBBSManager.Infrastructure/Jobs/MetaAdsSyncJob.cs
public class MetaAdsSyncJob(
    IMetaAdsMonthSyncService syncService,
    ILogger<MetaAdsSyncJob> logger)
```

---

## 2. Níveis de Log

O .NET tem 6 níveis, do menos para o mais severo:

| Nível | Método | Quando usar |
|---|---|---|
| `Trace` | `LogTrace` | Detalhes muito finos — raramente em produção |
| `Debug` | `LogDebug` | Debug de desenvolvimento |
| `Information` | `LogInformation` | Eventos normais do fluxo da aplicação |
| `Warning` | `LogWarning` | Algo inesperado, mas a aplicação continua |
| `Error` | `LogError` | Erros que não impediram a requisição de terminar |
| `Critical` | `LogCritical` | Falha catastrófica — aplicação pode não funcionar |

No projeto, você vai encontrar os três mais usados:

```csharp
// Information — fluxo normal
logger.LogInformation(
    "Meta Ads sync iniciado — tenant {TenantId}, {Since} a {Until}",
    tenantId, since, until);

// Warning — rate limit é inesperado, mas Hangfire fará retry
logger.LogWarning(ex,
    "Meta Ads: rate limit — tenant {TenantId}. Hangfire fará retry.", tenantId);

// Error — falha grave, com a exception
logger.LogError(ex,
    "Meta Ads: token inválido — tenant {TenantId}. Gere um novo System User Token.", tenantId);
```

---

## 3. Logging Estruturado vs. String Interpolação

Esta é a diferença mais importante para entender. Existem duas formas de logar:

```csharp
// ❌ ERRADO — string interpolação (não use)
logger.LogInformation($"Meta Ads sync: {all.Count} registros para tenant {tenantId}");

// ✅ CORRETO — logging estruturado (use sempre)
logger.LogInformation(
    "Meta Ads sync: {Count} registros para tenant {TenantId}",
    all.Count, tenantId);
```

**Por que a segunda forma é melhor?**

No logging estruturado, `{Count}` e `{TenantId}` são **propriedades nomeadas**, não apenas texto. O sistema de logging as armazena separadamente do template.

Isso significa que uma ferramenta como Datadog pode fazer:

```
filtrar por: TenantId = "00000000-0000-0000-0000-000000000001"
agrupar por: Count
```

Com interpolação (`$"..."`), isso é impossível — é só texto puro, sem estrutura.

**Veja no MetaAdsClient:**

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsClient.cs
logger.LogInformation(
    "Meta Ads GET insights level={Level} cursor={HasCursor}",
    level, afterCursor is not null);

// ...

logger.LogInformation(
    "Meta Ads insights [{Level}]: {Count} registros recebidos", level, all.Count);
```

Os placeholders `{Level}` e `{Count}` se tornam campos separados no log.

---

## 4. Passando a Exception

Quando você tem uma exceção, passe ela como **primeiro argumento** (não dentro da string):

```csharp
// src/VBBSManager.Infrastructure/Jobs/MetaAdsSyncJob.cs
catch (MetaTokenException ex)
{
    // ✅ CORRETO — ex como primeiro argumento
    logger.LogError(ex,
        "Meta Ads: token inválido — tenant {TenantId}. Gere um novo System User Token.",
        tenantId);
    throw;
}
```

Passando `ex` como primeiro argumento, o sistema de logging captura a stack trace completa — essencial para debugar em produção.

```csharp
// ❌ ERRADO — exception perdida dentro da string
logger.LogError($"Erro: {ex.Message} — tenant {tenantId}");
// Isso perde o stack trace e a estrutura do erro
```

---

## 5. Logging no Middleware

O `ExceptionMiddleware` mostra o padrão para logging no nível mais alto da aplicação:

```csharp
// src/VBBSManager.Api/Common/Middleware/ExceptionMiddleware.cs
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Loga com a exception (para stack trace) + informações da requisição
            logger.LogError(ex,
                "Unhandled exception on {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorResponse(context, ex);
        }
    }
}
```

`{Method}` e `{Path}` são campos estruturados que ficam indexados no sistema de log. Em produção, você pode filtrar: "mostrar todos os erros que aconteceram em `POST /api/financial/cash-flow/transactions`".

---

## 6. Logging em Services

O `DashboardOverviewService` mostra logging em serviço de negócio:

```csharp
// src/VBBSManager.Api/Features/Dashboard/Overview/DashboardOverviewService.cs
logger.LogInformation(
    "Dashboard overview — tenant {TenantId} {Year}/{Month} receita={Revenue} lucro={Profit}",
    tenantId, year, month, dre.Summary.GrossRevenue, dre.Summary.OperationalProfit);
```

Note: você não precisa logar início e fim de todo método. Logue quando o dado é significativo — valores calculados, contagens de registros, resultado de operações externas.

---

## 7. Logging em Jobs

O `MetaAdsSyncJob` é um bom exemplo de logging em background jobs:

```csharp
// src/VBBSManager.Infrastructure/Jobs/MetaAdsSyncJob.cs
public async Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
{
    var now = DateTime.UtcNow;

    logger.LogInformation(
        "Meta Ads sync job iniciado — tenant {TenantId}, mês {Year}/{Month}",
        tenantId, now.Year, now.Month);

    try
    {
        var summary = await syncService.SyncMonthAsync(tenantId, now.Year, now.Month, ct);

        logger.LogInformation(
            "Meta Ads sync job concluído — {Campaigns} campanhas, {AdSets} conjuntos, {Ads} anúncios",
            summary.Campaigns, summary.AdSets, summary.Ads);
    }
    catch (MetaTokenException ex)
    {
        logger.LogError(ex,
            "Meta Ads: token inválido — tenant {TenantId}. Gere um novo System User Token.", tenantId);
        throw;  // ← relança para Hangfire saber que falhou
    }
    // ...
}
```

**Padrão dos jobs:**
- Loga início com contexto (tenant, período)
- Loga conclusão com resultado (quantos registros)
- Loga cada categoria de erro com mensagem acionável ("Gere um novo token")
- Sempre `throw` após logar — Hangfire precisa saber que falhou para fazer retry

---

## 8. Configurando o nível mínimo

No `appsettings.json`, você controla quais níveis aparecem:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

Com essa config:
- Seu código (`Default`): loga a partir de `Information`
- ASP.NET Core interno: só `Warning` em diante (muito verbose em debug)
- EF Core: só `Warning` — você não quer ver cada SQL em produção

Em desenvolvimento, pode baixar para `Debug` para ver mais detalhes:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"  // mostra SQLs
    }
  }
}
```

---

## Resumo

| Conceito | Regra |
|---|---|
| Use `ILogger<T>` | Sempre. Nunca `Console.WriteLine` em produção |
| Estruturado vs interpolação | Use `{PlaceholderNomeado}` — nunca `$"..."` |
| Exception como argumento | Passe `ex` como **primeiro parâmetro** do método |
| Nível correto | Info: fluxo normal; Warning: inesperado mas ok; Error: falha com exception |
| Jobs | Loga início + conclusão + cada categoria de erro; sempre `throw` após logar |

---

*Próximo: [16-ioptions-configuracao.md](./16-ioptions-configuracao.md) — como as configurações chegam nas classes.*
