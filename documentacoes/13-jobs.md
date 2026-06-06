# Jobs Hangfire

Sincronização de dados externos via jobs agendados com persistência no PostgreSQL.

---

## Por que Hangfire com PostgreSQL?

Não adiciona um container a mais à infra (sem Redis). O dashboard web embutido permite monitorar jobs sem ferramenta adicional. Se o processo cair, os jobs sobrevivem no banco e são reexecutados na retomada — isso é crítico para não perder sincronizações noturnas.

---

## Jobs implementados

### MetaAdsSyncJob

**Arquivo:** `src/VBBSManager.Infrastructure/Jobs/MetaAdsSyncJob.cs`

| Atributo | Valor |
|---|---|
| JobId | `meta-ads-sync` |
| Parâmetros | `tenantId: Guid` |
| Retry | 3 tentativas: 1min, 5min, 15min |

**O que faz:**
1. Busca credenciais Meta Ads do tenant no banco (criptografadas)
2. Chama `MetaAdsClient` para obter métricas de campanhas e criativos
3. Persiste métricas no banco com `tenant_id`
4. Avalia semáforo de criativos e gera alertas se necessário

---

### HotmartSyncJob

**Arquivo:** `src/VBBSManager.Infrastructure/Jobs/HotmartSyncJob.cs`

| Atributo | Valor |
|---|---|
| JobId | `hotmart-sync` |
| Parâmetros | `tenantId: Guid` |
| Retry | 3 tentativas: 1min, 5min, 15min |

**O que faz:**
1. Busca credenciais Hotmart do tenant no banco
2. Chama `HotmartClient` para obter vendas do dia anterior
3. Persiste vendas com `tenant_id`

---

## Como agendar um job recorrente

No `Program.cs`, após o `app.Build()`:

```csharp
// Sync diário às 6h (horário de Brasília = UTC-3)
RecurringJob.AddOrUpdate<MetaAdsSyncJob>(
    MetaAdsSyncJob.JobId,
    job => job.ExecuteAsync(tenantId, CancellationToken.None),
    "0 9 * * *"   // 9h UTC = 6h BRT
);
```

---

## Como disparar um job manualmente

```csharp
BackgroundJob.Enqueue<HotmartSyncJob>(
    job => job.ExecuteAsync(tenantId, CancellationToken.None)
);
```

---

## Dashboard

O Hangfire Dashboard fica em `http://localhost:5000/hangfire`.

Mostra: jobs agendados, em execução, com falha, histórico de execuções e detalhes de erro com stack trace.

Em produção, proteger o dashboard com autenticação — configurar `IDashboardAuthorizationFilter` em `UseHangfireDashboard`.

---

## Política de retry

Os delays em segundos `[60, 300, 900]` correspondem a:
- 1ª tentativa após falha: 1 minuto
- 2ª tentativa após falha: 5 minutos  
- 3ª tentativa após falha: 15 minutos

Após 3 falhas, o job vai para a fila `Failed` e fica visível no dashboard para reprocessamento manual.
