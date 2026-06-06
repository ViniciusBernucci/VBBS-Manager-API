# Clients de API Externa

> **Ordem de leitura:** documento **11** da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md).

Cada integração externa tem uma classe isolada em `VBBSManager.Infrastructure/ExternalClients/`.

---

## Princípio

Nenhum Service de negócio chama `HttpClient` diretamente. Todo acesso a API externa passa por um client isolado que centraliza:

- Autenticação (Bearer token, OAuth, HMAC)
- Retry automático com backoff exponencial (Polly)
- Timeout configurável
- Logging estruturado de cada chamada (endpoint, status HTTP, latência, tenant)

---

## Clients disponíveis

### HotmartClient

**Documentação completa (para iniciantes):** [Integração Hotmart — Histórico de Vendas](./12-integracao-hotmart-vendas.md)

**Arquivos:** `src/VBBSManager.Infrastructure/ExternalClients/Hotmart/`

| Classe / Interface | Método | Descrição |
|---|---|---|
| `IHotmartAuthClient` | `GetAccessTokenAsync(ct)` | OAuth 2.0 Client Credentials |
| `IHotmartClient` | `GetSalesPageAsync(token, startMs, endMs, pageToken, ct)` | Uma página do histórico de vendas |
| `IHotmartSalesService` | `GetConsolidatedReportAsync(start, end, ct)` | Auth + paginação + consolidação (total de vendas e receita por moeda) |

O `HttpClient` é injetado com retry via Polly configurado em `ServiceCollectionExtensions`:

```csharp
services.AddHttpClient<IHotmartClient, HotmartClient>()
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

Backoff: 2s → 4s → 8s.

---

### MetaAdsClient *(a implementar — Fase 1)*

**Arquivo:** `src/VBBSManager.Infrastructure/ExternalClients/MetaAds/`

Métodos planejados:
- `GetCampaignInsightsAsync(accessToken, from, to, ct)`
- `GetAdCreativeInsightsAsync(accessToken, from, to, ct)`

Autenticação: OAuth 2.0 — access token de longa duração obtido via fluxo de autorização do Meta.

---

### BrevoClient *(a implementar — Fase 1)*

**Arquivo:** `src/VBBSManager.Infrastructure/ExternalClients/Brevo/`

Métodos planejados:
- `GetEmailStatsAsync(apiKey, from, to, ct)`

Autenticação: API key no header `api-key`.

---

### EvolutionClient *(a implementar — Fase 1)*

**Arquivo:** `src/VBBSManager.Infrastructure/ExternalClients/Evolution/`

Métodos planejados:
- `SendMessageAsync(instanceKey, phone, message, ct)`

Usado pelos jobs para disparar alertas via WhatsApp quando CPA ou ROAS ultrapassam thresholds.

---

## Padrão de logging

Todo client deve logar antes e depois de cada chamada:

```csharp
logger.LogInformation(
    "Hotmart API {Method} {Endpoint} for tenant {TenantId}",
    "GET", "/sales/history", tenantId);

// após receber resposta:
logger.LogInformation(
    "Hotmart API responded {StatusCode} in {ElapsedMs}ms",
    (int)response.StatusCode, elapsed.TotalMilliseconds);
```

Nunca logar o payload completo em produção — pode conter dados sensíveis do comprador.

---

## Credenciais

As credenciais de cada integração ficam na tabela `integrations`, criptografadas por tenant. O client recebe o `accessToken` ou `apiKey` como parâmetro — nunca lê direto do banco ou de config.

Isso garante que o mesmo client pode ser usado por diferentes tenants com credenciais diferentes.
