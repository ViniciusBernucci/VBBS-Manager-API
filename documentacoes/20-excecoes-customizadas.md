# Exceções Customizadas — Hierarquia e Quando Usar

> **Ordem de leitura:** documento **20** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [19-linq-avancado.md](./19-linq-avancado.md). Conecta com o Padrão Result do [doc 04, seção 10](./04-aula-dotnet-conceitos.md).

---

## Dois mundos: Result vs Exception

No [doc 04](./04-aula-dotnet-conceitos.md), você aprendeu o **Padrão Result** para erros de negócio:

```csharp
// Erro esperado de negócio → Result.Fail
if (expense is null)
    return Result<Guid>.Fail("Gasto fixo não encontrado.");

if (alreadyPaid)
    return Result<Guid>.Fail("Este gasto já foi marcado como pago neste mês.");
```

**Quando usar `Result.Fail`:** situações que fazem parte do fluxo normal do negócio — "não encontrado", "já existe", "valor inválido", "permissão negada".

**Quando usar `throw Exception`:** situações verdadeiramente excepcionais — falhas de infraestrutura, APIs externas respondendo de forma inesperada, erros que o código não pode recuperar sozinho.

A linha entre os dois é: **"o código que chamou meu método pode tratar esse caso?"**

- "Gasto não encontrado" → o Service pode retornar 404 para o frontend. Tratável. → `Result.Fail`
- "Token Meta expirado" → o background job não tem como regenerar o token sozinho. Não tratável automaticamente. → `throw`

---

## 1. A Hierarquia de Exceções da Meta Ads

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaExceptions.cs
namespace VBBSManager.Infrastructure.ExternalClients.Meta;

public class MetaApiException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

public class MetaTokenException(string message)      : MetaApiException(190, message);
public class MetaPermissionException(string message) : MetaApiException(200, message);
public class MetaRateLimitException(string message)  : MetaApiException(17, message);
```

### Por que uma hierarquia?

A hierarquia `MetaApiException → MetaTokenException / MetaPermissionException / MetaRateLimitException` serve para capturar erros com diferentes níveis de granularidade:

```csharp
// Captura APENAS o erro de token
catch (MetaTokenException ex) { /* loga e orienta a gerar novo token */ }

// Captura APENAS o erro de permissão
catch (MetaPermissionException ex) { /* loga e orienta a adicionar ads_read */ }

// Captura APENAS rate limit
catch (MetaRateLimitException ex) { /* loga que Hangfire fará retry */ }

// Captura QUALQUER erro da Meta API (não coberto acima)
catch (MetaApiException ex) { /* loga com código de erro */ }

// Captura QUALQUER exceção (rede, timeout, etc.)
catch (Exception ex) { /* erro genérico */ }
```

**Regra de hierarquia no C#:** o `catch` é sempre do mais específico para o mais genérico. Se você colocar `catch (Exception)` antes de `catch (MetaTokenException)`, o compilador avisa que o segundo nunca será alcançado.

### Primary Constructors em Exceptions

```csharp
// MetaApiException tem dois parâmetros: code e message
public class MetaApiException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
    // ": Exception(message)" passa a mensagem para a classe base
    // "public int Code" adiciona a propriedade específica desta exception
}

// MetaTokenException delega ao base com código fixo 190
public class MetaTokenException(string message) : MetaApiException(190, message);
// É equivalente a:
// public class MetaTokenException(string message) : MetaApiException(190, message) { }
// A classe filho sem body chama o construtor do pai com (190, message)
```

O código de erro 190 é o da API do Meta para "token inválido". Ao hardcodar no construtor da classe específica, você não precisa passar o código em todo `throw new MetaTokenException(...)`.

---

## 2. Lançando as Exceções (ThrowApiError)

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsClient.cs
private static void ThrowApiError(string content, int httpStatus)
{
    MetaApiErrorResponse? errorResponse = null;
    try { errorResponse = JsonSerializer.Deserialize<MetaApiErrorResponse>(content, JsonOptions); }
    catch { /* ignora falha de parse — usa fallback */ }

    var code    = errorResponse?.Error?.Code    ?? 0;
    var message = errorResponse?.Error?.Message ?? $"HTTP {httpStatus}: {content}";

    throw code switch
    {
        190           => new MetaTokenException($"Token Meta expirado ou inválido: {message}"),
        200           => new MetaPermissionException($"Permissão insuficiente: {message}"),
        4 or 17 or 32 or 613 => new MetaRateLimitException($"Rate limit (código {code}): {message}"),
        _             => new MetaApiException(code, $"Erro Meta API (HTTP {httpStatus}, código {code}): {message}")
    };
}
```

**`throw code switch`** — um `switch expression` que lança a exceção correta baseada no código de erro da API. É elegante e elimina múltiplos `if/else if`.

**`4 or 17 or 32 or 613`** — padrão "or" em switch expression: qualquer um desses códigos é rate limit.

**`_`** — o case padrão: qualquer outro código de erro cai aqui como `MetaApiException` genérica.

---

## 3. Capturando e Tratando as Exceções

### No Job (relança para Hangfire fazer retry)

```csharp
// src/VBBSManager.Infrastructure/Jobs/MetaAdsSyncJob.cs
try
{
    var summary = await syncService.SyncMonthAsync(tenantId, now.Year, now.Month, ct);
    logger.LogInformation("Sync concluído: ...", ...);
}
catch (MetaTokenException ex)
{
    logger.LogError(ex, "Token inválido — tenant {TenantId}. Gere um novo token.", tenantId);
    throw;  // ← relança — Hangfire marca o job como falho e faz retry
}
catch (MetaPermissionException ex)
{
    logger.LogError(ex, "Permissão insuficiente — tenant {TenantId}.", tenantId);
    throw;
}
catch (MetaRateLimitException ex)
{
    logger.LogWarning(ex, "Rate limit — tenant {TenantId}. Hangfire fará retry.", tenantId);
    throw;
}
```

**`throw;` (sem parâmetro)** — relança a exceção original preservando a stack trace. Diferente de `throw ex;` que reinicia a stack trace e perde informação de onde o erro realmente aconteceu.

### No Service (converte para Result)

```csharp
// src/VBBSManager.Api/Features/Traffic/Sync/TrafficSyncService.cs
try
{
    summary = await syncService.SyncMonthAsync(tenantId, year, month, ct);
}
catch (MetaTokenException)
{
    return Result<TrafficSyncResponse>.Fail(
        "Token Meta inválido ou expirado. Gere um novo System User Token com permissão ads_read.");
}
catch (MetaPermissionException)
{
    return Result<TrafficSyncResponse>.Fail(
        "Token Meta sem permissão ads_read. Adicione essa permissão ao token no Meta Business Manager.");
}
catch (MetaRateLimitException)
{
    return Result<TrafficSyncResponse>.Fail(
        "Rate limit da API Meta atingido. Aguarde alguns minutos e tente novamente.");
}
catch (MetaApiException ex)
{
    return Result<TrafficSyncResponse>.Fail(
        $"Erro na API Meta (código {ex.Code}): {ex.Message}");
}
```

Aqui, o Service captura as exceções e as converte em `Result.Fail` — porque o frontend pode exibir a mensagem de erro para o usuário. Note que `ex` não é passado no `catch (MetaTokenException)` — não precisamos do objeto se só vamos usar a mensagem que já está no Result.

**Diferença de comportamento:**
- **Job** → relança → Hangfire faz retry e notifica falha no dashboard
- **Service (requisição do frontend)** → converte para `Result.Fail` → controller retorna 400 com mensagem legível

---

## 4. A Convenção de Nomenclatura

Toda exceção customizada em C# termina com `Exception`:

```csharp
MetaApiException        ✓
MetaTokenException      ✓
MetaPermissionException ✓
MetaRateLimitException  ✓
```

Isso não é obrigatório pelo compilador, mas é convenção universal em C#. O IntelliSense e ferramentas de análise de código assumem esse padrão.

---

## 5. Quando criar exceções customizadas?

Crie exceções customizadas quando:

1. **Diferentes chamadores tratam o erro de formas diferentes** — no projeto, `MetaTokenException` vs `MetaRateLimitException` levam a ações diferentes (gerar token vs aguardar retry)

2. **Você precisa de dados estruturados com o erro** — `MetaApiException` carrega `Code` além da mensagem. Sem a exceção customizada, você perderia esse dado

3. **A exceção vem de uma biblioteca ou API externa** — mapear erros externos para sua hierarquia interna isola o código da API do restante do sistema

**Não crie exceções customizadas para:**
- Erros de validação de negócio → use `Result.Fail`
- Erros que o código pode recuperar → use tratamento e retorno de valor
- Apenas para "ter uma exceção específica" sem diferença de tratamento

---

## Resumo

| Situação | Abordagem |
|---|---|
| Erro de negócio tratável | `return Result.Fail("mensagem")` |
| Falha de infraestrutura / API externa | `throw new MinhaException(...)` |
| Hierarquia | Base genérica → especializadas por tipo de erro |
| `throw;` vs `throw ex;` | Sempre `throw;` — preserva stack trace original |
| Capturar e converter | Job: relança; Service de frontend: converte para `Result.Fail` |
| Nomenclatura | Sempre terminar com `Exception` |

---

*Próximo: [21-integracao-meta-ads.md](./21-integracao-meta-ads.md) — estudo de caso completo da integração com a API do Meta Ads.*
