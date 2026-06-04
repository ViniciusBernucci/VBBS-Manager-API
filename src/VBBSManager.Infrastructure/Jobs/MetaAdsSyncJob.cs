using Hangfire;
using Microsoft.Extensions.Logging;

namespace VBBSManager.Infrastructure.Jobs;

public class MetaAdsSyncJob(ILogger<MetaAdsSyncJob> logger)
{
    public const string JobId = "meta-ads-sync";

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900])]
    public async Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
    {
        logger.LogInformation("Starting Meta Ads sync for tenant {TenantId}", tenantId);

        // TODO: buscar credenciais Meta do tenant no banco (criptografadas)
        // TODO: chamar MetaAdsClient para obter métricas de campanhas e criativos
        // TODO: persistir métricas no banco com tenant_id
        // TODO: avaliar semáforo e gerar alertas se necessário

        await Task.CompletedTask;

        logger.LogInformation("Meta Ads sync completed for tenant {TenantId}", tenantId);
    }
}
