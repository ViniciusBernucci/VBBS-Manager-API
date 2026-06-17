# Typed HTTP Clients e Polly — Retry com Backoff Exponencial

> **Ordem de leitura:** documento **17** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [16-ioptions-configuracao.md](./16-ioptions-configuracao.md).

---

## O problema com HttpClient

Em .NET, `HttpClient` parece simples mas tem uma armadilha clássica:

```csharp
// ❌ ERRADO — nunca faça isso
public class MetaAdsClient
{
    public async Task<...> GetInsightsAsync(...)
    {
        using var client = new HttpClient();  // cria novo por chamada — esgota sockets!
        var response = await client.GetAsync("...");
    }
}
```

**Por que isso é errado?** `HttpClient` usa conexões TCP que ficam em estado `TIME_WAIT` por até 4 minutos após ser descartado. Se você criar um `new HttpClient()` para cada chamada, você esgota as portas disponíveis do servidor — é o chamado "socket exhaustion".

```csharp
// ❌ Também errado — Singleton tem problemas com DNS refresh
public class MetaAdsClient
{
    private static readonly HttpClient _client = new();  // não respeita mudanças de DNS
}
```

A solução correta é o **Typed HTTP Client** — padrão que o projeto usa para todas as integrações.

---

## 1. O que é um Typed HTTP Client?

Um Typed HTTP Client é uma classe que **recebe `HttpClient` pelo construtor** — o ASP.NET Core gerencia o ciclo de vida por você (via `IHttpClientFactory`):

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsClient.cs
public class MetaAdsClient(
    HttpClient httpClient,           // ← recebe HttpClient gerenciado pelo framework
    IOptions<MetaSettings> options,
    ILogger<MetaAdsClient> logger) : IMetaAdsClient
{
    // httpClient já vem configurado com BaseAddress, timeout, etc.
}
```

O `HttpClient` injetado é gerenciado pelo `IHttpClientFactory` internamente — ele reutiliza conexões TCP de forma eficiente.

---

## 2. Registrando Typed HTTP Clients

Em `ServiceCollectionExtensions`, você registra os clientes com `AddHttpClient<TInterface, TImplementation>`:

```csharp
// src/VBBSManager.Api/Common/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddExternalClients(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Hotmart Auth Client (sem retry — é só autenticação)
    services.AddHttpClient<IHotmartAuthClient, HotmartAuthClient>(client =>
        client.BaseAddress = new Uri("https://api-sec-vlc.hotmart.com"));

    // Hotmart Sales Client (com retry exponencial)
    services.AddHttpClient<IHotmartClient, HotmartClient>(client =>
        client.BaseAddress = new Uri("https://developers.hotmart.com/payments/api/v1/"))
        .AddTransientHttpErrorPolicy(p =>
            p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

    // Meta Ads Client (com retry + timeout)
    var metaApiVersion = configuration["FACEBOOK_API_VERSION"] ?? "v25.0";
    services.AddHttpClient<IMetaAdsClient, MetaAdsClient>(client =>
    {
        client.BaseAddress = new Uri($"https://graph.facebook.com/{metaApiVersion}/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

    return services;
}
```

**O que acontece aqui:**

1. `AddHttpClient<IInterface, Implementação>` — registra o cliente tipado no container de DI
2. `client =>` — configura o `HttpClient` antes de ele ser injetado (BaseAddress, timeout, headers)
3. `.AddTransientHttpErrorPolicy(...)` — adiciona política de retry via **Polly**

---

## 3. O que é Polly?

Polly é uma biblioteca de resiliência para .NET. Ela implementa padrões como:

- **Retry** — tenta novamente em caso de falha
- **Circuit Breaker** — para de tentar após N falhas (evita sobrecarregar serviço que está down)
- **Timeout** — cancela se demorar demais
- **Fallback** — usa um valor padrão se tudo falhar

No projeto, usamos apenas **Retry com backoff exponencial**.

---

## 4. WaitAndRetryAsync — Retry com Backoff Exponencial

```csharp
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
```

Vamos decompor:

**`AddTransientHttpErrorPolicy`** — aplica a política para erros transientes (erros de rede, 5xx, timeout). Não tenta retry em 4xx (BadRequest, NotFound) — esses são erros do cliente, não adianta tentar de novo.

**`WaitAndRetryAsync(3, ...)`** — tenta até 3 vezes em caso de falha.

**`attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))`** — calcula o tempo de espera entre tentativas:

| Tentativa | `Math.Pow(2, attempt)` | Espera |
|---|---|---|
| 1 | `Math.Pow(2, 1)` = 2 | 2 segundos |
| 2 | `Math.Pow(2, 2)` = 4 | 4 segundos |
| 3 | `Math.Pow(2, 3)` = 8 | 8 segundos |

**Por que esperar mais a cada tentativa (backoff exponencial)?**

Se a API está sobrecarregada ou com instabilidade temporária, dar mais tempo antes de tentar novamente reduz a pressão sobre o serviço. Se todos os clientes tentassem ao mesmo tempo sem espera, piorariam a situação.

**Fluxo visual:**

```
Chamada → FALHA
    espera 2s
    Tentativa 1 → FALHA
    espera 4s
    Tentativa 2 → FALHA
    espera 8s
    Tentativa 3 → SUCESSO ✓
                → FALHA → lança exceção para o chamador
```

---

## 5. BaseAddress — URLs relativas vs absolutas

Quando você define `BaseAddress`:

```csharp
client.BaseAddress = new Uri("https://graph.facebook.com/v25.0/");
```

Nas chamadas, você usa URLs relativas:

```csharp
// Dentro do MetaAdsClient
var url = $"{_settings.AdAccountId}/insights?{query}";
var response = await httpClient.GetAsync(url, ct);
// URL final: https://graph.facebook.com/v25.0/act_123/insights?...
```

**Regra importante:** a `BaseAddress` deve terminar com `/` e as URLs relativas **não** devem começar com `/`. Se a URL relativa começar com `/`, ela substitui o path completo (comportamento do `Uri`).

```csharp
// ✅ Correto
BaseAddress = "https://api.example.com/v1/"
relative    = "users/profile"
// resultado: https://api.example.com/v1/users/profile

// ❌ Errado — o /users substitui /v1/
BaseAddress = "https://api.example.com/v1/"
relative    = "/users/profile"
// resultado: https://api.example.com/users/profile  (sem o /v1/)
```

---

## 6. Timeout

```csharp
client.Timeout = TimeSpan.FromSeconds(30);
```

Se a API não responder em 30 segundos, o `HttpClient` lança `TaskCanceledException`. Sem timeout configurado, o padrão do .NET é 100 segundos — muito longo para uma API web.

O Meta Ads foi configurado com 30 segundos porque as queries de insights podem demorar, mas não devem ultrapassar isso.

---

## 7. Como usar o HttpClient dentro do cliente tipado

No `MetaAdsClient`, o `httpClient` injetado já tem a `BaseAddress` configurada:

```csharp
public class MetaAdsClient(
    HttpClient httpClient,
    IOptions<MetaSettings> options,
    ILogger<MetaAdsClient> logger) : IMetaAdsClient
{
    private readonly MetaSettings _settings = options.Value;

    private async Task<List<MetaInsightData>> FetchAllAsync(
        string level, string timeRange, CancellationToken ct)
    {
        var all = new List<MetaInsightData>();
        string? afterCursor = null;

        while (true)
        {
            var query = BuildQuery(level, timeRange, afterCursor);
            var url = $"{_settings.AdAccountId}/insights?{query}";
            //         ^ relativo à BaseAddress: https://graph.facebook.com/v25.0/act_123/insights?...

            logger.LogInformation(
                "Meta Ads GET insights level={Level} cursor={HasCursor}",
                level, afterCursor is not null);

            var response = await httpClient.GetAsync(url, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                ThrowApiError(content, (int)response.StatusCode);

            // ... processa resposta
        }
    }
}
```

O Polly retry é transparente — você não vê nada no código do cliente. Se `httpClient.GetAsync` falhar com erro transiente, o Polly aguarda e tenta novamente automaticamente.

---

## 8. `Uri.EscapeDataString` — URL Encoding

Qualquer valor que vai na query string precisa ser codificado. Caracteres como `{`, `}`, `"`, espaço têm significado especial em URLs e precisam ser escaped:

```csharp
private string BuildQuery(string level, string timeRange, string? afterCursor)
{
    var parts = new List<string>
    {
        $"access_token={Uri.EscapeDataString(_settings.AccessToken)}",
        // token pode conter caracteres especiais — escaping é obrigatório
        $"time_range={Uri.EscapeDataString(timeRange)}",
        // timeRange é JSON: {"since":"2026-06-01","until":"2026-06-30"}
        // sem encoding, as chaves quebrariam a URL
    };
    return string.Join("&", parts);
}
```

`Uri.EscapeDataString` converte:
- `{` → `%7B`
- `}` → `%7D`
- `"` → `%22`
- ` ` → `%20`

---

## Resumo

| Conceito | Regra |
|---|---|
| Nunca `new HttpClient()` | Use Typed Clients via `AddHttpClient<I, T>` |
| `BaseAddress` | Termina com `/`; URLs relativas não começam com `/` |
| Polly retry | `WaitAndRetryAsync(3, attempt => Math.Pow(2, attempt))` = 2s, 4s, 8s |
| Quando o Polly não faz retry | Em erros 4xx — são erros do cliente, retry não ajuda |
| Timeout | Configure explicitamente — padrão (100s) é muito alto |
| `Uri.EscapeDataString` | Sempre encode valores que vão na query string |

---

*Próximo: [18-ef-core-avancado.md](./18-ef-core-avancado.md) — AsNoTracking, bulk delete e transações.*
