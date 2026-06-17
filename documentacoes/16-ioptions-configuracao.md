# IOptions\<T\> — Configuração Tipada

> **Ordem de leitura:** documento **16** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md). Leia depois de [15-logging-estruturado.md](./15-logging-estruturado.md).

---

## O problema que IOptions resolve

No [doc 04](./04-aula-dotnet-conceitos.md), você viu que configurações ficam no `appsettings.json` ou `.env`. Para acessá-las, você poderia usar `IConfiguration` diretamente:

```csharp
// Acesso direto — funciona, mas tem problemas
public class MetaAdsClient(IConfiguration config)
{
    public async Task<...> GetInsightsAsync(...)
    {
        var token = config["FACEBOOK_TOKEN"];    // string bruta — sem tipo, sem validação
        var accountId = config["FACEBOOK_AD_ACCOUNT_ID"];
        // ...
    }
}
```

Problemas com esse approach:
1. **Sem tipo** — tudo é `string?`, você não sabe se o valor existe ou qual o formato esperado
2. **Acoplado ao nome da chave** — se renomear no config, quebra silenciosamente em runtime
3. **Sem validação** — `config["FACEBOOK_TOKEN"]` retorna `null` sem aviso se a chave não existe
4. **Espalhado** — cada classe que precisa do token acessa o config diretamente

O `IOptions<T>` resolve tudo isso com uma classe de configuração tipada.

---

## 1. Criando uma Settings class

Cada integração externa tem sua própria classe de configuração:

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaSettings.cs
namespace VBBSManager.Infrastructure.ExternalClients.Meta;

public class MetaSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string AdAccountId { get; set; } = string.Empty;
}
```

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Hotmart/HotmartSettings.cs
namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public class HotmartSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
```

São classes simples: propriedades públicas com valores padrão de string vazia (nunca null).

---

## 2. Registrando no container de DI

Em `ServiceCollectionExtensions`, você vincula os valores do `.env` à classe de settings:

```csharp
// src/VBBSManager.Api/Common/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddExternalClients(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Hotmart Settings
    services.Configure<HotmartSettings>(options =>
    {
        options.ClientId     = configuration["HOTMART_CLIENT_ID"]     ?? string.Empty;
        options.ClientSecret = configuration["HOTMART_CLIENT_SECRET"] ?? string.Empty;
    });

    // Meta Settings
    services.Configure<MetaSettings>(options =>
    {
        options.AccessToken = configuration["FACEBOOK_TOKEN"]          ?? string.Empty;
        options.AdAccountId = configuration["FACEBOOK_AD_ACCOUNT_ID"] ?? string.Empty;
    });

    return services;
}
```

`services.Configure<T>(...)` é o método que registra a classe de configuração no container de DI. A função lambda recebe o objeto `options` já instanciado e você preenche as propriedades.

Note o `?? string.Empty`: se a variável de ambiente não estiver definida, usa string vazia em vez de null — evita `NullReferenceException`.

---

## 3. Usando IOptions\<T\> na classe

Qualquer classe que precise das settings injeta `IOptions<T>`:

```csharp
// src/VBBSManager.Infrastructure/ExternalClients/Meta/MetaAdsClient.cs
public class MetaAdsClient(
    HttpClient httpClient,
    IOptions<MetaSettings> options,    // ← injeta o wrapper
    ILogger<MetaAdsClient> logger) : IMetaAdsClient
{
    private readonly MetaSettings _settings = options.Value;  // ← acessa o objeto tipado

    private string BuildQuery(string level, string timeRange, string? afterCursor)
    {
        var parts = new List<string>
        {
            $"access_token={Uri.EscapeDataString(_settings.AccessToken)}",  // ← acesso tipado
            // ...
        };
        // ...
    }
}
```

`options.Value` retorna o objeto `MetaSettings` já preenchido. A partir daí, você acessa propriedades tipadas: `_settings.AccessToken`, `_settings.AdAccountId`.

---

## 4. A diferença entre IOptions, IOptionsSnapshot e IOptionsMonitor

O .NET oferece três variantes:

| Tipo | Quando usar | Particularidade |
|---|---|---|
| `IOptions<T>` | A maioria dos casos | Valor fixo desde o startup |
| `IOptionsSnapshot<T>` | Config que muda entre requests | Recarregado por request |
| `IOptionsMonitor<T>` | Config que muda em tempo real | Notifica mudanças com evento |

No projeto, usamos `IOptions<T>` porque as credenciais das APIs externas não mudam em runtime — são lidas do `.env` no startup e ficam fixas.

---

## 5. Como os valores chegam do .env para as Settings

O fluxo completo:

```
.env
    FACEBOOK_TOKEN=EAA...
    FACEBOOK_AD_ACCOUNT_ID=act_123...
        ↓
Program.cs (startup)
    builder.Configuration
        ← lê o .env via DotNetEnv ou variáveis de ambiente
        ↓
ServiceCollectionExtensions.AddExternalClients(configuration)
    services.Configure<MetaSettings>(options => {
        options.AccessToken = configuration["FACEBOOK_TOKEN"];
    })
        ↓
Container de DI
    IOptions<MetaSettings> registrado com:
        AccessToken = "EAA..."
        AdAccountId = "act_123..."
        ↓
MetaAdsClient(IOptions<MetaSettings> options)
    _settings = options.Value
    _settings.AccessToken == "EAA..."  ← disponível aqui
```

---

## 6. Comparação com outras formas

```csharp
// ❌ Menos ideal — string pura, typo quebra silenciosamente
var token = config["FACEBOOK_TOKEN"];

// ❌ Menos ideal — acoplamento direto ao IConfiguration
public class MetaAdsClient(IConfiguration config) { }

// ✅ Correto — tipado, injetável, testável
public class MetaAdsClient(IOptions<MetaSettings> options) { }
```

Com `IOptions<T>`, se você mudar o nome da propriedade `AccessToken` para `Token`, o compilador vai reclamar em todos os lugares que usam `_settings.AccessToken`. Com string pura, o erro só aparece em runtime.

---

## 7. Usando `options.Value` vs guardando em campo

```csharp
// Opção 1: acessa options.Value toda vez
public class MetaAdsClient(IOptions<MetaSettings> options)
{
    public async Task<...> ExecuteAsync(...)
    {
        var token = options.Value.AccessToken;  // acessa options.Value em cada chamada
    }
}

// Opção 2: guarda no campo privado no construtor (padrão do projeto)
public class MetaAdsClient(IOptions<MetaSettings> options)
{
    private readonly MetaSettings _settings = options.Value;  // ← guarda uma vez

    public async Task<...> ExecuteAsync(...)
    {
        var token = _settings.AccessToken;  // acesso direto
    }
}
```

A Opção 2 é ligeiramente mais eficiente e usada no projeto. Para `IOptions<T>` (não Snapshot/Monitor), o valor não muda — então guardar em campo é seguro.

---

## Resumo

| Conceito | Detalhe |
|---|---|
| Settings class | Classe C# simples com propriedades públicas tipadas |
| `services.Configure<T>` | Registra a classe de settings no DI, preenchendo de `IConfiguration` |
| `IOptions<T>` no construtor | Injeta o wrapper das settings |
| `options.Value` | Acessa o objeto `T` preenchido |
| `?? string.Empty` | Garante que nunca será null mesmo se a variável não existir |

---

*Próximo: [17-typed-http-clients-polly.md](./17-typed-http-clients-polly.md) — como o `HttpClient` é configurado para cada API externa.*
